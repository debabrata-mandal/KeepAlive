using KeepAlive.Infrastructure.Persistence;
using KeepAlive.Settings.Models;

namespace KeepAlive.Settings.Services;

public sealed class JsonSettingsStore(string path) : ISettingsStore
{
    private readonly AtomicJsonFile _file = new(path);

    public AppSettings Load() => (_file.Read<AppSettings>() ?? new AppSettings()).Normalize();

    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _file.Write(settings.Normalize());
    }
}
