namespace KeepAlive.Settings.Models;

public sealed record AppSettings
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    public int DefaultDurationMinutes { get; init; } = 120;

    public bool StartWithWindows { get; init; }

    public bool StartMinimized { get; init; } = true;

    public bool NotifyBeforeEnd { get; init; } = true;

    public bool NotifyOnStop { get; init; } = true;

    public AppSettings Normalize()
    {
        int duration = DefaultDurationMinutes is >= 1 and <= 480 ? DefaultDurationMinutes : 120;
        return this with { SchemaVersion = CurrentSchemaVersion, DefaultDurationMinutes = duration };
    }
}
