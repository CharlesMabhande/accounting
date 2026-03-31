using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Accounting.Application.DTOs;
using Accounting.Desktop.Helpers;
using Accounting.Desktop.Services;
using Accounting.Desktop.Shell;

namespace Accounting.Desktop.Views;

public partial class CustomerStatementsView : UserControl
{
    private readonly AccountingApiClient _api;
    private List<CustomerDto> _all = new();

    public CustomerStatementsView(AccountingApiClient api)
    {
        _api = api;
        InitializeComponent();
        Loaded += OnLoaded;
        CompanyContext.Changed += OnCompanyChanged;
        Unloaded += OnUnloaded;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => CompanyContext.Changed -= OnCompanyChanged;

    private void OnCompanyChanged() => Dispatcher.InvokeAsync(() => _ = LoadAsync());

    private async void OnLoaded(object sender, RoutedEventArgs e) => await LoadAsync().ConfigureAwait(true);

    private async Task LoadAsync()
    {
        if (CompanyContext.SelectedCompanyId is not int cid)
        {
            StatusText.Text = "Select a company in the top bar.";
            Grid.ItemsSource = null;
            return;
        }

        StatusText.Text = "Loading…";
        var list = await _api.GetFromJsonAsync<List<CustomerDto>>($"api/companies/{cid}/Customers").ConfigureAwait(true);
        _all = list ?? new List<CustomerDto>();
        ApplyFilter();
        StatusText.Text = $"{_all.Count} customers.";
    }

    private void ApplyFilter()
    {
        var q = (SearchBox.Text ?? "").Trim();
        if (string.IsNullOrEmpty(q))
        {
            Grid.ItemsSource = _all;
            return;
        }

        Grid.ItemsSource = _all.Where(c =>
                c.Code.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                c.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private void SearchBox_OnTextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync().ConfigureAwait(true);

    private void ViewStatement_Click(object sender, RoutedEventArgs e) => OpenStatement();

    private void Grid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (Grid.SelectedItem is CustomerDto)
            OpenStatement();
    }

    private async void OpenStatement()
    {
        if (CompanyContext.SelectedCompanyId is not int cid)
        {
            MessageBox.Show("Select a company first.", "Statement", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (Grid.SelectedItem is not CustomerDto row)
        {
            MessageBox.Show("Select a customer.", "Statement", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var owner = Window.GetWindow(this);
        var period = new StatementPeriodDialog { Owner = owner };
        if (period.ShowDialog() != true)
            return;

        var url =
            $"api/companies/{cid}/Customers/{row.Id}/statement?from={period.FromDate:yyyy-MM-dd}&to={period.ToDate:yyyy-MM-dd}";
        var dto = await _api.GetFromJsonAsync<CustomerStatementDto>(url).ConfigureAwait(true);
        if (dto == null)
        {
            MessageBox.Show("Could not load the statement. Check the API and posted documents.", "Statement",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        new CustomerStatementPrintWindow(dto) { Owner = owner }.ShowDialog();
    }

    private void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        var w = Window.GetWindow(this);
        if (w != null)
            DataGridExportHelper.PromptExportCsv(Grid, w, "customers-statement-list");
    }

    private void ExportPdf_Click(object sender, RoutedEventArgs e)
    {
        var w = Window.GetWindow(this);
        if (w != null)
            DataGridExportHelper.PromptExportPdf(Grid, w, "customers-statement-list", "Customers (statement list)");
    }
}
