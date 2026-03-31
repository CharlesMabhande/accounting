using Microsoft.Extensions.Configuration;

namespace Accounting.Infrastructure.Services;

/// <summary>Resolves a dedicated folder for backup files (logical ZIP, native DB/bak copies).</summary>
public static class BackupPathResolver
{
    /// <summary>
    /// <see cref="IConfiguration"/> key <c>Backup:RootPath</c> overrides the default.
    /// Default: <c>%LOCALAPPDATA%\CharlzTechAccounting\Backups</c>
    /// </summary>
    public static string GetBackupRoot(IConfiguration configuration)
    {
        var custom = configuration["Backup:RootPath"];
        if (!string.IsNullOrWhiteSpace(custom))
            return Path.GetFullPath(custom.Trim());

        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CharlzTechAccounting",
            "Backups");
        return Path.GetFullPath(root);
    }

    public static string EnsureBackupRootExists(IConfiguration configuration)
    {
        var path = GetBackupRoot(configuration);
        Directory.CreateDirectory(path);
        return path;
    }
}
