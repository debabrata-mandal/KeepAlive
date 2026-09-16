using KeepAlive.Core.Models;
using KeepAlive.Presentation.ViewModels;
using KeepAlive.Tests.TestDoubles;

namespace KeepAlive.Tests.Presentation;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public void DefaultsToTwoHourSession()
    {
        FakeSessionController controller = new();
        using MainWindowViewModel viewModel = new(controller);

        Assert.Equal(TimeSpan.FromHours(2), viewModel.SelectedDuration);
        Assert.True(viewModel.StartCommand.CanExecute(null));
    }

    [Fact]
    public void StartCommandUsesSelectedDuration()
    {
        FakeSessionController controller = new();
        using MainWindowViewModel viewModel = new(controller);
        viewModel.SelectedDurationOption = viewModel.DurationOptions.Single(option => option.Duration == TimeSpan.FromMinutes(30));

        viewModel.StartCommand.Execute(null);

        Assert.Equal(TimeSpan.FromMinutes(30), controller.StartedDuration);
    }

    [Fact]
    public void CustomDurationMustBeBetweenOneAndFourHundredEightyMinutes()
    {
        FakeSessionController controller = new();
        using MainWindowViewModel viewModel = new(controller);
        viewModel.SelectedDurationOption = viewModel.DurationOptions.Single(option => option.IsCustom);

        viewModel.CustomMinutesText = "0";
        Assert.False(viewModel.StartCommand.CanExecute(null));
        Assert.NotNull(viewModel.DurationValidationMessage);

        viewModel.CustomMinutesText = "480";
        Assert.True(viewModel.StartCommand.CanExecute(null));
        Assert.Null(viewModel.DurationValidationMessage);
        Assert.Equal(TimeSpan.FromHours(8), viewModel.SelectedDuration);
    }

    [Fact]
    public void ActiveSnapshotUpdatesCountdownAndCommands()
    {
        FakeSessionController controller = new();
        using MainWindowViewModel viewModel = new(controller);
        SessionRecord session = CreateSession(TimeSpan.FromHours(2));

        controller.SetSnapshot(new SessionSnapshot(SessionStatus.Active, session, TimeSpan.FromMinutes(83)));

        Assert.True(viewModel.IsActive);
        Assert.False(viewModel.IsInactive);
        Assert.Equal("01:23:00", viewModel.CountdownText);
        Assert.False(viewModel.StartCommand.CanExecute(null));
        Assert.True(viewModel.StopCommand.CanExecute(null));
        Assert.NotEmpty(viewModel.EndTimeText);
    }

    [Fact]
    public void StopCommandDelegatesToController()
    {
        FakeSessionController controller = new();
        using MainWindowViewModel viewModel = new(controller);
        SessionRecord session = CreateSession(TimeSpan.FromHours(1));
        controller.SetSnapshot(new SessionSnapshot(SessionStatus.Active, session, TimeSpan.FromHours(1)));

        viewModel.StopCommand.Execute(null);

        Assert.Equal(1, controller.StopCalls);
    }

    private static SessionRecord CreateSession(TimeSpan duration)
    {
        DateTimeOffset start = new(2026, 9, 16, 12, 0, 0, TimeSpan.Zero);
        return new SessionRecord(Guid.NewGuid(), start, start.Add(duration), null, duration, null, null);
    }
}
