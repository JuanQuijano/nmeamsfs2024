namespace NmeaMsfsBridge.App.Models;

public sealed class TelemetryState
{
    private readonly object _sync = new();
    private FlightSample? _lastSample;

    public void Update(FlightSample sample)
    {
        lock (_sync)
        {
            _lastSample = sample;
        }
    }

    public FlightSample? GetLatest()
    {
        lock (_sync)
        {
            return _lastSample;
        }
    }
}
