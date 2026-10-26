using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace AudioSwitcher.Services;

public sealed record ControlRequest(string? Command, Guid? ProfileId);

public sealed record ControlProfile(Guid Id, string Name, bool Available);

public sealed record ControlResponse(
    bool Ok,
    string? Error,
    IReadOnlyList<ControlProfile> Profiles,
    Guid? ActiveProfileId,
    bool? MicrophoneMuted = null);

/// <summary>
/// Lets other local programs (e.g. stream-deck plugins) list and activate profiles.
/// Each line on the pipe is one JSON request; each request gets one JSON line back.
/// </summary>
public sealed class ProfileControlServer : IDisposable
{
    public const string DefaultPipeName = "AudioSwitcher.Control";

    // Requests are tiny; the limit keeps a misbehaving client from growing a line without bound.
    private const int MaxRequestLength = 4096;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly Func<ControlRequest, Task<ControlResponse>> _handler;
    private readonly string _pipeName;
    private readonly CancellationTokenSource _cancellation = new();

    public ProfileControlServer(Func<ControlRequest, Task<ControlResponse>> handler, string pipeName = DefaultPipeName)
    {
        _handler = handler;
        _pipeName = pipeName;
    }

    public void Start() => _ = AcceptLoopAsync(_cancellation.Token);

    public void Dispose()
    {
        _cancellation.Cancel();
        _cancellation.Dispose();
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            NamedPipeServerStream pipe;
            try
            {
                // CurrentUserOnly: other Windows users must not switch this user's devices.
                pipe = new NamedPipeServerStream(_pipeName, PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            }
            catch (IOException)
            {
                return;
            }

            try
            {
                await pipe.WaitForConnectionAsync(cancellationToken);
            }
            catch (Exception exception) when (exception is OperationCanceledException or IOException)
            {
                await pipe.DisposeAsync();
                continue;
            }

            _ = ServeAsync(pipe, cancellationToken);
        }
    }

    private async Task ServeAsync(NamedPipeServerStream pipe, CancellationToken cancellationToken)
    {
        await using (pipe)
        {
            using var reader = new StreamReader(pipe, new UTF8Encoding(false), false, MaxRequestLength, leaveOpen: true);
            await using var writer = new StreamWriter(pipe, new UTF8Encoding(false), MaxRequestLength, leaveOpen: true)
            {
                NewLine = "\n",
                AutoFlush = true,
            };

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var line = await ReadLineAsync(reader, cancellationToken);
                    if (line is null) return;
                    var response = await HandleAsync(line);
                    await writer.WriteLineAsync(JsonSerializer.Serialize(response, JsonOptions));
                }
            }
            catch (Exception exception) when (exception is IOException or OperationCanceledException or ObjectDisposedException)
            {
                // The client disconnected or the application is shutting down.
            }
        }
    }

    private async Task<ControlResponse> HandleAsync(string line)
    {
        ControlRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<ControlRequest>(line, JsonOptions);
        }
        catch (JsonException)
        {
            request = null;
        }

        if (request?.Command is null) return new ControlResponse(false, "Invalid request.", [], null);
        return await _handler(request);
    }

    /// <summary>Reads one line, or null at end of stream; too long lines end the connection.</summary>
    private static async Task<string?> ReadLineAsync(StreamReader reader, CancellationToken cancellationToken)
    {
        var line = new StringBuilder();
        var buffer = new char[1];
        while (await reader.ReadAsync(buffer, cancellationToken) == 1)
        {
            if (buffer[0] == '\n') return line.ToString().TrimEnd('\r');
            if (line.Length == MaxRequestLength) throw new IOException("Request too long.");
            line.Append(buffer[0]);
        }

        return null;
    }
}
