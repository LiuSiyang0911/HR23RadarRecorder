namespace HR23RadarRecorder.App.Core;

public enum RecorderState
{
    Idle,
    Prepared,
    Recording,
    Stopping,
    Stopped,
    Error
}

public static class RecorderStateExtensions
{
    public static string ToProtocolString(this RecorderState state) => state.ToString().ToLowerInvariant();
}
