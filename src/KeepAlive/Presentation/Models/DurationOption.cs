namespace KeepAlive.Presentation.Models;

public sealed record DurationOption(string Label, TimeSpan? Duration)
{
    public bool IsCustom => Duration is null;
}
