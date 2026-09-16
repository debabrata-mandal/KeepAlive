namespace KeepAlive.Core;

public sealed class KeepAwakeException : Exception
{
    public KeepAwakeException(string message)
        : base(message)
    {
    }

    public KeepAwakeException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
