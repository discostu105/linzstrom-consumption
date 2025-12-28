namespace LinzNetzApi.Tests;

/// <summary>
/// Runner to extract HTML samples. Execute with:
/// dotnet test --filter "FullyQualifiedName~ExtractHtmlRunner" LinzNetzApi.Tests
/// </summary>
public class ExtractHtmlRunner {
    [Fact(Skip = "Manual execution only - run to extract fresh HTML samples")]
    public async Task ExtractHtmlSamples() {
        // Read credentials from user secrets or environment
        var username = Environment.GetEnvironmentVariable("LINZNETZ_USERNAME") ?? "c.neumueller@gmail.com";
        var password = Environment.GetEnvironmentVariable("LINZNETZ_PASSWORD") ?? throw new Exception("Set LINZNETZ_PASSWORD env var");

        var outputDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData");
        await HtmlExtractor.ExtractAndSaveAsync(username, password, outputDir);
    }
}
