using System.IO;

namespace KeepAlive.Infrastructure.Persistence;

public static class AppDataPaths
{
    public static string RootDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "KeepAlive");

    public static string SettingsFile { get; } = Path.Combine(RootDirectory, "settings.json");

    public static string HistoryFile { get; } = Path.Combine(RootDirectory, "history.json");
}
