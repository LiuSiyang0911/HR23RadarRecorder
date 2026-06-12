using HR23RadarRecorder.App.Control;
using HR23RadarRecorder.App.Core;

namespace HR23RadarRecorder.App;

public partial class MainForm : Form
{
    private readonly AppSettings settings;
    private readonly string? startupMessage;
    private readonly CaptureRecorder recorder;
    private readonly JsonLineControlServer controlServer;
    private readonly System.Windows.Forms.Timer refreshTimer;
    private bool shutdownComplete;
    private bool shutdownStarted;

    public MainForm(AppSettings settings, string? startupMessage, CaptureRecorder recorder, JsonLineControlServer controlServer)
    {
        InitializeComponent();
        this.settings = settings;
        this.startupMessage = startupMessage;
        this.recorder = recorder;
        this.controlServer = controlServer;

        udpValueLabel.Text = $"{settings.Udp.LocalIp}:{settings.Udp.LocalPort}  <-  {settings.Udp.RemoteIp}:{settings.Udp.RemotePort}";
        controlValueLabel.Text = $"{settings.Control.Host}:{settings.Control.Port}";
        sessionIdTextBox.Text = $"session_{DateTime.Now:yyyyMMdd_HHmmss}";
        outputDirTextBox.Text = Path.Combine(settings.Recording.DefaultOutputRoot, sessionIdTextBox.Text, "raw", "hr23_radar");

        recorder.Log += OnRecorderLog;
        recorder.SnapshotChanged += OnSnapshotChanged;
        refreshTimer = new System.Windows.Forms.Timer { Interval = 500 };
        refreshTimer.Tick += (_, _) => UpdateSnapshot(recorder.GetSnapshot());
        refreshTimer.Start();
        UpdateSnapshot(recorder.GetSnapshot());
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (!string.IsNullOrWhiteSpace(startupMessage))
        {
            MessageBox.Show(this, startupMessage, "Configuration notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private async void startServerButton_Click(object? sender, EventArgs e)
    {
        await RunUiActionAsync(async () =>
        {
            await controlServer.StartAsync();
            AppendLog($"TCP control server started at {settings.Control.Host}:{settings.Control.Port}");
            UpdateServerButtons();
        });
    }

    private async void stopServerButton_Click(object? sender, EventArgs e)
    {
        await RunUiActionAsync(async () =>
        {
            await controlServer.StopAsync();
            AppendLog("TCP control server stopped");
            UpdateServerButtons();
        });
    }

    private void statusButton_Click(object? sender, EventArgs e)
    {
        UpdateSnapshot(recorder.GetSnapshot());
        AppendLog("status refreshed");
    }

    private async void prepareButton_Click(object? sender, EventArgs e)
    {
        await RunUiActionAsync(async () =>
        {
            ControlResponse response = await recorder.PrepareAsync(new PrepareCommand(sessionIdTextBox.Text.Trim(), outputDirTextBox.Text.Trim(), "manual_gui"));
            ReportResponse(response);
        });
    }

    private async void startButton_Click(object? sender, EventArgs e)
    {
        await RunUiActionAsync(async () => ReportResponse(await recorder.StartAsync()));
    }

    private async void stopButton_Click(object? sender, EventArgs e)
    {
        await RunUiActionAsync(async () => ReportResponse(await recorder.StopAsync()));
    }

    private void sessionIdTextBox_TextChanged(object? sender, EventArgs e)
    {
        if (!outputDirTextBox.Focused)
        {
            outputDirTextBox.Text = Path.Combine(settings.Recording.DefaultOutputRoot, sessionIdTextBox.Text.Trim(), "raw", "hr23_radar");
        }
    }

    private async Task RunUiActionAsync(Func<Task> action)
    {
        try
        {
            SetControlButtonsEnabled(false);
            await action();
        }
        catch (Exception exception)
        {
            AppendLog($"ERROR {exception.Message}");
        }
        finally
        {
            UpdateSnapshot(recorder.GetSnapshot());
        }
    }

    private void ReportResponse(ControlResponse response)
    {
        AppendLog(response.Ok ? $"{response.Cmd}: {response.State}" : $"{response.Cmd}: {response.Error} - {response.Message}");
    }

    private void OnRecorderLog(string message)
    {
        if (IsDisposed) return;
        if (InvokeRequired) BeginInvoke(() => AppendLog(message)); else AppendLog(message);
    }

    private void OnSnapshotChanged(RecorderSnapshot snapshot)
    {
        if (IsDisposed) return;
        if (InvokeRequired) BeginInvoke(() => UpdateSnapshot(snapshot)); else UpdateSnapshot(snapshot);
    }

    private void UpdateSnapshot(RecorderSnapshot snapshot)
    {
        stateValueLabel.Text = snapshot.State.ToProtocolString();
        sessionValueLabel.Text = snapshot.SessionId;
        captureDirValueLabel.Text = snapshot.CaptureDir;
        packetsValueLabel.Text = snapshot.PacketCount.ToString("N0");
        bytesValueLabel.Text = snapshot.TotalBytes.ToString("N0");
        firstPacketValueLabel.Text = snapshot.FirstPacketUtc;
        lastPacketValueLabel.Text = snapshot.LastPacketUtc;
        throughputValueLabel.Text = $"{snapshot.ThroughputBytesPerSecond / 1024d / 1024d:F3} MiB/s";
        SetControlButtonsEnabled(true);
        UpdateServerButtons();
    }

    private void SetControlButtonsEnabled(bool enabled)
    {
        RecorderState state = recorder.GetSnapshot().State;
        statusButton.Enabled = enabled;
        prepareButton.Enabled = enabled && state is RecorderState.Idle or RecorderState.Stopped;
        startButton.Enabled = enabled && state == RecorderState.Prepared;
        stopButton.Enabled = enabled && state == RecorderState.Recording;
    }

    private void UpdateServerButtons()
    {
        startServerButton.Enabled = !controlServer.IsRunning;
        stopServerButton.Enabled = controlServer.IsRunning;
        serverStatusValueLabel.Text = controlServer.IsRunning ? "running" : "stopped";
    }

    private void AppendLog(string message)
    {
        logTextBox.AppendText($"{DateTimeOffset.Now:HH:mm:ss.fff} {message}{Environment.NewLine}");
    }

    protected override async void OnFormClosing(FormClosingEventArgs e)
    {
        if (shutdownComplete)
        {
            base.OnFormClosing(e);
            return;
        }

        e.Cancel = true;
        if (shutdownStarted) return;
        shutdownStarted = true;
        Enabled = false;
        refreshTimer.Stop();
        AppendLog("shutting down safely...");
        try
        {
            await controlServer.DisposeAsync();
            await recorder.DisposeAsync();
        }
        catch (Exception exception)
        {
            AppendLog($"shutdown error: {exception.Message}");
        }
        finally
        {
            recorder.Log -= OnRecorderLog;
            recorder.SnapshotChanged -= OnSnapshotChanged;
            shutdownComplete = true;
            Close();
        }
    }
}
