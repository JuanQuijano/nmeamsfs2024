namespace NmeaMsfsBridge.App.Models;

public sealed class TelemetryState
{
    private readonly object _sync = new();
    private FlightSample? _lastSample;
    private bool _simulatorConnected;

    public bool IsSimulatorConnected
    {
        get
        {
            lock (_sync)
            {
                return _simulatorConnected;
            }
        }
    }

    public void SetSimulatorConnected(bool connected)
    {
        lock (_sync)
        {
            _simulatorConnected = connected;
        }
    }

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

    public DateTime? GetLatestTimestampUtc()
    {
        lock (_sync)
        {
            return _lastSample?.TimestampUtc;
        }
    }
}
