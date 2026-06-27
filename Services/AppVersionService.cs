namespace HamBusLog.Services;

public static class AppVersionService
{
    private const string DefaultVersion = "0.2.0";
    private const string VersionFileName = "VERSION.txt";
    private const string DefaultBuildNumber = "0";
    private const string BuildFileName = "BUILD.txt";

    private static readonly Lazy<string> _version = new(LoadVersion);
    private static readonly Lazy<string> _buildNumber = new(LoadBuildNumber);

    public static string Version => _version.Value;
    public static string BuildNumber => _buildNumber.Value;
    public static string DisplayText => $"v{Version} (build {BuildNumber})";

    private static string LoadVersion()
    {
        return ReadFlatFile(VersionFileName, DefaultVersion);
    }

    private static string LoadBuildNumber()
        => ReadFlatFile(BuildFileName, DefaultBuildNumber);

    private static string ReadFlatFile(string fileName, string fallback)
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, fileName);
            if (!File.Exists(path))
                return fallback;

            var value = File.ReadAllText(path).Trim();
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }
        catch
        {
            return fallback;
        }
    }
}


