using LinzStromFetcher;

var linzStrom = await LinzStrom.StartSession(
    username: "",
    password: ""
);


var csv = await linzStrom.FetchConsumptionAsCsv(
    dateFrom: "",
    dateTo: ""
);

Console.WriteLine(csv.Length);

