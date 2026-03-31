using System.Diagnostics;

namespace Accounting.Setup;

internal sealed class MainForm : Form
{
    private readonly TextBox _pathBox;
    private readonly TextBox _serverBox;
    private readonly TextBox _logBox;
    private readonly Button _browseBtn;
    private readonly Button _installBtn;
    private readonly Button _launchBtn;
    private readonly Button _shortcutBtn;

    public MainForm()
    {
        Text = "Accounting — Setup";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(560, 420);
        Font = new Font("Segoe UI", 9f);

        var title = new Label
        {
            AutoSize = true,
            Location = new Point(16, 16),
            Text = "Install and configure the Accounting API (SQL Server for SSMS).",
            Font = new Font(Font, FontStyle.Bold)
        };

        var hint = new Label
        {
            AutoSize = false,
            Location = new Point(16, 44),
            Size = new Size(520, 36),
            Text = "Installation folder must contain Accounting.Api.exe (run Build-Setup.ps1 to build this bundle)."
        };

        var lblPath = new Label { AutoSize = true, Location = new Point(16, 88), Text = "Folder:" };
        _pathBox = new TextBox
        {
            Location = new Point(16, 108),
            Width = 420,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        _browseBtn = new Button
        {
            Text = "Browse…",
            Location = new Point(448, 106),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _browseBtn.Click += (_, _) => BrowseFolder();

        var lblServer = new Label { AutoSize = true, Location = new Point(16, 144), Text = "SQL Server instance (same as SSMS \"Server name\"):" };
        _serverBox = new TextBox
        {
            Location = new Point(16, 164),
            Width = 520,
            Text = @"(localdb)\mssqllocaldb",
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        _installBtn = new Button
        {
            Text = "1 — Run install (config + firewall)",
            Location = new Point(16, 200),
            Width = 240,
            Height = 32
        };
        _installBtn.Click += async (_, _) => await RunInstallAsync();

        _launchBtn = new Button
        {
            Text = "2 — Start Accounting API",
            Location = new Point(264, 200),
            Width = 200,
            Height = 32
        };
        _launchBtn.Click += (_, _) => LaunchApi();

        _shortcutBtn = new Button
        {
            Text = "Desktop shortcut",
            Location = new Point(472, 200),
            Width = 64,
            Height = 32,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _shortcutBtn.Click += (_, _) => CreateDesktopShortcut();

        var lblLog = new Label { AutoSize = true, Location = new Point(16, 244), Text = "Log:" };
        _logBox = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Location = new Point(16, 264),
            Size = new Size(520, 100),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };

        var footer = new Label
        {
            AutoSize = false,
            Location = new Point(16, 372),
            Size = new Size(520, 40),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            ForeColor = SystemColors.GrayText,
            Text = "After install, open SSMS with the same server name as above; see Connect-with-SSMS.txt."
        };

        Controls.AddRange(new Control[]
        {
            title, hint, lblPath, _pathBox, _browseBtn, lblServer, _serverBox,
            _installBtn, _launchBtn, _shortcutBtn, lblLog, _logBox, footer
        });

        Load += (_, _) =>
        {
            var baseDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
            _pathBox.Text = baseDir;
        };
    }

    private void BrowseFolder()
    {
        using var d = new FolderBrowserDialog
        {
            SelectedPath = string.IsNullOrWhiteSpace(_pathBox.Text) ? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) : _pathBox.Text,
            Description = "Select folder containing Accounting.Api.exe"
        };
        if (d.ShowDialog(this) == DialogResult.OK)
            _pathBox.Text = d.SelectedPath;
    }

    private string InstallPath => _pathBox.Text.Trim().TrimEnd('\\');

    private void Log(string line)
    {
        _logBox.AppendText(line + Environment.NewLine);
    }

    private async Task RunInstallAsync()
    {
        _installBtn.Enabled = false;
        _logBox.Clear();
        try
        {
            var root = InstallPath;
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            {
                Log("Invalid folder.");
                return;
            }

            var api = Path.Combine(root, "Accounting.Api.exe");
            if (!File.Exists(api))
            {
                Log("Accounting.Api.exe not found in this folder. Choose the folder produced by Build-Setup.ps1.");
                return;
            }

            var script = Path.Combine(root, "Install-Accounting.ps1");
            if (!File.Exists(script))
            {
                Log("Install-Accounting.ps1 missing. Re-run Build-Setup.ps1 or copy files from install\\.");
                return;
            }

            var server = _serverBox.Text.Trim();
            if (string.IsNullOrEmpty(server))
            {
                Log("Enter SQL Server name.");
                return;
            }

            Log("Running PowerShell installer…");
            var args = $"-NoProfile -ExecutionPolicy Bypass -File \"{script}\" -Provider SqlServer -ApiPath \"{root}\" -Server \"{server}\"";
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = root
            };

            using var proc = Process.Start(psi);
            if (proc is null)
            {
                Log("Could not start PowerShell.");
                return;
            }

            var stdout = await proc.StandardOutput.ReadToEndAsync();
            var stderr = await proc.StandardError.ReadToEndAsync();
            await proc.WaitForExitAsync();

            if (!string.IsNullOrWhiteSpace(stdout))
                Log(stdout.TrimEnd());
            if (!string.IsNullOrWhiteSpace(stderr))
                Log(stderr.TrimEnd());
            Log(proc.ExitCode == 0 ? "Install script finished OK." : $"Install script exited with code {proc.ExitCode}.");
        }
        catch (Exception ex)
        {
            Log(ex.Message);
        }
        finally
        {
            _installBtn.Enabled = true;
        }
    }

    private void LaunchApi()
    {
        var root = InstallPath;
        var cmd = Path.Combine(root, "Run-Accounting.cmd");
        var api = Path.Combine(root, "Accounting.Api.exe");
        try
        {
            if (File.Exists(cmd))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = cmd,
                    WorkingDirectory = root,
                    UseShellExecute = true
                });
                Log("Started Run-Accounting.cmd");
            }
            else if (File.Exists(api))
            {
                var psi = new ProcessStartInfo
                {
                    FileName = api,
                    WorkingDirectory = root,
                    UseShellExecute = false
                };
                psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
                Process.Start(psi);
                Log("Started Accounting.Api.exe");
            }
            else
                Log("Run-Accounting.cmd / Accounting.Api.exe not found.");
        }
        catch (Exception ex)
        {
            Log(ex.Message);
        }
    }

    private void CreateDesktopShortcut()
    {
        var root = InstallPath;
        var cmd = Path.Combine(root, "Run-Accounting.cmd");
        if (!File.Exists(cmd))
        {
            Log("Run-Accounting.cmd not found.");
            return;
        }

        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var lnk = Path.Combine(desktop, "Accounting API.lnk");

        var psPath = Path.Combine(Path.GetTempPath(), "accounting-shortcut-" + Guid.NewGuid().ToString("N") + ".ps1");
        var script = $"""
            $WshShell = New-Object -ComObject WScript.Shell
            $sc = $WshShell.CreateShortcut('{lnk.Replace("'", "''")}')
            $sc.TargetPath = '{cmd.Replace("'", "''")}'
            $sc.WorkingDirectory = '{root.Replace("'", "''")}'
            $sc.Save()
            """;
        try
        {
            File.WriteAllText(psPath, script);
            using var p = Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{psPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = root
            });
            p?.WaitForExit();
            Log($"Shortcut created: {lnk}");
        }
        catch (Exception ex)
        {
            Log(ex.Message);
        }
        finally
        {
            try { File.Delete(psPath); } catch { /* ignore */ }
        }
    }
}
