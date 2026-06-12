namespace HR23RadarRecorder.App.Control;

public sealed class ControlResponse
{
    public bool Ok { get; init; }
    public string Cmd { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string SessionId { get; init; } = string.Empty;
    public string CaptureDir { get; init; } = string.Empty;
    public long PacketCount { get; init; }
    public long TotalBytes { get; init; }
    public string FirstPacketUtc { get; init; } = string.Empty;
    public string LastPacketUtc { get; init; } = string.Empty;
    public string Utc { get; init; } = string.Empty;
    public long MonoNs { get; init; }
    public string? Error { get; init; }
    public string? Message { get; init; }
    public object? Files { get; init; }
    public object? Time { get; init; }
}
