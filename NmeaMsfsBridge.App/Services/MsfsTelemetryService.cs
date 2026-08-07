using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NmeaMsfsBridge.App.Configuration;
using NmeaMsfsBridge.App.Models;
using SimConnect.NET;

namespace NmeaMsfsBridge.App.Services;

public sealed class MsfsTelemetryService : BackgroundService
{
    private const double FeetToMeters = 0.3048;
    private const double FeetPerMinuteToMetersPerSecond = 0.00508;
    private const double RadiansToDegrees = 57.29577951308232;

    private readonly ILogger<MsfsTelemetryService> _logger;
    private readonly TelemetryState _telemetryState;
    private readonly BridgeOptions _options;

    public MsfsTelemetryService(
        IOptions<BridgeOptions> options,
        TelemetryState telemetryState,
        ILogger<MsfsTelemetryService> logger)
    {
        _logger = logger;
        _telemetryState = telemetryState;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(1.0 / _options.Telemetry.PollHz);
        _logger.LogInformation("Telemetry service started at {PollHz} Hz", _options.Telemetry.PollHz);

        var angle = 0.0;
        if (!_options.Telemetry.UseSimulatorData)
        {
            _logger.LogWarning("UseSimulatorData=false. Using synthetic telemetry generator.");
            while (!stoppingToken.IsCancellationRequested)
            {
                _telemetryState.Update(BuildSyntheticSample(DateTime.UtcNow, angle));
                angle = IncrementAngle(angle);
                await Task.Delay(interval, stoppingToken);
            }

            return;
        }

        SimConnectClient? simConnect = null;
        var lastConnectAttempt = DateTime.MinValue;

        while (!stoppingToken.IsCancellationRequested)
        {
            if (simConnect is null || !simConnect.IsConnected)
            {
                var now = DateTime.UtcNow;
                if (now - lastConnectAttempt >= TimeSpan.FromSeconds(3))
                {
                    lastConnectAttempt = now;
                    simConnect = await TryConnectAsync(simConnect, stoppingToken);
                }
            }

            if (simConnect is not null && simConnect.IsConnected)
            {
                try
                {
                    var sample = await ReadFromSimulatorAsync(simConnect, stoppingToken);
                    _telemetryState.Update(sample);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogWarning(ex, "SimConnect read failed. Reconnecting and using fallback telemetry until available.");
                    simConnect = await DisconnectAndDisposeAsync(simConnect);
                    _telemetryState.Update(BuildSyntheticSample(DateTime.UtcNow, angle));
                    angle = IncrementAngle(angle);
                }
            }
            else
            {
                _telemetryState.Update(BuildSyntheticSample(DateTime.UtcNow, angle));
                angle = IncrementAngle(angle);
            }

            await Task.Delay(interval, stoppingToken);
        }

        await DisconnectAndDisposeAsync(simConnect);
    }

    private async Task<SimConnectClient?> TryConnectAsync(SimConnectClient? existingClient, CancellationToken cancellationToken)
    {
        await DisconnectAndDisposeAsync(existingClient);

        try
        {
            var client = new SimConnectClient("NmeaMsfsBridge");
            await client.ConnectAsync(IntPtr.Zero, 0, 0, cancellationToken);
            _logger.LogInformation("Connected to MSFS through SimConnect. MSFS2024={Is2024}", client.IsMSFS2024);
            return client;
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation(ex, "SimConnect is not available yet. Waiting for simulator...");
            return null;
        }
    }

    private async Task<FlightSample> ReadFromSimulatorAsync(SimConnectClient client, CancellationToken cancellationToken)
    {
        var lat = await client.SimVars.GetAsync<double>("PLANE LATITUDE", "degrees", 0, cancellationToken);
        var lon = await client.SimVars.GetAsync<double>("PLANE LONGITUDE", "degrees", 0, cancellationToken);
        var altitudeFeet = await client.SimVars.GetAsync<double>("PLANE ALTITUDE", "feet", 0, cancellationToken);
        var indicatedAltitudeFeet = await client.SimVars.GetAsync<double>("INDICATED ALTITUDE", "feet", 0, cancellationToken);
        var groundSpeedKnots = await client.SimVars.GetAsync<double>("GROUND VELOCITY", "knots", 0, cancellationToken);
        var trackRadians = await client.SimVars.GetAsync<double>("GPS GROUND TRUE TRACK", "radians", 0, cancellationToken);
        var verticalSpeedFpm = await client.SimVars.GetAsync<double>("VERTICAL SPEED", "feet per minute", 0, cancellationToken);
        var iasKnots = await client.SimVars.GetAsync<double>("AIRSPEED INDICATED", "knots", 0, cancellationToken);
        var magneticVariation = await client.SimVars.GetAsync<double>("MAGVAR", "degrees", 0, cancellationToken);
        var windDirectionTrue = await client.SimVars.GetAsync<double>("AMBIENT WIND DIRECTION", "degrees", 0, cancellationToken);
        var windSpeedKnots = await client.SimVars.GetAsync<double>("AMBIENT WIND VELOCITY", "knots", 0, cancellationToken);
        var onGround = await client.SimVars.GetAsync<int>("SIM ON GROUND", "bool", 0, cancellationToken) != 0;

        return new FlightSample(
            TimestampUtc: DateTime.UtcNow,
            LatitudeDeg: lat,
            LongitudeDeg: lon,
            AltitudeMeters: altitudeFeet * FeetToMeters,
            PressureAltitudeMeters: indicatedAltitudeFeet * FeetToMeters,
            GroundSpeedKnots: groundSpeedKnots,
            TrackDegreesTrue: NormalizeDegrees(trackRadians * RadiansToDegrees),
            VerticalSpeedMps: verticalSpeedFpm * FeetPerMinuteToMetersPerSecond,
            IndicatedAirspeedKnots: iasKnots,
            MagneticVariationDeg: magneticVariation,
            IsOnGround: onGround,
            FixQuality: _options.Defaults.FixQuality,
            Satellites: _options.Defaults.Satellites,
            Hdop: _options.Defaults.Hdop,
            WindDirectionTrueDeg: NormalizeDegrees(windDirectionTrue),
            WindSpeedKnots: Math.Max(0.0, windSpeedKnots));
    }

    private static async Task<SimConnectClient?> DisconnectAndDisposeAsync(SimConnectClient? client)
    {
        if (client is null)
        {
            return null;
        }

        try
        {
            await client.DisconnectAsync();
        }
        catch
        {
            // Ignore disconnect errors because we are disposing the client anyway.
        }

        await client.DisposeAsync();
        return null;
    }

    private static double NormalizeDegrees(double value)
    {
        var normalized = value % 360.0;
        if (normalized < 0)
        {
            normalized += 360.0;
        }

        return normalized;
    }

    private static double IncrementAngle(double angle)
    {
        angle += 0.02;
        if (angle > Math.PI * 2)
        {
            return 0.0;
        }

        return angle;
    }

    private FlightSample BuildSyntheticSample(DateTime utc, double angle)
    {
        var latitude = 40.4168 + Math.Sin(angle) * 0.01;
        var longitude = -3.7038 + Math.Cos(angle) * 0.01;
        var altitudeM = 900 + Math.Sin(angle * 0.5) * 120;
        var verticalSpeedMps = Math.Cos(angle * 0.5) * 0.6;
        var track = (angle * 180.0 / Math.PI) % 360.0;
        if (track < 0)
        {
            track += 360;
        }

        return new FlightSample(
            utc,
            latitude,
            longitude,
            altitudeM,
            altitudeM,
            GroundSpeedKnots: 52.0,
            TrackDegreesTrue: track,
            VerticalSpeedMps: verticalSpeedMps,
            IndicatedAirspeedKnots: 54.0,
            MagneticVariationDeg: 1.2,
            IsOnGround: false,
            FixQuality: _options.Defaults.FixQuality,
            Satellites: _options.Defaults.Satellites,
            Hdop: _options.Defaults.Hdop,
            WindDirectionTrueDeg: 230.0,
            WindSpeedKnots: 12.0);
    }
}
