using System.IO;

namespace Accounting.Desktop.Shell;

public static class AppPaths
{
    public static string LocalBackupFolder
    {
        get
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CharlzTechAccounting",
                "Backups");
            Directory.CreateDirectory(path);
            return path;
        }
    }
}
