using System.Diagnostics;
using Microsoft.Win32;

namespace CBB;

public static class AppInfo
{
    public const string Name = "CBB-CloseButtonBlocker";
    public const string Version = "0.9.4beta";
    public const string Repository = "https://github.com/BenRussoUSA/CBB-CloseButtonBlocker";
    public const string MutexName = "Local\\BenRussoUSA.CBB.CloseButtonBlocker";
    public const string StopEventName = "Local\\BenRussoUSA.CBB.CloseButtonBlocker.Stop";
    public const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public static void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Could not open browser"); }
    }

    public static bool StartupEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return string.Equals(key?.GetValue(Name) as string, StartupCommand, StringComparison.OrdinalIgnoreCase);
    }

    private static string StartupCommand => $"\"{Application.ExecutablePath}\"";
    public static void SetStartup(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (enabled) key.SetValue(Name, StartupCommand, RegistryValueKind.String);
        else key.DeleteValue(Name, false);
    }

}
