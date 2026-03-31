using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Accounting.Application.DTOs;
using Accounting.Desktop.Helpers;
using Accounting.Desktop.Services;
using Accounting.Desktop.Shell;

namespace Accounting.Desktop.Views;

public partial class StockItemsView : UserControl
{
    private readonly AccountingApiClient _api;
    private readonly ObservableCollection<StockItemQueryDto> _rows = new();

    public StockItemsView(AccountingApiClient api)
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
        var list = await _api.GetFromJsonAsync<List<StockItemQueryDto>>($"api/companies/{cid}/StockItems").ConfigureAwait(true);
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
            MessageBox.Show("Select a company first.", "Stock items", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var ledgers = await _api.GetFromJsonAsync<List<LedgerAccountOptionDto>>(
            $"api/companies/{cid}/ChartOfAccounts/postable-accounts").ConfigureAwait(true);
        if (ledgers == null || ledgers.Count == 0)
        {
            MessageBox.Show("No postable ledger accounts.", "Stock items", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dlg = new StockItemEditWindow(ledgers, null);
        if (dlg.ShowDialog() != true || dlg.Result == null)
            return;

        var res = await _api.PostJsonAsync<StockItemQueryDto, UpsertStockItemRequest>(
            $"api/companies/{cid}/StockItems", dlg.Result).ConfigureAwait(true);
        if (!res.Ok)
        {
            MessageBox.Show(res.ErrorMessage ?? "Save failed.", "Stock items", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await LoadAsync().ConfigureAwait(true);
    }

    private async void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (CompanyContext.SelectedCompanyId is not int cid)
        {
            MessageBox.Show("Select a company first.", "Stock items", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (Grid.SelectedItem is not StockItemQueryDto row)
        {
            MessageBox.Show("Select a row to edit.", "Stock items", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var ledgers = await _api.GetFromJsonAsync<List<LedgerAccountOptionDto>>(
            $"api/companies/{cid}/ChartOfAccounts/postable-accounts").ConfigureAwait(true);
        if (ledgers == null || ledgers.Count == 0)
        {
            MessageBox.Show("No postable ledger accounts.", "Stock items", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dlg = new StockItemEditWindow(ledgers, row);
        if (dlg.ShowDialog() != true || dlg.Result == null)
            return;

        var res = await _api.PutJsonAsync<StockItemQueryDto, UpsertStockItemRequest>(
            $"api/companies/{cid}/StockItems/{row.Id}", dlg.Result).ConfigureAwait(true);
        if (!res.Ok)
        {
            MessageBox.Show(res.ErrorMessage ?? "Save failed.", "Stock items", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await LoadAsync().ConfigureAwait(true);
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (CompanyContext.SelectedCompanyId is not int cid)
        {
            MessageBox.Show("Select a company first.", "Stock items", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (Grid.SelectedItem is not StockItemQueryDto row)
        {
            MessageBox.Show("Select a row to delete.", "Stock items", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (MessageBox.Show($"Remove stock item '{row.Code}'?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) !=
            MessageBoxResult.Yes)
            return;

        var res = await _api.DeleteAsync($"api/companies/{cid}/StockItems/{row.Id}").ConfigureAwait(true);
        if (!res.Ok)
        {
            MessageBox.Show(res.ErrorMessage ?? "Delete failed.", "Stock items", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await LoadAsync().ConfigureAwait(true);
    }

    private void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        var w = Window.GetWindow(this);
        if (w != null)
            DataGridExportHelper.PromptExportCsv(Grid, w, "stock-items");
    }

    private void ExportPdf_Click(object sender, RoutedEventArgs e)
    {
        var w = Window.GetWindow(this);
        if (w != null)
            DataGridExportHelper.PromptExportPdf(Grid, w, "stock-items", "Stock items");
    }
}
