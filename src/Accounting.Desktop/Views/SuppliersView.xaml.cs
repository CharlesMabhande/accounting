using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Accounting.Application.DTOs;
using Accounting.Desktop.Helpers;
using Accounting.Desktop.Services;
using Accounting.Desktop.Shell;

namespace Accounting.Desktop.Views;

public partial class SuppliersView : UserControl
{
    private readonly AccountingApiClient _api;
    private readonly ObservableCollection<SupplierDto> _rows = new();

    public SuppliersView(AccountingApiClient api)
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
        var list = await _api.GetFromJsonAsync<List<SupplierDto>>($"api/companies/{cid}/Suppliers").ConfigureAwait(true);
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
            MessageBox.Show("Select a company first.", "Suppliers", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var ledgers = await LoadLedgersAsync(cid).ConfigureAwait(true);
        if (ledgers == null)
            ledgers = new List<LedgerAccountOptionDto>();

        var dlg = new PartyEditWindow(PartyEditWindow.PartyMode.Supplier, ledgers);
        if (dlg.ShowDialog() != true || dlg.SupplierResult == null)
            return;

        var res = await _api.PostJsonAsync<SupplierDto, UpsertSupplierRequest>($"api/companies/{cid}/Suppliers", dlg.SupplierResult)
            .ConfigureAwait(true);
        if (!res.Ok)
        {
            MessageBox.Show(res.ErrorMessage ?? "Save failed.", "Suppliers", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await LoadAsync().ConfigureAwait(true);
    }

    private async void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (CompanyContext.SelectedCompanyId is not int cid)
        {
            MessageBox.Show("Select a company first.", "Suppliers", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (Grid.SelectedItem is not SupplierDto row)
        {
            MessageBox.Show("Select a row to edit.", "Suppliers", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var ledgers = await LoadLedgersAsync(cid).ConfigureAwait(true);
        if (ledgers == null)
            ledgers = new List<LedgerAccountOptionDto>();

        var dlg = new PartyEditWindow(PartyEditWindow.PartyMode.Supplier, ledgers, supplier: row);
        if (dlg.ShowDialog() != true || dlg.SupplierResult == null)
            return;

        var res = await _api.PutJsonAsync<SupplierDto, UpsertSupplierRequest>(
            $"api/companies/{cid}/Suppliers/{row.Id}", dlg.SupplierResult).ConfigureAwait(true);
        if (!res.Ok)
        {
            MessageBox.Show(res.ErrorMessage ?? "Save failed.", "Suppliers", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await LoadAsync().ConfigureAwait(true);
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (CompanyContext.SelectedCompanyId is not int cid)
        {
            MessageBox.Show("Select a company first.", "Suppliers", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (Grid.SelectedItem is not SupplierDto row)
        {
            MessageBox.Show("Select a row to delete.", "Suppliers", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (MessageBox.Show($"Remove supplier '{row.Code}'?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) !=
            MessageBoxResult.Yes)
            return;

        var res = await _api.DeleteAsync($"api/companies/{cid}/Suppliers/{row.Id}").ConfigureAwait(true);
        if (!res.Ok)
        {
            MessageBox.Show(res.ErrorMessage ?? "Delete failed.", "Suppliers", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await LoadAsync().ConfigureAwait(true);
    }

    private void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        var w = Window.GetWindow(this);
        if (w != null)
            DataGridExportHelper.PromptExportCsv(Grid, w, "suppliers");
    }

    private void ExportPdf_Click(object sender, RoutedEventArgs e)
    {
        var w = Window.GetWindow(this);
        if (w != null)
            DataGridExportHelper.PromptExportPdf(Grid, w, "suppliers", "Suppliers");
    }

    private async Task<List<LedgerAccountOptionDto>?> LoadLedgersAsync(int companyId)
    {
        return await _api.GetFromJsonAsync<List<LedgerAccountOptionDto>>(
            $"api/companies/{companyId}/ChartOfAccounts/postable-accounts").ConfigureAwait(false);
    }
}
