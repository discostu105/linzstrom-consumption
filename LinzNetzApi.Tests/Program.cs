using LinzNetzApi.Tests;

// Simple entry point to run HTML extraction
if (args.Length > 0 && args[0] == "extract") {
    var username = args.Length > 1 ? args[1] : throw new Exception("Usage: extract <username> <password>");
    var password = args.Length > 2 ? args[2] : throw new Exception("Usage: extract <username> <password>");
    var outputDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData");
    
    Console.WriteLine($"Extracting HTML samples to: {outputDir}");
    await HtmlExtractor.ExtractAndSaveAsync(username, password, outputDir);
} else {
    Console.WriteLine("Usage: dotnet run -- extract <username> <password>");
}
