using HR23RadarRecorder.App.Core;

namespace HR23RadarRecorder.App;

public partial class MainForm : Form
{
    private readonly string? startupMessage;

    public MainForm(AppSettings settings, string? startupMessage)
    {
        InitializeComponent();
        this.startupMessage = startupMessage;

        configSummaryLabel.Text =
            $"UDP local endpoint: {settings.Udp.LocalIp}:{settings.Udp.LocalPort}{Environment.NewLine}" +
            $"Control endpoint: {settings.Control.Host}:{settings.Control.Port}{Environment.NewLine}" +
            $"Default output root: {settings.Recording.DefaultOutputRoot}";
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);

        if (!string.IsNullOrWhiteSpace(startupMessage))
        {
            MessageBox.Show(
                this,
                startupMessage,
                "Configuration notice",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }
}
