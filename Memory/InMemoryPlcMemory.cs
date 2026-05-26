using Dreamine.PLC.Abstractions.Devices;
using Dreamine.PLC.Abstractions.Results;

namespace Dreamine.PLC.Core.Memory;

/// <summary>
/// Provides an in-memory PLC device memory store.
/// </summary>
public sealed class InMemoryPlcMemory
{
    private readonly object _syncRoot = new();
    private readonly Dictionary<PlcDeviceType, Dictionary<int, short>> _words = new();
    private readonly Dictionary<PlcDeviceType, Dictionary<int, bool>> _bits = new();

    /// <summary>
    /// Reads bit values from the memory store.
    /// </summary>
    /// <param name="address">The start PLC address.</param>
    /// <param name="count">The number of bits to read.</param>
    /// <returns>The read bit values.</returns>
    public PlcResult<bool[]> ReadBits(PlcAddress address, int count)
    {
        if (count <= 0)
        {
            return PlcResult<bool[]>.Failure("The read count must be greater than zero.");
        }

        lock (_syncRoot)
        {
            var values = new bool[count];
            var area = GetOrCreateBitArea(address.DeviceType);

            for (var i = 0; i < count; i++)
            {
                var offset = address.Offset + i;
                values[i] = area.TryGetValue(offset, out var value) && value;
            }

            return PlcResult<bool[]>.Success(values);
        }
    }

    /// <summary>
    /// Reads word values from the memory store.
    /// </summary>
    /// <param name="address">The start PLC address.</param>
    /// <param name="count">The number of words to read.</param>
    /// <returns>The read word values.</returns>
    public PlcResult<short[]> ReadWords(PlcAddress address, int count)
    {
        if (count <= 0)
        {
            return PlcResult<short[]>.Failure("The read count must be greater than zero.");
        }

        lock (_syncRoot)
        {
            var values = new short[count];
            var area = GetOrCreateWordArea(address.DeviceType);

            for (var i = 0; i < count; i++)
            {
                var offset = address.Offset + i;
                values[i] = area.TryGetValue(offset, out var value) ? value : default;
            }

            return PlcResult<short[]>.Success(values);
        }
    }

    /// <summary>
    /// Writes bit values to the memory store.
    /// </summary>
    /// <param name="address">The start PLC address.</param>
    /// <param name="values">The bit values to write.</param>
    /// <returns>The PLC operation result.</returns>
    public PlcResult WriteBits(PlcAddress address, IReadOnlyList<bool> values)
    {
        if (values.Count == 0)
        {
            return PlcResult.Failure("The bit value collection must not be empty.");
        }

        lock (_syncRoot)
        {
            var area = GetOrCreateBitArea(address.DeviceType);

            for (var i = 0; i < values.Count; i++)
            {
                var offset = address.Offset + i;
                area[offset] = values[i];
            }

            return PlcResult.Success();
        }
    }

    /// <summary>
    /// Writes word values to the memory store.
    /// </summary>
    /// <param name="address">The start PLC address.</param>
    /// <param name="values">The word values to write.</param>
    /// <returns>The PLC operation result.</returns>
    public PlcResult WriteWords(PlcAddress address, IReadOnlyList<short> values)
    {
        if (values.Count == 0)
        {
            return PlcResult.Failure("The word value collection must not be empty.");
        }

        lock (_syncRoot)
        {
            var area = GetOrCreateWordArea(address.DeviceType);

            for (var i = 0; i < values.Count; i++)
            {
                var offset = address.Offset + i;
                area[offset] = values[i];
            }

            return PlcResult.Success();
        }
    }

    /// <summary>
    /// Clears all memory areas.
    /// </summary>
    public void Clear()
    {
        lock (_syncRoot)
        {
            _bits.Clear();
            _words.Clear();
        }
    }

    private Dictionary<int, short> GetOrCreateWordArea(PlcDeviceType deviceType)
    {
        if (!_words.TryGetValue(deviceType, out var area))
        {
            area = new Dictionary<int, short>();
            _words[deviceType] = area;
        }

        return area;
    }

    private Dictionary<int, bool> GetOrCreateBitArea(PlcDeviceType deviceType)
    {
        if (!_bits.TryGetValue(deviceType, out var area))
        {
            area = new Dictionary<int, bool>();
            _bits[deviceType] = area;
        }

        return area;
    }
}