using System.Diagnostics;
using System.Reflection;

namespace Accounting.Bootstrapper;

internal sealed class MainForm : Form
{
    private readonly TextBox _destBox;
    private readonly TextBox _logBox;
    private readonly Button _browseBtn;
    private readonly Button _redistBtn;
    private readonly Button _installBtn;

    public MainForm()
    {
        Text = "Accounting — Offline setup";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(600, 500);
        Font = new Font("Segoe UI", 9f);

        var logo = new PictureBox
        {
            Location = new Point(16, 12),
            Size = new Size(64, 64),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent
        };
        try
        {
            var img = LoadEmbeddedLogo();
            if (img is not null)
                logo.Image = img;
        }
        catch { /* optional branding */ }

        var title = new Label
        {
            AutoSize = true,
            Location = new Point(88, 16),
            Text = "Offline bundle (no Internet required on the target PC)",
            Font = new Font(Font, FontStyle.Bold)
        };

        var hint = new Label
        {
            AutoSize = false,
            Location = new Point(88, 44),
            Size = new Size(488, 60),
            Text = "This folder must contain AccountingInstaller\\ next to this exe (from build\\Build-OfflineBundle.ps1). " +
                   "Includes API, desktop client, and Setup.exe. Optional: place VC++ / SQL LocalDB installers in redist\\, then run step 1."
        };

        var lblDest = new Label { AutoSize = true, Location = new Point(16, 120), Text = "Install application to:" };
        _destBox = new TextBox
        {
            Location = new Point(16, 140),
            Width = 440,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        _browseBtn = new Button
        {
            Text = "Browse…",
            Location = new Point(468, 138),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _browseBtn.Click += (_, _) => BrowseDest();

        _redistBtn = new Button
        {
            Text = "1 — Install optional redistributables (from redist\\)",
            Location = new Point(16, 180),
            Width = 360,
            Height = 32
        };
        _redistBtn.Click += async (_, _) => await RunRedistributablesAsync();

        _installBtn = new Button
        {
            Text = "2 — Copy Accounting and run configuration",
            Location = new Point(384, 180),
            Width = 200,
            Height = 32,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _installBtn.Click += async (_, _) => await RunInstallAsync();

        var lblLog = new Label { AutoSize = true, Location = new Point(16, 226), Text = "Log:" };
        _logBox = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Location = new Point(16, 246),
            Size = new Size(560, 200),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };

        Controls.AddRange(new Control[]
        {
            logo, title, hint, lblDest, _destBox, _browseBtn, _redistBtn, _installBtn, lblLog, _logBox
        });

        Load += (_, _) =>
        {
            var payload = ResolvePayloadDirectory();
            if (payload is null)
            {
                Log("ERROR: AccountingInstaller folder not found next to this executable.");
                Log("Expected: same folder as AccountingOfflineSetup.exe\\AccountingInstaller\\");
                _installBtn.Enabled = false;
            }
            else
            {
                Log($"Found payload: {payload}");
            }

            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            _destBox.Text = Path.Combine(programFiles, "AccountingApi");
        };
    }

    private static Image? LoadEmbeddedLogo()
    {
        var asm = Assembly.GetExecutingAssembly();
        foreach (var name in asm.GetManifestResourceNames())
        {
            if (!name.EndsWith("CharlzTechLogo.png", StringComparison.OrdinalIgnoreCase))
                continue;
            using var s = asm.GetManifestResourceStream(name);
            if (s is not null)
                return new Bitmap(s);
        }

        return null;
    }

    private static string? ResolvePayloadDirectory()
    {
        var baseDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
        var a = Path.Combine(baseDir, "AccountingInstaller");
        if (Directory.Exists(a) && File.Exists(Path.Combine(a, "Accounting.Api.exe")))
            return a;
        var parent = Directory.GetParent(baseDir)?.FullName;
        if (parent is not null)
        {
            var b = Path.Combine(parent, "AccountingInstaller");
            if (Directory.Exists(b) && File.Exists(Path.Combine(b, "Accounting.Api.exe")))
                return b;
        }

        return null;
    }

    private static string? ResolveRedistDirectory()
    {
        var baseDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
        var a = Path.Combine(baseDir, "redist");
        if (Directory.Exists(a))
            return a;
        var parent = Directory.GetParent(baseDir)?.FullName;
        if (parent is not null)
        {
            var b = Path.Combine(parent, "redist");
            if (Directory.Exists(b))
                return b;
        }

        return null;
    }

    private void BrowseDest()
    {
        using var d = new FolderBrowserDialog { Description = "Select folder for Accounting API files" };
        if (d.ShowDialog(this) == DialogResult.OK)
            _destBox.Text = d.SelectedPath;
    }

    private void Log(string line)
    {
        _logBox.AppendText(line + Environment.NewLine);
    }

    private async Task RunRedistributablesAsync()
    {
        _redistBtn.Enabled = false;
        try
        {
            var redist = ResolveRedistDirectory();
            if (redist is null || !Directory.EnumerateFileSystemEntries(redist).Any())
            {
                Log("redist\\ is empty or missing. Add installers here (see redist\\README.txt), then try again.");
                return;
            }

            foreach (var path in Directory.GetFiles(redist))
            {
                var name = Path.GetFileName(path);
                if (name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ||
                    name.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                    continue;

                Log($"Running: {name}");
                try
                {
                    if (path.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))
                    {
                        await RunProcessAsync("msiexec.exe", $"/i \"{path}\" /quiet /norestart", CancellationToken.None);
                    }
                    else if (path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        var args = name.Contains("VC", StringComparison.OrdinalIgnoreCase) ||
                                   name.Contains("redist", StringComparison.OrdinalIgnoreCase)
                            ? "/install /quiet /norestart"
                            : "/quiet";
                        await RunProcessAsync(path, args, CancellationToken.None);
                    }
                    else
                        Log($"  Skipped (unsupported type): {name}");
                }
                catch (Exception ex)
                {
                    Log($"  Error: {ex.Message}");
                }
            }

            Log("Redistributable step finished.");
        }
        finally
        {
            _redistBtn.Enabled = true;
        }
    }

    private static async Task RunProcessAsync(string fileName, string arguments, CancellationToken cancellationToken)
    {
        using var p = Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true
        });
        if (p is null)
            return;
        await p.WaitForExitAsync(cancellationToken);
    }

    private async Task RunInstallAsync()
    {
        var payload = ResolvePayloadDirectory();
        if (payload is null)
        {
            Log("Payload missing. Cannot install.");
            return;
        }

        var dest = _destBox.Text.Trim();
        if (string.IsNullOrEmpty(dest))
        {
            Log("Choose a destination folder.");
            return;
        }

        _installBtn.Enabled = false;
        try
        {
            Log($"Copying to {dest} ...");
            await Task.Run(() => CopyDirectory(payload, dest));
            Log("Copy finished.");

            var script = Path.Combine(dest, "Install-Accounting.ps1");
            if (!File.Exists(script))
            {
                Log("Install-Accounting.ps1 not found after copy.");
                return;
            }

            Log("Running Install-Accounting.ps1 (SQL Server / LocalDB config)...");
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments =
                    $"-NoProfile -ExecutionPolicy Bypass -File \"{script}\" -ApiPath \"{dest}\" -Provider SqlServer -SkipFirewall",
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = dest
            };
            using var proc = Process.Start(psi);
            if (proc is not null)
            {
                await proc.WaitForExitAsync();
                Log(proc.ExitCode == 0 ? "Configuration script completed OK." : $"Configuration script exit code: {proc.ExitCode}");
            }

            Log("Done. Start the API with Run-Accounting.cmd or Accounting.Api.exe");
        }
        catch (Exception ex)
        {
            Log(ex.ToString());
        }
        finally
        {
            _installBtn.Enabled = true;
        }
    }

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);
        foreach (var dir in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(sourceDir, dir);
            Directory.CreateDirectory(Path.Combine(destDir, rel));
        }

        foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(sourceDir, file);
            var destFile = Path.Combine(destDir, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
            File.Copy(file, destFile, true);
        }
    }
}
