using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Accounting.Application.DTOs;
using Accounting.Domain.Enums;
using Accounting.Desktop.Helpers;
using Accounting.Desktop.Services;

namespace Accounting.Desktop.Views;

public partial class UsersAndAgentsView : UserControl
{
    private readonly AccountingApiClient _api;

    public UsersAndAgentsView(AccountingApiClient api)
    {
        _api = api;
        InitializeComponent();
        Loaded += (_, _) => _ = RefreshAsync();
    }

    private sealed class UserGridRow
    {
        public int Id { get; init; }
        public string UserName { get; init; } = "";
        public string DisplayName { get; init; } = "";
        public string AccountKind { get; init; } = "";
        public bool IsActive { get; init; }
        public string RolesText { get; init; } = "";
    }

    private async Task RefreshAsync()
    {
        StatusText.Text = "Loading…";
        var list = await _api.GetFromJsonAsync<List<UserAccountListDto>>("api/users").ConfigureAwait(true);
        if (list is null)
        {
            StatusText.Text = "Failed to load.";
            Grid.ItemsSource = null;
            return;
        }

        Grid.ItemsSource = list.Select(u => new UserGridRow
        {
            Id = u.Id,
            UserName = u.UserName,
            DisplayName = u.DisplayName,
            AccountKind = u.AccountKind,
            IsActive = u.IsActive,
            RolesText = string.Join(", ", u.Roles)
        }).ToList();
        StatusText.Text = $"{list.Count} account(s).";
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => _ = RefreshAsync();

    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new UserAccountEditWindow(_api, null);
        if (dlg.ShowDialog() == true)
            await RefreshAsync().ConfigureAwait(true);
    }

    private async void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not UserGridRow row)
            return;
        var dlg = new UserAccountEditWindow(_api, row.Id);
        if (dlg.ShowDialog() == true)
            await RefreshAsync().ConfigureAwait(true);
    }

    private async void Deactivate_Click(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not UserGridRow row)
            return;
        if (MessageBox.Show($"Deactivate {row.UserName}?", "Confirm", MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
            return;
        var result = await _api.PostEmptyAsync($"api/users/{row.Id}/deactivate").ConfigureAwait(true);
        if (!result.Ok)
        {
            MessageBox.Show(result.ErrorMessage ?? "Failed.", "Deactivate", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await RefreshAsync().ConfigureAwait(true);
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not UserGridRow row)
            return;
        if (MessageBox.Show($"Permanently delete {row.UserName}? Sessions will be removed.", "Confirm", MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK)
            return;
        var result = await _api.DeleteAsync($"api/users/{row.Id}").ConfigureAwait(true);
        if (!result.Ok)
        {
            MessageBox.Show(result.ErrorMessage ?? "Failed.", "Delete", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await RefreshAsync().ConfigureAwait(true);
    }

    private void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        var w = Window.GetWindow(this);
        if (w != null)
            DataGridExportHelper.PromptExportCsv(Grid, w, "users");
    }

    private void ExportPdf_Click(object sender, RoutedEventArgs e)
    {
        var w = Window.GetWindow(this);
        if (w != null)
            DataGridExportHelper.PromptExportPdf(Grid, w, "users", "Users and agents");
    }
}
