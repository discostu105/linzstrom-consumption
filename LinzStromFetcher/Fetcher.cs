using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LinzStromFetcher;


class LinzStrom {
    private Browser browser;
    private Page page;

    public BaseInfo BaseInfo { get; set; }

    private LinzStrom() { }

    public static async Task<LinzStrom> StartSession(string username, string password) {
        var ls = new LinzStrom();
        await ls.Login(username, password);
        return ls;
    }

    public async Task Login(string username, string password) {
        browser = await SetupBrowser();
        page = await browser.NewPageAsync();
        await NavigateToLogin(page);
        await Login(username, password, page);
        await NavigateToConsumption(page);
        BaseInfo = await ParseBaseInfo(page);
    }

    public async Task EndSession(string username, string password) {
        await ShutdownBrowser(browser);
    }


    public async Task<string> FetchConsumptionAsCsv(string dateFrom, string dateTo) {
        DirectoryInfo downloadDir = SetupDownloads();
        await EnableFileDownloads(page, downloadDir);
        await SelectQuarterHourResolution(page);
        await LoadResults(page);
        string csv = await ExportCsv(page, downloadDir);
        CleanupDownloads(downloadDir);
        return csv;
    }

    private static async Task ShutdownBrowser(Browser browser) {
        await browser.CloseAsync();
    }

    private static DirectoryInfo SetupDownloads() {
        // download behavior
        var downloadDir = new DirectoryInfo("download");
        if (downloadDir.Exists) downloadDir.Delete(true);
        downloadDir.Create();
        return downloadDir;
    }

    private async Task<string> ExportCsv(Page page, DirectoryInfo downloadDir) {
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
        var viertels = await FindElementByText(page, "label", "Viertelstundenwerte");
        Console.WriteLine(await GetTextValueFromElement2(viertels));

        await viertels.ClickAsync();
    }

    private static void PrintBaseInfo(BaseInfo baseInfo) {
        Console.WriteLine(baseInfo);
        foreach (var anlage in baseInfo.anlagen) {
            Console.WriteLine(anlage);
        }
    }

    private static async Task NavigateToConsumption(Page page) {
        Console.WriteLine("go to Verbrauchsdateninformation");
        // go to Verbrauchsdateninformation:
        await page.WaitForSelectorAsync("#j_idt932");
        await page.ClickAsync("#j_idt932");

        Console.WriteLine("click again!");
        await page.WaitForSelectorAsync("#j_idt932");
        await page.ClickAsync("#j_idt932");

        Console.WriteLine("are we there yet?");

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

    private async Task<Browser> SetupBrowser() {
        await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultChromiumRevision);

        Browser browser = await Puppeteer.LaunchAsync(new LaunchOptions {
            Headless = true
        });
        Console.WriteLine("Chrome ready");
        return browser;
    }

    private void CleanupDownloads(DirectoryInfo downloadDir) {
        downloadDir.Delete(true);
    }

    private static async Task<string> WaitForFileDownloadAsync(DirectoryInfo downloadDir) => await WaitForFileDownloadAsync(downloadDir, TimeSpan.FromSeconds(30));

    private static async Task<string> WaitForFileDownloadAsync(DirectoryInfo downloadDir, TimeSpan timeout) {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout) {
            if (downloadDir.GetFiles().Count() > 0) {
                break;
            }
            await Task.Delay(500);
        }
        return await File.ReadAllTextAsync(downloadDir.GetFiles().Single().FullName);
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
}

record BaseInfo(string Address, List<Anlage> anlagen);
record Anlage(string name, string zaehlerNummer, string zaehlPunktNummer);
