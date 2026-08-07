using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NmeaMsfsBridge.App.Configuration;
using NmeaMsfsBridge.App.Models;
using NmeaMsfsBridge.App.Services;

var entryAssembly = Assembly.GetEntryAssembly();
var informationalVersion = entryAssembly?
	.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
	.InformationalVersion;
var version = informationalVersion?.Split('+')[0]
	?? entryAssembly?.GetName().Version?.ToString(3)
	?? "desconocida";

Console.WriteLine($"NMEA MSFS2024 XCSoar Bridge {version}");
Console.WriteLine($"Creado por Juan Carlos Quijano Abad - {DateTime.Now.Year}");
Console.WriteLine();

var builder = Host.CreateApplicationBuilder(args);

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

builder.Services.AddLogging(logging =>
{
	logging.ClearProviders();
});

var app = builder.Build();

var options = app.Services.GetRequiredService<IOptions<BridgeOptions>>().Value;
Console.WriteLine(
	"Estableciendo el Bridge: {0} en la direccion {1}:{2}, a {3}hz de tx y {4}Hz de telemetria.",
	options.Output.Protocol.ToUpperInvariant(),
	options.Output.Host,
	options.Output.Port,
	options.Output.TransmitHz,
	options.Telemetry.PollHz);
Console.WriteLine("Presione Ctrl+C para cerrar la aplicacion");
Console.WriteLine();

await app.RunAsync();
