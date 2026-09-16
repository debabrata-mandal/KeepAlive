namespace KeepAlive.Settings.Services;

public interface IStartupRegistrationService
{
    bool IsEnabled();

    void SetEnabled(bool enabled);
}
