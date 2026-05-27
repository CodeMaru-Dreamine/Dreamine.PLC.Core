using System.Globalization;

namespace Dreamine.PLC.Core.Simulation;

/// <summary>
/// \brief Provides helpers for the Dreamine PLC simulator line protocol.
/// </summary>
public static class PlcSimulatorProtocol
{
    /// <summary>
    /// \brief The read bit command name.
    /// </summary>
    public const string ReadBits = "READ_BITS";

    /// <summary>
    /// \brief The read word command name.
    /// </summary>
    public const string ReadWords = "READ_WORDS";

    /// <summary>
    /// \brief The write bit command name.
    /// </summary>
    public const string WriteBits = "WRITE_BITS";

    /// <summary>
    /// \brief The write word command name.
    /// </summary>
    public const string WriteWords = "WRITE_WORDS";

    /// <summary>
    /// \brief Builds a successful response line.
    /// </summary>
    /// <param name="payload">The response payload.</param>
    /// <returns>The successful response line.</returns>
    public static string Ok(string? payload = null)
    {
        return string.IsNullOrWhiteSpace(payload) ? "OK" : $"OK {payload}";
    }

    /// <summary>
    /// \brief Builds a failure response line.
    /// </summary>
    /// <param name="message">The failure message.</param>
    /// <returns>The failure response line.</returns>
    public static string Error(string message)
    {
        return $"ERR {message.Replace('\r', ' ').Replace('\n', ' ')}";
    }

    /// <summary>
    /// \brief Converts bit values to a protocol payload.
    /// </summary>
    /// <param name="values">The bit values.</param>
    /// <returns>The payload text.</returns>
    public static string FormatBits(IEnumerable<bool> values)
    {
        return string.Join(',', values.Select(x => x ? "1" : "0"));
    }

    /// <summary>
    /// \brief Converts word values to a protocol payload.
    /// </summary>
    /// <param name="values">The word values.</param>
    /// <returns>The payload text.</returns>
    public static string FormatWords(IEnumerable<short> values)
    {
        return string.Join(',', values.Select(x => x.ToString(CultureInfo.InvariantCulture)));
    }

    /// <summary>
    /// \brief Parses bit values from a protocol payload.
    /// </summary>
    /// <param name="payload">The payload text.</param>
    /// <returns>The parsed bit values.</returns>
    public static bool[] ParseBits(string payload)
    {
        return SplitValues(payload)
            .Select(ParseBit)
            .ToArray();
    }

    /// <summary>
    /// \brief Parses word values from a protocol payload.
    /// </summary>
    /// <param name="payload">The payload text.</param>
    /// <returns>The parsed word values.</returns>
    public static short[] ParseWords(string payload)
    {
        return SplitValues(payload)
            .Select(x => short.Parse(x, CultureInfo.InvariantCulture))
            .ToArray();
    }

    /// <summary>
    /// \brief Extracts the OK payload from a response line.
    /// </summary>
    /// <param name="line">The response line.</param>
    /// <returns>The response payload.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the response is an error response.</exception>
    public static string ReadOkPayload(string line)
    {
        if (line.StartsWith("OK", StringComparison.OrdinalIgnoreCase))
        {
            return line.Length > 2 ? line[3..].Trim() : string.Empty;
        }

        if (line.StartsWith("ERR", StringComparison.OrdinalIgnoreCase))
        {
            var message = line.Length > 3 ? line[4..].Trim() : "PLC simulator error.";
            throw new InvalidOperationException(message);
        }

        throw new InvalidOperationException($"Invalid PLC simulator response: {line}");
    }

    private static string[] SplitValues(string payload)
    {
        return payload.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static bool ParseBit(string text)
    {
        return text switch
        {
            "1" => true,
            "0" => false,
            _ when bool.TryParse(text, out var value) => value,
            _ => throw new FormatException($"Invalid bit value: {text}")
        };
    }
}
