using HR23RadarRecorder.App.Core;

namespace HR23RadarRecorder.App;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        ConfigLoadResult config = AppConfigLoader.Load();
        Application.Run(new MainForm(config.Settings, config.Message));
    }
}
