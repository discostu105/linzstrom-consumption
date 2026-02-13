# CLAUDE.md

This project scrapes electricity consumption data from the [Linz Netz AG](https://www.linznetz.at) customer portal using headless Chrome, then writes it to InfluxDB. It runs as an hourly CronJob on a k3s cluster.

## Project Structure

```
LinzNetzApi/                    # Core: headless Chrome scraper (login, navigation, CSV export)
LinzNetzCsvUtil/                # CSV parsers for quarter-hour and daily measurement formats
InfluxDbUtil/                   # InfluxDB client wrapper for writing time-series data
LinzNetzConsumptionToInfluxDb/  # Main executable: orchestrates the full pipeline
LinzNetzCsvUtil.Tests/          # Tests for CSV parsing
LinzNetzApi.Tests/              # Tests for HTML extraction (uses fixture HTML files)
InfluxDbUtil.Tests/             # Tests for InfluxDB write operations
```

## Build & Test

```bash
# Build everything
dotnet build LinzStromFetcher.sln

# Run all tests
dotnet test LinzStromFetcher.sln

# Run the main app locally (requires .env file, see below)
cd LinzNetzConsumptionToInfluxDb && dotnet run
```

## Local Configuration

Create `LinzNetzConsumptionToInfluxDb/.env` (see `.env.example`):
```
LinzNetzUsername=your-email@example.com
LinzNetzPassword=your-password
InfluxDbEndpoint=http://influxdb.home
InfluxDbToken=your-token
```

When running locally, the app will fail at the InfluxDB connection step (not reachable outside the cluster) — that's expected. The scraping/navigation part can be tested end-to-end locally.

## Docker Build & Deploy

```bash
# Build image
docker build -f LinzNetzConsumptionToInfluxDb/Dockerfile -t discostu105/linznetzconsumptiontoinfluxdb:latest .

# Push to Docker Hub
docker push discostu105/linznetzconsumptiontoinfluxdb:latest

# Update the k3s cronjob to use the new image
kubectl set image cronjob/linzstrom linzstrom=docker.io/discostu105/linznetzconsumptiontoinfluxdb:latest -n linzstromjobs

# Trigger a manual test run in the cluster
kubectl delete job linzstrom-test -n linzstromjobs --ignore-not-found
kubectl create job --from=cronjob/linzstrom linzstrom-test -n linzstromjobs
kubectl logs -f -l job-name=linzstrom-test -n linzstromjobs
```

The cronjob runs every hour (`0 * * * *`) in the `linzstromjobs` namespace with `imagePullPolicy: Always`.

## Key Architecture Notes

**Scraper fragility**: The scraper depends on the LinzNetz AG portal UI structure. If it breaks, check:
1. Navigation flow: home → login → "Verbrauchsdateninformation" → "Meine Verbräuche anzeigen"
2. Cookie consent banner (OneTrust) — dismissed automatically before navigation
3. Form selectors: `#myform`, `label[for=plant-*]`, `#myForm1:btnIdA1`, etc.

**Two Anlagen** are configured for this account:
- `plant-140787` (Basisanlage) — exports quarter-hour data (960 records / 10 days)
- `plant-208192` (Rücklieferung Photovoltaik) — no quarter-hour resolution available; currently skipped

**CSV format**: Quarter-hour CSVs are semicolon-separated with columns: `Timestamp_From; Timestamp_To; kWh; ReplacementKWh`. Daily CSVs have a different format and are skipped (no valid records match the quarter-hour mapping).

## .NET & Dependencies

- **.NET 10.0** (see `global.json`)
- **PuppeteerSharp 20.2.5** — headless Chrome; uses system Chrome in Docker (`PUPPETEER_EXECUTABLE_PATH=/usr/bin/google-chrome-unstable`)
- **InfluxDB.Client 4.18.0** — writes to `linzstrom` bucket, `home` org
- **TinyCsvParser 2.7.1** — CSV parsing
- **DotNetEnv** — `.env` file loading
