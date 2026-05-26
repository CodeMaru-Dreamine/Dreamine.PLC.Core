using Dreamine.PLC.Abstractions.Clients;
using Dreamine.PLC.Abstractions.Requests;
using Dreamine.PLC.Abstractions.Results;

namespace Dreamine.PLC.Core.Extensions;

/// <summary>
/// Provides extension methods for PLC clients.
/// </summary>
public static class PlcClientExtensions
{
    /// <summary>
    /// Reads bit values by using a PLC read request.
    /// </summary>
    /// <param name="client">The PLC client.</param>
    /// <param name="request">The PLC read request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The read bit values.</returns>
    public static Task<PlcResult<bool[]>> ReadBitsAsync(
        this IPlcClient client,
        PlcReadRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);

        return client.ReadBitsAsync(
            request.Address,
            request.Count,
            cancellationToken);
    }

    /// <summary>
    /// Reads word values by using a PLC read request.
    /// </summary>
    /// <param name="client">The PLC client.</param>
    /// <param name="request">The PLC read request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The read word values.</returns>
    public static Task<PlcResult<short[]>> ReadWordsAsync(
        this IPlcClient client,
        PlcReadRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);

        return client.ReadWordsAsync(
            request.Address,
            request.Count,
            cancellationToken);
    }

    /// <summary>
    /// Writes bit values by using a PLC bit write request.
    /// </summary>
    /// <param name="client">The PLC client.</param>
    /// <param name="request">The PLC bit write request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The PLC operation result.</returns>
    public static Task<PlcResult> WriteBitsAsync(
        this IPlcClient client,
        PlcWriteBitRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(request);

        return client.WriteBitsAsync(
            request.Address,
            request.Values,
            cancellationToken);
    }

    /// <summary>
    /// Writes word values by using a PLC word write request.
    /// </summary>
    /// <param name="client">The PLC client.</param>
    /// <param name="request">The PLC word write request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The PLC operation result.</returns>
    public static Task<PlcResult> WriteWordsAsync(
        this IPlcClient client,
        PlcWriteWordRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(request);

        return client.WriteWordsAsync(
            request.Address,
            request.Values,
            cancellationToken);
    }
}