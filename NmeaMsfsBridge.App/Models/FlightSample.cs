namespace NmeaMsfsBridge.App.Models;

public sealed record FlightSample(
    DateTime TimestampUtc,
    double LatitudeDeg,
    double LongitudeDeg,
    double AltitudeMeters,
    double PressureAltitudeMeters,
    double GroundSpeedKnots,
    double TrackDegreesTrue,
    double VerticalSpeedMps,
    double IndicatedAirspeedKnots,
    double MagneticVariationDeg,
    bool IsOnGround,
    int FixQuality,
    int Satellites,
    double Hdop,
    double WindDirectionTrueDeg = 0.0,
    double WindSpeedKnots = 0.0);
