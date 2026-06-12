using System.Net.Sockets;
using HR23RadarRecorder.App.Control;
using HR23RadarRecorder.App.Radar;
using HR23RadarRecorder.App.Recording;
using HR23RadarRecorder.App.Utils;

namespace HR23RadarRecorder.App.Core;

public sealed class CaptureRecorder : IAsyncDisposable
{
    private readonly RadarNetworkConfig network;
    private readonly ControlSettings control;
    private readonly TimeStampProvider clock;
    private readonly SemaphoreSlim commandGate = new(1, 1);
    private readonly object snapshotLock = new();
    private volatile RecorderState state = RecorderState.Idle;
    private CaptureSession? session;
    private EventCsvWriter? eventWriter;
    private CaptureFileWriter? fileWriter;
    private RadarUdpClient? udpClient;

    public CaptureRecorder(RadarNetworkConfig network, ControlSettings control, TimeStampProvider clock)
    {
        this.network = network;
        this.control = control;
        this.clock = clock;
    }

    public event Action<string>? Log;
    public event Action<RecorderSnapshot>? SnapshotChanged;

    public RecorderSnapshot GetSnapshot()
    {
        lock (snapshotLock)
        {
            CaptureStatistics? statistics = session?.Statistics;
            double throughput = 0;
            if (session?.RecordingStartedAt is TimeStamp started && statistics is not null)
            {
                long endMonoNs = state == RecorderState.Recording
                    ? clock.GetTimestamp().MonoNs
                    : session.RawFileClosedMonoNs ?? statistics.LastPacketMonoNs ?? started.MonoNs;
                long elapsedNs = Math.Max(1, endMonoNs - started.MonoNs);
                throughput = statistics.TotalBytes / (elapsedNs / 1_000_000_000d);
            }

            return new RecorderSnapshot(
                state,
                session?.SessionId ?? string.Empty,
                session?.CaptureDir ?? string.Empty,
                statistics?.PacketCount ?? 0,
                statistics?.TotalBytes ?? 0,
                statistics?.FirstPacketUtc?.ToString("O") ?? string.Empty,
                statistics?.LastPacketUtc?.ToString("O") ?? string.Empty,
                throughput);
        }
    }

    public ControlResponse GetStatus() => Success("status", clock.GetTimestamp());

    public async Task<ControlResponse> PrepareAsync(PrepareCommand command)
    {
        await commandGate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (state is not (RecorderState.Idle or RecorderState.Stopped))
            {
                return InvalidState("prepare", "prepare is only allowed in Idle or Stopped state");
            }

            string captureDir = FilePathUtils.ResolveCaptureDirectory(command.OutputDir);
            Directory.CreateDirectory(captureDir);
            TimeStamp received = clock.GetTimestamp();
            session = new CaptureSession
            {
                SessionId = string.IsNullOrWhiteSpace(command.SessionId) ? throw new ArgumentException("sessionId is required.") : command.SessionId,
                CaptureDir = captureDir,
                PreparedAt = received,
                MasterName = command.Master,
                PrepareCmdSendEpochS = command.PrepareCmdSendEpochS,
                PrepareCmdSendPerfS = command.PrepareCmdSendPerfS,
                MasterMetadata = command.Metadata?.Clone(),
                RecordingStartEpochS = command.RecordingStartEpochS
            };
            eventWriter = new EventCsvWriter(Path.Combine(captureDir, "events.csv"));
            eventWriter.Write(received, null, "prepare_command_received");
            state = RecorderState.Prepared;
            TimeStamp prepared = clock.GetTimestamp();
            eventWriter.Write(prepared, null, "prepared");
            Notify("prepared");
            return Success("prepare", prepared);
        }
        catch (Exception exception)
        {
            return await HandleErrorAsync("prepare", exception).ConfigureAwait(false);
        }
        finally
        {
            commandGate.Release();
        }
    }

    public async Task<ControlResponse> StartAsync()
    {
        await commandGate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (state != RecorderState.Prepared || session is null || eventWriter is null)
            {
                return InvalidState("start", "start is only allowed in Prepared state");
            }

            TimeStamp commandTime = clock.GetTimestamp();
            eventWriter.Write(commandTime, null, "start_command_received");
            fileWriter = new CaptureFileWriter(session.CaptureDir);
            session.RecordingStartedAt = clock.GetTimestamp();
            eventWriter.Write(session.RecordingStartedAt.Value, 0, "recording_started");
            state = RecorderState.Recording;
            udpClient = new RadarUdpClient(network);
            await udpClient.StartAsync(HandlePacketAsync).ConfigureAwait(false);
            Notify("recording_started");
            return Success("start", clock.GetTimestamp());
        }
        catch (Exception exception)
        {
            return await HandleErrorAsync("start", exception).ConfigureAwait(false);
        }
        finally
        {
            commandGate.Release();
        }
    }

    public async Task<ControlResponse> StopAsync()
    {
        await commandGate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (state != RecorderState.Recording || session is null || eventWriter is null)
            {
                return InvalidState("stop", "stop is only allowed in Recording state");
            }

            state = RecorderState.Stopping;
            TimeStamp stopTime = clock.GetTimestamp();
            eventWriter.Write(stopTime, Elapsed(stopTime), "stop_command_received");
            Notify("stop_command_received");

            if (udpClient is not null)
            {
                await udpClient.StopAsync().ConfigureAwait(false);
                await udpClient.DisposeAsync().ConfigureAwait(false);
                udpClient = null;
            }

            if (session.Statistics.PacketCount > 0)
            {
                TimeStamp last = new(session.Statistics.LastPacketUtc!.Value, session.Statistics.LastPacketMonoNs!.Value);
                eventWriter.Write(last, Elapsed(last), "last_packet_received");
            }

            if (fileWriter is not null)
            {
                await fileWriter.DisposeAsync().ConfigureAwait(false);
                fileWriter = null;
            }

            TimeStamp closed = clock.GetTimestamp();
            session.RawFileClosedUtc = closed.Utc;
            session.RawFileClosedMonoNs = closed.MonoNs;
            eventWriter.Write(closed, Elapsed(closed), "raw_file_closed");
            await MetadataWriter.WriteAsync(Path.Combine(session.CaptureDir, "metadata.json"), session, network, control).ConfigureAwait(false);
            TimeStamp stopped = clock.GetTimestamp();
            eventWriter.Write(stopped, Elapsed(stopped), "stopped");
            await eventWriter.DisposeAsync().ConfigureAwait(false);
            eventWriter = null;
            state = RecorderState.Stopped;
            Notify("stopped");
            return Success("stop", stopped, includeStopDetails: true);
        }
        catch (Exception exception)
        {
            return await HandleErrorAsync("stop", exception).ConfigureAwait(false);
        }
        finally
        {
            commandGate.Release();
        }
    }

    private async Task HandlePacketAsync(UdpReceiveResult packet)
    {
        if (state != RecorderState.Recording || session?.RecordingStartedAt is not TimeStamp started || fileWriter is null)
        {
            return;
        }

        TimeStamp timestamp = clock.GetTimestamp();
        long index = session.Statistics.PacketCount;
        await fileWriter.WritePacketAsync(index, timestamp, timestamp.MonoNs - started.MonoNs, packet.RemoteEndPoint, packet.Buffer).ConfigureAwait(false);
        session.Statistics.RecordPacket(packet.Buffer.Length, timestamp);
        if (index == 0)
        {
            eventWriter?.Write(timestamp, timestamp.MonoNs - started.MonoNs, "first_packet_received");
            Notify("first_packet_received");
        }
        else
        {
            RaiseSnapshot();
        }
    }

    private long? Elapsed(TimeStamp timestamp) => session?.RecordingStartedAt is TimeStamp start ? timestamp.MonoNs - start.MonoNs : null;

    private ControlResponse Success(string command, TimeStamp timestamp, bool includeStopDetails = false)
    {
        RecorderSnapshot snapshot = GetSnapshot();
        return new ControlResponse
        {
            Ok = true,
            Cmd = command,
            State = snapshot.State.ToProtocolString(),
            SessionId = snapshot.SessionId,
            CaptureDir = snapshot.CaptureDir,
            PacketCount = snapshot.PacketCount,
            TotalBytes = snapshot.TotalBytes,
            FirstPacketUtc = snapshot.FirstPacketUtc,
            LastPacketUtc = snapshot.LastPacketUtc,
            Utc = timestamp.UtcText,
            MonoNs = timestamp.MonoNs,
            Files = includeStopDetails ? new { raw = "raw.dat", packets = "packets.csv", events = "events.csv", metadata = "metadata.json" } : null,
            Time = includeStopDetails && session is not null ? new
            {
                recordingStartedUtc = session.RecordingStartedAt?.UtcText ?? string.Empty,
                firstPacketUtc = snapshot.FirstPacketUtc,
                lastPacketUtc = snapshot.LastPacketUtc,
                rawFileClosedUtc = session.RawFileClosedUtc?.ToString("O") ?? string.Empty
            } : null
        };
    }

    private ControlResponse InvalidState(string command, string message)
    {
        TimeStamp timestamp = clock.GetTimestamp();
        RecorderSnapshot snapshot = GetSnapshot();
        return new ControlResponse { Ok = false, Cmd = command, State = snapshot.State.ToProtocolString(), SessionId = snapshot.SessionId, Error = "invalid_state", Message = message, Utc = timestamp.UtcText, MonoNs = timestamp.MonoNs };
    }

    private async Task<ControlResponse> HandleErrorAsync(string command, Exception exception)
    {
        state = RecorderState.Error;
        TimeStamp timestamp = clock.GetTimestamp();
        eventWriter?.Write(timestamp, Elapsed(timestamp), "error", exception.Message);
        Log?.Invoke($"ERROR {command}: {exception.Message}");
        RaiseSnapshot();
        await CloseResourcesAsync().ConfigureAwait(false);
        return new ControlResponse { Ok = false, Cmd = command, State = "error", SessionId = session?.SessionId ?? string.Empty, Error = "operation_failed", Message = exception.Message, Utc = timestamp.UtcText, MonoNs = timestamp.MonoNs };
    }

    private void Notify(string message)
    {
        Log?.Invoke(message);
        RaiseSnapshot();
    }

    private void RaiseSnapshot() => SnapshotChanged?.Invoke(GetSnapshot());

    private async Task CloseResourcesAsync()
    {
        if (udpClient is not null) { await udpClient.DisposeAsync().ConfigureAwait(false); udpClient = null; }
        if (fileWriter is not null) { await fileWriter.DisposeAsync().ConfigureAwait(false); fileWriter = null; }
        if (eventWriter is not null) { await eventWriter.DisposeAsync().ConfigureAwait(false); eventWriter = null; }
    }

    public async ValueTask DisposeAsync()
    {
        if (state == RecorderState.Recording)
        {
            await StopAsync().ConfigureAwait(false);
        }
        else
        {
            await CloseResourcesAsync().ConfigureAwait(false);
        }
        commandGate.Dispose();
    }
}
