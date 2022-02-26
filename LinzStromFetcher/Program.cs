using CommandLine;
using LinzNetzApi;
using System.Linq;

public class Options {
    [Option('u', "verbose", Required = true, HelpText = "Linz Strom Username (e-mail).")]
    public string Username { get; set; }

    [Option('p', "password", Required = true, HelpText = "Linz Strom Password.")]
    public string Password { get; set; }
    
    [Option('f', "fromdate", Required = false, HelpText = "default -1d. format: dd.MM.yyyy (25.02.2022)")]
    public string FromDate { get; set; }
    
    [Option('t', "todate", Required = false, HelpText = "default=today. format: dd.MM.yyyy (26.02.2022)")]
    public string ToDate { get; set; }
}

class Program {
    public static async Task Main(string[] args) {
        await Parser.Default.ParseArguments<Options>(args)
            .WithParsedAsync<Options>(async o => {
                if (string.IsNullOrEmpty(o.FromDate)) o.FromDate = DateTime.Now.Subtract(TimeSpan.FromDays(1)).ToString("dd.MM.yyyy");
                if (string.IsNullOrEmpty(o.ToDate)) o.ToDate = DateTime.Now.ToString("dd.MM.yyyy");
                await Parse(o);
            });
    }

    private static async Task Parse(Options options) {
        await using var linzStrom = await LinzNetz.StartSession(
            username: options.Username,
            password: options.Password
        );

        var csv = await linzStrom.FetchConsumptionAsCsv(
            dateFrom: options.FromDate,
            dateTo: options.ToDate
        );

        Console.WriteLine("lines: " + csv.Length);
        Console.WriteLine("first: " + csv[1]);
        Console.WriteLine("last: " + csv.Last());
    }
}
