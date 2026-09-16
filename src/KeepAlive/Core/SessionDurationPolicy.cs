namespace KeepAlive.Core;

public static class SessionDurationPolicy
{
    public static readonly TimeSpan Minimum = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan Maximum = TimeSpan.FromHours(8);
    public static readonly TimeSpan Default = TimeSpan.FromHours(2);

    public static IReadOnlyList<TimeSpan> Presets { get; } =
    [
        TimeSpan.FromMinutes(15),
        TimeSpan.FromMinutes(30),
        TimeSpan.FromHours(1),
        TimeSpan.FromHours(2),
        TimeSpan.FromHours(4),
    ];

    public static void Validate(TimeSpan duration)
    {
        if (duration < Minimum || duration > Maximum)
        {
            throw new ArgumentOutOfRangeException(
                nameof(duration),
                duration,
                $"Session duration must be between {Minimum.TotalMinutes:0} minute and {Maximum.TotalHours:0} hours.");
        }
    }
}
