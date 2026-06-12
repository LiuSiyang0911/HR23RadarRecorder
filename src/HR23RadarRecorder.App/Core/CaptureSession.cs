using System.Text.Json;

namespace HR23RadarRecorder.App.Core;

public sealed class CaptureSession
{
    public required string SessionId { get; init; }
    public required string CaptureDir { get; init; }
    public required TimeStamp PreparedAt { get; init; }
    public string MasterName { get; init; } = string.Empty;
    public double? PrepareCmdSendEpochS { get; init; }
    public double? PrepareCmdSendPerfS { get; init; }
    public JsonElement? MasterMetadata { get; init; }
    public double? RecordingStartEpochS { get; init; }
    public TimeStamp? RecordingStartedAt { get; set; }
    public DateTimeOffset? RawFileClosedUtc { get; set; }
    public long? RawFileClosedMonoNs { get; set; }
    public CaptureStatistics Statistics { get; } = new();
}
