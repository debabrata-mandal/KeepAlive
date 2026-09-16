using KeepAlive.Settings.Services;

namespace KeepAlive.Tests.TestDoubles;

internal sealed class FakeStartupRegistrationService : IStartupRegistrationService
{
    public bool Enabled { get; private set; }

    public bool ThrowOnSet { get; set; }

    public bool IsEnabled() => Enabled;

    public void SetEnabled(bool enabled)
    {
        if (ThrowOnSet)
        {
            throw new StartupRegistrationException("Startup update failed.", new InvalidOperationException());
        }

        Enabled = enabled;
    }
}
