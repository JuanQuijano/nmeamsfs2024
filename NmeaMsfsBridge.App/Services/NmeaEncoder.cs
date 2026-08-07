using System.Globalization;
using NmeaMsfsBridge.App.Models;

namespace NmeaMsfsBridge.App.Services;

public sealed class NmeaEncoder : INmeaEncoder
{
    public IReadOnlyList<string> Encode(FlightSample sample)
    {
        var sentences = new List<string>
        {
            BuildGga(sample),
            BuildRmc(sample),
            BuildMwv(sample)
        };

        return sentences;
    }

    private static string BuildGga(FlightSample sample)
    {
        var utc = sample.TimestampUtc;
        var time = utc.ToString("HHmmss.00", CultureInfo.InvariantCulture);
        var (lat, latHemisphere) = ToLatitudeNmea(sample.LatitudeDeg);
        var (lon, lonHemisphere) = ToLongitudeNmea(sample.LongitudeDeg);

        var payload = string.Join(",",
            "GPGGA",
            time,
            lat,
            latHemisphere,
            lon,
            lonHemisphere,
            sample.FixQuality.ToString(CultureInfo.InvariantCulture),
            sample.Satellites.ToString("00", CultureInfo.InvariantCulture),
            sample.Hdop.ToString("0.0", CultureInfo.InvariantCulture),
            sample.AltitudeMeters.ToString("0.0", CultureInfo.InvariantCulture),
            "M",
            "0.0",
            "M",
            string.Empty,
            string.Empty);

        return BuildSentence(payload);
    }

    private static string BuildRmc(FlightSample sample)
    {
        var utc = sample.TimestampUtc;
        var time = utc.ToString("HHmmss.00", CultureInfo.InvariantCulture);
        var date = utc.ToString("ddMMyy", CultureInfo.InvariantCulture);
        var (lat, latHemisphere) = ToLatitudeNmea(sample.LatitudeDeg);
        var (lon, lonHemisphere) = ToLongitudeNmea(sample.LongitudeDeg);
        var status = sample.FixQuality > 0 ? "A" : "V";

        var payload = string.Join(",",
            "GPRMC",
            time,
            status,
            lat,
            latHemisphere,
            lon,
            lonHemisphere,
            sample.GroundSpeedKnots.ToString("0.0", CultureInfo.InvariantCulture),
            sample.TrackDegreesTrue.ToString("0.0", CultureInfo.InvariantCulture),
            date,
            Math.Abs(sample.MagneticVariationDeg).ToString("0.0", CultureInfo.InvariantCulture),
            sample.MagneticVariationDeg >= 0 ? "E" : "W");

        return BuildSentence(payload);
    }

    private static string BuildPgrmz(FlightSample sample)
    {
        var altitudeFeet = MetersToFeet(sample.PressureAltitudeMeters);
        var payload = string.Join(",",
            "PGRMZ",
            altitudeFeet.ToString("0.0", CultureInfo.InvariantCulture),
            "f",
            "3");

        return BuildSentence(payload);
    }

    private static string BuildLxwp0(FlightSample sample)
    {
        var payload = string.Join(",",
            "LXWP0",
            sample.VerticalSpeedMps.ToString("0.00", CultureInfo.InvariantCulture),
            sample.IndicatedAirspeedKnots.ToString("0.0", CultureInfo.InvariantCulture),
            MetersToFeet(sample.PressureAltitudeMeters).ToString("0.0", CultureInfo.InvariantCulture),
            sample.TrackDegreesTrue.ToString("0.0", CultureInfo.InvariantCulture),
            sample.GroundSpeedKnots.ToString("0.0", CultureInfo.InvariantCulture));

        return BuildSentence(payload);
    }

    private static string BuildPflau(FlightSample sample)
    {
        var payload = string.Join(",",
            "PFLAU",
            "1", // RX status
            "1", // TX status
            "1", // GPS status
            "1", // power status
            "0", // alarm level
            "0", // relative bearing
            "0", // alarm type
            "ABC123");

        return BuildSentence(payload);
    }

    private static string BuildPflaa(FlightSample sample)
    {
        var payload = string.Join(",",
            "PFLAA",
            "0", // alarm level
            "250", // relative north (m)
            "-80", // relative east (m)
            "20", // relative vertical (m)
            "ABC123", // traffic id
            sample.TrackDegreesTrue.ToString("0.0", CultureInfo.InvariantCulture),
            "0", // turn rate
            sample.GroundSpeedKnots.ToString("0.0", CultureInfo.InvariantCulture),
            sample.VerticalSpeedMps.ToString("0.00", CultureInfo.InvariantCulture),
            "1"); // aircraft type

        return BuildSentence(payload);
    }

    private static string BuildHdt(FlightSample sample)
    {
        var payload = string.Join(",",
            "HCHDT",
            NormalizeHeading(sample.TrackDegreesTrue).ToString("0.0", CultureInfo.InvariantCulture),
            "T");

        return BuildSentence(payload);
    }

    private static string BuildHdm(FlightSample sample)
    {
        var magneticHeading = NormalizeHeading(sample.TrackDegreesTrue - sample.MagneticVariationDeg);
        var payload = string.Join(",",
            "HCHDM",
            magneticHeading.ToString("0.0", CultureInfo.InvariantCulture),
            "M");

        return BuildSentence(payload);
    }

    private static string BuildMwv(FlightSample sample)
    {
        var windAngle = NormalizeHeading(sample.WindDirectionTrueDeg);
        var windSpeedKnots = Math.Max(0.0, sample.WindSpeedKnots);

        var payload = string.Join(",",
            "WIMWV",
            windAngle.ToString("0.0", CultureInfo.InvariantCulture),
            "T",
            windSpeedKnots.ToString("0.0", CultureInfo.InvariantCulture),
            "N",
            "A");

        return BuildSentence(payload);
    }

    private static (string Value, string Hemisphere) ToLatitudeNmea(double latitudeDeg)
    {
        var hemisphere = latitudeDeg >= 0 ? "N" : "S";
        var abs = Math.Abs(latitudeDeg);
        var degrees = Math.Floor(abs);
        var minutes = (abs - degrees) * 60;
        var value = string.Format(CultureInfo.InvariantCulture, "{0:00}{1:00.0000}", degrees, minutes);
        return (value, hemisphere);
    }

    private static (string Value, string Hemisphere) ToLongitudeNmea(double longitudeDeg)
    {
        var hemisphere = longitudeDeg >= 0 ? "E" : "W";
        var abs = Math.Abs(longitudeDeg);
        var degrees = Math.Floor(abs);
        var minutes = (abs - degrees) * 60;
        var value = string.Format(CultureInfo.InvariantCulture, "{0:000}{1:00.0000}", degrees, minutes);
        return (value, hemisphere);
    }

    private static string BuildSentence(string payload)
    {
        var checksum = ComputeChecksum(payload);
        return $"${payload}*{checksum}\r\n";
    }

    private static string ComputeChecksum(string payload)
    {
        var checksum = 0;
        foreach (var ch in payload)
        {
            checksum ^= ch;
        }

        return checksum.ToString("X2", CultureInfo.InvariantCulture);
    }

    private static double MetersToFeet(double meters)
    {
        return meters * 3.280839895;
    }

    private static double NormalizeHeading(double heading)
    {
        var normalized = heading % 360.0;
        if (normalized < 0)
        {
            normalized += 360.0;
        }

        return normalized;
    }
}
