using PuppeteerSharp;
using System.Text.RegularExpressions;

namespace LinzNetzApi.Tests;

/// <summary>
/// Extracts sample HTML pages from LinzNetz portal for unit testing.
/// Run ExtractMultipleAsync to capture multiple HTML samples for variation testing.
/// </summary>
public static class HtmlExtractor {
    /// <summary>
    /// Extracts HTML from the consumption page multiple times to capture potential variations.
    /// Each extraction navigates fresh from home to consumption page.
    /// </summary>
    public static async Task ExtractMultipleAsync(string username, string password, string outputDir, int count = 10) {
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

        // Login once
        await LoginAsync(page, username, password);

        for (int i = 1; i <= count; i++) {
            Console.WriteLine($"\n=== Extraction {i}/{count} ===");
            
            try {
                // Navigate to consumption page (re-navigate each time to catch variations)
                await NavigateToConsumptionPageAsync(page);

                // Capture HTML
                var html = await page.GetContentAsync();
                var filename = $"consumption_page_{i:D2}.html";
                var outputPath = Path.Combine(outputDir, filename);
                await File.WriteAllTextAsync(outputPath, html);
                Console.WriteLine($"Saved: {filename} ({html.Length} bytes)");

                // Navigate back to home before next iteration
                if (i < count) {
                    Console.WriteLine("Navigating back to home...");
                    await page.GoToAsync("https://www.linznetz.at/portal/de/home");
                    await Task.Delay(2000);
                }
            } catch (Exception e) {
                Console.WriteLine($"Error on extraction {i}: {e.Message}");
                // Try to recover by going back home
                try {
                    await page.GoToAsync("https://www.linznetz.at/portal/de/home");
                    await Task.Delay(2000);
                } catch { }
            }
        }

        Console.WriteLine($"\nDone! Extracted {count} HTML samples.");
    }

    private static async Task LoginAsync(IPage page, string username, string password) {
        Console.WriteLine("Navigating to login page...");
        await page.GoToAsync("https://www.linznetz.at/portal/de/home");
        await page.WaitForSelectorAsync(".netz-login-link");
        await page.ClickAsync(".netz-login-link");

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

        Console.WriteLine("Waiting for login to complete...");
        await Task.Delay(3000);
    }

    private static async Task NavigateToConsumptionPageAsync(IPage page) {
        Console.WriteLine("Navigating to Verbrauchsdateninformation...");
        var options = new WaitForSelectorOptions { Timeout = 15000 };
        
        var link1 = await page.WaitForXPathAsync("//a[contains(., 'Verbrauchsdateninformation')]", options);
        await link1!.ClickAsync();
        await Task.Delay(2000);

        var link2 = await page.WaitForXPathAsync("//a[contains(., 'Meine Verbräuche anzeigen')]", options);
        await link2!.ClickAsync();
        await Task.Delay(2000);

        await page.WaitForSelectorAsync("h1");
        await Task.Delay(1000);
    }

    /// <summary>
    /// Legacy single extraction method for backwards compatibility.
    /// </summary>
    public static async Task ExtractAndSaveAsync(string username, string password, string outputDir) {
        await ExtractMultipleAsync(username, password, outputDir, 1);
        
        // Rename to legacy filename
        var source = Path.Combine(outputDir, "consumption_page_01.html");
        var dest = Path.Combine(outputDir, "consumption_page.html");
        if (File.Exists(source)) {
            if (File.Exists(dest)) File.Delete(dest);
            File.Move(source, dest);
        }
    }
}
