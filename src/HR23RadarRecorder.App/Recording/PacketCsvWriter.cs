using System.Net;

namespace HR23RadarRecorder.App.Recording;

public sealed class PacketCsvWriter : IAsyncDisposable
{
    private const int FlushIntervalRows = 100;
    private readonly StreamWriter writer;
    private int rowsSinceFlush;

    public PacketCsvWriter(string path)
    {
        writer = new StreamWriter(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read), new System.Text.UTF8Encoding(false));
        writer.WriteLine("Index,Utc,MonoNs,SessionElapsedNs,SenderIp,SenderPort,Length,FileOffset");
    }

    public void Write(long index, Core.TimeStamp timestamp, long elapsedNs, IPEndPoint sender, int length, long fileOffset)
    {
        writer.WriteLine($"{index},{timestamp.UtcText},{timestamp.MonoNs},{elapsedNs},{sender.Address},{sender.Port},{length},{fileOffset}");
        rowsSinceFlush++;
        if (rowsSinceFlush >= FlushIntervalRows)
        {
            writer.Flush();
            rowsSinceFlush = 0;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await writer.FlushAsync().ConfigureAwait(false);
        await writer.DisposeAsync().ConfigureAwait(false);
    }
}
