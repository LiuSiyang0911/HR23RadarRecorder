namespace HR23RadarRecorder.App.Utils;

public static class FilePathUtils
{
    public static string ResolveCaptureDirectory(string outputDir)
    {
        if (string.IsNullOrWhiteSpace(outputDir))
        {
            throw new ArgumentException("outputDir is required.", nameof(outputDir));
        }

        return Path.GetFullPath(outputDir);
    }
}
