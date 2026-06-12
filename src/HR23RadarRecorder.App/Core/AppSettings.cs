namespace HR23RadarRecorder.App.Core;

public sealed class AppSettings
{
    public UdpSettings Udp { get; init; } = new();

    public ControlSettings Control { get; init; } = new();

    public RecordingSettings Recording { get; init; } = new();
}

public sealed class UdpSettings
{
    public string LocalIp { get; init; } = "192.168.0.110";

    public int LocalPort { get; init; } = 20202;

    public string RemoteIp { get; init; } = "192.168.0.255";

    public int RemotePort { get; init; } = 23480;
}

public sealed class ControlSettings
{
    public string Host { get; init; } = "127.0.0.1";

    public int Port { get; init; } = 7070;
}

public sealed class RecordingSettings
{
    public string DefaultOutputRoot { get; init; } = "records";
}
