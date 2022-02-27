using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;

namespace ConsumptionStorageInfluxDb;
public class InfluxDb : IAsyncDisposable {
    private InfluxDBClient client;
    private string bucket;
    private string org;

    private InfluxDb(string bucket, string org) {
        this.bucket = bucket;
        this.org = org;
    }

    public static async Task<InfluxDb> Connect(string endpoint, string token, string bucket, string org) {
        var influxdb = new InfluxDb(bucket, org);
        await influxdb.ConnectInternal(endpoint, token);
        return influxdb;
    }

    public async ValueTask DisposeAsync() {
        client.Dispose();
    }

    private async Task ConnectInternal(string endpoint, string token) {
        client = InfluxDBClientFactory.Create(endpoint, token);
        client.SetLogLevel(InfluxDB.Client.Core.LogLevel.Body);
        bool ping = await client.PingAsync();
        if (!ping) throw new Exception("could not connect to " + endpoint);
    }

    public async Task Test() {
        using (var writeApi = client.GetWriteApi()) {
            var point = PointData.Measurement("temperature")
                .Tag("location", "west")
                .Field("value", 55D)
                .Timestamp(DateTime.UtcNow.AddSeconds(-10), WritePrecision.Ns);

            writeApi.WritePoint(point, this.bucket, this.org);

        }
    }

    public async Task WriteKwhQuarterHourMeasurements(IEnumerable<KwhQuarterHourMeasurement> measurements, string location) {
        using (var writeApi = client.GetWriteApi()) {
            foreach (var measurement in measurements) {
                var point = PointData.Measurement("kwh_consumption")
                    .Tag("precision", "quarterhour")
                    .Tag("location", location)
                    .Field("value", measurement.KWh)
                    .Timestamp(measurement.Timestamp_To.ToUniversalTime(), WritePrecision.S);

                writeApi.WritePoint(point, this.bucket, this.org);
            }
            writeApi.Flush();
        }
    }
}
