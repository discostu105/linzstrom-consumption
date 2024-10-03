using CommandLine;
using ConsumptionStorageInfluxDb;
using LinzNetzApi;
using Microsoft.Extensions.Configuration;
using System.Linq;
using TinyCsvParser;

public class Options {
    [Option('u', "username", Required = false, HelpText = "Linz Strom Username (e-mail).")]
    public string Username { get; set; }

    [Option('p', "password", Required = false, HelpText = "Linz Strom Password.")]
    public string Password { get; set; }

    [Option('d', "days", Required = false, Default = 7, HelpText = "default 7 days")]
    public int Days { get; set; }


    [Option('e', "influxendpoint", Required = false)]
    public string InfluxDbEndpoint { get; set; }

    [Option('k', "influxtoken", Required = false)]
    public string InfluxDbToken { get; set; }
}

class Program {
    public static async Task Main(string[] args) {

        await Parser.Default.ParseArguments<Options>(args)
            .WithParsedAsync<Options>(async o => {
                var configuration = new ConfigurationBuilder()
                  .AddUserSecrets<Program>()
                  .Build();

                if (o.Username == null) o.Username = configuration["LinzNetzUsername"];
                if (o.Password == null) o.Password = configuration["LinzNetzPassword"];
                if (o.InfluxDbEndpoint == null) o.InfluxDbEndpoint = configuration["InfluxDbEndpoint"];
                if (o.InfluxDbToken == null) o.InfluxDbToken = configuration["InfluxDbToken"];

                await Parse(o);
            });
    }

    private static async Task Parse(Options options) {
        var timeout = TimeSpan.FromSeconds(120);
        await using var linzStrom = await LinzNetz.StartSession(
            username: options.Username,
            password: options.Password,
            timeout: timeout,
            headless: true
        );

        linzStrom.PrintBaseInfo();

        var influxdb = await InfluxDb.Connect(options.InfluxDbEndpoint, options.InfluxDbToken, "linzstrom", "home");

        var fromDate = DateTime.Now.Subtract(TimeSpan.FromDays(options.Days)).ToString("dd.MM.yyyy");
        var toDate = DateTime.Now.ToString("dd.MM.yyyy");

        foreach (var anlage in linzStrom.BaseInfo.anlagen) {
            var csv = await linzStrom.FetchConsumptionAsCsv(
                dateFrom: fromDate,
                dateTo: toDate,
                anlage: anlage.id
            );

            CsvParserOptions csvParserOptions = new CsvParserOptions(true, ';');
            CsvReaderOptions csvReaderOptions = new CsvReaderOptions(new string[] { "\n" });

            var csvParser = new CsvParser<KwhQuarterHourMeasurement>(csvParserOptions, new KwhQuarterHourMeasurementMapping());
            var records = csvParser.ReadFromString(csvReaderOptions, csv).Select(x => x.Result);
            Console.WriteLine("writing " + records.Count() + " csv records");
            Console.WriteLine("first: " + records.First());
            Console.WriteLine("last: " + records.Last());
            await influxdb.WriteKwhQuarterHourMeasurements(records, anlage.name, anlage.zaehlPunktNummer);
        }
    }
}
