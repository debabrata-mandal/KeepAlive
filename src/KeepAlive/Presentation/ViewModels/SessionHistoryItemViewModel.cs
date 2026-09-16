using System.Globalization;
using KeepAlive.Core.Models;

namespace KeepAlive.Presentation.ViewModels;

public sealed class SessionHistoryItemViewModel
{
    public SessionHistoryItemViewModel(SessionRecord session)
    {
        StartText = session.StartedAtUtc.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);
        EndText = session.EndedAtUtc?.ToLocalTime().ToString("g", CultureInfo.CurrentCulture) ?? "—";
        DurationText = FormatDuration(session.RequestedDuration);
        OutcomeText = FormatOutcome(session.EndReason);
    }

    public string StartText { get; }

    public string EndText { get; }

    public string DurationText { get; }

    public string OutcomeText { get; }

    private static string FormatDuration(TimeSpan duration)
    {
        int hours = (int)duration.TotalHours;
        return hours > 0 ? $"{hours}h {duration.Minutes}m" : $"{duration.Minutes}m";
    }

    private static string FormatOutcome(SessionEndReason? reason)
    {
        return reason switch
        {
            SessionEndReason.TimerExpired => "Completed",
            SessionEndReason.StoppedManually => "Stopped manually",
            SessionEndReason.ApplicationExit => "Application exited",
            SessionEndReason.Interrupted => "Interrupted",
            SessionEndReason.NativeError => "Windows error",
            _ => "Active",
        };
    }
}
