using System.Reflection;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NmeaMsfsBridge.App.Configuration;
using NmeaMsfsBridge.App.Models;
using NmeaMsfsBridge.App.Services;

namespace NmeaMsfsBridge.App;

public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var builder = Host.CreateApplicationBuilder(e.Args);
        builder.Configuration
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables(prefix: "NMEA_BRIDGE_");

        builder.Services
            .AddOptions<BridgeOptions>()
            .Bind(builder.Configuration.GetSection(BridgeOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddSingleton<TelemetryState>();
        builder.Services.AddSingleton<INmeaEncoder, NmeaEncoder>();
        builder.Services.AddSingleton<INmeaOutput, NmeaOutputService>();
        builder.Services.AddHostedService<MsfsTelemetryService>();
        builder.Services.AddHostedService<NmeaBridgeService>();
        builder.Services.AddHostedService<BridgeWatchdogService>();
        builder.Services.AddLogging(logging => logging.ClearProviders());

        _host = builder.Build();
        try
        {
            await _host.StartAsync();
            var window = new MainWindow(
                _host.Services.GetRequiredService<TelemetryState>(),
                _host.Services.GetRequiredService<INmeaOutput>(),
                _host.Services.GetRequiredService<IOptions<BridgeOptions>>());
            MainWindow = window;
            window.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"No se pudo iniciar el bridge:\n{ex.Message}",
                "NMEA MSFS Bridge",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        base.OnExit(e);
    }
}
