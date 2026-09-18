namespace NmeaMsfsBridge.App.Services;

public interface INmeaOutput : IAsyncDisposable
{
    int ConnectedClientCount { get; }
    Task StartAsync(CancellationToken cancellationToken);
    Task BroadcastAsync(IEnumerable<string> sentences, CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}
