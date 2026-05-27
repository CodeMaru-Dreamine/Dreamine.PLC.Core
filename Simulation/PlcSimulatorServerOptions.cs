namespace Dreamine.PLC.Core.Simulation;

/// <summary>
/// \brief Defines TCP PLC simulator server options.
/// </summary>
public sealed class PlcSimulatorServerOptions
{
    /// <summary>
    /// \brief Gets or sets the server bind address.
    /// </summary>
    public string Host { get; set; } = "0.0.0.0";

    /// <summary>
    /// \brief Gets or sets the server port.
    /// </summary>
    public int Port { get; set; } = 55000;

    /// <summary>
    /// \brief Gets or sets whether the simulator writes an automatic response word after a trigger word is written.
    /// </summary>
    public bool EnableAutoWordResponse { get; set; } = true;

    /// <summary>
    /// \brief Gets or sets the trigger word address for the automatic response test.
    /// </summary>
    public string AutoResponseTriggerAddress { get; set; } = "D100";

    /// <summary>
    /// \brief Gets or sets the response word address for the automatic response test.
    /// </summary>
    public string AutoResponseAddress { get; set; } = "D101";

    /// <summary>
    /// \brief Gets or sets the value increment used by the automatic response test.
    /// </summary>
    public short AutoResponseIncrement { get; set; } = 1;
}
