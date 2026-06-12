using System.Text.Json;
using HR23RadarRecorder.App.Core;

namespace HR23RadarRecorder.App.Control;

public sealed class ControlCommandHandler
{
    private readonly CaptureRecorder recorder;
    private readonly SemaphoreSlim gate = new(1, 1);

    public ControlCommandHandler(CaptureRecorder recorder)
    {
        this.recorder = recorder;
    }

    public async Task<ControlResponse> HandleAsync(string json)
    {
        await gate.WaitAsync().ConfigureAwait(false);
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            string command = root.TryGetProperty("cmd", out JsonElement cmd) ? cmd.GetString()?.ToLowerInvariant() ?? string.Empty : string.Empty;
            return command switch
            {
                "status" => recorder.GetStatus(),
                "prepare" => await recorder.PrepareAsync(ParsePrepare(root)).ConfigureAwait(false),
                "start" => await recorder.StartAsync().ConfigureAwait(false),
                "stop" => await recorder.StopAsync().ConfigureAwait(false),
                _ => Error(command, "unknown_command", "Supported commands are status, prepare, start, and stop.")
            };
        }
        catch (JsonException exception)
        {
            return Error(string.Empty, "invalid_json", exception.Message);
        }
        catch (Exception exception)
        {
            return Error(string.Empty, "invalid_request", exception.Message);
        }
        finally
        {
            gate.Release();
        }
    }

    private static PrepareCommand ParsePrepare(JsonElement root)
    {
        string sessionId = root.GetProperty("sessionId").GetString() ?? string.Empty;
        string outputDir = root.GetProperty("outputDir").GetString() ?? string.Empty;
        string master = string.Empty;
        double? epoch = null;
        double? perf = null;
        double? recordingStartEpochS = null;
        if (root.TryGetProperty("timeBase", out JsonElement timeBase))
        {
            if (timeBase.TryGetProperty("master", out JsonElement masterElement)) master = masterElement.GetString() ?? string.Empty;
            if (timeBase.TryGetProperty("prepareCmdSendEpochS", out JsonElement epochElement) && epochElement.TryGetDouble(out double epochValue)) epoch = epochValue;
            if (timeBase.TryGetProperty("prepareCmdSendPerfS", out JsonElement perfElement) && perfElement.TryGetDouble(out double perfValue)) perf = perfValue;
            if (timeBase.TryGetProperty("recordingStartEpochS", out JsonElement startElement) && startElement.TryGetDouble(out double startValue)) recordingStartEpochS = startValue;
        }
        JsonElement? metadata = root.TryGetProperty("metadata", out JsonElement metadataElement) ? metadataElement.Clone() : null;
        return new PrepareCommand(sessionId, outputDir, master, epoch, perf, metadata, recordingStartEpochS);
    }

    private ControlResponse Error(string command, string error, string message)
    {
        RecorderSnapshot snapshot = recorder.GetSnapshot();
        TimeStamp timestamp = new TimeStampProvider().GetTimestamp();
        return new ControlResponse { Ok = false, Cmd = command, State = snapshot.State.ToProtocolString(), SessionId = snapshot.SessionId, Error = error, Message = message, Utc = timestamp.UtcText, MonoNs = timestamp.MonoNs };
    }
}
