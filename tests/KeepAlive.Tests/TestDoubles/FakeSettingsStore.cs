using KeepAlive.Settings.Models;
using KeepAlive.Settings.Services;

namespace KeepAlive.Tests.TestDoubles;

internal sealed class FakeSettingsStore : ISettingsStore
{
    public AppSettings Settings { get; set; } = new();

    public bool ThrowOnSave { get; set; }

    public AppSettings Load() => Settings;

    public void Save(AppSettings settings)
    {
        if (ThrowOnSave)
        {
            throw new IOException("Save failed.");
        }

        Settings = settings;
    }
}
