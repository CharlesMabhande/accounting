using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Accounting.Desktop.Services;
using Accounting.Desktop.Shell;

namespace Accounting.Desktop.Views;

public partial class DashboardView : UserControl
{
    private readonly AccountingApiClient _api;

    public DashboardView(AccountingApiClient api)
    {
        _api = api;
        InitializeComponent();
        BaseUrlText.Text = _api.BaseUrl;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e) => await RefreshAsync();

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await RefreshAsync();

    private void TaskApi_Click(object sender, RoutedEventArgs e) => _ = RefreshAsync();

    private void TaskCompanies_Click(object sender, RoutedEventArgs e) => ShellNavigationHub.OpenModule("companies");

    private void QuickLedger_Click(object sender, RoutedEventArgs e) => ShellNavigationHub.OpenModule("ledger");

    private void QuickAr_Click(object sender, RoutedEventArgs e) => ShellNavigationHub.OpenModule("ar_invoices");

    private void QuickAp_Click(object sender, RoutedEventArgs e) => ShellNavigationHub.OpenModule("ap_invoices");

    private void QuickBank_Click(object sender, RoutedEventArgs e) => ShellNavigationHub.OpenModule("cb_banks");

    private void QuickStock_Click(object sender, RoutedEventArgs e) => ShellNavigationHub.OpenModule("inv_items");

    private void QuickImport_Click(object sender, RoutedEventArgs e) => ShellNavigationHub.OpenModule("import");

    private void RunReport_Click(object sender, RoutedEventArgs e)
    {
        var baseUri = _api.BaseUrl.TrimEnd('/') + "/swagger";
        try
        {
            Process.Start(new ProcessStartInfo { FileName = baseUri, UseShellExecute = true });
        }
        catch
        {
            MessageBox.Show($"Open Swagger in a browser: {baseUri}", "Reporting", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void RecentList_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (RecentList.SelectedItem is RecentModuleEntry r)
            ShellNavigationHub.OpenModule(r.ModuleKey);
    }

    private async Task RefreshAsync()
    {
        StatusText.Text = "Checking…";
        StatusDot.Fill = new SolidColorBrush(Color.FromRgb(0x6B, 0x7C, 0x93));

        var ok = await _api.IsReachableAsync();
        if (ok)
        {
            StatusText.Text = "Connected";
            StatusDot.Fill = new SolidColorBrush(Color.FromRgb(0x00, 0x66, 0xFF));
        }
        else
        {
            StatusText.Text = "Unreachable — start Accounting.Api or check BaseUrl in appsettings.json";
            StatusDot.Fill = new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26));
        }

        try
        {
            var companies = await _api.GetCompaniesAsync();
            CompanySummary.Text = companies.Count == 0
                ? "No companies returned."
                : string.Join(Environment.NewLine, companies.Select(c => $"{c.Code} — {c.Name} ({c.BaseCurrency})"));
        }
        catch (Exception ex)
        {
            CompanySummary.Text = "Could not load companies: " + ex.Message;
        }
    }
}
