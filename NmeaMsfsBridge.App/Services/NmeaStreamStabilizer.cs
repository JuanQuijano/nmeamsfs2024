using NmeaMsfsBridge.App.Configuration;
using NmeaMsfsBridge.App.Models;

namespace NmeaMsfsBridge.App.Services;

public sealed class NmeaStreamStabilizer
{
    private const double EarthRadiusKilometers = 6371.0;

    private readonly StreamStabilityOptions _options;
    private FlightSample? _lastEmitted;
    private FlightSample? _pendingSample;
    private int _stableSampleCount;
    private DateTime _lastTimestampUtc = DateTime.MinValue;

    public NmeaStreamStabilizer(StreamStabilityOptions options)
    {
        _options = options;
    }

    public bool TryAccept(FlightSample sample, out FlightSample stabilizedSample)
    {
        stabilizedSample = sample;

        if (!HasValidTelemetry(sample))
        {
            ResetPending();
            return false;
        }

        if (_lastEmitted is not null && _options.Enabled &&
            DistanceKilometers(_lastEmitted, sample) >= _options.GeographicJumpKilometers)
        {
            _lastEmitted = null;
            ResetPending();
        }

        if (_lastEmitted is null && _options.Enabled)
        {
            if (_pendingSample is null ||
                DistanceKilometers(_pendingSample, sample) >= _options.GeographicJumpKilometers)
            {
                _pendingSample = sample;
                _stableSampleCount = 1;
            }
            else
            {
                _pendingSample = sample;
                _stableSampleCount++;
            }

            if (_stableSampleCount < _options.StableSamplesRequired)
            {
                return false;
            }
        }

        stabilizedSample = sample with { TimestampUtc = GetMonotonicTimestamp(sample.TimestampUtc) };
        _lastEmitted = stabilizedSample;
        ResetPending();
        return true;
    }

    private DateTime GetMonotonicTimestamp(DateTime timestampUtc)
    {
        var utc = timestampUtc.Kind == DateTimeKind.Utc
            ? timestampUtc
            : timestampUtc.ToUniversalTime();
        var minimum = _lastTimestampUtc.AddMilliseconds(10);
        var monotonicTimestamp = utc < minimum ? minimum : utc;
        _lastTimestampUtc = monotonicTimestamp;
        return monotonicTimestamp;
    }

    private void ResetPending()
    {
        _pendingSample = null;
        _stableSampleCount = 0;
    }

    private static bool HasValidTelemetry(FlightSample sample) =>
        double.IsFinite(sample.LatitudeDeg) && double.IsFinite(sample.LongitudeDeg) &&
        double.IsFinite(sample.AltitudeMeters) && double.IsFinite(sample.PressureAltitudeMeters) &&
        double.IsFinite(sample.GroundSpeedKnots) && double.IsFinite(sample.TrackDegreesTrue) &&
        double.IsFinite(sample.VerticalSpeedMps) && double.IsFinite(sample.IndicatedAirspeedKnots) &&
        double.IsFinite(sample.MagneticVariationDeg) && double.IsFinite(sample.Hdop) &&
        double.IsFinite(sample.WindDirectionTrueDeg) && double.IsFinite(sample.WindSpeedKnots) &&
        sample.LatitudeDeg is >= -90 and <= 90 && sample.LongitudeDeg is >= -180 and <= 180;

    private static double DistanceKilometers(FlightSample first, FlightSample second)
    {
        var latitudeDeltaRadians = DegreesToRadians(second.LatitudeDeg - first.LatitudeDeg);
        var longitudeDeltaRadians = DegreesToRadians(second.LongitudeDeg - first.LongitudeDeg);
        var firstLatitudeRadians = DegreesToRadians(first.LatitudeDeg);
        var secondLatitudeRadians = DegreesToRadians(second.LatitudeDeg);
        var haversine = Math.Pow(Math.Sin(latitudeDeltaRadians / 2), 2) +
            Math.Cos(firstLatitudeRadians) * Math.Cos(secondLatitudeRadians) *
            Math.Pow(Math.Sin(longitudeDeltaRadians / 2), 2);
        return EarthRadiusKilometers * 2 * Math.Atan2(Math.Sqrt(haversine), Math.Sqrt(1 - haversine));
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
}