using Microsoft.Win32;

namespace LiveWallpaperStudio.App.Services;

public sealed class WindowsStartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "Wallora";

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
        return key?.GetValue(ValueName) is string value && !string.IsNullOrWhiteSpace(value);
    }

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true)
            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, true);

        if (!enabled)
        {
            key.DeleteValue(ValueName, false);
            return;
        }

        var exePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(exePath))
        {
            return;
        }

        key.SetValue(ValueName, $"\"{exePath}\"");
    }
}
