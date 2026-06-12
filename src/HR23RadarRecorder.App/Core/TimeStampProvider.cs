using System.Diagnostics;

namespace HR23RadarRecorder.App.Core;

public sealed class TimeStampProvider
{
    public TimeStamp GetTimestamp()
    {
        long ticks = Stopwatch.GetTimestamp();
        long monoNs = (long)(ticks * (1_000_000_000d / Stopwatch.Frequency));
        return new TimeStamp(DateTimeOffset.UtcNow, monoNs);
    }
}

public readonly record struct TimeStamp(DateTimeOffset Utc, long MonoNs)
{
    public string UtcText => Utc.ToString("O");
}
