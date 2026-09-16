using KeepAlive.Settings.Models;

namespace KeepAlive.Settings.Services;

public interface ISettingsStore
{
    AppSettings Load();

    void Save(AppSettings settings);
}
