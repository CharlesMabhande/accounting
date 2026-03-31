using System.Windows;
using System.Windows.Controls;
using Accounting.Desktop.Helpers;
using Accounting.Desktop.Services;
using Accounting.Desktop.Shell;
using Microsoft.Win32;

namespace Accounting.Desktop.Views;

public partial class BackupRestoreView : UserControl
{
    private readonly AccountingApiClient _api;

    public BackupRestoreView(AccountingApiClient api)
    {
        _api = api;
        InitializeComponent();
        Loaded += (_, _) => _ = LoadBackupHintsAsync();
    }

    private async Task LoadBackupHintsAsync()
    {
        var local = AppPaths.LocalBackupFolder;
        BackupPathHint.Text =
            $"Local default folder for dialogs: {local}\n(Loading server path from API…)";
        var server = await _api.GetBackupFolderAsync().ConfigureAwait(true);
        if (server is { Path: { Length: > 0 } p })
            BackupPathHint.Text =
                $"Local default folder for save/open dialogs: {local}\nServer backup folder (API): {p}";
        else
            BackupPathHint.Text =
                $"Local default folder for save/open dialogs: {local}\nServer backup path: (not available — sign in with backup permission or check API).";
    }

    private async void ExportJson_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog
        {
            Filter = "ZIP archive (*.zip)|*.zip|All files (*.*)|*.*",
            FileName = $"accounting-export-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip",
            InitialDirectory = AppPaths.LocalBackupFolder
        };
        if (dlg.ShowDialog() != true)
            return;

        StatusText.Text = "Exporting…";
        LogBox.Text = "";
        var res = await _api.DownloadBackupFileAsync("api/database-backup/export-json", dlg.FileName).ConfigureAwait(true);
        if (!res.Ok)
        {
            StatusText.Text = "Export failed.";
            LogBox.Text = res.ErrorMessage ?? "Unknown error.";
            return;
        }

        StatusText.Text = "Export saved.";
        LogBox.Text = dlg.FileName;
    }

    private async void ImportJson_Click(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBox.Show(
            "Import will delete existing data and replace it from the ZIP. Continue?",
            "Confirm import",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes)
            return;

        var dlg = new OpenFileDialog
        {
            Filter = "ZIP archive (*.zip)|*.zip|All files (*.*)|*.*"
        };
        if (dlg.ShowDialog() != true)
            return;

        StatusText.Text = "Importing…";
        LogBox.Text = "";
        var res = await _api.ImportBackupZipAsync(dlg.FileName).ConfigureAwait(true);
        if (!res.Ok || res.Value is null || !res.Value.Ok)
        {
            StatusText.Text = "Import failed.";
            LogBox.Text = res.ErrorMessage ?? res.Value?.ErrorMessage ?? "Unknown error.";
            return;
        }

        StatusText.Text = "Import completed.";
        LogBox.Text = "Database was updated from the archive. You may need to refresh open screens.";
    }

    private async void NativeBackup_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog
        {
            Filter = "Database (*.db;*.bak)|*.db;*.bak|All files (*.*)|*.*",
            FileName = $"accounting-native-{DateTime.UtcNow:yyyyMMdd-HHmmss}.db",
            InitialDirectory = AppPaths.LocalBackupFolder
        };
        if (dlg.ShowDialog() != true)
            return;

        StatusText.Text = "Downloading…";
        LogBox.Text = "";
        var res = await _api.DownloadBackupFileAsync("api/database-backup/native", dlg.FileName).ConfigureAwait(true);
        if (!res.Ok)
        {
            StatusText.Text = "Native backup failed.";
            LogBox.Text = res.ErrorMessage ?? "Unknown error.";
            return;
        }

        StatusText.Text = "Saved.";
        LogBox.Text = dlg.FileName;
    }

    private void SaveLog_Click(object sender, RoutedEventArgs e)
    {
        var w = Window.GetWindow(this);
        if (w != null)
        {
            var sb = (StatusText.Text ?? "") + "\r\n" + (LogBox.Text ?? "");
            TextExportHelper.PromptSaveText(w, sb, "backup-restore-log");
        }
    }
}
