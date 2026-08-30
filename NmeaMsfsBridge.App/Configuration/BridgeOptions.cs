using System.ComponentModel.DataAnnotations;

namespace NmeaMsfsBridge.App.Configuration;

public sealed class BridgeOptions
{
    public const string SectionName = "Bridge";

    [Required]
    public TelemetryOptions Telemetry { get; init; } = new();

    [Required]
    public OutputOptions Output { get; init; } = new();

    [Required]
    public WatchdogOptions Watchdog { get; init; } = new();

    [Required]
    public DefaultsOptions Defaults { get; init; } = new();

    [Required]
    public StreamStabilityOptions StreamStability { get; init; } = new();
}

public sealed class TelemetryOptions
{
    [Range(1, 20)]
    public int PollHz { get; init; } = 5;

    public bool UseSimulatorData { get; init; } = true;
}

public sealed class OutputOptions
{
    [Required]
    [RegularExpression("^(Tcp|Udp)$")]
    public string Protocol { get; init; } = "Tcp";

    [Required]
    public string Host { get; init; } = "127.0.0.1";

    [Range(1, 65535)]
    public int Port { get; init; } = 4353;

    [Range(1, 20)]
    public int TransmitHz { get; init; } = 5;
}

public sealed class WatchdogOptions
{
    [Range(1, 60)]
    public int StaleAfterSeconds { get; init; } = 3;
}

public sealed class DefaultsOptions
{
    [Range(0, 8)]
    public int FixQuality { get; init; } = 1;

    [Range(0, 30)]
    public int Satellites { get; init; } = 10;

    [Range(0.1, 99.9)]
    public double Hdop { get; init; } = 0.9;
}

public sealed class StreamStabilityOptions
{
    public bool Enabled { get; init; } = true;

    [Range(1, 20000)]
    public double GeographicJumpKilometers { get; init; } = 100;

    [Range(1, 20)]
    public int StableSamplesRequired { get; init; } = 3;
}
