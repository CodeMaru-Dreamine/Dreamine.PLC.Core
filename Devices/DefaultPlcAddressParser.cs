using System.Globalization;
using Dreamine.PLC.Abstractions.Devices;
using Dreamine.PLC.Abstractions.Results;

namespace Dreamine.PLC.Core.Devices;

/// <summary>
/// Provides the default PLC address parser.
/// </summary>
public sealed class DefaultPlcAddressParser : IPlcAddressParser
{
    private static readonly string[] KnownDevicePrefixes =
    [
        "ZR",
        "D",
        "M",
        "X",
        "Y",
        "B",
        "W",
        "R"
    ];

    /// <inheritdoc />
    public PlcResult<PlcAddress> Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return PlcResult<PlcAddress>.Failure("The PLC address text must not be empty.");
        }

        var normalizedText = text.Trim().ToUpperInvariant();
        var devicePrefix = FindDevicePrefix(normalizedText);

        if (devicePrefix is null)
        {
            return PlcResult<PlcAddress>.Failure($"Unsupported PLC device prefix. Address: {text}");
        }

        var deviceType = ConvertDeviceType(devicePrefix);
        if (deviceType == PlcDeviceType.Unknown)
        {
            return PlcResult<PlcAddress>.Failure($"Unknown PLC device type. Address: {text}");
        }

        var body = normalizedText[devicePrefix.Length..];
        if (string.IsNullOrWhiteSpace(body))
        {
            return PlcResult<PlcAddress>.Failure($"PLC address offset is missing. Address: {text}");
        }

        var split = body.Split('.', StringSplitOptions.TrimEntries);
        if (split.Length > 2)
        {
            return PlcResult<PlcAddress>.Failure($"Invalid PLC bit address format. Address: {text}");
        }

        if (!int.TryParse(split[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var offset))
        {
            return PlcResult<PlcAddress>.Failure($"Invalid PLC address offset. Address: {text}");
        }

        if (offset < 0)
        {
            return PlcResult<PlcAddress>.Failure($"PLC address offset must be greater than or equal to zero. Address: {text}");
        }

        int? bitOffset = null;

        if (split.Length == 2)
        {
            if (!int.TryParse(split[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedBitOffset))
            {
                return PlcResult<PlcAddress>.Failure($"Invalid PLC bit offset. Address: {text}");
            }

            if (parsedBitOffset is < 0 or > 15)
            {
                return PlcResult<PlcAddress>.Failure($"PLC bit offset must be between 0 and 15. Address: {text}");
            }

            bitOffset = parsedBitOffset;
        }

        return PlcResult<PlcAddress>.Success(new PlcAddress(deviceType, offset, bitOffset));
    }

    /// <inheritdoc />
    public bool TryParse(string text, out PlcAddress address)
    {
        var result = Parse(text);

        if (!result.IsSuccess)
        {
            address = default;
            return false;
        }

        address = result.Value;
        return true;
    }

    private static string? FindDevicePrefix(string text)
    {
        foreach (var prefix in KnownDevicePrefixes)
        {
            if (text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return prefix;
            }
        }

        return null;
    }

    private static PlcDeviceType ConvertDeviceType(string devicePrefix)
    {
        return devicePrefix switch
        {
            "D" => PlcDeviceType.D,
            "M" => PlcDeviceType.M,
            "X" => PlcDeviceType.X,
            "Y" => PlcDeviceType.Y,
            "B" => PlcDeviceType.B,
            "W" => PlcDeviceType.W,
            "R" => PlcDeviceType.R,
            "ZR" => PlcDeviceType.ZR,
            _ => PlcDeviceType.Unknown
        };
    }
}