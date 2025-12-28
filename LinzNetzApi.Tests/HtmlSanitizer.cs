using System.Text.RegularExpressions;

namespace LinzNetzApi.Tests;

/// <summary>
/// Sanitizes HTML files by replacing personal information with anonymous placeholders.
/// This ensures test data can be safely committed to public repositories.
/// </summary>
public static class HtmlSanitizer {
    /// <summary>
    /// Sanitizes all HTML files in the specified directory.
    /// </summary>
    public static async Task SanitizeAllAsync(string directory) {
        var files = Directory.GetFiles(directory, "consumption_page*.html");
        Console.WriteLine($"Found {files.Length} HTML files to sanitize.");

        foreach (var file in files) {
            Console.WriteLine($"Sanitizing: {Path.GetFileName(file)}");
            await SanitizeFileAsync(file);
        }

        Console.WriteLine("Sanitization complete.");
    }

    /// <summary>
    /// Sanitizes a single HTML file.
    /// </summary>
    public static async Task SanitizeFileAsync(string filePath) {
        var html = await File.ReadAllTextAsync(filePath);
        var sanitized = SanitizeHtml(html);
        await File.WriteAllTextAsync(filePath, sanitized);
    }

    /// <summary>
    /// Sanitizes HTML content by replacing personal information.
    /// </summary>
    public static string SanitizeHtml(string html) {
        var result = html;

        // Track replacements for consistent anonymization
        var addressReplacements = new Dictionary<string, string>();
        var meterNumberReplacements = new Dictionary<string, string>();
        var meterPointReplacements = new Dictionary<string, string>();
        var plantIdReplacements = new Dictionary<string, string>();

        // 1. Sanitize Austrian addresses (pattern: 4-digit PLZ + city, street name + number)
        // Examples: "4030 Linz, Hauptstraße 123" or "4209 Engerwitzdorf, Am Rothenbühl 50"
        result = SanitizeAddresses(result, addressReplacements);

        // 2. Sanitize Zählernummer (meter numbers) - typically 8 digits
        result = SanitizeMeterNumbers(result, meterNumberReplacements);

        // 3. Sanitize Zählpunktnummer (meter point numbers) - AT + 31 digits
        result = SanitizeMeterPointNumbers(result, meterPointReplacements);

        // 4. Sanitize plant IDs (plant-XXXXXX)
        result = SanitizePlantIds(result, plantIdReplacements);

        // 5. Sanitize email addresses
        result = SanitizeEmails(result);

        // 6. Sanitize any remaining phone numbers
        result = SanitizePhoneNumbers(result);

        return result;
    }

    private static string SanitizeAddresses(string html, Dictionary<string, string> replacements) {
        // Austrian address pattern: 4-digit PLZ, City, Street Name Number
        // Match the full address in legend tags or similar contexts
        var addressPattern = @"(\d{4})\s+([A-Za-zäöüÄÖÜß\-]+(?:\s+[A-Za-zäöüÄÖÜß\-]+)*),\s*([A-Za-zäöüÄÖÜß\s\-]+)\s+(\d+[a-zA-Z]?)";
        
        var counter = 1;
        return Regex.Replace(html, addressPattern, match => {
            var original = match.Value;
            if (!replacements.ContainsKey(original)) {
                replacements[original] = $"1234 Musterstadt, Beispielstraße {counter++}";
            }
            return replacements[original];
        });
    }

    private static string SanitizeMeterNumbers(string html, Dictionary<string, string> replacements) {
        // Meter numbers are typically 8 digits, often appearing after "Zählernummer:" or in specific HTML contexts
        // Be careful not to match other 8-digit numbers
        
        // Pattern 1: After "Zählernummer:" text
        var pattern1 = @"(Zählernummer:.*?)(\d{8})";
        var counter = 10000001;
        html = Regex.Replace(html, pattern1, match => {
            var prefix = match.Groups[1].Value;
            var number = match.Groups[2].Value;
            if (!replacements.ContainsKey(number)) {
                replacements[number] = (counter++).ToString();
            }
            return prefix + replacements[number];
        }, RegexOptions.Singleline);

        // Pattern 2: In value attributes or specific contexts with 8-digit numbers that look like meter numbers
        // Only replace numbers we've already identified
        foreach (var kvp in replacements) {
            html = html.Replace(kvp.Key, kvp.Value);
        }

        return html;
    }

    private static string SanitizeMeterPointNumbers(string html, Dictionary<string, string> replacements) {
        // Austrian meter point numbers: AT + 31 digits (total 33 chars)
        // Example: AT0031000000000000000000225692000
        var pattern = @"AT\d{31}";
        
        var counter = 1;
        return Regex.Replace(html, pattern, match => {
            var original = match.Value;
            if (!replacements.ContainsKey(original)) {
                // Create anonymized version: AT003 + 0000...0 + counter
                var anonNumber = $"AT003{new string('0', 25)}{counter++:D6}";
                replacements[original] = anonNumber;
            }
            return replacements[original];
        });
    }

    private static string SanitizePlantIds(string html, Dictionary<string, string> replacements) {
        // Plant IDs: plant-XXXXXX (typically 5-6 digits)
        var pattern = @"plant-(\d{5,6})";
        
        var counter = 100001;
        return Regex.Replace(html, pattern, match => {
            var original = match.Value;
            if (!replacements.ContainsKey(original)) {
                replacements[original] = $"plant-{counter++}";
            }
            return replacements[original];
        });
    }

    private static string SanitizeEmails(string html) {
        // Email pattern
        var pattern = @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}";
        return Regex.Replace(html, pattern, "test@example.com");
    }

    private static string SanitizePhoneNumbers(string html) {
        // Austrian phone patterns: +43..., 0043..., or local 0xxx formats
        // Be conservative to avoid matching other numbers
        var pattern = @"(\+43|0043)\s*\d[\d\s\-/]{8,15}";
        return Regex.Replace(html, pattern, "+43 123 456789");
    }
}
