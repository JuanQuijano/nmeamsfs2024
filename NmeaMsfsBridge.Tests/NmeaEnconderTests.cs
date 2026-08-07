using System.Globalization;
using NmeaMsfsBridge.App.Models;
using NmeaMsfsBridge.App.Services;

namespace NmeaMsfsBridge.Tests;

public class NmeaEncoderTests
{
    [Fact]
    public void Encode_ShouldGenerateGgaAndRmc_WithValidChecksumAndCrLf()
    {
        var encoder = new NmeaEncoder();
        var sample = BuildSample();

        var sentences = encoder.Encode(sample);

        Assert.True(sentences.Count >= 2);
        Assert.Contains(sentences, s => s.StartsWith("$GPGGA", StringComparison.Ordinal));
        Assert.Contains(sentences, s => s.StartsWith("$GPRMC", StringComparison.Ordinal));
        foreach (var sentence in sentences)
        {
            Assert.StartsWith("$", sentence);
            Assert.EndsWith("\r\n", sentence);
            Assert.True(HasValidChecksum(sentence));
        }
    }

    [Fact]
    public void Encode_Gga_ShouldContainExpectedCoordinateAndAltitudeFormatting()
    {
        var encoder = new NmeaEncoder();
        var sample = BuildSample(
            latitude: 40.4168,
            longitude: -3.7038,
            altitudeMeters: 1234.5,
            satellites: 7,
            hdop: 0.9);

        var gga = encoder.Encode(sample)[0];
        var fields = ExtractPayloadFields(gga);

        Assert.Equal("GPGGA", fields[0]);
        Assert.Equal("4025.0080", fields[2]);
        Assert.Equal("N", fields[3]);
        Assert.Equal("00342.2280", fields[4]);
        Assert.Equal("W", fields[5]);
        Assert.Equal("07", fields[7]);
        Assert.Equal("0.9", fields[8]);
        Assert.Equal("1234.5", fields[9]);
    }

    [Fact]
    public void Encode_Rmc_ShouldUseAbsoluteMagneticVariation_AndCorrectHemisphere()
    {
        var encoder = new NmeaEncoder();
        var sample = BuildSample(magneticVariationDeg: -2.5);

        var rmc = encoder.Encode(sample)[1];
        var fields = ExtractPayloadFields(rmc);

        Assert.Equal("GPRMC", fields[0]);
        Assert.Equal("2.5", fields[10]);
        Assert.Equal("W", fields[11]);
    }

    [Fact]
    public void Encode_ShouldUseInvariantCulture_ForDecimals()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("es-ES");
            CultureInfo.CurrentUICulture = new CultureInfo("es-ES");

            var encoder = new NmeaEncoder();
            var sample = BuildSample(
                groundSpeedKnots: 52.3,
                trackDegreesTrue: 183.7,
                altitudeMeters: 987.6);

            var sentences = encoder.Encode(sample);

            Assert.DoesNotContain(',', sentences[0].Split('*')[0].Split('$')[1].Split(',')[9]);
            Assert.Contains("52.3", sentences[1]);
            Assert.Contains("183.7", sentences[1]);
            Assert.Contains("987.6", sentences[0]);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
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

    private static FlightSample BuildSample(
        double latitude = 40.4168,
        double longitude = -3.7038,
        double altitudeMeters = 1000.0,
        double groundSpeedKnots = 52.0,
        double trackDegreesTrue = 175.0,
        double magneticVariationDeg = 1.2,
        int satellites = 10,
        double hdop = 0.9)
    {
        return new FlightSample(
            TimestampUtc: new DateTime(2026, 08, 06, 12, 34, 56, DateTimeKind.Utc),
            LatitudeDeg: latitude,
            LongitudeDeg: longitude,
            AltitudeMeters: altitudeMeters,
            PressureAltitudeMeters: altitudeMeters,
            GroundSpeedKnots: groundSpeedKnots,
            TrackDegreesTrue: trackDegreesTrue,
            VerticalSpeedMps: 0.5,
            IndicatedAirspeedKnots: 53.0,
            MagneticVariationDeg: magneticVariationDeg,
            IsOnGround: false,
            FixQuality: 1,
            Satellites: satellites,
            Hdop: hdop);
    }
}
