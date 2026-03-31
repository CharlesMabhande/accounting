using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Accounting.Application.DTOs;
using Accounting.Desktop.Helpers;
using Accounting.Desktop.Services;

namespace Accounting.Desktop.Views;

public partial class RolesPermissionsView : UserControl
{
    private readonly AccountingApiClient _api;

    public RolesPermissionsView(AccountingApiClient api)
    {
        _api = api;
        InitializeComponent();
        Loaded += (_, _) => _ = RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        StatusText.Text = "Loading…";
        var list = await _api.GetFromJsonAsync<List<RoleListDto>>("api/roles").ConfigureAwait(true);
        Grid.ItemsSource = list;
        StatusText.Text = list is null ? "Failed." : $"{list.Count} role(s).";
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => _ = RefreshAsync();

    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new RoleEditWindow(_api, null);
        if (dlg.ShowDialog() == true)
            await RefreshAsync().ConfigureAwait(true);
    }

    private async void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not RoleListDto row)
            return;
        var dlg = new RoleEditWindow(_api, row.Id);
        if (dlg.ShowDialog() == true)
            await RefreshAsync().ConfigureAwait(true);
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not RoleListDto row)
            return;
        if (MessageBox.Show($"Delete role {row.Name}?", "Confirm", MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
            return;
        var result = await _api.DeleteAsync($"api/roles/{row.Id}").ConfigureAwait(true);
        if (!result.Ok)
            MessageBox.Show(result.ErrorMessage ?? "Failed.", "Delete", MessageBoxButton.OK, MessageBoxImage.Warning);
        else
            await RefreshAsync().ConfigureAwait(true);
    }

    private void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        var w = Window.GetWindow(this);
        if (w != null)
            DataGridExportHelper.PromptExportCsv(Grid, w, "roles");
    }

    private void ExportPdf_Click(object sender, RoutedEventArgs e)
    {
        var w = Window.GetWindow(this);
        if (w != null)
            DataGridExportHelper.PromptExportPdf(Grid, w, "roles", "Roles and permissions");
    }
}
