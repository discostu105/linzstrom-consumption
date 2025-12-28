using ConsumptionStorageInfluxDb;
using TinyCsvParser;
using Xunit;

namespace LinzNetzCsvUtil.Tests;

/// <summary>
/// Integration tests verifying TinyCsvParser + Mapping classes work correctly together.
/// These tests catch mapping misconfigurations and CSV format changes.
/// </summary>
public class CsvParsingIntegrationTests {
    [Fact]
    public void ParseQuarterHourCsv_ReturnsCorrectMeasurements() {
        // Arrange - Real CSV format from LinzNetz export
        var csv = """
            Von;Bis;Verbrauch in kWh;Ersatzwert in kWh
            19.02.2022 00:00;19.02.2022 00:15;0,138;
            19.02.2022 00:15;19.02.2022 00:30;0,142;
            25.02.2022 23:45;26.02.2022 00:00;0,137;0,200
            """;

        var csvParserOptions = new CsvParserOptions(skipHeader: true, fieldsSeparator: ';');
        var csvReaderOptions = new CsvReaderOptions(new[] { "\n" });
        var csvParser = new CsvParser<KwhQuarterHourMeasurement>(csvParserOptions, new KwhQuarterHourMeasurementMapping());

        // Act
        var records = csvParser.ReadFromString(csvReaderOptions, csv)
            .Where(r => r.IsValid)
            .Select(r => r.Result)
            .ToList();

        // Assert
        Assert.Equal(3, records.Count);

        // First record
        Assert.Equal(new DateTime(2022, 2, 19, 0, 0, 0), records[0].Timestamp_From);
        Assert.Equal(new DateTime(2022, 2, 19, 0, 15, 0), records[0].Timestamp_To);
        Assert.Equal(0.138m, records[0].KWh);
        Assert.Null(records[0].ReplacementKwh);

        // Last record with replacement value
        Assert.Equal(0.137m, records[2].KWh);
        Assert.Equal(0.200m, records[2].ReplacementKwh);
    }

    [Fact]
    public void ParseDailyCsv_ReturnsCorrectMeasurements() {
        // Arrange - Daily format
        var csv = """
            Datum;Verbrauch in kWh;Ersatzwert in kWh
            2025-12-28;1,234;
            2025-12-29;2,567;3,000
            """;

        var csvParserOptions = new CsvParserOptions(skipHeader: true, fieldsSeparator: ';');
        var csvReaderOptions = new CsvReaderOptions(new[] { "\n" });
        var csvParser = new CsvParser<KwhDailyMeasurement>(csvParserOptions, new KwhDailyMeasurementMapping());

        // Act
        var records = csvParser.ReadFromString(csvReaderOptions, csv)
            .Where(r => r.IsValid)
            .Select(r => r.Result)
            .ToList();

        // Assert
        Assert.Equal(2, records.Count);
        Assert.Equal(new DateTime(2025, 12, 28), records[0].Timestamp);
        Assert.Equal(1.234m, records[0].KWh);
        Assert.Null(records[0].ReplacementKwh);
        Assert.Equal(3.000m, records[1].ReplacementKwh);
    }

    [Fact]
    public void ParseCsv_WithEmptyInput_ReturnsEmptyList() {
        var csv = "Von;Bis;Verbrauch in kWh;Ersatzwert in kWh\n";

        var csvParserOptions = new CsvParserOptions(skipHeader: true, fieldsSeparator: ';');
        var csvReaderOptions = new CsvReaderOptions(new[] { "\n" });
        var csvParser = new CsvParser<KwhQuarterHourMeasurement>(csvParserOptions, new KwhQuarterHourMeasurementMapping());

        var records = csvParser.ReadFromString(csvReaderOptions, csv)
            .Where(r => r.IsValid)
            .Select(r => r.Result)
            .ToList();

        Assert.Empty(records);
    }
}
