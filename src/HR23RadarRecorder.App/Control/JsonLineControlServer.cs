using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;

namespace HR23RadarRecorder.App.Control;

public sealed class JsonLineControlServer : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };
    private readonly IPAddress address;
    private readonly int port;
    private readonly ControlCommandHandler handler;
    private TcpListener? listener;
    private CancellationTokenSource? cancellation;
    private Task? acceptTask;
    private readonly ConcurrentDictionary<int, TcpClient> clients = new();
    private readonly ConcurrentDictionary<int, Task> clientTasks = new();
    private int nextClientId;

    public JsonLineControlServer(IPAddress address, int port, ControlCommandHandler handler)
    {
        this.address = address;
        this.port = port;
        this.handler = handler;
    }

    public bool IsRunning => listener is not null;

    public Task StartAsync()
    {
        if (listener is not null) return Task.CompletedTask;
        listener = new TcpListener(address, port);
        listener.Start();
        cancellation = new CancellationTokenSource();
        acceptTask = AcceptLoopAsync(listener, cancellation.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (listener is null) return;
        cancellation?.Cancel();
        listener.Stop();
        foreach (TcpClient client in clients.Values)
        {
            client.Dispose();
        }
        if (acceptTask is not null)
        {
            try { await acceptTask.ConfigureAwait(false); }
            catch (OperationCanceledException) { }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }
        }
        if (!clientTasks.IsEmpty)
        {
            await Task.WhenAll(clientTasks.Values).ConfigureAwait(false);
        }
        cancellation?.Dispose();
        cancellation = null;
        acceptTask = null;
        listener = null;
    }

    private async Task AcceptLoopAsync(TcpListener tcpListener, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            TcpClient client = await tcpListener.AcceptTcpClientAsync(token).ConfigureAwait(false);
            int clientId = Interlocked.Increment(ref nextClientId);
            clients[clientId] = client;
            Task task = HandleClientSafelyAsync(clientId, client, token);
            clientTasks[clientId] = task;
        }
    }

    private async Task HandleClientSafelyAsync(int clientId, TcpClient client, CancellationToken token)
    {
        try
        {
            await HandleClientAsync(client, token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (IOException) when (token.IsCancellationRequested)
        {
        }
        catch (ObjectDisposedException) when (token.IsCancellationRequested)
        {
        }
        finally
        {
            clients.TryRemove(clientId, out _);
            clientTasks.TryRemove(clientId, out _);
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken token)
    {
        using (client)
        using (NetworkStream stream = client.GetStream())
        using (StreamReader reader = new(stream, Encoding.UTF8, leaveOpen: true))
        using (StreamWriter writer = new(stream, new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true })
        {
            while (!token.IsCancellationRequested)
            {
                string? line = await reader.ReadLineAsync(token).ConfigureAwait(false);
                if (line is null) break;
                ControlResponse response = await handler.HandleAsync(line).ConfigureAwait(false);
                await writer.WriteLineAsync(JsonSerializer.Serialize(response, JsonOptions)).ConfigureAwait(false);
            }
        }
    }

    public async ValueTask DisposeAsync() => await StopAsync().ConfigureAwait(false);
}
