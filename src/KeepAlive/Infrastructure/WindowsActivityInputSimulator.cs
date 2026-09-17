using System.Runtime.InteropServices;
using KeepAlive.Core;

namespace KeepAlive.Infrastructure;

/// <summary>
/// Periodically injects a zero-distance mouse move via SendInput while active. This resets
/// Windows' last-input timestamp (used by the screen saver, lock timeout, and presence-based
/// status in apps like Teams) without visibly moving the cursor or emitting keystrokes that
/// could land in whatever window currently has focus.
/// </summary>
public sealed class WindowsActivityInputSimulator : IActivityInputSimulator, IDisposable
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(60);

    private readonly System.Threading.Timer _timer;
    private volatile bool _isActive;
    private bool _disposed;

    public WindowsActivityInputSimulator()
    {
        _timer = new System.Threading.Timer(OnTick, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    public bool IsActive => _isActive;

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_isActive)
        {
            return;
        }

        _isActive = true;
        _timer.Change(Interval, Interval);
    }

    public void Stop()
    {
        if (!_isActive)
        {
            return;
        }

        _isActive = false;
        _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _isActive = false;
        _timer.Dispose();
    }

    private void OnTick(object? state)
    {
        if (!_isActive)
        {
            return;
        }

        INPUT input = default;
        input.Type = InputTypeMouse;
        input.Mouse.Flags = MouseEventFlagMove;

        SendInput(1, [input], Marshal.SizeOf<INPUT>());
    }

    private const uint InputTypeMouse = 0;
    private const uint MouseEventFlagMove = 0x0001;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint numberOfInputs, INPUT[] inputs, int structSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint Type;
        public MOUSEINPUT Mouse;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int Dx;
        public int Dy;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }
}
