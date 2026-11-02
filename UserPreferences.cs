using Microsoft.Win32;
using System.IO;
using System.Runtime.InteropServices;

namespace RedLight;

internal readonly record struct SavedPreferences(bool Enabled, double WarmthPosition, double DimPosition);

internal static class UserPreferences
{
    private const string SettingsKey = @"Software\RedLightCompact";
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "RedLightCompact";
    private const string ShortcutName = "RedLightCompact.lnk";

    public static SavedPreferences Load()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(SettingsKey);
            bool enabled = key?.GetValue("Enabled") is int value && value != 0;
            // On upgrade, keep the old slider position so the user can retune it.
            object? savedWarmth = key?.GetValue("WarmthPosition") ?? key?.GetValue("SliderPosition");
            double warmthPosition = savedWarmth is int position
                ? Math.Clamp(position, 0, 100)
                : 50;
            double dimPosition = key?.GetValue("DimPosition") is int dim
                ? Math.Clamp(dim, 0, 70)
                : 0;
            return new SavedPreferences(enabled, warmthPosition, dimPosition);
        }
        catch
        {
            return new SavedPreferences(false, 50, 0);
        }
    }

    public static void Save(bool enabled, double warmthPosition, double dimPosition)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(SettingsKey);
            if (key is null) return;
            key.SetValue("Enabled", enabled ? 1 : 0, RegistryValueKind.DWord);
            key.SetValue("WarmthPosition", (int)Math.Round(warmthPosition), RegistryValueKind.DWord);
            key.SetValue("DimPosition", (int)Math.Round(dimPosition), RegistryValueKind.DWord);
            key.DeleteValue("FilterType", throwOnMissingValue: false);
            key.DeleteValue("SliderPosition", throwOnMissingValue: false);
        }
        catch
        {
            // The filter still works for this session if settings cannot be persisted.
        }
    }

    public static bool IsStartupEnabled()
    {
        try
        {
            string? currentPath = CurrentExecutablePath();
            string linkPath = StartupShortcutPath();
            return currentPath is not null && File.Exists(linkPath) &&
                   string.Equals(ReadShortcutTarget(linkPath), currentPath,
                       StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public static void SetStartupEnabled(bool enabled)
    {
        string linkPath = StartupShortcutPath();
        if (!enabled)
        {
            File.Delete(linkPath);
            RemoveOldRunEntry();
            return;
        }

        string target = CurrentExecutablePath()
            ?? throw new InvalidOperationException("Run the published .exe before enabling startup.");

        WriteShortcut(linkPath, target);
        RemoveOldRunEntry();
    }

    private static string? CurrentExecutablePath()
    {
        string? path = Environment.ProcessPath;
        if (path is null || !path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Path.GetFileName(path), "dotnet.exe", StringComparison.OrdinalIgnoreCase))
            return null;
        return File.Exists(path) ? path : null;
    }

    private static string StartupShortcutPath()
    {
        string folder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        if (string.IsNullOrWhiteSpace(folder))
            throw new InvalidOperationException("Cannot locate your Windows Startup folder.");
        return Path.Combine(folder, ShortcutName);
    }

    private static void RemoveOldRunEntry()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        key?.DeleteValue(RunValueName, throwOnMissingValue: false);
    }

    private static object CreateScriptShell()
    {
        Type type = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("Windows could not create a Startup shortcut.");
        return Activator.CreateInstance(type)
            ?? throw new InvalidOperationException("Windows could not create a Startup shortcut.");
    }

    private static string ReadShortcutTarget(string linkPath)
    {
        object shell = CreateScriptShell();
        object? link = null;
        try
        {
            dynamic scriptShell = shell;
            link = scriptShell.CreateShortcut(linkPath);
            dynamic shortcut = link;
            return (string)shortcut.TargetPath;
        }
        finally
        {
            ReleaseComObject(link);
            ReleaseComObject(shell);
        }
    }

    private static void WriteShortcut(string linkPath, string target)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(linkPath)!);
        object shell = CreateScriptShell();
        object? link = null;
        try
        {
            dynamic scriptShell = shell;
            link = scriptShell.CreateShortcut(linkPath);
            dynamic shortcut = link;
            shortcut.TargetPath = target;
            shortcut.WorkingDirectory = Path.GetDirectoryName(target)!;
            shortcut.Description = "Red Light Compact";
            shortcut.Save();
        }
        finally
        {
            ReleaseComObject(link);
            ReleaseComObject(shell);
        }
    }

    private static void ReleaseComObject(object? value)
    {
        if (value is not null && Marshal.IsComObject(value))
            Marshal.FinalReleaseComObject(value);
    }
}
