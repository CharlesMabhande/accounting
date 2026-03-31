using System.Windows;
using System.Windows.Controls;
using Accounting.Desktop.Helpers;
using Accounting.Desktop.Services;
using Accounting.Desktop.Shell;

namespace Accounting.Desktop.Views;

public partial class TrialBalanceView : UserControl
{
    private readonly AccountingApiClient _api;

    public TrialBalanceView(AccountingApiClient api)
    {
        _api = api;
        InitializeComponent();
        AsOfPicker.SelectedDate = DateTime.Today;
        Loaded += OnLoaded;
        CompanyContext.Changed += OnCompanyChanged;
        Unloaded += (_, _) => CompanyContext.Changed -= OnCompanyChanged;
    }

    private void OnCompanyChanged() => Dispatcher.InvokeAsync(() => _ = LoadAsync());

    private async void OnLoaded(object sender, RoutedEventArgs e) => await LoadAsync();

    private async void Load_Click(object sender, RoutedEventArgs e) => await LoadAsync();

    private async Task LoadAsync()
    {
        if (CompanyContext.SelectedCompanyId is not int cid)
        {
            Grid.ItemsSource = null;
            StatusText.Text = "Select a company.";
            return;
        }

        var asOf = AsOfPicker.SelectedDate?.Date ?? DateTime.Today;
        var asOfStr = DateOnly.FromDateTime(asOf).ToString("yyyy-MM-dd");
        StatusText.Text = "Loading…";
        var url = $"api/Reporting/companies/{cid}/trial-balance?asOf={Uri.EscapeDataString(asOfStr)}";
        var json = await _api.GetJsonStringAsync(url);
        var view = JsonGridHelper.JsonArrayToView(json);
        Grid.ItemsSource = view;
        StatusText.Text = view == null ? "Failed." : $"{view.Count} rows.";
    }

    private void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        var w = Window.GetWindow(this);
        if (w != null)
            DataGridExportHelper.PromptExportCsv(Grid, w, "trial-balance");
    }

    private void ExportPdf_Click(object sender, RoutedEventArgs e)
    {
        var w = Window.GetWindow(this);
        if (w != null)
            DataGridExportHelper.PromptExportPdf(Grid, w, "trial-balance", "Trial balance");
    }
}
