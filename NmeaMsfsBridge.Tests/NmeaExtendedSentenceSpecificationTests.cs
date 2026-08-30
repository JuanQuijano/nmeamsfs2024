using System.Globalization;
using NmeaMsfsBridge.App.Models;
using NmeaMsfsBridge.App.Services;

namespace NmeaMsfsBridge.Tests;

public class NmeaExtendedSentenceSpecificationTests
{
    [Fact]
    public void Encode_ShouldOnlyEmitCompatibilitySet_GgaRmcAndMwv()
    {
        var encoder = new NmeaEncoder();
        var sample = BuildSample();
        var sentences = encoder.Encode(sample);

        Assert.Equal(3, sentences.Count);
        Assert.StartsWith("$GPRMC,", sentences[0], StringComparison.Ordinal);
        Assert.StartsWith("$GPGGA,", sentences[1], StringComparison.Ordinal);
        Assert.StartsWith("$WIMWV,", sentences[2], StringComparison.Ordinal);
    }

    [Fact]
    public void Encode_ShouldIncludeMwv_WithTrueReference_ValidStatusAndUnits()
    {
        var encoder = new NmeaEncoder();
        var sample = BuildSample(windDirectionTrueDeg: 245.4, windSpeedKnots: 14.2);

        var sentence = FindByPrefix(encoder.Encode(sample), "$WIMWV,");
        var fields = ExtractPayloadFields(sentence);

        Assert.Equal("WIMWV", fields[0]);
        Assert.Equal("245.4", fields[1]);
        Assert.Equal("T", fields[2]);
        Assert.Equal("14.2", fields[3]);
        Assert.Equal("N", fields[4]);
        Assert.Equal("A", fields[5]);
        Assert.True(HasValidChecksum(sentence));
    }

    private static FlightSample BuildSample(
        double pressureAltitudeMeters = 1000.0,
        double verticalSpeedMps = 0.5,
        double indicatedAirspeedKnots = 53.0,
        double trackDegreesTrue = 175.0,
        double windDirectionTrueDeg = 230.0,
        double windSpeedKnots = 12.0)
    {
        return new FlightSample(
            TimestampUtc: new DateTime(2026, 08, 06, 12, 34, 56, DateTimeKind.Utc),
            LatitudeDeg: 40.4168,
            LongitudeDeg: -3.7038,
            AltitudeMeters: 1000.0,
            PressureAltitudeMeters: pressureAltitudeMeters,
            GroundSpeedKnots: 52.0,
            TrackDegreesTrue: trackDegreesTrue,
            VerticalSpeedMps: verticalSpeedMps,
            IndicatedAirspeedKnots: indicatedAirspeedKnots,
            MagneticVariationDeg: 1.2,
            IsOnGround: false,
            FixQuality: 1,
            Satellites: 10,
                Hdop: 0.9,
                WindDirectionTrueDeg: windDirectionTrueDeg,
                WindSpeedKnots: windSpeedKnots);
    }

    private static string FindByPrefix(IReadOnlyList<string> sentences, string prefix)
    {
        return sentences.First(s => s.StartsWith(prefix, StringComparison.Ordinal));
    }

    private static bool HasValidChecksum(string sentence)
    {
        var trimmed = sentence.TrimEnd('\r', '\n');
        var star = trimmed.LastIndexOf('*');
        if (star < 0 || !trimmed.StartsWith('$') || star + 2 >= trimmed.Length)
        {
            return false;
        }

        var payload = trimmed.Substring(1, star - 1);
        var expected = trimmed.Substring(star + 1, 2);

        var checksum = 0;
        foreach (var ch in payload)
        {
            checksum ^= ch;
        }

        return string.Equals(checksum.ToString("X2", CultureInfo.InvariantCulture), expected, StringComparison.Ordinal);
    }

    private static string[] ExtractPayloadFields(string sentence)
    {
        var trimmed = sentence.TrimEnd('\r', '\n');
        var payload = trimmed.Substring(1, trimmed.IndexOf('*') - 1);
        return payload.Split(',');
    }
}
