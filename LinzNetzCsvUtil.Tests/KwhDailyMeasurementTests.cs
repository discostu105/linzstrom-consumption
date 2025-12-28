using ConsumptionStorageInfluxDb;
using Xunit;

namespace LinzNetzCsvUtil.Tests;

public class KwhDailyMeasurementTests {
    [Fact]
    public void Timestamp_ParsesCorrectly() {
        var measurement = new KwhDailyMeasurement {
            _01 = "2025-12-28",
            _02 = "1.234",
            _03 = ""
        };

        Assert.Equal(new DateTime(2025, 12, 28), measurement.Timestamp);
    }

    [Fact]
    public void KWh_ParsesDecimalWithComma() {
        var measurement = new KwhDailyMeasurement {
            _01 = "2025-12-28",
            _02 = "1,234",
            _03 = ""
        };

        Assert.Equal(1.234m, measurement.KWh);
    }

    [Fact]
    public void KWh_ParsesDecimalWithDot() {
        var measurement = new KwhDailyMeasurement {
            _01 = "2025-12-28",
            _02 = "1.234",
            _03 = ""
        };

        Assert.Equal(1.234m, measurement.KWh);
    }

    [Fact]
    public void ReplacementKwh_IsNull_WhenEmpty() {
        var measurement = new KwhDailyMeasurement {
            _01 = "2025-12-28",
            _02 = "1.234",
            _03 = ""
        };

        Assert.Null(measurement.ReplacementKwh);
    }

    [Fact]
    public void ReplacementKwh_ParsesValue_WhenProvided() {
        var measurement = new KwhDailyMeasurement {
            _01 = "2025-12-28",
            _02 = "1.234",
            _03 = "5,678"
        };

        Assert.Equal(5.678m, measurement.ReplacementKwh);
    }
}
