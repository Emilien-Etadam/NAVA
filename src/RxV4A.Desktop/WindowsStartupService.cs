using Microsoft.Win32;
using RxV4A.Host.Localization;

namespace RxV4A.Desktop;

internal sealed class WindowsStartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "NAVA";
    private readonly string _startupCommand;

    public WindowsStartupService()
    {
        var executablePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            throw new InvalidOperationException(Strings.Startup_ExeUnknown);
        }

        _startupCommand = $"\"{executablePath}\"";
    }

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        var registeredCommand = key?.GetValue(ValueName) as string;
        return string.Equals(registeredCommand, _startupCommand, StringComparison.OrdinalIgnoreCase);
    }

    public void SetEnabled(bool enabled)
    {
        if (enabled)
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
                ?? throw new InvalidOperationException(Strings.Startup_KeyUnopenable);
            key.SetValue(ValueName, _startupCommand, RegistryValueKind.String);
            return;
        }

        using var writableKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        writableKey?.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
