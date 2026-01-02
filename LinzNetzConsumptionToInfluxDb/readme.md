# Configuration

The application requires credentials to connect to Linz Netz API and InfluxDB. You can configure these using a `.env` file or command-line arguments.

## Using .env File (Recommended)

1. Copy the example file:
   ```bash
   cp .env.example .env
   ```

2. Edit `.env` with your credentials:
   ```
   LinzNetzUsername=your-email@example.com
   LinzNetzPassword=your-password
   InfluxDbEndpoint=http://your-influxdb-host:8086
   InfluxDbToken=your-influxdb-token
   ```

3. Run the application:
   ```bash
   dotnet run
   ```

## Using Command-Line Arguments

You can override .env values using command-line arguments:

```bash
dotnet run -- -u your-email@example.com -p your-password -e http://localhost:8086 -k your-token
```

## Available Options

- `-u, --username` - Linz Strom Username (e-mail)
- `-p, --password` - Linz Strom Password
- `-d, --days` - Number of days to fetch (default: 7)
- `-e, --influxendpoint` - InfluxDB endpoint URL
- `-k, --influxtoken` - InfluxDB authentication token

## Priority Order

Configuration values are resolved in this order (highest to lowest priority):
1. Command-line arguments
2. Environment variables (from .env file or system environment)

## Security Note

The `.env` file is automatically ignored by git and should never be committed to version control. Keep your `.env` file secure and never share it publicly.
