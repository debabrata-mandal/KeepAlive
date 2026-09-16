using KeepAlive.Presentation.ViewModels;
using KeepAlive.Settings.Models;
using KeepAlive.Tests.TestDoubles;

namespace KeepAlive.Tests.Settings;

public sealed class SettingsViewModelTests
{
    [Fact]
    public void LoadsPersistedSettings()
    {
        FakeSettingsStore store = new()
        {
            Settings = new AppSettings { DefaultDurationMinutes = 30, StartMinimized = false },
        };

        SettingsViewModel viewModel = new(store, new FakeStartupRegistrationService());

        Assert.Equal(TimeSpan.FromMinutes(30), viewModel.SelectedDefaultDuration.Duration);
        Assert.False(viewModel.StartMinimized);
    }

    [Fact]
    public void StartupToggleUpdatesRegistrationAndPersistence()
    {
        FakeSettingsStore store = new();
        FakeStartupRegistrationService startup = new();
        SettingsViewModel viewModel = new(store, startup);

        viewModel.StartWithWindows = true;

        Assert.True(startup.Enabled);
        Assert.True(store.Settings.StartWithWindows);
    }

    [Fact]
    public void FailedStartupRegistrationDoesNotChangeSetting()
    {
        FakeStartupRegistrationService startup = new() { ThrowOnSet = true };
        SettingsViewModel viewModel = new(new FakeSettingsStore(), startup);

        viewModel.StartWithWindows = true;

        Assert.False(viewModel.StartWithWindows);
        Assert.NotNull(viewModel.ErrorMessage);
    }

    [Fact]
    public void RuntimeChangeIsRaisedEvenWhenPersistenceFails()
    {
        FakeSettingsStore store = new() { ThrowOnSave = true };
        SettingsViewModel viewModel = new(store, new FakeStartupRegistrationService());
        int changeCount = 0;
        viewModel.SettingsChanged += (_, _) => changeCount++;

        viewModel.NotifyOnStop = false;

        Assert.Equal(1, changeCount);
        Assert.False(viewModel.NotifyOnStop);
        Assert.NotNull(viewModel.ErrorMessage);
    }
}
