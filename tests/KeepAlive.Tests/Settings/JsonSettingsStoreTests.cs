using KeepAlive.Settings.Models;
using KeepAlive.Settings.Services;

namespace KeepAlive.Tests.Settings;

public sealed class JsonSettingsStoreTests
{
    [Fact]
    public void SaveAndLoadRoundTripsSettings()
    {
        string path = CreateTemporaryPath();

        try
        {
            JsonSettingsStore store = new(path);
            AppSettings expected = new()
            {
                DefaultDurationMinutes = 30,
                StartWithWindows = true,
                StartMinimized = false,
                NotifyBeforeEnd = false,
                NotifyOnStop = false,
            };

            store.Save(expected);

            Assert.Equal(expected, store.Load());
        }
        finally
        {
            DeleteIfPresent(path);
        }
    }

    [Fact]
    public void MalformedFileFallsBackToDefaults()
    {
        string path = CreateTemporaryPath();

        try
        {
            File.WriteAllText(path, "not json");

            AppSettings settings = new JsonSettingsStore(path).Load();

            Assert.Equal(120, settings.DefaultDurationMinutes);
            Assert.True(settings.StartMinimized);
        }
        finally
        {
            DeleteIfPresent(path);
        }
    }

    private static string CreateTemporaryPath() => Path.Combine(Path.GetTempPath(), $"keep-alive-{Guid.NewGuid():N}.json");

    private static void DeleteIfPresent(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
