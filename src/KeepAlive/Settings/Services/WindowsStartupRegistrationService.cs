using System.Security;
using Microsoft.Win32;

namespace KeepAlive.Settings.Services;

public sealed class WindowsStartupRegistrationService : IStartupRegistrationService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "KeepAlive";

    public bool IsEnabled()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            string? registeredCommand = key?.GetValue(ValueName) as string;
            return string.Equals(registeredCommand, BuildCommand(), StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or SecurityException)
        {
            throw new StartupRegistrationException("Windows blocked access to the current-user startup setting.", exception);
        }
    }

    public void SetEnabled(bool enabled)
    {
        try
        {
            if (enabled)
            {
                using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
                key.SetValue(ValueName, BuildCommand(), RegistryValueKind.String);
                return;
            }

            using RegistryKey? existingKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            existingKey?.DeleteValue(ValueName, throwOnMissingValue: false);
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or SecurityException)
        {
            throw new StartupRegistrationException("Windows blocked the requested startup-setting change.", exception);
        }
    }

    private static string BuildCommand()
    {
        string executablePath = Environment.ProcessPath
            ?? throw new InvalidOperationException("The Keep Alive executable path is unavailable.");
        return $"\"{executablePath}\" --startup";
    }
}
