using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NmeaMsfsBridge.App.Configuration;
using NmeaMsfsBridge.App.Models;

namespace NmeaMsfsBridge.App.Services;

public sealed class BridgeWatchdogService : BackgroundService
{
    private readonly ILogger<BridgeWatchdogService> _logger;
    private readonly TelemetryState _telemetryState;
    private readonly TimeSpan _staleAfter;

    public BridgeWatchdogService(
        TelemetryState telemetryState,
        IOptions<BridgeOptions> options,
        ILogger<BridgeWatchdogService> logger)
    {
        _telemetryState = telemetryState;
        _logger = logger;
        _staleAfter = TimeSpan.FromSeconds(options.Value.Watchdog.StaleAfterSeconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var sample = _telemetryState.GetLatest();
            if (sample is not null)
            {
                var age = DateTime.UtcNow - sample.TimestampUtc;
                if (age > _staleAfter)
                {
                    _logger.LogWarning("Telemetry is stale: age={AgeMs}ms", age.TotalMilliseconds);
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }
}
