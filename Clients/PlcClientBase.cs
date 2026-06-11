using Dreamine.PLC.Abstractions.Clients;
using Dreamine.PLC.Abstractions.Connections;
using Dreamine.PLC.Abstractions.Devices;
using Dreamine.PLC.Abstractions.Results;
using Dreamine.PLC.Core.Internal;
using Dreamine.PLC.Core.Validation;

namespace Dreamine.PLC.Core.Clients;

/// <summary>
/// Provides a base implementation for PLC clients.
/// </summary>
public abstract class PlcClientBase : IPlcClient
{
    private readonly AsyncLock _syncLock = new();
    private bool _disposed;

    /// <inheritdoc />
    public PlcConnectionState State { get; private set; } = PlcConnectionState.Disconnected;

    /// <inheritdoc />
    public event EventHandler<PlcConnectionState>? StateChanged;

    /// <inheritdoc />
    public async Task<PlcResult> ConnectAsync(CancellationToken cancellationToken = default)
    {
        using (await _syncLock.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            ThrowIfDisposed();

            if (State == PlcConnectionState.Connected)
            {
                return PlcResult.Success();
            }

            SetState(PlcConnectionState.Connecting);

            try
            {
                var result = await ConnectCoreAsync(cancellationToken).ConfigureAwait(false);

                SetState(result.IsSuccess
                    ? PlcConnectionState.Connected
                    : PlcConnectionState.Faulted);

                return result;
            }
            catch (OperationCanceledException ex)
            {
                SetState(PlcConnectionState.Disconnected);
                return PlcResult.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                SetState(PlcConnectionState.Faulted);
                return PlcResult.Failure(ex.Message);
            }
        }
    }

    /// <inheritdoc />
    public async Task<PlcResult> DisconnectAsync(CancellationToken cancellationToken = default)
    {
        using (await _syncLock.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            if (_disposed)
            {
                return PlcResult.Success();
            }

            if (State == PlcConnectionState.Disconnected)
            {
                return PlcResult.Success();
            }

            var previousState = State;
            SetState(PlcConnectionState.Disconnecting);

            try
            {
                var result = await DisconnectCoreAsync(cancellationToken).ConfigureAwait(false);

                SetState(PlcConnectionState.Disconnected);
                return result;
            }
            catch (OperationCanceledException ex)
            {
                SetState(previousState);
                return PlcResult.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                SetState(PlcConnectionState.Faulted);
                return PlcResult.Failure(ex.Message);
            }
        }
    }

    /// <inheritdoc />
    public async Task<PlcResult<bool[]>> ReadBitsAsync(
        PlcAddress address,
        int count,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var validationResult = ValidateReadRequest(address, count);
        if (!validationResult.IsSuccess)
        {
            return PlcResult<bool[]>.Failure(validationResult.Message ?? "Invalid PLC bit read request.", validationResult.ErrorCode);
        }

        if (State != PlcConnectionState.Connected)
        {
            return PlcResult<bool[]>.Failure("The PLC client is not connected.");
        }

        try
        {
            return await ReadBitsCoreAsync(address, count, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            return PlcResult<bool[]>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            SetState(PlcConnectionState.Faulted);
            return PlcResult<bool[]>.Failure(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<PlcResult<short[]>> ReadWordsAsync(
        PlcAddress address,
        int count,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var validationResult = ValidateReadRequest(address, count);
        if (!validationResult.IsSuccess)
        {
            return PlcResult<short[]>.Failure(validationResult.Message ?? "Invalid PLC word read request.", validationResult.ErrorCode);
        }

        if (State != PlcConnectionState.Connected)
        {
            return PlcResult<short[]>.Failure("The PLC client is not connected.");
        }

        try
        {
            return await ReadWordsCoreAsync(address, count, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            return PlcResult<short[]>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            SetState(PlcConnectionState.Faulted);
            return PlcResult<short[]>.Failure(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<PlcResult> WriteBitsAsync(
        PlcAddress address,
        IReadOnlyList<bool> values,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var validationResult = ValidateWriteBitRequest(address, values);
        if (!validationResult.IsSuccess)
        {
            return validationResult;
        }

        if (State != PlcConnectionState.Connected)
        {
            return PlcResult.Failure("The PLC client is not connected.");
        }

        try
        {
            return await WriteBitsCoreAsync(address, values, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            return PlcResult.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            SetState(PlcConnectionState.Faulted);
            return PlcResult.Failure(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<PlcResult> WriteWordsAsync(
        PlcAddress address,
        IReadOnlyList<short> values,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var validationResult = ValidateWriteWordRequest(address, values);
        if (!validationResult.IsSuccess)
        {
            return validationResult;
        }

        if (State != PlcConnectionState.Connected)
        {
            return PlcResult.Failure("The PLC client is not connected.");
        }

        try
        {
            return await WriteWordsCoreAsync(address, values, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            return PlcResult.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            SetState(PlcConnectionState.Faulted);
            return PlcResult.Failure(ex.Message);
        }
    }

    /// <inheritdoc />
    public virtual async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await DisconnectAsync().ConfigureAwait(false);
        _disposed = true;

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Connects to the concrete PLC transport.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The PLC operation result.</returns>
    protected abstract Task<PlcResult> ConnectCoreAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Disconnects from the concrete PLC transport.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The PLC operation result.</returns>
    protected abstract Task<PlcResult> DisconnectCoreAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Reads bit values from the concrete PLC transport.
    /// </summary>
    /// <param name="address">The start PLC address.</param>
    /// <param name="count">The number of bits to read.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The read bit values.</returns>
    protected abstract Task<PlcResult<bool[]>> ReadBitsCoreAsync(
        PlcAddress address,
        int count,
        CancellationToken cancellationToken);

    /// <summary>
    /// Reads word values from the concrete PLC transport.
    /// </summary>
    /// <param name="address">The start PLC address.</param>
    /// <param name="count">The number of words to read.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The read word values.</returns>
    protected abstract Task<PlcResult<short[]>> ReadWordsCoreAsync(
        PlcAddress address,
        int count,
        CancellationToken cancellationToken);

    /// <summary>
    /// Writes bit values to the concrete PLC transport.
    /// </summary>
    /// <param name="address">The start PLC address.</param>
    /// <param name="values">The bit values to write.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The PLC operation result.</returns>
    protected abstract Task<PlcResult> WriteBitsCoreAsync(
        PlcAddress address,
        IReadOnlyList<bool> values,
        CancellationToken cancellationToken);

    /// <summary>
    /// Writes word values to the concrete PLC transport.
    /// </summary>
    /// <param name="address">The start PLC address.</param>
    /// <param name="values">The word values to write.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The PLC operation result.</returns>
    protected abstract Task<PlcResult> WriteWordsCoreAsync(
        PlcAddress address,
        IReadOnlyList<short> values,
        CancellationToken cancellationToken);

    /// <summary>
    /// Changes the connection state and raises the state changed event.
    /// </summary>
    /// <param name="state">The new connection state.</param>
    protected void SetState(PlcConnectionState state)
    {
        if (State == state)
        {
            return;
        }

        State = state;
        StateChanged?.Invoke(this, state);
    }

    private static PlcResult ValidateReadRequest(PlcAddress address, int count)
    {
        var addressResult = PlcValidation.ValidateAddress(address);
        if (!addressResult.IsSuccess)
        {
            return addressResult;
        }

        return PlcValidation.ValidateCount(count);
    }

    private static PlcResult ValidateWriteBitRequest(PlcAddress address, IReadOnlyList<bool> values)
    {
        var addressResult = PlcValidation.ValidateAddress(address);
        if (!addressResult.IsSuccess)
        {
            return addressResult;
        }

        return PlcValidation.ValidateBitValues(values);
    }

    private static PlcResult ValidateWriteWordRequest(PlcAddress address, IReadOnlyList<short> values)
    {
        var addressResult = PlcValidation.ValidateAddress(address);
        if (!addressResult.IsSuccess)
        {
            return addressResult;
        }

        return PlcValidation.ValidateWordValues(values);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, GetType().Name);
    }
}
