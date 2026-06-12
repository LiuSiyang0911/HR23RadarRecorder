#nullable enable

namespace HR23RadarRecorder.App;

partial class MainForm
{
    private System.ComponentModel.IContainer? components = null;
    private Label titleLabel = null!;
    private Label statusLabel = null!;
    private Label configSummaryLabel = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        titleLabel = new Label();
        statusLabel = new Label();
        configSummaryLabel = new Label();
        SuspendLayout();
        //
        // titleLabel
        //
        titleLabel.AutoSize = true;
        titleLabel.Font = new Font("Segoe UI", 18F, FontStyle.Bold, GraphicsUnit.Point);
        titleLabel.Location = new Point(28, 25);
        titleLabel.Name = "titleLabel";
        titleLabel.Size = new Size(277, 32);
        titleLabel.TabIndex = 0;
        titleLabel.Text = "HR2.3 Radar Recorder";
        //
        // statusLabel
        //
        statusLabel.AutoSize = true;
        statusLabel.Location = new Point(31, 75);
        statusLabel.Name = "statusLabel";
        statusLabel.Size = new Size(298, 15);
        statusLabel.TabIndex = 1;
        statusLabel.Text = "Project skeleton is running. Radar services are not started.";
        //
        // configSummaryLabel
        //
        configSummaryLabel.AutoSize = true;
        configSummaryLabel.Location = new Point(31, 116);
        configSummaryLabel.Name = "configSummaryLabel";
        configSummaryLabel.Size = new Size(119, 15);
        configSummaryLabel.TabIndex = 2;
        configSummaryLabel.Text = "Configuration summary";
        //
        // MainForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(620, 260);
        Controls.Add(configSummaryLabel);
        Controls.Add(statusLabel);
        Controls.Add(titleLabel);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "HR2.3 Radar Recorder";
        ResumeLayout(false);
        PerformLayout();
    }
}
