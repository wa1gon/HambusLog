# Serilog Logging in HamBusLog

This project uses **Serilog** for structured logging.

## Configuration

Serilog is configured in `Program.cs` during application startup:

- **Log Level**: Information and above
- **Output Targets**:
  - **Console**: Logs to console for development/debugging
  - **File**: Logs to `$HOME/HamBusLog/applogs/hambuslog-*.log`
- **Log Rotation**: Daily (files named with date, e.g., `hambuslog-20260629.log`)
- **Retention**: Last 30 days of logs are retained

## Usage

Since Serilog is added to global usings in `zGlobal.cs`, you can use it anywhere in the application:

```csharp
// Information level logging
Log.Information("User performed action X");

// Warning level logging
Log.Warning("Configuration issue detected: {Issue}", issueDescription);

// Error level logging
Log.Error(ex, "Failed to save QSO for call {Call}", callsign);

// Debug level logging (filtered out by default)
Log.Debug("Processing message from {Source}", source);
```

## Log Format

Logs are formatted with the following template:

```
[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}
```

Example log output:
```
[2026-06-29 14:23:45.123 +00:00] [INF] HamBusLog application started
[2026-06-29 14:23:46.456 +00:00] [INF] Initializing HamBusLog framework
[2026-06-29 14:23:47.789 +00:00] [INF] Starting DX Cluster and rig control services
```

## Viewing Logs

View real-time logs in the console when running the application:

```bash
dotnet run --project /home/darryl/github/Hambus/HamBusLog/HamBusLog.csproj
```

View persisted logs:

```bash
# View today's log
cat $HOME/HamBusLog/applogs/hambuslog-*.log

# Search for specific messages
grep "error\|warning" $HOME/HamBusLog/applogs/hambuslog-*.log

# Follow log file (like tail -f)
tail -f $HOME/HamBusLog/applogs/hambuslog-*.log
```

## Troubleshooting

If logs are not appearing:

1. Check that the directory `$HOME/HamBusLog/applogs` exists and is writable
2. Ensure Serilog configuration in `Program.cs` hasn't been modified
3. Verify the minimum log level in the configuration

## Further Documentation

- [Serilog Documentation](https://serilog.net/)
- [Serilog Sinks](https://github.com/serilog/serilog/wiki/Provided-Sinks)

