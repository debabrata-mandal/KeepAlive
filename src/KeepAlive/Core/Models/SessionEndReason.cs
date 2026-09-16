namespace KeepAlive.Core.Models;

public enum SessionEndReason
{
    TimerExpired,
    StoppedManually,
    ApplicationExit,
    Interrupted,
    NativeError,
}
