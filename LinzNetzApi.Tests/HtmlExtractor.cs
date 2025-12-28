using PuppeteerSharp;

namespace LinzNetzApi.Tests;

/// <summary>
/// Extracts sample HTML pages from LinzNetz portal for unit testing.
/// Run ExtractAndSaveAsync once to capture HTML samples, then use in tests.
/// </summary>
public static class HtmlExtractor {
    public static async Task ExtractAndSaveAsync(string username, string password, string outputDir) {
        Directory.CreateDirectory(outputDir);

        var timeout = TimeSpan.FromSeconds(120);

        Console.WriteLine("Downloading browser...");
        await new BrowserFetcher().DownloadAsync();

        Console.WriteLine("Launching browser...");
        await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions {
            Headless = true,
            Timeout = (int)timeout.TotalMilliseconds,
            Args = new[] { "--disable-gpu", "--no-sandbox" }
        });

        var page = await browser.NewPageAsync();
        page.DefaultTimeout = (int)timeout.TotalMilliseconds;

        // Navigate to login
        Console.WriteLine("Navigating to login page...");
        await page.GoToAsync("https://www.linznetz.at/portal/de/home");
        await page.WaitForSelectorAsync(".netz-login-link");
        await page.ClickAsync(".netz-login-link");

        // Login
        Console.WriteLine("Logging in...");
        await page.WaitForSelectorAsync("#username");
        await page.FocusAsync("#username");
        await Task.Delay(100);
        await page.Keyboard.TypeAsync(username);
        await page.WaitForSelectorAsync("#password");
        await page.FocusAsync("#password");
        await page.Keyboard.TypeAsync(password);
        await page.WaitForSelectorAsync("form .netz-btn--primary");
        await page.ClickAsync("form .netz-btn--primary");

        // Wait for login to complete
        Console.WriteLine("Waiting for login to complete...");
        await Task.Delay(3000);

        // Navigate to Verbrauchsdateninformation
        Console.WriteLine("Navigating to Verbrauchsdateninformation...");
        var options = new WaitForSelectorOptions { Timeout = 15000 };
        try {
            var link1 = await page.WaitForXPathAsync("//a[contains(., 'Verbrauchsdateninformation')]", options);
            await link1!.ClickAsync();
            await Task.Delay(2000);

            var link2 = await page.WaitForXPathAsync("//a[contains(., 'Meine Verbräuche anzeigen')]", options);
            await link2!.ClickAsync();
            await Task.Delay(2000);

            await page.WaitForSelectorAsync("h1");
            await Task.Delay(1000);
        } catch (Exception e) {
            Console.WriteLine($"Navigation warning: {e.Message}");
        }

        // Capture the consumption page HTML (contains BaseInfo and Anlagen)
        Console.WriteLine("Capturing consumption page HTML...");
        var html = await page.GetContentAsync();
        var outputPath = Path.Combine(outputDir, "consumption_page.html");
        await File.WriteAllTextAsync(outputPath, html);
        Console.WriteLine($"Saved: {outputPath} ({html.Length} bytes)");

        Console.WriteLine("Done!");
    }
}
