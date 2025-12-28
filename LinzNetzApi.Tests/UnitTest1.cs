using AngleSharp;
using AngleSharp.Dom;

namespace LinzNetzApi.Tests;

/// <summary>
/// Unit tests for HTML parsing logic used in LinzNetz.cs.
/// These tests verify that CSS selectors correctly extract data from the LinzNetz portal HTML.
/// If the portal HTML structure changes, these tests will fail - alerting us to update the selectors.
/// Tests run against 10 different HTML variations to catch potential structure changes.
/// </summary>
public class HtmlParsingTests {
    private static readonly string TestDataDir = Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "TestData");
    
    private static readonly string[] TestDataFiles = {
        "consumption_page_01.html",
        "consumption_page_02.html", 
        "consumption_page_03.html",
        "consumption_page_04.html",
        "consumption_page_05.html",
        "consumption_page_06.html",
        "consumption_page_07.html",
        "consumption_page_08.html",
        "consumption_page_09.html",
        "consumption_page_10.html"
    };

    /// <summary>
    /// Gets a document from a specific HTML file for testing.
    /// </summary>
    private static async Task<IDocument> GetDocument(string fileName) {
        var filePath = Path.Combine(TestDataDir, fileName);
        var config = Configuration.Default;
        var context = BrowsingContext.New(config);
        var html = await File.ReadAllTextAsync(filePath);
        return await context.OpenAsync(req => req.Content(html));
    }

    /// <summary>
    /// Runs a test against all HTML variations.
    /// </summary>
    private static async Task RunAgainstAllVariations(Func<IDocument, Task> testAction) {
        foreach (var file in TestDataFiles) {
            var document = await GetDocument(file);
            await testAction(document);
        }
    }

    /// <summary>
    /// Runs a test against all HTML variations (sync version).
    /// </summary>
    private static async Task RunAgainstAllVariations(Action<IDocument> testAction) {
        foreach (var file in TestDataFiles) {
            var document = await GetDocument(file);
            testAction(document);
        }
    }

    [Fact]
    public async Task ParseBaseInfo_ExtractsAddressFromLegend() {
        // This tests the selector: #myform > fieldset > legend
        await RunAgainstAllVariations(document => {
            var legend = document.QuerySelector("#myform > fieldset > legend");

            Assert.NotNull(legend);
            Assert.Contains("Musterstadt", legend.TextContent);
            Assert.Contains("Beispielstraße", legend.TextContent);
        });
    }

    [Fact]
    public async Task GetAnlagen_FindsAnlageRows() {
        // This tests the selector: #myform .netz-fieldset-inner .row
        await RunAgainstAllVariations(document => {
            var rows = document.QuerySelectorAll("#myform .netz-fieldset-inner .row");

            Assert.NotEmpty(rows);
            // Should find at least one anlage row
            Assert.True(rows.Length >= 1, $"Expected at least 1 anlage row, found {rows.Length}");
        });
    }

    [Fact]
    public async Task GetAnlagen_ExtractsAnlageName() {
        // This tests the selector: .netz-label-radio b span (for anlage name)
        await RunAgainstAllVariations(document => {
            var row = document.QuerySelector("#myform .netz-fieldset-inner .row");
            Assert.NotNull(row);

            var nameElement = row.QuerySelector(".netz-label-radio b span");

            Assert.NotNull(nameElement);
            var name = nameElement.TextContent.Trim();
            Assert.False(string.IsNullOrWhiteSpace(name), "Anlage name should not be empty");
            Assert.Equal("Basisanlage", name);
        });
    }

    [Fact]
    public async Task GetAnlagen_ExtractsZaehlerNummer() {
        // This tests extraction of Zählernummer from the row structure
        // Looking at HTML: it's in the 3rd column div with text "Zählernummer:" followed by <br> and the number
        await RunAgainstAllVariations(document => {
            var row = document.QuerySelector("#myform .netz-fieldset-inner .row");
            Assert.NotNull(row);

            // The zaehlerNummer is in a div containing "Zählernummer:"
            var divs = row.QuerySelectorAll("div[class*='col-']");
            string? zaehlerNummer = null;

            foreach (var div in divs) {
                var innerHTML = div.InnerHtml;
                if (innerHTML.Contains("Zählernummer:")) {
                    // Extract the number after <br>
                    var text = div.TextContent;
                    var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines) {
                        var trimmed = line.Trim();
                        if (!string.IsNullOrEmpty(trimmed) && !trimmed.Contains("Zählernummer") && char.IsDigit(trimmed[0])) {
                            zaehlerNummer = trimmed;
                            break;
                        }
                    }
                    break;
                }
            }

            Assert.NotNull(zaehlerNummer);
            Assert.Matches(@"^\d+$", zaehlerNummer); // Should be all digits
            Assert.Equal("10000001", zaehlerNummer);
        });
    }

    [Fact]
    public async Task GetAnlagen_ExtractsZaehlPunktNummer() {
        // This tests the selector: span.netz-word-break (for Zählpunktnummer)
        await RunAgainstAllVariations(document => {
            var row = document.QuerySelector("#myform .netz-fieldset-inner .row");
            Assert.NotNull(row);

            var zpnElement = row.QuerySelector("span.netz-word-break");

            Assert.NotNull(zpnElement);
            var zaehlPunktNummer = zpnElement.TextContent.Trim();
            Assert.False(string.IsNullOrWhiteSpace(zaehlPunktNummer), "Zählpunktnummer should not be empty");
            Assert.StartsWith("AT", zaehlPunktNummer); // Austrian meter point numbers start with AT
            Assert.Equal("AT0030000000000000000000000000000001", zaehlPunktNummer);
        });
    }

    [Fact]
    public async Task GetAnlagen_ExtractsAnlageId() {
        // This tests the selector: input.netz-radio (for anlage id)
        await RunAgainstAllVariations(document => {
            var row = document.QuerySelector("#myform .netz-fieldset-inner .row");
            Assert.NotNull(row);

            var radioInput = row.QuerySelector("input.netz-radio");

            Assert.NotNull(radioInput);
            var id = radioInput.GetAttribute("id");
            Assert.NotNull(id);
            Assert.StartsWith("plant-", id); // IDs follow pattern plant-XXXXX
            Assert.Equal("plant-100001", id);
        });
    }

    [Fact]
    public async Task NavigationLinks_AboVerbrauchswerteExists() {
        // This tests that the Abo-Verbrauchswerte link exists
        // Note: CSV export button only appears after loading table results (dynamic)
        await RunAgainstAllVariations(document => {
            var anchorTexts = document.QuerySelectorAll("span.netz-anchor-text");
            var aboLinkExists = anchorTexts.Any(el => el.TextContent.Contains("Abo-Verbrauchswerte"));

            Assert.True(aboLinkExists, "Abo-Verbrauchswerte link should exist on the page");
        });
    }

    [Fact]
    public async Task FormElements_DateInputsExist() {
        // This tests that date input fields exist (used for setting date range)
        // Selectors used: #myForm1\\:calendarFromRegion, #myForm1\\:calendarToRegion
        await RunAgainstAllVariations(document => {
            // Note: In the actual code, the ID contains colons which need escaping
            // AngleSharp handles this differently than Puppeteer
            var fromInput = document.QuerySelector("[id*='calendarFromRegion']");
            var toInput = document.QuerySelector("[id*='calendarToRegion']");

            Assert.NotNull(fromInput);
            Assert.NotNull(toInput);
        });
    }

    [Fact]
    public async Task FormElements_AnzeigeButtonExists() {
        // This tests that the "Anzeigen" button exists
        // Selector used: #myForm1\\:btnIdA1
        await RunAgainstAllVariations(document => {
            var button = document.QuerySelector("[id*='btnIdA1']");

            Assert.NotNull(button);
        });
    }

    [Fact]
    public async Task RadioButtons_ViertelstundenwerteExists() {
        // This tests that the quarter-hour resolution option exists
        // Used to select "Viertelstundenwerte" in the UI
        await RunAgainstAllVariations(document => {
            var labels = document.QuerySelectorAll("label");
            var quarterHourLabel = labels.FirstOrDefault(l => l.TextContent.Contains("Viertelstundenwerte"));

            Assert.NotNull(quarterHourLabel);
        });
    }
}
