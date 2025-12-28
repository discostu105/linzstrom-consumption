using LinzNetzApi.Tests;

var outputDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData");

if (args.Length == 0) {
    PrintUsage();
    return;
}

switch (args[0]) {
    case "extract":
        await RunExtract(args, outputDir);
        break;
    case "extract-multiple":
        await RunExtractMultiple(args, outputDir);
        break;
    case "sanitize":
        await RunSanitize(outputDir);
        break;
    case "extract-and-sanitize":
        await RunExtractAndSanitize(args, outputDir);
        break;
    default:
        PrintUsage();
        break;
}

void PrintUsage() {
    Console.WriteLine("LinzNetzApi.Tests - HTML Extraction and Sanitization Tool");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run -- extract <username> <password>           Extract single HTML sample");
    Console.WriteLine("  dotnet run -- extract-multiple <username> <password>  Extract 10 HTML samples");
    Console.WriteLine("  dotnet run -- sanitize                                Sanitize all HTML files in TestData");
    Console.WriteLine("  dotnet run -- extract-and-sanitize <username> <password>  Extract 10 + sanitize");
}

async Task RunExtract(string[] args, string outputDir) {
    var username = args.Length > 1 ? args[1] : throw new Exception("Missing username");
    var password = args.Length > 2 ? args[2] : throw new Exception("Missing password");
    
    Console.WriteLine($"Extracting single HTML sample to: {outputDir}");
    await HtmlExtractor.ExtractAndSaveAsync(username, password, outputDir);
}

async Task RunExtractMultiple(string[] args, string outputDir) {
    var username = args.Length > 1 ? args[1] : throw new Exception("Missing username");
    var password = args.Length > 2 ? args[2] : throw new Exception("Missing password");
    var count = args.Length > 3 ? int.Parse(args[3]) : 10;
    
    Console.WriteLine($"Extracting {count} HTML samples to: {outputDir}");
    await HtmlExtractor.ExtractMultipleAsync(username, password, outputDir, count);
}

async Task RunSanitize(string outputDir) {
    Console.WriteLine($"Sanitizing HTML files in: {outputDir}");
    await HtmlSanitizer.SanitizeAllAsync(outputDir);
}

async Task RunExtractAndSanitize(string[] args, string outputDir) {
    await RunExtractMultiple(args, outputDir);
    Console.WriteLine();
    await RunSanitize(outputDir);
}
