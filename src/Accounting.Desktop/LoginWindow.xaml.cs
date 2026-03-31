using System.Windows;
using Accounting.Application.DTOs;
using Accounting.Desktop.Services;

namespace Accounting.Desktop;

public partial class LoginWindow : Window
{
    private readonly AccountingApiClient _api;

    public LoginResponseDto? LoginResult { get; private set; }

    public LoginWindow(AccountingApiClient api)
    {
        _api = api;
        InitializeComponent();
        UserBox.Text = "admin";
    }

    private async void SignIn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ErrorText.Visibility = Visibility.Collapsed;
            var user = UserBox.Text?.Trim() ?? "";
            var pass = PasswordBox.Password ?? "";
            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                ShowError("Enter user name and password.");
                return;
            }

            var result = await _api.LoginAsync(new LoginRequest { UserName = user, Password = pass }).ConfigureAwait(true);
            if (!result.Ok || result.Value is null)
            {
                ShowError(result.ErrorMessage ?? "Login failed.");
                return;
            }

            _api.SetSessionToken(result.Value.SessionToken);
            var session = await _api.GetSessionAsync().ConfigureAwait(true);
            if (!session.Ok || session.Value is null)
            {
                _api.SetSessionToken(null);
                ShowError(session.ErrorMessage ?? "Could not load session.");
                return;
            }

            var s = session.Value;
            var login = result.Value!;
            LoginResult = new LoginResponseDto
            {
                UserId = login.UserId,
                DisplayName = login.DisplayName,
                Roles = login.Roles ?? Array.Empty<string>(),
                Permissions = s.Permissions ?? Array.Empty<string>(),
                SessionToken = login.SessionToken,
                ExpiresAtUtc = login.ExpiresAtUtc,
                AccountKind = s.AccountKind
            };
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
