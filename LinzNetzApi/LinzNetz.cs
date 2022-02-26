using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LinzNetzApi;

/// <summary>
/// Logs in to services.linznetz.at
/// </summary>
public class LinzNetz : IAsyncDisposable {
    private Browser browser;
    private Page page;

    public BaseInfo BaseInfo { get; set; }

    private LinzNetz() { }

    public static async Task<LinzNetz> StartSession(string username, string password) {
        var ls = new LinzNetz();
        await ls.Login(username, password);
        return ls;
    }

    private async Task<Browser> SetupBrowser() {
        await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultChromiumRevision);

        Browser browser = await Puppeteer.LaunchAsync(new LaunchOptions {
            Headless = true
        });
        Console.WriteLine("Chrome ready");
        return browser;
    }

    public async Task Login(string username, string password) {
        browser = await SetupBrowser();
        page = await browser.NewPageAsync();
        await NavigateToLogin(page);
        await Login(username, password, page);
        await NavigateToConsumption(page);
        BaseInfo = await ParseBaseInfo(page);
    }

    public async Task<string[]> FetchConsumptionAsCsv(string dateFrom, string dateTo) {
        DirectoryInfo downloadDir = SetupDownloads();
        await EnableFileDownloads(page, downloadDir);
        await SelectQuarterHourResolution(page);
        await SetFromDate(page, dateFrom);
        await SetToDate(page, dateTo);
        await LoadResults(page);
        var csv = await ExportCsv(page, downloadDir);
        CleanupDownloads(downloadDir);
        return csv;
    }

    private async Task ShutdownBrowser() {
        await browser.CloseAsync();
    }

    private static DirectoryInfo SetupDownloads() {
        // download behavior
        var downloadDir = new DirectoryInfo("download");
        if (downloadDir.Exists) downloadDir.Delete(true);
        downloadDir.Create();
        return downloadDir;
    }

    private async Task<string[]> ExportCsv(Page page, DirectoryInfo downloadDir) {
        // click export button
        var exportCsv = await FindElementByText(page, "span.netz-anchor-text", "CSV-Datei exportieren");

        Console.WriteLine("csv export");
        await exportCsv.ClickAsync();

        var csv = await WaitForFileDownloadAsync(downloadDir);
        Console.WriteLine("export finished");
        Console.WriteLine("len: " + csv.Length);
        return csv;
    }

    private static async Task LoadResults(Page page) {
        // click Anzeigen
        Console.WriteLine("load table");
        await page.ClickAsync(@"#myForm1\:btnIdA1");

        // wait for table
        await page.WaitForSelectorAsync(".netz-table-export");
        Console.WriteLine("table load finished");
    }

    private async Task SelectQuarterHourResolution(Page page) {
        Console.WriteLine("Selecting Viertelstunden");
        await (await FindElementByText(page, "label", "Viertelstundenwerte")).ClickAsync();
    }

    private async Task SetFromDate(Page page, string date) {
        Console.WriteLine($"Setting fromdate to {date}");
        await SetDate(page, date, @"myForm1\:calendarFromRegion");
    }

    private async Task SetToDate(Page page, string date) {
        Console.WriteLine($"Setting todate to {date}");
        await SetDate(page, date, @"myForm1\:calendarToRegion");
    }

    private static async Task SetDate(Page page, string date, string id) {
        await page.FocusAsync($"#{id}");
        await page.WaitForTimeoutAsync(100);
        await page.EvaluateExpressionAsync(@$"document.getElementById('{id}').value = ''");
        await page.TypeAsync($"#{id}", date);
        await page.WaitForTimeoutAsync(100);
    }

    public void PrintBaseInfo() {
        Console.WriteLine(BaseInfo);
        foreach (var anlage in BaseInfo.anlagen) {
            Console.WriteLine(anlage);
        }
    }

    private static async Task NavigateToConsumption(Page page) {
        Console.WriteLine("go to Verbrauchsdateninformation");
        // go to Verbrauchsdateninformation:
        await page.WaitForSelectorAsync("#j_idt932");
        await page.ClickAsync("#j_idt932");

        // click again
        await page.WaitForSelectorAsync("#j_idt932");
        await page.ClickAsync("#j_idt932");

        await page.WaitForSelectorAsync("h1");
    }

    private static async Task Login(string username, string password, Page page) {
        Console.WriteLine("enter credentials");

        await page.WaitForSelectorAsync("#username");
        await page.FocusAsync("#username");
        await page.Keyboard.TypeAsync(username);

        await page.WaitForSelectorAsync("#password");
        await page.FocusAsync("#password");
        await page.Keyboard.TypeAsync(password);

        Console.WriteLine("click login submit");
        await page.WaitForSelectorAsync("form .netz-btn--primary");
        await page.ClickAsync("form .netz-btn--primary");

        Console.WriteLine("login done!");
    }

    private static async Task NavigateToLogin(Page page) {
        await page.GoToAsync("https://www.linznetz.at/portal/de/home");
        Console.WriteLine("go to login page");

        await page.WaitForSelectorAsync(".netz-login-link");
        await page.ClickAsync(".netz-login-link");
    }


    private void CleanupDownloads(DirectoryInfo downloadDir) {
        downloadDir.Delete(true);
    }

    private static async Task<string[]> WaitForFileDownloadAsync(DirectoryInfo downloadDir) => await WaitForFileDownloadAsync(downloadDir, TimeSpan.FromSeconds(30));

    private static async Task<string[]> WaitForFileDownloadAsync(DirectoryInfo downloadDir, TimeSpan timeout) {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout) {
            if (downloadDir.GetFiles().Count() > 0) {
                try {
                    return await File.ReadAllLinesAsync(downloadDir.GetFiles().Single().FullName);
                } catch (IOException) {
                    continue; // file is still being written
                }
            }
            await Task.Delay(200);
        }
        throw new Exception($"WaitForFileDownloadAsync: Could not read file within timeout of {timeout}");
    }

    private async Task EnableFileDownloads(Page page, DirectoryInfo downloadDir) {
        var clientProp = page.GetType().GetTypeInfo().GetDeclaredProperty("Client");
        var client = clientProp.GetValue(page) as CDPSession;
        var m = client.GetType().GetTypeInfo().GetDeclaredMethods("SendAsync").ElementAt(1) as MethodInfo;
        await (Task)m.Invoke(client, new object[]{
            "Page.setDownloadBehavior",
            new {
                behavior = "allow",
                downloadPath = downloadDir.FullName
            },
            false
        });
    }

    async Task<ElementHandle> FindElementByText(Page page, string selector, string search) {
        var labels = await page.QuerySelectorAllAsync(selector);
        foreach (var label in labels) {
            var text = await GetTextValueFromElement2(label);
            if (text.Contains(search)) {
                return label;
            }
        }
        throw new Exception("element not found: " + selector + ", " + search);
    }

    async Task<BaseInfo> ParseBaseInfo(Page page) {
        var selector = "#myform > fieldset > legend";
        string str = await GetTextValue(page, selector);
        return new BaseInfo(
            str,
            await GetAnlagen(page));
    }

    async Task<List<Anlage>> GetAnlagen(Page page) {
        var anlagen = new List<Anlage>();
        var rows = await page.QuerySelectorAllAsync("#myform .netz-fieldset-inner .row");
        foreach (var row in rows) {
            anlagen.Add(new Anlage(
                name: (await GetTextValueFromElement(row, ".netz-label-radio b")).Trim(),
                zaehlerNummer: (await GetTextValueFromElement(row, "div:nth-child(3) > div"))
                    .Replace("<b>", "")
                    .Replace("Zählernummer:", "")
                    .Replace("</b>", "")
                    .Replace("<br>", "")
                    .Trim(),
                zaehlPunktNummer: (await GetTextValueFromElement(row, "span.netz-word-break")).Trim()
            ));
        }
        return anlagen;
    }

    async Task<string> GetTextValue(Page page, string selector) {
        var element = await page.QuerySelectorAsync(selector);
        var content = await element.GetPropertyAsync("innerHTML");
        var str = await content.JsonValueAsync<string>();
        return str;
    }
    async Task<string> GetTextValueFromElement(ElementHandle elementHandle, string selector) {
        var element = await elementHandle.QuerySelectorAsync(selector);
        return await GetTextValueFromElement2(element);
    }

    async Task<string> GetTextValueFromElement2(ElementHandle elementHandle) {
        var content = await elementHandle.GetPropertyAsync("innerHTML");
        var str = await content.JsonValueAsync<string>();
        return str;
    }

    public async ValueTask DisposeAsync() {
        await ShutdownBrowser();
    }
}

public record BaseInfo(string Address, List<Anlage> anlagen);
public record Anlage(string name, string zaehlerNummer, string zaehlPunktNummer);
