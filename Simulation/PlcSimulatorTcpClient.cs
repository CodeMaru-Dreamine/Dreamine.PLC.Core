using System.Net.Sockets;
using Dreamine.PLC.Abstractions.Devices;
using Dreamine.PLC.Abstractions.Results;
using Dreamine.PLC.Core.Clients;

namespace Dreamine.PLC.Core.Simulation;

/// <summary>
/// Provides an <see cref="Abstractions.Clients.IPlcClient"/> implementation for the Dreamine TCP PLC simulator protocol.
/// </summary>
public sealed class PlcSimulatorTcpClient : PlcClientBase
{
    private readonly PlcSimulatorClientOptions _options;
    private TcpClient? _client;
    private StreamReader? _reader;
    private StreamWriter? _writer;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlcSimulatorTcpClient"/> class.
    /// </summary>
    /// <param name="options">The simulator client options.</param>
    public PlcSimulatorTcpClient(PlcSimulatorClientOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    protected override async Task<PlcResult> ConnectCoreAsync(CancellationToken cancellationToken)
    {
        var client = new TcpClient();
        using var timeoutCts = new CancellationTokenSource(_options.ConnectTimeoutMs);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            await client.ConnectAsync(_options.Host, _options.Port, linkedCts.Token).ConfigureAwait(false);

            var stream = client.GetStream();
            _reader = new StreamReader(stream);
            _writer = new StreamWriter(stream) { AutoFlush = true };
            _client = client;

            return PlcResult.Success();
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    /// <inheritdoc />
    protected override Task<PlcResult> DisconnectCoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _writer?.Dispose();
        _reader?.Dispose();
        _client?.Dispose();
        _writer = null;
        _reader = null;
        _client = null;

        return Task.FromResult(PlcResult.Success());
    }

    /// <inheritdoc />
    protected override async Task<PlcResult<bool[]>> ReadBitsCoreAsync(PlcAddress address, int count, CancellationToken cancellationToken)
    {
        var payload = await SendCommandAsync($"{PlcSimulatorProtocol.ReadBits} {address} {count}", cancellationToken).ConfigureAwait(false);
        return PlcResult<bool[]>.Success(PlcSimulatorProtocol.ParseBits(payload));
    }

    /// <inheritdoc />
    protected override async Task<PlcResult<short[]>> ReadWordsCoreAsync(PlcAddress address, int count, CancellationToken cancellationToken)
    {
        var payload = await SendCommandAsync($"{PlcSimulatorProtocol.ReadWords} {address} {count}", cancellationToken).ConfigureAwait(false);
        return PlcResult<short[]>.Success(PlcSimulatorProtocol.ParseWords(payload));
    }

    /// <inheritdoc />
    protected override async Task<PlcResult> WriteBitsCoreAsync(PlcAddress address, IReadOnlyList<bool> values, CancellationToken cancellationToken)
    {
        var valuesText = PlcSimulatorProtocol.FormatBits(values);
        await SendCommandAsync($"{PlcSimulatorProtocol.WriteBits} {address} {valuesText}", cancellationToken).ConfigureAwait(false);
        return PlcResult.Success();
    }

    /// <inheritdoc />
    protected override async Task<PlcResult> WriteWordsCoreAsync(PlcAddress address, IReadOnlyList<short> values, CancellationToken cancellationToken)
    {
        var valuesText = PlcSimulatorProtocol.FormatWords(values);
        await SendCommandAsync($"{PlcSimulatorProtocol.WriteWords} {address} {valuesText}", cancellationToken).ConfigureAwait(false);
        return PlcResult.Success();
    }

    private async Task<string> SendCommandAsync(string command, CancellationToken cancellationToken)
    {
        if (_writer is null || _reader is null)
        {
            throw new InvalidOperationException("The PLC simulator TCP client is not connected.");
        }

        await _writer.WriteLineAsync(command.AsMemory(), cancellationToken).ConfigureAwait(false);
        var line = await _reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
        if (line is null)
        {
            throw new IOException("The PLC simulator server closed the connection.");
        }

        return PlcSimulatorProtocol.ReadOkPayload(line);
    }
}
