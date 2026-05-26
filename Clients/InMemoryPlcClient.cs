using Dreamine.PLC.Abstractions.Devices;
using Dreamine.PLC.Abstractions.Results;
using Dreamine.PLC.Core.Memory;

namespace Dreamine.PLC.Core.Clients;

/// <summary>
/// Provides an in-memory PLC client implementation for tests, demos, and simulator foundations.
/// </summary>
public sealed class InMemoryPlcClient : PlcClientBase
{
    private readonly InMemoryPlcMemory _memory;
    private bool _isConnected;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryPlcClient"/> class.
    /// </summary>
    public InMemoryPlcClient()
        : this(new InMemoryPlcMemory())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryPlcClient"/> class.
    /// </summary>
    /// <param name="memory">The in-memory PLC memory store.</param>
    public InMemoryPlcClient(InMemoryPlcMemory memory)
    {
        _memory = memory ?? throw new ArgumentNullException(nameof(memory));
    }

    /// <summary>
    /// Gets the in-memory PLC memory store.
    /// </summary>
    public InMemoryPlcMemory Memory => _memory;

    /// <inheritdoc />
    protected override Task<PlcResult> ConnectCoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _isConnected = true;

        return Task.FromResult(PlcResult.Success());
    }

    /// <inheritdoc />
    protected override Task<PlcResult> DisconnectCoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _isConnected = false;

        return Task.FromResult(PlcResult.Success());
    }

    /// <inheritdoc />
    protected override Task<PlcResult<bool[]>> ReadBitsCoreAsync(
        PlcAddress address,
        int count,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_isConnected)
        {
            return Task.FromResult(PlcResult<bool[]>.Failure("The in-memory PLC client is not connected."));
        }

        return Task.FromResult(_memory.ReadBits(address, count));
    }

    /// <inheritdoc />
    protected override Task<PlcResult<short[]>> ReadWordsCoreAsync(
        PlcAddress address,
        int count,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_isConnected)
        {
            return Task.FromResult(PlcResult<short[]>.Failure("The in-memory PLC client is not connected."));
        }

        return Task.FromResult(_memory.ReadWords(address, count));
    }

    /// <inheritdoc />
    protected override Task<PlcResult> WriteBitsCoreAsync(
        PlcAddress address,
        IReadOnlyList<bool> values,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_isConnected)
        {
            return Task.FromResult(PlcResult.Failure("The in-memory PLC client is not connected."));
        }

        return Task.FromResult(_memory.WriteBits(address, values));
    }

    /// <inheritdoc />
    protected override Task<PlcResult> WriteWordsCoreAsync(
        PlcAddress address,
        IReadOnlyList<short> values,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_isConnected)
        {
            return Task.FromResult(PlcResult.Failure("The in-memory PLC client is not connected."));
        }

        return Task.FromResult(_memory.WriteWords(address, values));
    }
}