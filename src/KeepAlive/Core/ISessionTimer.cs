namespace KeepAlive.Core;

public interface ISessionTimer : IDisposable
{
    event EventHandler? Tick;

    void Start();

    void Stop();
}
