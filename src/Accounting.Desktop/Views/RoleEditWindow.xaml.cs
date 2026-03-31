using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using Accounting.Application.Common;
using Accounting.Application.DTOs;
using Accounting.Desktop.Services;

namespace Accounting.Desktop.Views;

public partial class RoleEditWindow : Window
{
    private readonly AccountingApiClient _api;
    private readonly int? _roleId;

    public RoleEditWindow(AccountingApiClient api, int? roleId)
    {
        _api = api;
        _roleId = roleId;
        InitializeComponent();
        Loaded += (_, _) => _ = LoadAsync();
    }

    private sealed class PermissionRow : INotifyPropertyChanged
    {
        private bool _isChecked;

        public string Name { get; init; } = "";

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked == value)
                    return;
                _isChecked = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    private async Task LoadAsync()
    {
        var perms = await _api.GetFromJsonAsync<List<PermissionDto>>("api/permissions").ConfigureAwait(true);
        var rows = perms?.Select(p => new PermissionRow { Name = p.Name, IsChecked = false }).ToList() ?? new List<PermissionRow>();
        PermItems.ItemsSource = rows;

        if (_roleId is null)
        {
            Title = "New role";
            return;
        }

        Title = "Edit role";
        var detail = await _api.GetFromJsonAsync<RoleDetailDto>($"api/roles/{_roleId.Value}").ConfigureAwait(true);
        if (detail is null)
        {
            MessageBox.Show("Could not load role.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        NameBox.Text = detail.Name;
        var set = new HashSet<string>(detail.Permissions, StringComparer.OrdinalIgnoreCase);
        foreach (PermissionRow row in rows)
            row.IsChecked = set.Contains(row.Name);
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(name))
        {
            MessageBox.Show("Role name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var selected = PermItems.Items.OfType<PermissionRow>().Where(p => p.IsChecked).Select(p => p.Name).ToList();

        if (_roleId is null)
        {
            var create = new CreateRoleRequest { Name = name, PermissionNames = selected };
            var result = await _api.PostJsonAsync<CreatedEntityInfo, CreateRoleRequest>("api/roles", create).ConfigureAwait(true);
            if (!result.Ok)
            {
                MessageBox.Show(result.ErrorMessage ?? "Save failed.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }
        else
        {
            var update = new UpdateRoleRequest { Name = name, PermissionNames = selected };
            var result = await _api.PutJsonNoContentAsync($"api/roles/{_roleId.Value}", update).ConfigureAwait(true);
            if (!result.Ok)
            {
                MessageBox.Show(result.ErrorMessage ?? "Save failed.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        DialogResult = true;
        Close();
    }
}
