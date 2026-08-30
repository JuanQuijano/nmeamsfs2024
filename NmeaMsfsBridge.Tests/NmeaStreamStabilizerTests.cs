using NmeaMsfsBridge.App.Configuration;
using NmeaMsfsBridge.App.Models;
using NmeaMsfsBridge.App.Services;

namespace NmeaMsfsBridge.Tests;

public class NmeaStreamStabilizerTests
{
    [Fact]
    public void TryAccept_WaitsForStableSamplesBeforeStartingStream()
    {
        var stabilizer = CreateStabilizer(stableSamplesRequired: 3);

        Assert.False(stabilizer.TryAccept(BuildSample(), out _));
        Assert.False(stabilizer.TryAccept(BuildSample(), out _));
        Assert.True(stabilizer.TryAccept(BuildSample(), out _));
    }

    [Fact]
    public void TryAccept_GeographicJumpPausesUntilNewLocationIsStable()
    {
        var stabilizer = CreateStabilizer(stableSamplesRequired: 2, geographicJumpKilometers: 100);

        Assert.False(stabilizer.TryAccept(BuildSample(latitude: 40.4168, longitude: -3.7038), out _));
        Assert.True(stabilizer.TryAccept(BuildSample(latitude: 40.4168, longitude: -3.7038), out _));

        Assert.False(stabilizer.TryAccept(BuildSample(latitude: 51.5072, longitude: -0.1276), out _));
        Assert.True(stabilizer.TryAccept(BuildSample(latitude: 51.5072, longitude: -0.1276), out _));
    }

    [Fact]
    public void TryAccept_MakesTimestampsStrictlyMonotonic()
    {
        var stabilizer = CreateStabilizer(stableSamplesRequired: 1);
        var later = new DateTime(2026, 08, 30, 12, 0, 1, DateTimeKind.Utc);
        var earlier = later.AddSeconds(-10);

        Assert.True(stabilizer.TryAccept(BuildSample(timestampUtc: later), out var first));
        Assert.True(stabilizer.TryAccept(BuildSample(timestampUtc: earlier), out var second));

        Assert.True(second.TimestampUtc > first.TimestampUtc);
    }

    [Fact]
    public void TryAccept_IncompleteTelemetryDoesNotResumeStream()
    {
        var stabilizer = CreateStabilizer(stableSamplesRequired: 1);
        var incompleteSample = BuildSample() with { GroundSpeedKnots = double.NaN };

        Assert.False(stabilizer.TryAccept(incompleteSample, out _));
    }

    private static NmeaStreamStabilizer CreateStabilizer(int stableSamplesRequired, double geographicJumpKilometers = 100)
    {
        return new NmeaStreamStabilizer(new StreamStabilityOptions
        {
            GeographicJumpKilometers = geographicJumpKilometers,
            StableSamplesRequired = stableSamplesRequired
        });
    }

    private static FlightSample BuildSample(
        double latitude = 40.4168,
        double longitude = -3.7038,
        DateTime? timestampUtc = null)
    {
        return new FlightSample(
            TimestampUtc: timestampUtc ?? new DateTime(2026, 08, 30, 12, 0, 0, DateTimeKind.Utc),
            LatitudeDeg: latitude,
            LongitudeDeg: longitude,
            AltitudeMeters: 1000,
            PressureAltitudeMeters: 1000,
            GroundSpeedKnots: 50,
            TrackDegreesTrue: 180,
            VerticalSpeedMps: 0,
            IndicatedAirspeedKnots: 50,
            MagneticVariationDeg: 0,
            IsOnGround: false,
            FixQuality: 1,
            Satellites: 10,
            Hdop: 0.9);
    }
}