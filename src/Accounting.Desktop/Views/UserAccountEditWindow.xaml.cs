using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Accounting.Application.Common;
using Accounting.Application.DTOs;
using Accounting.Domain.Enums;
using Accounting.Desktop.Services;

namespace Accounting.Desktop.Views;

public partial class UserAccountEditWindow : Window
{
    private readonly AccountingApiClient _api;
    private readonly int? _userId;

    public UserAccountEditWindow(AccountingApiClient api, int? userId)
    {
        _api = api;
        _userId = userId;
        InitializeComponent();
        Loaded += (_, _) => _ = LoadAsync();
    }

    private sealed class RoleItem
    {
        public int Id { get; init; }
        public string Name { get; init; } = "";
    }

    private async Task LoadAsync()
    {
        var roles = await _api.GetFromJsonAsync<List<RoleListDto>>("api/roles").ConfigureAwait(true);
        RolesList.ItemsSource = roles?.Select(r => new RoleItem { Id = r.Id, Name = r.Name }).ToList() ?? new List<RoleItem>();

        if (_userId is null)
        {
            Title = "New user";
            UserNameBox.IsEnabled = true;
            KindCombo.SelectedIndex = 0;
            return;
        }

        Title = "Edit user";
        UserNameBox.IsEnabled = false;
        var detail = await _api.GetFromJsonAsync<UserAccountDetailDto>($"api/users/{_userId.Value}").ConfigureAwait(true);
        if (detail is null)
        {
            MessageBox.Show("Could not load user.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        UserNameBox.Text = detail.UserName;
        DisplayNameBox.Text = detail.DisplayName;
        ActiveCheck.IsChecked = detail.IsActive;
        KindCombo.SelectedIndex = detail.AccountKind == "Agent" ? 1 : 0;

        foreach (RoleItem item in RolesList.Items)
        {
            if (detail.RoleIds.Contains(item.Id))
                RolesList.SelectedItems.Add(item);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        var roleIds = RolesList.SelectedItems.OfType<RoleItem>().Select(r => r.Id).ToList();
        var kind = KindCombo.SelectedIndex == 1 ? UserAccountKind.Agent : UserAccountKind.Staff;
        var pass = PasswordBox.Password ?? "";

        if (_userId is null)
        {
            if (string.IsNullOrWhiteSpace(UserNameBox.Text) || string.IsNullOrWhiteSpace(pass))
            {
                MessageBox.Show("User name is required, and you must enter the password to assign to this account.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var create = new CreateUserAccountRequest
            {
                UserName = UserNameBox.Text.Trim(),
                DisplayName = string.IsNullOrWhiteSpace(DisplayNameBox.Text) ? UserNameBox.Text.Trim() : DisplayNameBox.Text.Trim(),
                Password = pass,
                IsActive = ActiveCheck.IsChecked == true,
                AccountKind = kind,
                RoleIds = roleIds
            };
            var result = await _api.PostJsonAsync<CreatedEntityInfo, CreateUserAccountRequest>("api/users", create).ConfigureAwait(true);
            if (!result.Ok)
            {
                MessageBox.Show(result.ErrorMessage ?? "Save failed.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }
        else
        {
            var update = new UpdateUserAccountRequest
            {
                DisplayName = DisplayNameBox.Text?.Trim() ?? "",
                Password = string.IsNullOrWhiteSpace(pass) ? null : pass,
                IsActive = ActiveCheck.IsChecked == true,
                AccountKind = kind,
                RoleIds = roleIds
            };
            var result = await _api.PutJsonNoContentAsync($"api/users/{_userId.Value}", update).ConfigureAwait(true);
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
