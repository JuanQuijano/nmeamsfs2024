using NmeaMsfsBridge.App.Models;

namespace NmeaMsfsBridge.App.Services;

public interface INmeaEncoder
{
    IReadOnlyList<string> Encode(FlightSample sample);
}
