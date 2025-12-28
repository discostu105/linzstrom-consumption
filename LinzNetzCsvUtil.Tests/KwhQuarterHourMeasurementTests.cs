using ConsumptionStorageInfluxDb;
using Xunit;

namespace LinzNetzCsvUtil.Tests;

public class KwhQuarterHourMeasurementTests {
    [Fact]
    public void TimestampFrom_ParsesCorrectly() {
        var measurement = new KwhQuarterHourMeasurement {
            _01 = "19.02.2022 00:00",
            _02 = "19.02.2022 00:15",
            _03 = "0,138",
            _04 = ""
        };

        Assert.Equal(new DateTime(2022, 2, 19, 0, 0, 0), measurement.Timestamp_From);
    }

    [Fact]
    public void TimestampTo_ParsesCorrectly() {
        var measurement = new KwhQuarterHourMeasurement {
            _01 = "19.02.2022 00:00",
            _02 = "19.02.2022 00:15",
            _03 = "0,138",
            _04 = ""
        };

        Assert.Equal(new DateTime(2022, 2, 19, 0, 15, 0), measurement.Timestamp_To);
    }

    [Fact]
    public void KWh_ParsesDecimalWithComma() {
        var measurement = new KwhQuarterHourMeasurement {
            _01 = "19.02.2022 00:00",
            _02 = "19.02.2022 00:15",
            _03 = "0,138",
            _04 = ""
        };

        Assert.Equal(0.138m, measurement.KWh);
    }

    [Fact]
    public void KWh_ParsesDecimalWithDot() {
        var measurement = new KwhQuarterHourMeasurement {
            _01 = "19.02.2022 00:00",
            _02 = "19.02.2022 00:15",
            _03 = "0.138",
            _04 = ""
        };

        Assert.Equal(0.138m, measurement.KWh);
    }

    [Fact]
    public void ReplacementKwh_IsNull_WhenEmpty() {
        var measurement = new KwhQuarterHourMeasurement {
            _01 = "19.02.2022 00:00",
            _02 = "19.02.2022 00:15",
            _03 = "0,138",
            _04 = ""
        };

        Assert.Null(measurement.ReplacementKwh);
    }

    [Fact]
    public void ReplacementKwh_ParsesValue_WhenProvided() {
        var measurement = new KwhQuarterHourMeasurement {
            _01 = "19.02.2022 00:00",
            _02 = "19.02.2022 00:15",
            _03 = "0,138",
            _04 = "0,236"
        };

        Assert.Equal(0.236m, measurement.ReplacementKwh);
    }

    [Fact]
    public void Handles_DayTransition() {
        var measurement = new KwhQuarterHourMeasurement {
            _01 = "25.02.2022 23:45",
            _02 = "26.02.2022 00:00",
            _03 = "0,137",
            _04 = ""
        };

        Assert.Equal(new DateTime(2022, 2, 25, 23, 45, 0), measurement.Timestamp_From);
        Assert.Equal(new DateTime(2022, 2, 26, 0, 0, 0), measurement.Timestamp_To);
        Assert.Equal(0.137m, measurement.KWh);
    }

    [Fact]
    public void InvalidDateFormat_ThrowsFormatException() {
        var measurement = new KwhQuarterHourMeasurement {
            _01 = "2022-02-19 00:00", // Wrong format (ISO instead of dd.MM.yyyy HH:mm)
            _02 = "2022-02-19 00:15",
            _03 = "0,138",
            _04 = ""
        };

        Assert.Throws<FormatException>(() => measurement.Timestamp_From);
    }

    [Fact]
    public void InvalidKwhValue_ThrowsFormatException() {
        var measurement = new KwhQuarterHourMeasurement {
            _01 = "19.02.2022 00:00",
            _02 = "19.02.2022 00:15",
            _03 = "invalid",
            _04 = ""
        };

        Assert.Throws<FormatException>(() => measurement.KWh);
    }
}
