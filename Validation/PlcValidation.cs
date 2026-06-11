using Dreamine.PLC.Abstractions.Devices;
using Dreamine.PLC.Abstractions.Results;

namespace Dreamine.PLC.Core.Validation;

/// <summary>
/// Provides common PLC request validation helpers.
/// </summary>
public static class PlcValidation
{
    /// <summary>
    /// Validates a PLC read or write count.
    /// </summary>
    /// <param name="count">The requested item count.</param>
    /// <returns>The validation result.</returns>
    public static PlcResult ValidateCount(int count)
    {
        if (count <= 0)
        {
            return PlcResult.Failure("The PLC request count must be greater than zero.");
        }

        return PlcResult.Success();
    }

    /// <summary>
    /// Validates a PLC address.
    /// </summary>
    /// <param name="address">The PLC address.</param>
    /// <returns>The validation result.</returns>
    public static PlcResult ValidateAddress(PlcAddress address)
    {
        if (address.DeviceType == PlcDeviceType.Unknown)
        {
            return PlcResult.Failure("The PLC device type is unknown.");
        }

        if (address.Offset < 0)
        {
            return PlcResult.Failure("The PLC address offset must be greater than or equal to zero.");
        }

        if (address.BitOffset.HasValue &&
            address.BitOffset is < 0 or > 15)
        {
            return PlcResult.Failure("The PLC bit offset must be between 0 and 15.");
        }

        return PlcResult.Success();
    }

    /// <summary>
    /// Validates PLC word values.
    /// </summary>
    /// <param name="values">The word values.</param>
    /// <returns>The validation result.</returns>
    public static PlcResult ValidateWordValues(IReadOnlyList<short> values)
    {
        if (values.Count == 0)
        {
            return PlcResult.Failure("The PLC word value collection must not be empty.");
        }

        return PlcResult.Success();
    }

    /// <summary>
    /// Validates PLC bit values.
    /// </summary>
    /// <param name="values">The bit values.</param>
    /// <returns>The validation result.</returns>
    public static PlcResult ValidateBitValues(IReadOnlyList<bool> values)
    {
        if (values.Count == 0)
        {
            return PlcResult.Failure("The PLC bit value collection must not be empty.");
        }

        return PlcResult.Success();
    }
}
