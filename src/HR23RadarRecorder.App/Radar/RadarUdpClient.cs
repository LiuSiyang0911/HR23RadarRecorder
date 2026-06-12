using System.Net;
using System.Net.Sockets;

namespace HR23RadarRecorder.App.Radar;

public sealed class RadarUdpClient : IAsyncDisposable
{
    private readonly RadarNetworkConfig config;
    private UdpClient? client;
    private CancellationTokenSource? cancellation;
    private Task? receiveTask;

    public RadarUdpClient(RadarNetworkConfig config)
    {
        this.config = config;
    }

    public Task StartAsync(Func<UdpReceiveResult, Task> packetHandler)
    {
        if (client is not null)
        {
            throw new InvalidOperationException("UDP receiver is already running.");
        }

        IPAddress localAddress = IPAddress.Parse(config.LocalIp);
        client = new UdpClient(new IPEndPoint(localAddress, config.LocalPort));
        cancellation = new CancellationTokenSource();
        receiveTask = ReceiveLoopAsync(client, packetHandler, cancellation.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (client is null)
        {
            return;
        }

        cancellation?.Cancel();
        client.Dispose();

        if (receiveTask is not null)
        {
            try
            {
                await receiveTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }

        cancellation?.Dispose();
        cancellation = null;
        receiveTask = null;
        client = null;
    }

    private static async Task ReceiveLoopAsync(UdpClient udpClient, Func<UdpReceiveResult, Task> packetHandler, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            UdpReceiveResult result = await udpClient.ReceiveAsync(cancellationToken).ConfigureAwait(false);
            await packetHandler(result).ConfigureAwait(false);
        }
    }

    public async ValueTask DisposeAsync() => await StopAsync().ConfigureAwait(false);
}
