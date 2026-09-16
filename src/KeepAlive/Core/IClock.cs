namespace KeepAlive.Core;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
