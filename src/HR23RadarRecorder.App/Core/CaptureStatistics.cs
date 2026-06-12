namespace HR23RadarRecorder.App.Core;

public sealed class CaptureStatistics
{
    private readonly object sync = new();
    private long packetCount;
    private long totalBytes;
    private DateTimeOffset? firstPacketUtc;
    private DateTimeOffset? lastPacketUtc;
    private long? firstPacketMonoNs;
    private long? lastPacketMonoNs;

    public long PacketCount { get { lock (sync) return packetCount; } }
    public long TotalBytes { get { lock (sync) return totalBytes; } }
    public DateTimeOffset? FirstPacketUtc { get { lock (sync) return firstPacketUtc; } }
    public DateTimeOffset? LastPacketUtc { get { lock (sync) return lastPacketUtc; } }
    public long? FirstPacketMonoNs { get { lock (sync) return firstPacketMonoNs; } }
    public long? LastPacketMonoNs { get { lock (sync) return lastPacketMonoNs; } }

    public void RecordPacket(int length, TimeStamp timestamp)
    {
        lock (sync)
        {
            if (packetCount == 0)
            {
                firstPacketUtc = timestamp.Utc;
                firstPacketMonoNs = timestamp.MonoNs;
            }

            packetCount++;
            totalBytes += length;
            lastPacketUtc = timestamp.Utc;
            lastPacketMonoNs = timestamp.MonoNs;
        }
    }
}

public sealed record RecorderSnapshot(
    RecorderState State,
    string SessionId,
    string CaptureDir,
    long PacketCount,
    long TotalBytes,
    string FirstPacketUtc,
    string LastPacketUtc,
    double ThroughputBytesPerSecond);
