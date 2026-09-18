using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Extensions.Options;
using NmeaMsfsBridge.App.Configuration;
using NmeaMsfsBridge.App.Models;
using NmeaMsfsBridge.App.Services;

namespace NmeaMsfsBridge.App;

public partial class MainWindow : Window
{
    private readonly TelemetryState _telemetryState;
    private readonly INmeaOutput _output;
    private readonly BridgeOptions _options;
    private readonly DispatcherTimer _refreshTimer;

    public MainWindow(
        TelemetryState telemetryState,
        INmeaOutput output,
        IOptions<BridgeOptions> options)
    {
        InitializeComponent();
        _telemetryState = telemetryState;
        _output = output;
        _options = options.Value;
        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _refreshTimer.Tick += (_, _) => RefreshStatus();
        _refreshTimer.Start();
        RefreshStatus();
    }

    protected override void OnClosed(EventArgs e)
    {
        _refreshTimer.Stop();
        base.OnClosed(e);
    }

    private void RefreshStatus()
    {
        var latest = _telemetryState.GetLatest();
        var age = latest is null ? (TimeSpan?)null : DateTime.UtcNow - latest.TimestampUtc;
        var isTelemetryCurrent = _telemetryState.IsSimulatorConnected
            && age is not null
            && age.Value <= TimeSpan.FromSeconds(_options.Watchdog.StaleAfterSeconds);

        SimulatorStatusText.Text = isTelemetryCurrent ? "Conectado / recibiendo datos" : "Esperando al simulador";
        SimulatorStatusText.Foreground = isTelemetryCurrent ? Brushes.ForestGreen : Brushes.DarkOrange;
        ClientsText.Text = _options.Output.Protocol.Equals("Tcp", StringComparison.OrdinalIgnoreCase)
            ? $"{_output.ConnectedClientCount} conectado(s)"
            : "UDP activo";
        TelemetryAgeText.Text = age is null ? "Sin datos" : $"{Math.Max(0, age.Value.TotalSeconds):0.0} s";
        TelemetryRateText.Text = $"{_options.Telemetry.PollHz} Hz";
        TransmitRateText.Text = $"{_options.Output.TransmitHz} Hz";
        OutputText.Text = $"{_options.Output.Protocol.ToUpperInvariant()} {_options.Output.Host}:{_options.Output.Port}";
        SummaryText.Text = isTelemetryCurrent
            ? "El bridge está funcionando correctamente."
            : "El bridge está iniciado y esperando telemetría de MSFS.";
        FooterText.Text = $"Actualizado {DateTime.Now:HH:mm:ss} · Cierra esta ventana para detener el bridge";
    }
}
