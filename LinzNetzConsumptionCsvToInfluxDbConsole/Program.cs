using CommandLine;
using ConsumptionStorageInfluxDb;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using System.Globalization;
using System.Linq;
using System.Text;
using TinyCsvParser;
using TinyCsvParser.Mapping;
using TinyCsvParser.TypeConverter;

public class Options {
    [Option('e', "endpoint", Required = true)]
    public string InfluxDbEndpoint { get; set; }

    [Option('t', "token", Required = true)]
    public string InfluxDbToken { get; set; }

    [Option('f', "inputfile", Required = true)]
    public string Inputfile { get; set; }

    [Option('l', "location", Required = true, HelpText = "basisanlage oder waermepumpe")]
    public string Location { get; set; }

}

class Program {
    public static async Task Main(string[] args) {
        await Parser.Default.ParseArguments<Options>(args)
            .WithParsedAsync<Options>(async o => {
                await Parse(o);
            });
    }

    private static async Task Parse(Options options) {
        var influxdb = await InfluxDb.Connect(options.InfluxDbEndpoint, options.InfluxDbToken, "linzstrom", "home");

        CsvParserOptions csvParserOptions = new CsvParserOptions(true, ';');
        
        var csvParser = new CsvParser<KwhQuarterHourMeasurement>(csvParserOptions, new KwhQuarterHourMeasurementMapping());
        var records = csvParser.ReadFromFile(options.Inputfile, Encoding.UTF8).Select(x => x.Result);
        await influxdb.WriteKwhQuarterHourMeasurements(records, options.Location);
    }

}
