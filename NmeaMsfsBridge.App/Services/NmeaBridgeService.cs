using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NmeaMsfsBridge.App.Configuration;
using NmeaMsfsBridge.App.Models;

namespace NmeaMsfsBridge.App.Services;

public sealed class NmeaBridgeService : BackgroundService
{
    private readonly ILogger<NmeaBridgeService> _logger;
    private readonly INmeaEncoder _encoder;
    private readonly INmeaOutput _output;
    private readonly TelemetryState _telemetryState;
    private readonly BridgeOptions _options;

    public NmeaBridgeService(
        INmeaEncoder encoder,
        INmeaOutput output,
        TelemetryState telemetryState,
        IOptions<BridgeOptions> options,
        ILogger<NmeaBridgeService> logger)
    {
        _encoder = encoder;
        _output = output;
        _telemetryState = telemetryState;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _output.StartAsync(stoppingToken);
        _logger.LogInformation("NMEA bridge started at {TransmitHz} Hz", _options.Output.TransmitHz);

        var interval = TimeSpan.FromSeconds(1.0 / _options.Output.TransmitHz);
        while (!stoppingToken.IsCancellationRequested)
        {
            var sample = _telemetryState.GetLatest();
            if (sample is not null)
            {
                var sentences = _encoder.Encode(sample);
                await _output.BroadcastAsync(sentences, stoppingToken);
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _output.StopAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
