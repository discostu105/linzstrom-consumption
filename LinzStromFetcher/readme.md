# LinzStromFetcher

```
LinzStromFetcher.exe --days 7
```

This will fetch the quarter-hourly consumption data of the last 7 days, and export it into InfluxDb. Make sure to setup the .NET secret store with all the credentials. Alternatively, they can also be provided per commandline.

```
LinzNetzConsumptionFetcherConsole 1.0.0
Copyright (C) 2022 LinzNetzConsumptionFetcherConsole

  -u, --username          Linz Strom Username (e-mail).
  -p, --password          Linz Strom Password.
  -d, --days              (Default: 7) default 7 days)
  -e, --influxendpoint
  -k, --influxtoken
  --help                  Display this help screen.
  --version               Display version information.
```
  
# Secrets

```
dotnet user-secrets init
dotnet user-secrets set "InfluxDbToken" "xxx"
dotnet user-secrets set "InfluxDbEndpoint" "xxx"
dotnet user-secrets set "LinzNetzUsername" "xxx"
dotnet user-secrets set "LinzNetzPassword" "xxx"
```
