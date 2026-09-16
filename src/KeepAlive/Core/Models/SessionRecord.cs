namespace KeepAlive.Core.Models;

public sealed record SessionRecord(
    Guid Id,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset PlannedEndUtc,
    DateTimeOffset? EndedAtUtc,
    TimeSpan RequestedDuration,
    SessionEndReason? EndReason,
    string? ErrorMessage);
