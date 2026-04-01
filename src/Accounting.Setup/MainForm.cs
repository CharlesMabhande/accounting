using System.Diagnostics;
using System.Net.Http;
using System.Reflection;

namespace Accounting.Setup;

internal sealed class MainForm : Form
{
    /// <summary>Loopback avoids Windows resolving "localhost" to ::1 while the host only listened on IPv4.</summary>
    private const string ProductionApiBase = "http://127.0.0.1:8080";

    private readonly Panel[] _steps;
    private int _step;

    private readonly TextBox _pathBox;
    private readonly CheckBox _copyFromMedia;
    private readonly TextBox _connectionStringBox;
    private readonly TextBox _logBox;
    private readonly ProgressBar _progress;
    private readonly Button _btnBack;
    private readonly Button _btnNext;
    private readonly Button _btnCancel;
    private readonly Label _finishHint;
    private FlowLayoutPanel _finishActions = null!;

    public MainForm()
    {
        Text = "CharlzTech Accounting — Setup";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(640, 560);
        Font = new Font("Segoe UI", 9f);
        DoubleBuffered = true;

        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 76,
            Padding = new Padding(16, 12, 16, 8),
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.White
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var logo = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent
        };
        try
        {
            var img = LoadEmbeddedLogo();
            if (img is not null)
                logo.Image = img;
        }
        catch { /* optional */ }

        var titleBlock = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        titleBlock.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        titleBlock.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var title = new Label
        {
            Text = "Accounting Setup",
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            AutoSize = true,
            ForeColor = Color.FromArgb(32, 32, 32)
        };
        var subtitle = new Label
        {
            Text = "Step 1: Welcome  →  Step 2: Install folder + SQL connection (SSMS)  →  Step 3: Automatic configuration & startup.",
            AutoSize = true,
            ForeColor = SystemColors.GrayText
        };
        titleBlock.Controls.Add(title, 0, 0);
        titleBlock.Controls.Add(subtitle, 0, 1);
        header.Controls.Add(logo, 0, 0);
        header.Controls.Add(titleBlock, 1, 0);

        var contentHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16), AutoScroll = true };

        _pathBox = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top };
        _copyFromMedia = new CheckBox
        {
            Text = "Copy all files from this setup folder to the folder above (use for USB or zip installs to another location).",
            AutoSize = true,
            Checked = true
        };
        _connectionStringBox = new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Height = 120,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Font = new Font("Consolas", 9f),
            Text =
                "Server=(localdb)\\mssqllocaldb;Database=AccountingDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
        };
        _finishHint = new Label
        {
            Text = "Installation will configure the database, start the API, wait until it is ready, then start the desktop app.",
            AutoSize = false,
            Size = new Size(560, 48),
            ForeColor = Color.FromArgb(48, 48, 48)
        };
        _logBox = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            Height = 220,
            Font = new Font("Consolas", 9f),
            BackColor = Color.FromArgb(250, 250, 250)
        };
        _progress = new ProgressBar
        {
            Style = ProgressBarStyle.Marquee,
            MarqueeAnimationSpeed = 30,
            Height = 22,
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            Visible = false
        };

        _steps = new[]
        {
            BuildStepWelcome(),
            BuildStepConfigure(),
            BuildStepProgress(),
            BuildStepFinish()
        };

        foreach (var p in _steps)
        {
            p.Visible = false;
            p.Dock = DockStyle.Fill;
            contentHost.Controls.Add(p);
        }

        var nav = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 52,
            Padding = new Padding(16, 8, 16, 12),
            ColumnCount = 3
        };
        nav.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        nav.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        nav.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));

        _btnBack = new Button { Text = "Back", Anchor = AnchorStyles.Left, Width = 100 };
        _btnNext = new Button { Text = "Next", Anchor = AnchorStyles.Right, Width = 100 };
        _btnCancel = new Button { Text = "Cancel", Anchor = AnchorStyles.Right, Width = 100 };
        _btnBack.Click += (_, _) => GoBack();
        _btnNext.Click += (_, _) => GoNext();
        _btnCancel.Click += (_, _) => Close();
        nav.Controls.Add(_btnBack, 0, 0);
        nav.Controls.Add(_btnCancel, 1, 0);
        nav.Controls.Add(_btnNext, 2, 0);

        Controls.Add(contentHost);
        Controls.Add(nav);
        Controls.Add(header);

        Load += (_, _) =>
        {
            var baseDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
            var suggested = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Accounting");
            _pathBox.Text = suggested;
            _copyFromMedia.Checked = true;

            var bundledApi = Path.Combine(baseDir, "Accounting.Api.exe");
            if (File.Exists(bundledApi))
            {
                _pathBox.Text = baseDir;
                _copyFromMedia.Checked = false;
            }
            else
            {
                try
                {
                    if (!Directory.Exists(suggested))
                        Directory.CreateDirectory(suggested);
                }
                catch
                {
                    _pathBox.Text = baseDir;
                    _copyFromMedia.Checked = false;
                }
            }

            ShowStep(0);
        };
    }

    private Panel BuildStepWelcome()
    {
        var p = new Panel { Padding = new Padding(0, 8, 0, 0) };
        var body = new Label
        {
            AutoSize = false,
            Width = 560,
            Height = 280,
            Text =
                "This is a one-click, fully bundled setup:\r\n\r\n" +
                " • Self-contained API and desktop (no separate .NET runtime install)\r\n" +
                " • You only paste your SQL Server connection string (same as SSMS)\r\n" +
                " • Setup writes configuration, starts the API, opens the login window, and registers the API for Windows sign-in\r\n\r\n" +
                "Prerequisites: SQL Server Express, LocalDB, or SQL Server (SSMS optional).\r\n\r\n" +
                "Click Next, choose where to install (or keep the default), paste your connection string, then click Install.",
            ForeColor = Color.FromArgb(48, 48, 48)
        };
        p.Controls.Add(body);
        return p;
    }

    /// <summary>Single step: install folder + SSMS connection string (only user input required).</summary>
    private Panel BuildStepConfigure()
    {
        var p = new Panel { AutoScroll = true };
        var y = 0;
        var lblFolder = new Label
        {
            Text = "Where to install",
            Font = new Font(Font, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(0, y)
        };
        y += 26;
        var hintFolder = new Label
        {
            Text = "Choose where the program files will live. Default is under your user profile (no Administrator needed). If you opened setup from the full installer folder, files usually stay in that same folder. Turn on \"Copy…\" only when installing from USB or a zip to a different drive or folder.",
            AutoSize = false,
            Size = new Size(560, 48),
            Location = new Point(0, y),
            ForeColor = SystemColors.GrayText
        };
        y += 54;
        _pathBox.Location = new Point(0, y);
        _pathBox.Width = 470;
        var browse = new Button { Text = "Browse…", Location = new Point(478, y - 4), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        browse.Click += (_, _) => BrowseFolder();
        y += 40;
        _copyFromMedia.Location = new Point(0, y);
        y += 40;

        var lblSql = new Label
        {
            Text = "SQL Server connection (SSMS) — required",
            Font = new Font(Font, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(0, y)
        };
        y += 26;
        var hintSql = new Label
        {
            Text =
                "Paste the same connection string you use in SQL Server Management Studio (Server=...;Database=...;Trusted_Connection=True;...).",
            AutoSize = false,
            Size = new Size(560, 40),
            Location = new Point(0, y),
            ForeColor = SystemColors.GrayText
        };
        y += 46;
        var lblConn = new Label { Text = "Connection string:", AutoSize = true, Location = new Point(0, y) };
        y += 22;
        _connectionStringBox.Location = new Point(0, y);
        _connectionStringBox.Width = 560;
        _connectionStringBox.Height = 120;
        _connectionStringBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        p.Controls.AddRange(new Control[] { lblFolder, hintFolder, _pathBox, browse, _copyFromMedia, lblSql, hintSql, lblConn, _connectionStringBox });
        return p;
    }

    private Panel BuildStepProgress()
    {
        var p = new Panel();
        var lbl = new Label
        {
            Text = "Installing and starting services…",
            Font = new Font(Font, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(0, 0)
        };
        _progress.Location = new Point(0, 32);
        _progress.Width = 560;
        _logBox.Location = new Point(0, 64);
        _logBox.Width = 560;
        _logBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        p.Controls.AddRange(new Control[] { lbl, _progress, _logBox });
        return p;
    }

    private Panel BuildStepFinish()
    {
        var p = new Panel();
        _finishHint.Location = new Point(0, 0);
        _finishHint.Width = 560;
        _finishActions = new FlowLayoutPanel
        {
            Location = new Point(0, 56),
            Width = 560,
            Height = 200,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true
        };
        _finishActions.Controls.Add(MakeFinishButton("Start Accounting API", () => LaunchApi()));
        _finishActions.Controls.Add(MakeFinishButton("Start Accounting Desktop", () => LaunchDesktop()));
        _finishActions.Controls.Add(MakeFinishButton("Create desktop shortcuts", () => CreateDesktopShortcuts()));
        _finishActions.Controls.Add(MakeFinishButton("Open API documentation (Swagger)", () => OpenSwagger()));
        p.Controls.Add(_finishHint);
        p.Controls.Add(_finishActions);
        return p;
    }

    private static Button MakeFinishButton(string text, Action onClick)
    {
        var b = new Button { Text = text, AutoSize = true, Padding = new Padding(16, 8, 16, 8), Margin = new Padding(0, 0, 0, 8) };
        b.Click += (_, _) => onClick();
        return b;
    }

    private void ShowStep(int index)
    {
        _step = index;
        for (var i = 0; i < _steps.Length; i++)
            _steps[i].Visible = i == index;

        _btnBack.Enabled = index == 1;
        _btnNext.Visible = index <= 1;
        _btnNext.Text = index == 1 ? "Install" : "Next";
        _btnCancel.Text = index == 3 ? "Close" : "Cancel";
        _btnCancel.Visible = true;

        if (index == 2)
        {
            _progress.Visible = true;
            _progress.Style = ProgressBarStyle.Marquee;
            _btnNext.Visible = false;
        }
        else
        {
            _progress.Visible = false;
        }
    }

    private void GoBack()
    {
        if (_step != 1)
            return;
        ShowStep(0);
    }

    private async void GoNext()
    {
        switch (_step)
        {
            case 0:
                ShowStep(1);
                break;
            case 1:
                if (!TryEnsureInstallLocation())
                    return;
                if (!TryValidateConnectionString())
                    return;
                ShowStep(2);
                _btnNext.Enabled = false;
                _btnBack.Enabled = false;
                var scriptOk = await RunInstallAsync();
                var automationOk = false;
                if (scriptOk)
                    automationOk = await PostInstallAutomationAsync();
                _progress.Style = ProgressBarStyle.Blocks;
                _progress.Value = 100;
                _btnNext.Enabled = true;
                _btnBack.Enabled = false;
                if (scriptOk)
                {
                    try
                    {
                        InstallRegistration.RegisterInstall(InstallPath);
                        Log("Registered in Settings → Apps (Programs and Features).");
                    }
                    catch (Exception ex)
                    {
                        Log("Programs and Features registration failed: " + ex.Message);
                    }
                }

                if (scriptOk && automationOk)
                {
                    _finishHint.Text =
                        "Installation complete. The API was verified on port 8080, the desktop should be running, and the API is set to start when you sign in to Windows.";
                    _finishActions.Visible = false;
                }
                else if (scriptOk)
                {
                    _finishHint.Text =
                        "Configuration was written, but the API did not respond in time or automation failed. Use the buttons below to start the API, then sign in on the desktop.";
                    _finishActions.Visible = true;
                }
                else
                {
                    _finishHint.Text =
                        "Configuration failed. Fix the connection string or SQL Server, then run this setup again.";
                    _finishActions.Visible = true;
                }

                ShowStep(3);
                break;
        }
    }

    private bool TryEnsureInstallLocation()
    {
        var dest = _pathBox.Text.Trim().TrimEnd('\\');
        if (string.IsNullOrWhiteSpace(dest))
        {
            MessageBox.Show(this, "Choose an installation folder.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        try
        {
            Directory.CreateDirectory(dest);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Cannot create or access that folder:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        var source = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
        var same = string.Equals(Path.GetFullPath(dest), Path.GetFullPath(source), StringComparison.OrdinalIgnoreCase);

        if (_copyFromMedia.Checked && !same)
        {
            try
            {
                CopyInstallPayload(source, dest);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "Could not copy files to the destination.\n\n" + ex.Message +
                    "\n\nTry: run Setup as Administrator for Program Files, choose a folder under your user profile, or copy the AccountingInstaller folder manually.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        if (!File.Exists(Path.Combine(dest, "Accounting.Api.exe")))
        {
            MessageBox.Show(this,
                "Accounting.Api.exe was not found in:\n" + dest +
                "\n\nEnable \"Copy all files from this setup folder\" or select the folder that contains the full installer output (AccountingInstaller).",
                Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        _pathBox.Text = dest;
        return true;
    }

    private static void CopyInstallPayload(string sourceDir, string destDir)
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
            File.Copy(file, destFile, overwrite: true);
        }
    }

    private void BrowseFolder()
    {
        using var d = new FolderBrowserDialog
        {
            SelectedPath = string.IsNullOrWhiteSpace(_pathBox.Text)
                ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
                : _pathBox.Text,
            Description = "Select folder for Accounting (API + Desktop)"
        };
        if (d.ShowDialog(this) == DialogResult.OK)
            _pathBox.Text = d.SelectedPath;
    }

    private string InstallPath => _pathBox.Text.Trim().TrimEnd('\\');

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

    private void Log(string line)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => Log(line));
            return;
        }

        _logBox.AppendText(line + Environment.NewLine);
    }

    private bool TryValidateConnectionString()
    {
        var s = _connectionStringBox.Text.Trim();
        if (string.IsNullOrEmpty(s))
        {
            MessageBox.Show(this, "Paste your SQL Server connection string.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        if (!s.Contains('=', StringComparison.Ordinal) || !s.Contains("Server", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show(this,
                "That does not look like a connection string. Include at least Server=... and Database=... (same format as SSMS).",
                Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        return true;
    }

    private async Task<bool> RunInstallAsync()
    {
        _logBox.Clear();
        string? connFile = null;
        try
        {
            var root = InstallPath;
            var script = Path.Combine(root, "Install-Accounting.ps1");
            if (!File.Exists(script))
            {
                Log("Install-Accounting.ps1 missing in the installation folder.");
                return false;
            }

            var conn = _connectionStringBox.Text.Trim();
            connFile = Path.Combine(Path.GetTempPath(), "accounting-conn-" + Guid.NewGuid().ToString("N") + ".txt");
            await File.WriteAllTextAsync(connFile, conn, System.Text.Encoding.UTF8);

            Log("Writing configuration (PowerShell)…");
            var args =
                $"-NoProfile -ExecutionPolicy Bypass -File \"{script}\" -Provider SqlServer -ApiPath \"{root}\" -ConnectionStringFile \"{connFile}\" -SkipSqlProbe";
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
                return false;
            }

            var stdout = await proc.StandardOutput.ReadToEndAsync();
            var stderr = await proc.StandardError.ReadToEndAsync();
            await proc.WaitForExitAsync();

            if (!string.IsNullOrWhiteSpace(stdout))
                Log(stdout.TrimEnd());
            if (!string.IsNullOrWhiteSpace(stderr))
                Log(stderr.TrimEnd());
            var ok = proc.ExitCode == 0;
            Log(ok ? "Configuration finished successfully." : $"Configuration exited with code {proc.ExitCode}.");
            return ok;
        }
        catch (Exception ex)
        {
            Log(ex.Message);
            return false;
        }
        finally
        {
            if (connFile is not null)
            {
                try { File.Delete(connFile); } catch { /* ignore */ }
            }
        }
    }

    private async Task<bool> PostInstallAutomationAsync()
    {
        var root = InstallPath;
        try
        {
            if (await IsApiHealthyAsync())
            {
                Log("API already responding on port 8080.");
            }
            else
            {
                Log("Starting Accounting API (Production)…");
                StartApiProcess(root);
            }

            Log($"Waiting for {ProductionApiBase}/api/health …");
            var ok = await WaitForApiHealthyAsync(TimeSpan.FromSeconds(240));
            if (!ok)
            {
                Log("WARNING: The API did not become ready in time. Check SQL Server and firewall, then use Start Accounting API below.");
                return false;
            }

            Log("API is ready.");
            RegisterStartupShortcut(root);
            Log("Registered startup shortcut (API runs when you sign in to Windows).");

            var desktopExe = Path.Combine(root, "Desktop", "Accounting.Desktop.exe");
            if (File.Exists(desktopExe))
            {
                Log("Starting Accounting Desktop…");
                StartDesktopProcess(root);
            }

            return true;
        }
        catch (Exception ex)
        {
            Log(ex.Message);
            return false;
        }
    }

    private static async Task<bool> IsApiHealthyAsync()
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            var r = await http.GetAsync($"{ProductionApiBase}/api/health");
            return r.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> WaitForApiHealthyAsync(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var r = await http.GetAsync($"{ProductionApiBase}/api/health");
                if (r.IsSuccessStatusCode)
                    return true;
            }
            catch
            {
                /* retry */
            }

            await Task.Delay(500);
        }

        return false;
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

    private static void StartDesktopProcess(string root)
    {
        var desktopExe = Path.Combine(root, "Desktop", "Accounting.Desktop.exe");
        if (!File.Exists(desktopExe))
            return;
        Process.Start(new ProcessStartInfo
        {
            FileName = desktopExe,
            WorkingDirectory = Path.Combine(root, "Desktop"),
            UseShellExecute = true
        });
    }

    private static void RegisterStartupShortcut(string root)
    {
        var runCmd = Path.Combine(root, "Run-Accounting.cmd");
        if (!File.Exists(runCmd))
            return;
        var startup = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        var lnk = Path.Combine(startup, "Accounting API.lnk");
        var psPath = Path.Combine(Path.GetTempPath(), "accounting-startup-" + Guid.NewGuid().ToString("N") + ".ps1");
        var rootEsc = root.Replace("'", "''");
        var cmdEsc = runCmd.Replace("'", "''");
        var script = $"""
            $WshShell = New-Object -ComObject WScript.Shell
            $sc = $WshShell.CreateShortcut('{lnk.Replace("'", "''")}')
            $sc.TargetPath = '{cmdEsc}'
            $sc.WorkingDirectory = '{rootEsc}'
            $sc.Description = 'Accounting API'
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
        }
        finally
        {
            try { File.Delete(psPath); } catch { /* ignore */ }
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
            }
            else
            {
                MessageBox.Show(this, "Accounting.Api.exe not found.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LaunchDesktop()
    {
        var root = InstallPath;
        var desktopExe = Path.Combine(root, "Desktop", "Accounting.Desktop.exe");
        try
        {
            if (!File.Exists(desktopExe))
            {
                MessageBox.Show(this, "Accounting.Desktop.exe not found under Desktop\\ folder.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = desktopExe,
                WorkingDirectory = Path.Combine(root, "Desktop"),
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void OpenSwagger()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = $"{ProductionApiBase}/swagger",
                UseShellExecute = true
            });
        }
        catch
        {
            /* ignore */
        }
    }

    private void CreateDesktopShortcuts()
    {
        var root = InstallPath;
        var cmd = Path.Combine(root, "Run-Accounting.cmd");
        var desktopExe = Path.Combine(root, "Desktop", "Accounting.Desktop.exe");

        if (!File.Exists(cmd))
        {
            MessageBox.Show(this, "Run-Accounting.cmd not found.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var lnkApi = Path.Combine(desktop, "Accounting API.lnk");
        var lnkUi = Path.Combine(desktop, "Accounting Desktop.lnk");

        var psPath = Path.Combine(Path.GetTempPath(), "accounting-shortcut-" + Guid.NewGuid().ToString("N") + ".ps1");
        var rootEsc = root.Replace("'", "''");
        var cmdEsc = cmd.Replace("'", "''");
        var desktopEsc = desktopExe.Replace("'", "''");
        var script = $"""
            $WshShell = New-Object -ComObject WScript.Shell
            $sc = $WshShell.CreateShortcut('{lnkApi.Replace("'", "''")}')
            $sc.TargetPath = '{cmdEsc}'
            $sc.WorkingDirectory = '{rootEsc}'
            $sc.Save()
            """;
        if (File.Exists(desktopExe))
        {
            script += $"""

            $sc2 = $WshShell.CreateShortcut('{lnkUi.Replace("'", "''")}')
            $sc2.TargetPath = '{desktopEsc}'
            $sc2.WorkingDirectory = '{rootEsc}\Desktop'
            $sc2.Save()
            """;
        }

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
            MessageBox.Show(this,
                File.Exists(desktopExe)
                    ? "Shortcuts created on your desktop:\n• Accounting API\n• Accounting Desktop"
                    : "Shortcut created: Accounting API",
                Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            try { File.Delete(psPath); } catch { /* ignore */ }
        }
    }
}
