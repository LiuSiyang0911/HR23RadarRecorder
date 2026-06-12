using System.Net;

namespace HR23RadarRecorder.App.Recording;

public sealed class CaptureFileWriter : IAsyncDisposable
{
    private readonly FileStream rawStream;
    private readonly PacketCsvWriter packetWriter;

    public CaptureFileWriter(string captureDir)
    {
        rawStream = new FileStream(Path.Combine(captureDir, "raw.dat"), FileMode.Create, FileAccess.Write, FileShare.Read, 64 * 1024, FileOptions.Asynchronous);
        packetWriter = new PacketCsvWriter(Path.Combine(captureDir, "packets.csv"));
    }

    public async Task<long> WritePacketAsync(long index, Core.TimeStamp timestamp, long elapsedNs, IPEndPoint sender, byte[] payload)
    {
        long offset = rawStream.Position;
        await rawStream.WriteAsync(payload).ConfigureAwait(false);
        packetWriter.Write(index, timestamp, elapsedNs, sender, payload.Length, offset);
        return offset;
    }

    public async ValueTask DisposeAsync()
    {
        await rawStream.FlushAsync().ConfigureAwait(false);
        await rawStream.DisposeAsync().ConfigureAwait(false);
        await packetWriter.DisposeAsync().ConfigureAwait(false);
    }
}
