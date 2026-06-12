#nullable enable

namespace HR23RadarRecorder.App;

partial class MainForm
{
    private System.ComponentModel.IContainer? components;
    private Label udpValueLabel = null!;
    private Label controlValueLabel = null!;
    private Label serverStatusValueLabel = null!;
    private Label stateValueLabel = null!;
    private Label sessionValueLabel = null!;
    private Label captureDirValueLabel = null!;
    private Label packetsValueLabel = null!;
    private Label bytesValueLabel = null!;
    private Label firstPacketValueLabel = null!;
    private Label lastPacketValueLabel = null!;
    private Label throughputValueLabel = null!;
    private TextBox sessionIdTextBox = null!;
    private TextBox outputDirTextBox = null!;
    private TextBox logTextBox = null!;
    private Button startServerButton = null!;
    private Button stopServerButton = null!;
    private Button statusButton = null!;
    private Button prepareButton = null!;
    private Button startButton = null!;
    private Button stopButton = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        Text = "HR2.3 Radar Recorder";
        ClientSize = new Size(980, 700);
        MinimumSize = new Size(900, 650);
        StartPosition = FormStartPosition.CenterScreen;

        TableLayoutPanel root = new() { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 1, RowCount = 5 };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 55));

        Label title = new() { Text = "HR2.3 Radar Recorder", AutoSize = true, Font = new Font("Segoe UI", 18F, FontStyle.Bold), Margin = new Padding(3, 3, 3, 10) };
        root.Controls.Add(title, 0, 0);

        GroupBox networkGroup = new() { Text = "Network", Dock = DockStyle.Top, AutoSize = true };
        TableLayoutPanel network = CreateTable(4);
        network.Controls.Add(new Label { Text = "UDP local <- remote", AutoSize = true }, 0, 0);
        udpValueLabel = new Label { AutoSize = true }; network.Controls.Add(udpValueLabel, 1, 0);
        network.Controls.Add(new Label { Text = "TCP control", AutoSize = true }, 0, 1);
        controlValueLabel = new Label { AutoSize = true }; network.Controls.Add(controlValueLabel, 1, 1);
        network.Controls.Add(new Label { Text = "Server state", AutoSize = true }, 0, 2);
        serverStatusValueLabel = new Label { AutoSize = true }; network.Controls.Add(serverStatusValueLabel, 1, 2);
        FlowLayoutPanel serverButtons = new() { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        startServerButton = MakeButton("Start control server", startServerButton_Click);
        stopServerButton = MakeButton("Stop control server", stopServerButton_Click);
        serverButtons.Controls.AddRange([startServerButton, stopServerButton]);
        network.Controls.Add(serverButtons, 1, 3);
        networkGroup.Controls.Add(network); root.Controls.Add(networkGroup, 0, 1);

        GroupBox manualGroup = new() { Text = "Manual control", Dock = DockStyle.Top, AutoSize = true };
        TableLayoutPanel manual = CreateTable(3);
        manual.Controls.Add(new Label { Text = "Session ID", AutoSize = true }, 0, 0);
        sessionIdTextBox = new TextBox { Dock = DockStyle.Fill }; sessionIdTextBox.TextChanged += sessionIdTextBox_TextChanged; manual.Controls.Add(sessionIdTextBox, 1, 0);
        manual.Controls.Add(new Label { Text = "Output directory", AutoSize = true }, 0, 1);
        outputDirTextBox = new TextBox { Dock = DockStyle.Fill }; manual.Controls.Add(outputDirTextBox, 1, 1);
        FlowLayoutPanel commandButtons = new() { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        statusButton = MakeButton("Status refresh", statusButton_Click);
        prepareButton = MakeButton("Prepare", prepareButton_Click);
        startButton = MakeButton("Start", startButton_Click);
        stopButton = MakeButton("Stop", stopButton_Click);
        commandButtons.Controls.AddRange([statusButton, prepareButton, startButton, stopButton]);
        manual.Controls.Add(commandButtons, 1, 2);
        manualGroup.Controls.Add(manual); root.Controls.Add(manualGroup, 0, 2);

        GroupBox statusGroup = new() { Text = "Capture status", Dock = DockStyle.Fill };
        TableLayoutPanel status = CreateTable(8);
        AddStatusRow(status, 0, "State", out stateValueLabel);
        AddStatusRow(status, 1, "Session ID", out sessionValueLabel);
        AddStatusRow(status, 2, "Capture directory", out captureDirValueLabel);
        AddStatusRow(status, 3, "Packet count", out packetsValueLabel);
        AddStatusRow(status, 4, "Total bytes", out bytesValueLabel);
        AddStatusRow(status, 5, "First packet UTC", out firstPacketValueLabel);
        AddStatusRow(status, 6, "Last packet UTC", out lastPacketValueLabel);
        AddStatusRow(status, 7, "Throughput", out throughputValueLabel);
        statusGroup.Controls.Add(status); root.Controls.Add(statusGroup, 0, 3);

        GroupBox logGroup = new() { Text = "Event log", Dock = DockStyle.Fill };
        logTextBox = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Font = new Font("Consolas", 9F) };
        logGroup.Controls.Add(logTextBox); root.Controls.Add(logGroup, 0, 4);
        Controls.Add(root);
    }

    private static TableLayoutPanel CreateTable(int rows)
    {
        TableLayoutPanel table = new() { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2, RowCount = rows, Padding = new Padding(8) };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 155));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        return table;
    }

    private static Button MakeButton(string text, EventHandler handler)
    {
        Button button = new() { Text = text, AutoSize = true, Margin = new Padding(0, 2, 8, 2) };
        button.Click += handler;
        return button;
    }

    private static void AddStatusRow(TableLayoutPanel table, int row, string name, out Label value)
    {
        table.Controls.Add(new Label { Text = name, AutoSize = true }, 0, row);
        value = new Label { AutoSize = true, MaximumSize = new Size(760, 0) };
        table.Controls.Add(value, 1, row);
    }
}
