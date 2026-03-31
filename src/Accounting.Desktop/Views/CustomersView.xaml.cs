using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Accounting.Application.DTOs;
using Accounting.Desktop.Helpers;
using Accounting.Desktop.Services;
using Accounting.Desktop.Shell;

namespace Accounting.Desktop.Views;

public partial class CustomersView : UserControl
{
    private readonly AccountingApiClient _api;
    private readonly ObservableCollection<CustomerDto> _rows = new();

    public CustomersView(AccountingApiClient api)
    {
        _api = api;
        InitializeComponent();
        Grid.ItemsSource = _rows;
        Loaded += OnLoaded;
        CompanyContext.Changed += OnCompanyChanged;
        Unloaded += OnUnloaded;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => CompanyContext.Changed -= OnCompanyChanged;

    private async void OnLoaded(object sender, RoutedEventArgs e) => await LoadAsync();

    private void OnCompanyChanged() => Dispatcher.InvokeAsync(() => _ = LoadAsync());

    private async Task LoadAsync()
    {
        if (CompanyContext.SelectedCompanyId is not int cid)
        {
            _rows.Clear();
            StatusText.Text = "Select a company in the top bar.";
            return;
        }

        StatusText.Text = "Loading…";
        var list = await _api.GetFromJsonAsync<List<CustomerDto>>($"api/companies/{cid}/Customers").ConfigureAwait(true);
        _rows.Clear();
        if (list != null)
        {
            foreach (var r in list)
                _rows.Add(r);
        }

        StatusText.Text = list == null ? "Request failed." : $"{_rows.Count} row(s).";
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();

    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        if (CompanyContext.SelectedCompanyId is not int cid)
        {
            MessageBox.Show("Select a company first.", "Customers", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var ledgers = await LoadLedgersAsync(cid).ConfigureAwait(true);
        if (ledgers == null)
            ledgers = new List<LedgerAccountOptionDto>();

        var dlg = new PartyEditWindow(PartyEditWindow.PartyMode.Customer, ledgers);
        if (dlg.ShowDialog() != true || dlg.CustomerResult == null)
            return;

        var res = await _api.PostJsonAsync<CustomerDto, UpsertCustomerRequest>($"api/companies/{cid}/Customers", dlg.CustomerResult)
            .ConfigureAwait(true);
        if (!res.Ok)
        {
            MessageBox.Show(res.ErrorMessage ?? "Save failed.", "Customers", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await LoadAsync().ConfigureAwait(true);
    }

    private async void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (CompanyContext.SelectedCompanyId is not int cid)
        {
            MessageBox.Show("Select a company first.", "Customers", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (Grid.SelectedItem is not CustomerDto row)
        {
            MessageBox.Show("Select a row to edit.", "Customers", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var ledgers = await LoadLedgersAsync(cid).ConfigureAwait(true);
        if (ledgers == null)
            ledgers = new List<LedgerAccountOptionDto>();

        var dlg = new PartyEditWindow(PartyEditWindow.PartyMode.Customer, ledgers, row);
        if (dlg.ShowDialog() != true || dlg.CustomerResult == null)
            return;

        var res = await _api.PutJsonAsync<CustomerDto, UpsertCustomerRequest>(
            $"api/companies/{cid}/Customers/{row.Id}", dlg.CustomerResult).ConfigureAwait(true);
        if (!res.Ok)
        {
            MessageBox.Show(res.ErrorMessage ?? "Save failed.", "Customers", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await LoadAsync().ConfigureAwait(true);
    }

    private void Grid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (Grid.SelectedItem is CustomerDto)
            Edit_Click(sender, e);
    }

    private async void PrintStatement_Click(object sender, RoutedEventArgs e)
    {
        if (CompanyContext.SelectedCompanyId is not int cid)
        {
            MessageBox.Show("Select a company first.", "Statement", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (Grid.SelectedItem is not CustomerDto row)
        {
            MessageBox.Show("Select a customer, then print the statement.", "Statement", MessageBoxButton.OK, MessageBoxImage.Information);
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
            MessageBox.Show("Could not load the statement. Check the API and that documents are posted.", "Statement",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        new CustomerStatementPrintWindow(dto) { Owner = owner }.ShowDialog();
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (CompanyContext.SelectedCompanyId is not int cid)
        {
            MessageBox.Show("Select a company first.", "Customers", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (Grid.SelectedItem is not CustomerDto row)
        {
            MessageBox.Show("Select a row to delete.", "Customers", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (MessageBox.Show($"Remove customer '{row.Code}'?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) !=
            MessageBoxResult.Yes)
            return;

        var res = await _api.DeleteAsync($"api/companies/{cid}/Customers/{row.Id}").ConfigureAwait(true);
        if (!res.Ok)
        {
            MessageBox.Show(res.ErrorMessage ?? "Delete failed.", "Customers", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await LoadAsync().ConfigureAwait(true);
    }

    private void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        var w = Window.GetWindow(this);
        if (w != null)
            DataGridExportHelper.PromptExportCsv(Grid, w, "customers");
    }

    private void ExportPdf_Click(object sender, RoutedEventArgs e)
    {
        var w = Window.GetWindow(this);
        if (w != null)
            DataGridExportHelper.PromptExportPdf(Grid, w, "customers", "Customers");
    }

    private async Task<List<LedgerAccountOptionDto>?> LoadLedgersAsync(int companyId)
    {
        return await _api.GetFromJsonAsync<List<LedgerAccountOptionDto>>(
            $"api/companies/{companyId}/ChartOfAccounts/postable-accounts").ConfigureAwait(false);
    }
}
