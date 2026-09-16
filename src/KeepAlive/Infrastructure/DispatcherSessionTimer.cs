using System.Windows.Threading;
using KeepAlive.Core;

namespace KeepAlive.Infrastructure;

public sealed class DispatcherSessionTimer : ISessionTimer
{
    private readonly DispatcherTimer _timer;

    public DispatcherSessionTimer(Dispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);

        _timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Normal, OnTick, dispatcher);
        _timer.Stop();
    }

    public event EventHandler? Tick;

    public void Start() => _timer.Start();

    public void Stop() => _timer.Stop();

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnTick;
    }

    private void OnTick(object? sender, EventArgs e)
    {
        Tick?.Invoke(this, EventArgs.Empty);
    }
}
