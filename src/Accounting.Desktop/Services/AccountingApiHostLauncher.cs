using System.Diagnostics;
using System.IO;
using System.Net.Http;

namespace Accounting.Desktop.Services;

/// <summary>
/// Ensures the Accounting API is running when the desktop targets a local URL (installed layout).
/// </summary>
internal static class AccountingApiHostLauncher
{
    private static readonly TimeSpan HealthPollInterval = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan HealthHttpTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan LocalStartupTimeout = TimeSpan.FromSeconds(180);

    public static bool IsLocalApiBase(string baseUrl)
    {
        if (!Uri.TryCreate(baseUrl.Trim(), UriKind.Absolute, out var u))
            return false;
        var localHost = string.Equals(u.Host, "localhost", StringComparison.OrdinalIgnoreCase)
            || string.Equals(u.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(u.Host, "::1", StringComparison.OrdinalIgnoreCase);
        return (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps) && localHost;
    }

    public static async Task<bool> IsHealthyAsync(string baseUrl)
    {
        try
        {
            using var http = new HttpClient { Timeout = HealthHttpTimeout };
            var url = baseUrl.TrimEnd('/') + "/api/health";
            var r = await http.GetAsync(url).ConfigureAwait(false);
            return r.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// If <paramref name="baseUrl"/> is local, starts the API from the install folder when needed and waits for health.
    /// </summary>
    public static async Task<bool> EnsureLocalApiRunningAsync(string baseUrl, Action<string>? status = null)
    {
        if (!IsLocalApiBase(baseUrl))
            return true;

        if (await IsHealthyAsync(baseUrl).ConfigureAwait(false))
        {
            status?.Invoke("API is ready.");
            return true;
        }

        var root = ResolveInstallRoot();
        if (root is null)
        {
            status?.Invoke("Could not find the API in the install folder (use the full AccountingInstaller layout).");
            return false;
        }

        status?.Invoke("Starting Accounting API…");
        StartApiProcess(root);

        var deadline = DateTime.UtcNow + LocalStartupTimeout;
        while (DateTime.UtcNow < deadline)
        {
            if (await IsHealthyAsync(baseUrl).ConfigureAwait(false))
            {
                status?.Invoke("API is ready.");
                return true;
            }

            await Task.Delay(HealthPollInterval).ConfigureAwait(false);
        }

        status?.Invoke("The API did not respond in time.");
        return false;
    }

    /// <summary>
    /// Published layout: <c>…\Desktop\Accounting.Desktop.exe</c> with API in the parent folder.
    /// </summary>
    private static string? ResolveInstallRoot()
    {
        var baseDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var parent = Path.GetFullPath(Path.Combine(baseDir, ".."));
        if (File.Exists(Path.Combine(parent, "Run-Accounting.cmd")) || File.Exists(Path.Combine(parent, "Accounting.Api.exe")))
            return parent;

        if (File.Exists(Path.Combine(baseDir, "Run-Accounting.cmd")) || File.Exists(Path.Combine(baseDir, "Accounting.Api.exe")))
            return baseDir;

        return null;
    }

    private static void StartApiProcess(string root)
    {
        var runCmd = Path.Combine(root, "Run-Accounting.cmd");
        if (File.Exists(runCmd))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = runCmd,
                WorkingDirectory = root,
                UseShellExecute = true
            });
            return;
        }

        var api = Path.Combine(root, "Accounting.Api.exe");
        if (!File.Exists(api))
            return;

        var psi = new ProcessStartInfo
        {
            FileName = api,
            WorkingDirectory = root,
            UseShellExecute = false
        };
        foreach (System.Collections.DictionaryEntry e in Environment.GetEnvironmentVariables())
        {
            var key = e.Key?.ToString();
            if (string.IsNullOrEmpty(key))
                continue;
            try
            {
                psi.Environment[key] = e.Value?.ToString() ?? string.Empty;
            }
            catch
            {
                /* skip invalid keys */
            }
        }

        psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
        Process.Start(psi);
    }
}
