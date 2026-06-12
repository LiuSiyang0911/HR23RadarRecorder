using HR23RadarRecorder.App.Core;
using HR23RadarRecorder.App.Control;
using HR23RadarRecorder.App.Radar;
using System.Net;

namespace HR23RadarRecorder.App;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        ConfigLoadResult config = AppConfigLoader.Load();
        RadarNetworkConfig network = new(
            config.Settings.Udp.LocalIp,
            config.Settings.Udp.LocalPort,
            config.Settings.Udp.RemoteIp,
            config.Settings.Udp.RemotePort);
        CaptureRecorder recorder = new(network, config.Settings.Control, new TimeStampProvider());
        JsonLineControlServer controlServer = new(
            IPAddress.Parse(config.Settings.Control.Host),
            config.Settings.Control.Port,
            new ControlCommandHandler(recorder));

        Application.Run(new MainForm(config.Settings, config.Message, recorder, controlServer));
    }
}
