namespace KeepAlive.History.Services;

public sealed class HistoryErrorEventArgs(string message) : EventArgs
{
    public string Message { get; } = message;
}
