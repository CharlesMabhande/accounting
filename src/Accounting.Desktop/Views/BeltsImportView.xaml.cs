using System.Text;
using System.Windows;
using System.Windows.Controls;
using Accounting.Application.DTOs;
using Accounting.Desktop.Helpers;
using Accounting.Desktop.Services;
using Accounting.Desktop.Shell;

namespace Accounting.Desktop.Views;

public partial class BeltsImportView : UserControl
{
    private readonly AccountingApiClient _api;

    public BeltsImportView(AccountingApiClient api)
    {
        _api = api;
        InitializeComponent();
    }

    private async void Import_Click(object sender, RoutedEventArgs e)
    {
        if (CompanyContext.SelectedCompanyId is not int cid)
        {
            MessageBox.Show("Select a company in the top bar first.", "Import", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        StatusText.Text = "Importing…";
        LogBox.Text = "";

        var body = new BeltsImportRequest
        {
            ImportStockItems = ChkStock.IsChecked == true,
            ImportCustomers = ChkCustomers.IsChecked == true,
            ImportSuppliers = ChkSuppliers.IsChecked == true,
            OverwriteExisting = ChkOverwrite.IsChecked == true
        };

        var res = await _api.PostJsonAsync<BeltsImportResultDto, BeltsImportRequest>(
            $"api/companies/{cid}/import/belts", body).ConfigureAwait(true);

        if (!res.Ok || res.Value == null)
        {
            StatusText.Text = "Import failed.";
            LogBox.Text = res.ErrorMessage ?? "Unknown error.";
            return;
        }

        var r = res.Value;
        var sb = new StringBuilder();
        sb.AppendLine($"Stock: inserted {r.StockItemsInserted}, updated {r.StockItemsUpdated}, skipped {r.StockItemsSkipped}");
        sb.AppendLine($"Customers: inserted {r.CustomersInserted}, updated {r.CustomersUpdated}, skipped {r.CustomersSkipped}");
        sb.AppendLine($"Suppliers: inserted {r.SuppliersInserted}, updated {r.SuppliersUpdated}, skipped {r.SuppliersSkipped}");
        sb.AppendLine($"Errors: {r.Errors}");
        sb.AppendLine(r.Message ?? "");
        LogBox.Text = sb.ToString();
        StatusText.Text = "Done.";
    }

    private void SaveLog_Click(object sender, RoutedEventArgs e)
    {
        var w = Window.GetWindow(this);
        if (w != null)
            TextExportHelper.PromptSaveText(w, LogBox.Text ?? "", "belts-import-log");
    }
}
