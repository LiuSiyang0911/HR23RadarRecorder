using System.Text.Json;

namespace HR23RadarRecorder.App.Core;

public static class AppConfigLoader
{
    private const string ConfigFileName = "appsettings.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static ConfigLoadResult Load()
    {
        string configPath = Path.Combine(AppContext.BaseDirectory, ConfigFileName);

        if (!File.Exists(configPath))
        {
            return new ConfigLoadResult(
                new AppSettings(),
                $"{ConfigFileName} was not found. Built-in default values are being used.");
        }

        try
        {
            string json = File.ReadAllText(configPath);
            AppSettings? settings = JsonSerializer.Deserialize<AppSettings>(json, SerializerOptions);

            return settings is null
                ? new ConfigLoadResult(new AppSettings(), $"{ConfigFileName} was empty. Built-in default values are being used.")
                : new ConfigLoadResult(settings, null);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return new ConfigLoadResult(
                new AppSettings(),
                $"{ConfigFileName} could not be loaded ({exception.Message}). Built-in default values are being used.");
        }
    }
}

public sealed record ConfigLoadResult(AppSettings Settings, string? Message);
