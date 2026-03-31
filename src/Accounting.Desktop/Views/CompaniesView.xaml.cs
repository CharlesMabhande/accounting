using System.Windows;
using System.Windows.Controls;
using Accounting.Desktop.Helpers;
using Accounting.Desktop.Services;

namespace Accounting.Desktop.Views;

public partial class CompaniesView : UserControl
{
    private readonly AccountingApiClient _api;

    public CompaniesView(AccountingApiClient api)
    {
        _api = api;
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e) => await LoadAsync();

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();

    private async Task LoadAsync()
    {
        try
        {
            var rows = await _api.GetCompaniesAsync();
            Grid.ItemsSource = rows;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load companies: {ex.Message}", "Accounting", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        var w = Window.GetWindow(this);
        if (w != null)
            DataGridExportHelper.PromptExportCsv(Grid, w, "companies");
    }

    private void ExportPdf_Click(object sender, RoutedEventArgs e)
    {
        var w = Window.GetWindow(this);
        if (w != null)
            DataGridExportHelper.PromptExportPdf(Grid, w, "companies", "Companies");
    }
}
