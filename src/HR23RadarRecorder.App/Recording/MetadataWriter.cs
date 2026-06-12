using System.Text.Json;
using HR23RadarRecorder.App.Core;
using HR23RadarRecorder.App.Radar;

namespace HR23RadarRecorder.App.Recording;

public static class MetadataWriter
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static async Task WriteAsync(string path, CaptureSession session, RadarNetworkConfig network, ControlSettings control)
    {
        object document = new
        {
            software = new { name = "HR2.3 Radar Recorder", version = "1.0.0" },
            session = new { sessionId = session.SessionId, captureDir = session.CaptureDir },
            master = new
            {
                name = session.MasterName,
                prepareCmdSendEpochS = session.PrepareCmdSendEpochS,
                prepareCmdSendPerfS = session.PrepareCmdSendPerfS,
                metadata = session.MasterMetadata
            },
            control = new { protocol = "tcp_json_lines", host = control.Host, port = control.Port },
            network = new { localIp = network.LocalIp, localPort = network.LocalPort, remoteIp = network.RemoteIp, remotePort = network.RemotePort },
            timeBase = new
            {
                timestampPolicy = "host_receive_time",
                monoNsPolicy = "recorder_internal_monotonic_clock",
                requestedRecordingStartEpochS = session.RecordingStartEpochS,
                utcAtPrepare = session.PreparedAt.UtcText,
                monoNsAtPrepare = session.PreparedAt.MonoNs,
                utcAtRecordingStart = session.RecordingStartedAt?.UtcText,
                monoNsAtRecordingStart = session.RecordingStartedAt?.MonoNs
            },
            files = new { raw = "raw.dat", packets = "packets.csv", events = "events.csv" },
            summary = new
            {
                packetCount = session.Statistics.PacketCount,
                totalBytes = session.Statistics.TotalBytes,
                firstPacketUtc = session.Statistics.FirstPacketUtc?.ToString("O") ?? string.Empty,
                lastPacketUtc = session.Statistics.LastPacketUtc?.ToString("O") ?? string.Empty,
                rawFileClosedUtc = session.RawFileClosedUtc?.ToString("O") ?? string.Empty
            }
        };

        await using FileStream stream = new(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        await JsonSerializer.SerializeAsync(stream, document, Options).ConfigureAwait(false);
        await stream.FlushAsync().ConfigureAwait(false);
    }
}
