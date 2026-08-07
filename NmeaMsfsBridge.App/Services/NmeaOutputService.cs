using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NmeaMsfsBridge.App.Configuration;

namespace NmeaMsfsBridge.App.Services;

public sealed class NmeaOutputService : INmeaOutput
{
    private readonly ILogger<NmeaOutputService> _logger;
    private readonly OutputOptions _options;
    private readonly ConcurrentDictionary<int, TcpClient> _tcpClients = new();
    private TcpListener? _tcpListener;
    private UdpClient? _udpClient;
    private Task? _acceptLoop;
    private CancellationTokenSource? _internalCts;
    private int _clientId;

    public NmeaOutputService(IOptions<BridgeOptions> options, ILogger<NmeaOutputService> logger)
    {
        _logger = logger;
        _options = options.Value.Output;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _internalCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        if (_options.Protocol.Equals("Tcp", StringComparison.OrdinalIgnoreCase))
        {
            var ipAddress = IPAddress.Parse(_options.Host);
            _tcpListener = new TcpListener(ipAddress, _options.Port);
            _tcpListener.Start();
            _acceptLoop = AcceptTcpClientsAsync(_internalCts.Token);
            _logger.LogInformation("TCP output listening on {Host}:{Port}", _options.Host, _options.Port);
        }
        else
        {
            _udpClient = new UdpClient();
            _logger.LogInformation("UDP output ready for {Host}:{Port}", _options.Host, _options.Port);
        }

        return Task.CompletedTask;
    }

    public async Task BroadcastAsync(IEnumerable<string> sentences, CancellationToken cancellationToken)
    {
        var payload = string.Concat(sentences);
        var bytes = Encoding.ASCII.GetBytes(payload);

        if (_options.Protocol.Equals("Tcp", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var entry in _tcpClients)
            {
                var clientId = entry.Key;
                var client = entry.Value;

                try
                {
                    if (!client.Connected)
                    {
                        RemoveClient(clientId);
                        continue;
                    }

                    await client.GetStream().WriteAsync(bytes, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Removing TCP client {ClientId} after write failure", clientId);
                    RemoveClient(clientId);
                }
            }

            return;
        }

        if (_udpClient is not null)
        {
            await _udpClient.SendAsync(bytes, bytes.Length, _options.Host, _options.Port);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _internalCts?.Cancel();

        _tcpListener?.Stop();
        _tcpListener = null;

        foreach (var entry in _tcpClients)
        {
            RemoveClient(entry.Key);
        }

        _udpClient?.Dispose();
        _udpClient = null;

        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
        _internalCts?.Dispose();
    }

    private async Task AcceptTcpClientsAsync(CancellationToken cancellationToken)
    {
        if (_tcpListener is null)
        {
            return;
        }

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var client = await _tcpListener.AcceptTcpClientAsync(cancellationToken);
                var id = Interlocked.Increment(ref _clientId);
                _tcpClients.TryAdd(id, client);
                _logger.LogInformation("TCP client connected: {ClientId}", id);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Accept loop terminated unexpectedly");
        }
    }

    private void RemoveClient(int clientId)
    {
        if (_tcpClients.TryRemove(clientId, out var client))
        {
            try
            {
                client.Dispose();
            }
            catch
            {
            }

            _logger.LogInformation("TCP client disconnected: {ClientId}", clientId);
        }
    }
}
