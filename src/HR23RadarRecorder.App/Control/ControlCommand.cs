using System.Text.Json;

namespace HR23RadarRecorder.App.Control;

public sealed record PrepareCommand(
    string SessionId,
    string OutputDir,
    string Master = "",
    double? PrepareCmdSendEpochS = null,
    double? PrepareCmdSendPerfS = null,
    JsonElement? Metadata = null,
    double? RecordingStartEpochS = null);
