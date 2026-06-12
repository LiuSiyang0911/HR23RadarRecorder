namespace HR23RadarRecorder.App.Utils;

public static class CsvUtils
{
    public static string Escape(string? value)
    {
        value ??= string.Empty;
        return value.IndexOfAny([',', '"', '\r', '\n']) < 0
            ? value
            : $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
