using System.Net;

namespace HR23RadarRecorder.App.Recording;

public sealed class PacketCsvWriter : IAsyncDisposable
{
    private readonly StreamWriter writer;

    public PacketCsvWriter(string path)
    {
        writer = new StreamWriter(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read), new System.Text.UTF8Encoding(false));
        writer.WriteLine("Index,Utc,MonoNs,SessionElapsedNs,SenderIp,SenderPort,Length,FileOffset");
    }

    public void Write(long index, Core.TimeStamp timestamp, long elapsedNs, IPEndPoint sender, int length, long fileOffset)
    {
        writer.WriteLine($"{index},{timestamp.UtcText},{timestamp.MonoNs},{elapsedNs},{sender.Address},{sender.Port},{length},{fileOffset}");
    }

    public async ValueTask DisposeAsync()
    {
        await writer.FlushAsync().ConfigureAwait(false);
        await writer.DisposeAsync().ConfigureAwait(false);
    }
}
