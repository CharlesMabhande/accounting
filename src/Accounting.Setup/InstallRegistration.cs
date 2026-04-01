using System.Reflection;
using Microsoft.Win32;

namespace Accounting.Setup;

/// <summary>
/// Registers the product in Windows Settings → Apps → Installed apps (per-user, HKCU Uninstall key).
/// </summary>
internal static class InstallRegistration
{
    /// <summary>Stable product code for Add/Remove Programs and uninstall.</summary>
    internal const string UninstallRegistryKeyRelative =
        @"Software\Microsoft\Windows\CurrentVersion\Uninstall\{B7F3A1C2-4E8D-4F9A-8B1E-6C2D3E4F5A0B}";

    public static void RegisterInstall(string installRoot)
    {
        if (string.IsNullOrWhiteSpace(installRoot))
            return;

        installRoot = Path.GetFullPath(installRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var uninstallCmd = Path.Combine(installRoot, "Uninstall-Accounting.cmd");
        var uninstallPs1 = Path.Combine(installRoot, "Uninstall-Accounting.ps1");
        if (!File.Exists(uninstallCmd) || !File.Exists(uninstallPs1))
            return;

        var displayIcon = Path.Combine(installRoot, "Desktop", "Accounting.Desktop.exe");
        if (!File.Exists(displayIcon))
            displayIcon = Path.Combine(installRoot, "CharlzTech.exe");

        var ver = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";
        var uninstallQuoted = "\"" + uninstallCmd + "\"";
        var quietUninstall =
            "powershell.exe -NoProfile -ExecutionPolicy Bypass -File \"" + uninstallPs1 + "\" -InstallPath \"" + installRoot + "\" -Quiet";

        using var key = Registry.CurrentUser.CreateSubKey(UninstallRegistryKeyRelative, writable: true)
            ?? throw new InvalidOperationException("Could not create uninstall registry key.");

        key.SetValue("DisplayName", "CharlzTech Accounting", RegistryValueKind.String);
        key.SetValue("DisplayVersion", ver, RegistryValueKind.String);
        key.SetValue("Publisher", "CharlzTech", RegistryValueKind.String);
        key.SetValue("InstallLocation", installRoot, RegistryValueKind.String);
        key.SetValue("UninstallString", uninstallQuoted, RegistryValueKind.String);
        key.SetValue("QuietUninstallString", quietUninstall, RegistryValueKind.String);
        key.SetValue("DisplayIcon", displayIcon + ",0", RegistryValueKind.String);
        key.SetValue("InstallDate", DateTime.Now.ToString("yyyyMMdd"), RegistryValueKind.String);
        key.SetValue("NoModify", 1, RegistryValueKind.DWord);
        key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
    }
}
