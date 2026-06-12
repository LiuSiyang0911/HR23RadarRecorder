using HR23RadarRecorder.App.Utils;

namespace HR23RadarRecorder.App.Recording;

public sealed class EventCsvWriter : IAsyncDisposable
{
    private readonly StreamWriter writer;
    private long index;

    public EventCsvWriter(string path)
    {
        writer = new StreamWriter(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read), new System.Text.UTF8Encoding(false));
        writer.WriteLine("Index,Utc,MonoNs,SessionElapsedNs,Event,Value");
        writer.Flush();
    }

    public void Write(Core.TimeStamp timestamp, long? elapsedNs, string eventName, string? value = null)
    {
        writer.WriteLine($"{index++},{timestamp.UtcText},{timestamp.MonoNs},{elapsedNs?.ToString() ?? string.Empty},{CsvUtils.Escape(eventName)},{CsvUtils.Escape(value)}");
        writer.Flush();
    }

    public async ValueTask DisposeAsync()
    {
        await writer.FlushAsync().ConfigureAwait(false);
        await writer.DisposeAsync().ConfigureAwait(false);
    }
}
