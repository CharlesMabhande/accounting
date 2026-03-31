using System.Diagnostics;
using System.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;

namespace Accounting.Infrastructure.Persistence;

/// <summary>
/// When <c>Database:AutoProvision</c> is true (Windows), attempts unattended setup: start LocalDB,
/// install SQL Server Express and/or SSMS via winget, then return a working connection string if the server changed.
/// SSMS is a management client; the database engine comes from LocalDB or Express.
/// </summary>
public static class SqlServerRuntimeProvisioner
{
    private const string WingetExpress2022 = "Microsoft.SQLServer.2022.Express";
    private const string WingetExpress2019 = "Microsoft.SQLServer.2019.Express";
    private const string WingetSsms = "Microsoft.SQLServerManagementStudio";

    /// <summary>Returns a replacement connection string if the configured server was unreachable and another was provisioned.</summary>
    public static async Task<string?> ResolveWorkingConnectionAsync(
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        if (!configuration.GetValue("Database:AutoProvision", false))
            return null;

        if (!OperatingSystem.IsWindows())
        {
            Log("AutoProvision is only supported on Windows.");
            return null;
        }

        var original = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(original))
            return null;

        var builder = new SqlConnectionStringBuilder(original);
        var database = builder.InitialCatalog;
        var trusted = IsTrustedConnection(builder);

        var engineOk = await CanConnectAsync(original, cancellationToken).ConfigureAwait(false);
        string? resolved = null;

        if (!engineOk)
        {
            Log("AutoProvision: initial SQL connection failed; attempting unattended setup.");
            TryStartLocalDb();
            engineOk = await CanConnectAsync(original, cancellationToken).ConfigureAwait(false);
            if (engineOk)
                Log("AutoProvision: OK after starting LocalDB.");
        }

        if (!engineOk)
        {
            if (!TryFindWinget(out var wingetPath))
            {
                Log("AutoProvision: winget.exe not found. Install SQL Server Express/LocalDB manually, install App Installer from the Microsoft Store, or use Database:Provider Sqlite.");
                return null;
            }

            await InstallSqlEngineIfNeededAsync(wingetPath, cancellationToken).ConfigureAwait(false);

            foreach (var server in EnumerateCandidateServers())
            {
                var candidate = BuildConnectionString(server, database, trusted);
                if (!await CanConnectAsync(candidate, cancellationToken).ConfigureAwait(false))
                    continue;
                Log($"AutoProvision: reachable server '{server}'.");
                engineOk = true;
                if (!string.Equals(NormalizeConnection(original), NormalizeConnection(candidate), StringComparison.Ordinal))
                    resolved = candidate;
                break;
            }
        }

        if (engineOk)
            await MaybeInstallSsmsAsync(configuration, cancellationToken).ConfigureAwait(false);

        return resolved;
    }

    private static async Task MaybeInstallSsmsAsync(IConfiguration configuration, CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows())
            return;
        if (!configuration.GetValue("Database:AutoInstallSsms", true))
            return;
        if (IsSsmsInstalled())
            return;
        if (!TryFindWinget(out var wingetPath))
            return;

        Log("AutoProvision: SQL Server Management Studio not found; installing via winget...");
        await RunWingetInstallAsync(wingetPath, WingetSsms, cancellationToken).ConfigureAwait(false);
    }

    private static bool IsTrustedConnection(SqlConnectionStringBuilder builder)
    {
        if (builder.IntegratedSecurity)
            return true;
        if (!builder.ContainsKey("Trusted_Connection"))
            return false;
        return string.Equals(builder["Trusted_Connection"]?.ToString(), "true", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeConnection(string cs)
    {
        var b = new SqlConnectionStringBuilder(cs);
        return b.ConnectionString;
    }

    private static IEnumerable<string> EnumerateCandidateServers()
    {
        yield return "(localdb)\\mssqllocaldb";
        yield return ".\\SQLEXPRESS";
        yield return "localhost\\SQLEXPRESS";
        yield return $"{Environment.MachineName}\\SQLEXPRESS";
    }

    private static string BuildConnectionString(string server, string database, bool trustedConnection)
    {
        var b = new SqlConnectionStringBuilder
        {
            DataSource = server,
            InitialCatalog = database,
            TrustServerCertificate = true,
            MultipleActiveResultSets = true
        };
        if (trustedConnection)
            b.IntegratedSecurity = true;
        return b.ConnectionString;
    }

    private static async Task InstallSqlEngineIfNeededAsync(string wingetPath, CancellationToken cancellationToken)
    {
        if (await CanConnectToMasterAsync("(localdb)\\mssqllocaldb", cancellationToken).ConfigureAwait(false))
            return;

        Log("AutoProvision: installing SQL Server Express (database engine; large download)...");
        var exit = await RunWingetInstallAsync(wingetPath, WingetExpress2022, cancellationToken).ConfigureAwait(false);
        if (exit != 0)
        {
            Log($"AutoProvision: Express 2022 winget exit {exit}; trying 2019 Express.");
            await RunWingetInstallAsync(wingetPath, WingetExpress2019, cancellationToken).ConfigureAwait(false);
        }

        TryStartLocalDb();
        await Task.Delay(TimeSpan.FromSeconds(8), cancellationToken).ConfigureAwait(false);
    }

    private static async Task<bool> CanConnectAsync(string connectionString, CancellationToken cancellationToken)
    {
        try
        {
            await using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> CanConnectToMasterAsync(string server, CancellationToken cancellationToken)
    {
        try
        {
            var b = new SqlConnectionStringBuilder
            {
                DataSource = server,
                InitialCatalog = "master",
                IntegratedSecurity = true,
                TrustServerCertificate = true
            };
            await using var conn = new SqlConnection(b.ConnectionString);
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void TryStartLocalDb()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "sqllocaldb",
                Arguments = "start mssqllocaldb",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var p = Process.Start(psi);
            p?.WaitForExit(30_000);
        }
        catch
        {
            // sqllocaldb missing until SQL is installed
        }
    }

    private static bool TryFindWinget(out string path)
    {
        var apps = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft", "WindowsApps", "winget.exe");
        if (File.Exists(apps))
        {
            path = apps;
            return true;
        }

        path = "winget";
        try
        {
            using var p = Process.Start(new ProcessStartInfo
            {
                FileName = "where.exe",
                Arguments = "winget",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            });
            if (p == null)
                return false;
            var line = p.StandardOutput.ReadLine();
            p.WaitForExit(5_000);
            if (!string.IsNullOrWhiteSpace(line) && File.Exists(line.Trim()))
            {
                path = line.Trim();
                return true;
            }
        }
        catch
        {
            // ignore
        }

        return false;
    }

    private static async Task<int> RunWingetInstallAsync(string wingetPath, string packageId, CancellationToken cancellationToken)
    {
        var args =
            $"install --id {packageId} -e --accept-package-agreements --accept-source-agreements --disable-interactivity";
        try
        {
            using var p = Process.Start(new ProcessStartInfo
            {
                FileName = wingetPath,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            });
            if (p == null)
                return -1;
            using (cancellationToken.Register(() => { try { p.Kill(entireProcessTree: true); } catch { } }))
                await p.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            return p.ExitCode;
        }
        catch (Exception ex)
        {
            Log($"AutoProvision: winget install {packageId} failed: {ex.Message}");
            return -1;
        }
    }

    public static bool IsSsmsInstalled()
    {
        if (!OperatingSystem.IsWindows())
            return false;

        foreach (var root in new[] { Environment.SpecialFolder.ProgramFiles, Environment.SpecialFolder.ProgramFilesX86 })
        {
            var baseDir = Environment.GetFolderPath(root);
            var probe = Path.Combine(baseDir, "Microsoft SQL Server Management Studio");
            if (!Directory.Exists(probe))
                continue;
            try
            {
                if (Directory.EnumerateFiles(probe, "Ssms.exe", SearchOption.AllDirectories).Any())
                    return true;
            }
            catch
            {
                // ignore
            }
        }

#pragma warning disable CA1416
        if (RegistrySsmsPresent(Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall")))
            return true;
        return RegistrySsmsPresent(Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"));
#pragma warning restore CA1416
    }

    private static bool RegistrySsmsPresent(RegistryKey? uninstallRoot)
    {
#pragma warning disable CA1416
        if (uninstallRoot == null)
            return false;
        foreach (var name in uninstallRoot.GetSubKeyNames())
        {
            using var sub = uninstallRoot.OpenSubKey(name);
            var dn = sub?.GetValue("DisplayName") as string;
            if (dn != null && dn.Contains("SQL Server Management Studio", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
#pragma warning restore CA1416
    }

    private static void Log(string message)
    {
        Trace.WriteLine("[Accounting.Sql] " + message);
        Console.Error.WriteLine("[Accounting.Sql] " + message);
    }
}
