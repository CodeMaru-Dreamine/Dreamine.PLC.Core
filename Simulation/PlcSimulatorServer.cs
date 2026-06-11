using System.Net;
using System.Net.Sockets;
using Dreamine.PLC.Core.Devices;
using Dreamine.PLC.Core.Memory;

namespace Dreamine.PLC.Core.Simulation;

/// <summary>
/// Provides a lightweight TCP PLC simulator server for samples and cross-PC tests.
/// </summary>
public sealed class PlcSimulatorServer : IAsyncDisposable
{
    private readonly PlcSimulatorServerOptions _options;
    private readonly InMemoryPlcMemory _memory;
    private readonly DefaultPlcAddressParser _addressParser = new();
    private readonly List<Task> _clientTasks = [];
    private readonly object _syncRoot = new();
    private CancellationTokenSource? _cts;
    private TcpListener? _listener;
    private Task? _acceptTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlcSimulatorServer"/> class.
    /// </summary>
    /// <param name="options">The server options.</param>
    public PlcSimulatorServer(PlcSimulatorServerOptions options)
        : this(options, new InMemoryPlcMemory())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlcSimulatorServer"/> class.
    /// </summary>
    /// <param name="options">The server options.</param>
    /// <param name="memory">The shared in-memory PLC memory.</param>
    public PlcSimulatorServer(PlcSimulatorServerOptions options, InMemoryPlcMemory memory)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _memory = memory ?? throw new ArgumentNullException(nameof(memory));
    }

    /// <summary>
    /// Occurs when the server status changes.
    /// </summary>
    public event EventHandler<string>? StatusChanged;

    /// <summary>
    /// Gets whether the simulator server is running.
    /// </summary>
    public bool IsRunning => _listener is not null;

    /// <summary>
    /// Starts the TCP simulator server.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_listener is not null)
        {
            return Task.CompletedTask;
        }

        var address = ParseAddress(_options.Host);
        _listener = new TcpListener(address, _options.Port);
        _listener.Start();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _acceptTask = Task.Run(() => AcceptLoopAsync(_cts.Token), CancellationToken.None);
        StatusChanged?.Invoke(this, $"PLC simulator server started. {_options.Host}:{_options.Port}");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops the TCP simulator server.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task StopAsync()
    {
        if (_listener is null)
        {
            return;
        }

        _cts?.Cancel();
        _listener.Stop();
        _listener = null;

        if (_acceptTask is not null)
        {
            try
            {
                await _acceptTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }

        Task[] clientTasks;
        lock (_syncRoot)
        {
            clientTasks = _clientTasks.ToArray();
            _clientTasks.Clear();
        }

        try
        {
            await Task.WhenAll(clientTasks).ConfigureAwait(false);
        }
        catch
        {
        }

        _cts?.Dispose();
        _cts = null;
        StatusChanged?.Invoke(this, "PLC simulator server stopped.");
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _listener is not null)
        {
            TcpClient client;

            try
            {
                client = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }

            var task = Task.Run(() => HandleClientAsync(client, cancellationToken), CancellationToken.None);
            lock (_syncRoot)
            {
                _clientTasks.RemoveAll(static item => item.IsCompleted);
                _clientTasks.Add(task);
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using (client)
        using (var stream = client.GetStream())
        using (var reader = new StreamReader(stream))
        using (var writer = new StreamWriter(stream) { AutoFlush = true })
        {
            StatusChanged?.Invoke(this, "PLC simulator client connected.");

            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (line is null)
                {
                    break;
                }

                var response = ExecuteLine(line);
                await writer.WriteLineAsync(response.AsMemory(), cancellationToken).ConfigureAwait(false);
            }
        }

        StatusChanged?.Invoke(this, "PLC simulator client disconnected.");
    }

    private string ExecuteLine(string line)
    {
        try
        {
            var parts = line.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length < 3)
            {
                return PlcSimulatorProtocol.Error("Invalid command format.");
            }

            var command = parts[0].ToUpperInvariant();
            var addressResult = _addressParser.Parse(parts[1]);
            if (!addressResult.IsSuccess)
            {
                return PlcSimulatorProtocol.Error(addressResult.Message ?? "Invalid PLC address.");
            }

            var address = addressResult.Value;
            var argument = parts[2];

            return command switch
            {
                PlcSimulatorProtocol.ReadBits => ExecuteReadBits(address, argument),
                PlcSimulatorProtocol.ReadWords => ExecuteReadWords(address, argument),
                PlcSimulatorProtocol.WriteBits => ExecuteWriteBits(address, argument),
                PlcSimulatorProtocol.WriteWords => ExecuteWriteWords(address, argument),
                _ => PlcSimulatorProtocol.Error($"Unsupported command: {command}")
            };
        }
        catch (Exception ex)
        {
            return PlcSimulatorProtocol.Error(ex.Message);
        }
    }

    private string ExecuteReadBits(Abstractions.Devices.PlcAddress address, string argument)
    {
        if (!TryParseCount(argument, out var count, out var error))
        {
            return PlcSimulatorProtocol.Error(error);
        }

        var result = _memory.ReadBits(address, count);
        return result.IsSuccess && result.Value is not null
            ? PlcSimulatorProtocol.Ok(PlcSimulatorProtocol.FormatBits(result.Value))
            : PlcSimulatorProtocol.Error(result.Message ?? "Read bits failed.");
    }

    private string ExecuteReadWords(Abstractions.Devices.PlcAddress address, string argument)
    {
        if (!TryParseCount(argument, out var count, out var error))
        {
            return PlcSimulatorProtocol.Error(error);
        }

        var result = _memory.ReadWords(address, count);
        return result.IsSuccess && result.Value is not null
            ? PlcSimulatorProtocol.Ok(PlcSimulatorProtocol.FormatWords(result.Value))
            : PlcSimulatorProtocol.Error(result.Message ?? "Read words failed.");
    }

    private static bool TryParseCount(string text, out int count, out string error)
    {
        if (!int.TryParse(text, out count))
        {
            error = $"Invalid read count: {text}";
            return false;
        }

        if (count <= 0)
        {
            error = "Read count must be greater than zero.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private string ExecuteWriteBits(Abstractions.Devices.PlcAddress address, string argument)
    {
        var values = PlcSimulatorProtocol.ParseBits(argument);
        var result = _memory.WriteBits(address, values);
        return result.IsSuccess ? PlcSimulatorProtocol.Ok() : PlcSimulatorProtocol.Error(result.Message ?? "Write bits failed.");
    }

    private string ExecuteWriteWords(Abstractions.Devices.PlcAddress address, string argument)
    {
        var values = PlcSimulatorProtocol.ParseWords(argument);
        var result = _memory.WriteWords(address, values);
        if (!result.IsSuccess)
        {
            return PlcSimulatorProtocol.Error(result.Message ?? "Write words failed.");
        }

        ApplyAutoWordResponse(address, values);
        return PlcSimulatorProtocol.Ok();
    }

    private void ApplyAutoWordResponse(Abstractions.Devices.PlcAddress writtenAddress, IReadOnlyList<short> values)
    {
        if (!_options.EnableAutoWordResponse || values.Count != 1)
        {
            return;
        }

        var triggerResult = _addressParser.Parse(_options.AutoResponseTriggerAddress);
        var responseResult = _addressParser.Parse(_options.AutoResponseAddress);
        if (!triggerResult.IsSuccess || !responseResult.IsSuccess)
        {
            return;
        }

        var triggerAddress = triggerResult.Value;
        if (writtenAddress.DeviceType != triggerAddress.DeviceType || writtenAddress.Offset != triggerAddress.Offset)
        {
            return;
        }

        var rawResponseValue = values[0] + _options.AutoResponseIncrement;
        if (rawResponseValue is < short.MinValue or > short.MaxValue)
        {
            StatusChanged?.Invoke(this, $"Auto response skipped: value overflow. {_options.AutoResponseAddress}={rawResponseValue}");
            return;
        }

        var responseValue = (short)rawResponseValue;
        _memory.WriteWords(responseResult.Value, [responseValue]);
        StatusChanged?.Invoke(this, $"Auto response: {_options.AutoResponseAddress}={responseValue}");
    }

    private static IPAddress ParseAddress(string host)
    {
        if (string.IsNullOrWhiteSpace(host) || host == "*" || host == "+")
        {
            return IPAddress.Any;
        }

        return IPAddress.TryParse(host, out var address) ? address : IPAddress.Any;
    }
}
