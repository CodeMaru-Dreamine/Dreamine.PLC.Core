namespace Dreamine.PLC.Core.Simulation;

/// <summary>
/// Defines TCP PLC simulator server options.
/// </summary>
public sealed class PlcSimulatorServerOptions
{
    /// <summary>
    /// Gets or sets the server bind address.
    /// </summary>
    public string Host { get; set; } = "0.0.0.0";

    /// <summary>
    /// Gets or sets the server port.
    /// </summary>
    public int Port { get; set; } = 55000;

    /// <summary>
    /// Gets or sets whether the simulator writes an automatic response word after a trigger word is written.
    /// </summary>
    public bool EnableAutoWordResponse { get; set; } = true;

    /// <summary>
    /// Gets or sets the trigger word address for the automatic response test.
    /// </summary>
    public string AutoResponseTriggerAddress { get; set; } = "D100";

    /// <summary>
    /// Gets or sets the response word address for the automatic response test.
    /// </summary>
    public string AutoResponseAddress { get; set; } = "D101";

    /// <summary>
    /// Gets or sets the value increment used by the automatic response test.
    /// </summary>
    public short AutoResponseIncrement { get; set; } = 1;
}
