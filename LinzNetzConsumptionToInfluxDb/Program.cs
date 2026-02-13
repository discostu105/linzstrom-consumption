using CommandLine;
using ConsumptionStorageInfluxDb;
using DotNetEnv;
using LinzNetzApi;
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
                // Load .env file from current directory
                var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
                if (File.Exists(envPath)) {
                    DotNetEnv.Env.Load(envPath);
                }

                // Read from environment variables (fallback pattern maintained)
                if (o.Username == null) o.Username = Environment.GetEnvironmentVariable("LinzNetzUsername");
                if (o.Password == null) o.Password = Environment.GetEnvironmentVariable("LinzNetzPassword");
                if (o.InfluxDbEndpoint == null) o.InfluxDbEndpoint = Environment.GetEnvironmentVariable("InfluxDbEndpoint");
                if (o.InfluxDbToken == null) o.InfluxDbToken = Environment.GetEnvironmentVariable("InfluxDbToken");

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
            var records = csvParser.ReadFromString(csvReaderOptions, csv).Where(x => x.IsValid).Select(x => x.Result).ToList();
            Console.WriteLine("writing " + records.Count + " csv records");
            if (records.Count == 0) { Console.WriteLine("No valid records, skipping anlage."); continue; }
            Console.WriteLine("first: " + records.First());
            Console.WriteLine("last: " + records.Last());
            await influxdb.WriteKwhQuarterHourMeasurements(records, anlage.name, anlage.zaehlPunktNummer);
        }
    }
}
