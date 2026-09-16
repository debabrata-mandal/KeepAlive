namespace KeepAlive.Settings.Services;

public sealed class StartupRegistrationException : Exception
{
    public StartupRegistrationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
