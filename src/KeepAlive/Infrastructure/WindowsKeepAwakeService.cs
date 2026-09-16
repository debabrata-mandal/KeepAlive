using System.ComponentModel;
using System.Runtime.InteropServices;
using KeepAlive.Core;

namespace KeepAlive.Infrastructure;

public sealed class WindowsKeepAwakeService : IKeepAwakeService
{
    private int? _owningThreadId;

    public bool IsActive { get; private set; }

    public void Start()
    {
        if (IsActive)
        {
            return;
        }

        var result = SetThreadExecutionState(ExecutionState.Continuous | ExecutionState.SystemRequired);

        if (result == 0)
        {
            throw CreateNativeException("Windows rejected the keep-awake request.");
        }

        _owningThreadId = Environment.CurrentManagedThreadId;
        IsActive = true;
    }

    public void Stop()
    {
        if (!IsActive)
        {
            return;
        }

        if (_owningThreadId != Environment.CurrentManagedThreadId)
        {
            throw new KeepAwakeException("The keep-awake request must be cleared on the thread that created it.");
        }

        var result = SetThreadExecutionState(ExecutionState.Continuous);
        if (result == 0)
        {
            throw CreateNativeException("Windows did not clear the keep-awake request.");
        }

        _owningThreadId = null;
        IsActive = false;
    }

    private static KeepAwakeException CreateNativeException(string message)
    {
        var errorCode = Marshal.GetLastWin32Error();
        return errorCode == 0
            ? new KeepAwakeException(message)
            : new KeepAwakeException(message, new Win32Exception(errorCode));
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern ExecutionState SetThreadExecutionState(ExecutionState executionState);

    [Flags]
    private enum ExecutionState : uint
    {
        SystemRequired = 0x00000001,
        Continuous = 0x80000000,
    }
}
