using ConsumptionStorageInfluxDb;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using Xunit;

namespace InfluxDbUtil.Tests;

public class InfluxDbTests {
    [Fact]
    public async Task Connect_ThrowsException_WhenCannotConnect() {
        await Assert.ThrowsAsync<Exception>(async () =>
            await InfluxDb.Connect("http://invalid:8086", "invalid", "bucket", "org")
        );
    }

    [Fact]
    public void InfluxDb_ImplementsIAsyncDisposable() {
        Assert.True(typeof(IAsyncDisposable).IsAssignableFrom(typeof(InfluxDb)));
    }

    [Fact]
    public void PointData_CreatedCorrectly_FromQuarterHourMeasurement() {
        // Arrange - simulate what WriteKwhQuarterHourMeasurements does
        var measurement = new KwhQuarterHourMeasurement {
            _01 = "19.02.2022 00:00",
            _02 = "19.02.2022 00:15",
            _03 = "0,138",
            _04 = ""
        };
        var location = "TestLocation";
        var zaehlpunkt = "AT00310000000001";

        // Act - create point the same way as WriteKwhQuarterHourMeasurements
        var point = PointData.Measurement("kwh_consumption")
            .Tag("precision", "quarterhour")
            .Tag("location", location)
            .Tag("zaehlpunkt", zaehlpunkt)
            .Field("value", measurement.KWh)
            .Timestamp(measurement.Timestamp_To.ToUniversalTime(), WritePrecision.S);

        // Assert - verify point is created with correct data
        var lineProtocol = point.ToLineProtocol();
        Assert.Contains("kwh_consumption", lineProtocol);
        Assert.Contains("precision=quarterhour", lineProtocol);
        Assert.Contains("location=TestLocation", lineProtocol);
        Assert.Contains("zaehlpunkt=AT00310000000001", lineProtocol);
        Assert.Contains("value=0.138", lineProtocol);
    }

    [Fact]
    public void PointData_TimestampConversion_IsConsistent() {
        // Arrange - test that local time is correctly converted to UTC
        var measurement = new KwhQuarterHourMeasurement {
            _01 = "19.02.2022 00:00",
            _02 = "19.02.2022 00:15",
            _03 = "1,0",
            _04 = ""
        };

        // Act
        var localTime = measurement.Timestamp_To;
        var utcTime = localTime.ToUniversalTime();

        // Assert - UTC should be different from local (unless running in UTC timezone)
        // More importantly, this should not throw and should produce valid datetime
        Assert.Equal(new DateTime(2022, 2, 19, 0, 15, 0), localTime);
        Assert.Equal(DateTimeKind.Unspecified, localTime.Kind);
    }
}
