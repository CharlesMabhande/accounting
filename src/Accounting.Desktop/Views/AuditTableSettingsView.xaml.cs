using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Accounting.Application.DTOs;
using Accounting.Desktop.Helpers;
using Accounting.Desktop.Services;

namespace Accounting.Desktop.Views;

public partial class AuditTableSettingsView : UserControl
{
    private readonly AccountingApiClient _api;

    public AuditTableSettingsView(AccountingApiClient api)
    {
        _api = api;
        InitializeComponent();
        Loaded += (_, _) => _ = RefreshAsync();
    }

    private sealed class AuditRow
    {
        public string EntityTypeName { get; set; } = "";
        public bool IsEnabled { get; set; }
        public bool AuditInserts { get; set; }
        public bool AuditUpdates { get; set; }
        public bool AuditDeletes { get; set; }
    }

    private async Task RefreshAsync()
    {
        StatusText.Text = "Loading…";
        var list = await _api.GetFromJsonAsync<List<AuditTableSettingDto>>("api/audit/table-settings").ConfigureAwait(true);
        if (list is null)
        {
            StatusText.Text = "Failed to load.";
            Grid.ItemsSource = null;
            return;
        }

        Grid.ItemsSource = list.Select(x => new AuditRow
        {
            EntityTypeName = x.EntityTypeName,
            IsEnabled = x.IsEnabled,
            AuditInserts = x.AuditInserts,
            AuditUpdates = x.AuditUpdates,
            AuditDeletes = x.AuditDeletes
        }).ToList();
        StatusText.Text = $"{list.Count} entity type(s).";
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => _ = RefreshAsync();

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (Grid.ItemsSource is not IEnumerable<AuditRow> rows)
            return;
        var body = new SaveAuditTableSettingsRequest
        {
            Settings = rows.Select(r => new AuditTableSettingDto
            {
                EntityTypeName = r.EntityTypeName,
                IsEnabled = r.IsEnabled,
                AuditInserts = r.AuditInserts,
                AuditUpdates = r.AuditUpdates,
                AuditDeletes = r.AuditDeletes
            }).ToList()
        };
        var result = await _api.PutJsonNoContentAsync("api/audit/table-settings", body).ConfigureAwait(true);
        StatusText.Text = result.Ok ? "Saved." : (result.ErrorMessage ?? "Save failed.");
        if (!result.Ok)
            MessageBox.Show(result.ErrorMessage ?? "Save failed.", "Audit settings", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        var w = Window.GetWindow(this);
        if (w != null)
            DataGridExportHelper.PromptExportCsv(Grid, w, "audit-table-settings");
    }

    private void ExportPdf_Click(object sender, RoutedEventArgs e)
    {
        var w = Window.GetWindow(this);
        if (w != null)
            DataGridExportHelper.PromptExportPdf(Grid, w, "audit-table-settings", "Audit table settings");
    }
}
