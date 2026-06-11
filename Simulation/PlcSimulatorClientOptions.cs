namespace Dreamine.PLC.Core.Simulation;

/// <summary>
/// Defines TCP PLC simulator client options.
/// </summary>
public sealed class PlcSimulatorClientOptions
{
    /// <summary>
    /// Gets or sets the remote host.
    /// </summary>
    public string Host { get; set; } = "127.0.0.1";

    /// <summary>
    /// Gets or sets the remote port.
    /// </summary>
    public int Port { get; set; } = 55000;

    /// <summary>
    /// Gets or sets the connect timeout in milliseconds.
    /// </summary>
    public int ConnectTimeoutMs { get; set; } = 3000;
}
