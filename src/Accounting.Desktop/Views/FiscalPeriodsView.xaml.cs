using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Accounting.Application.DTOs;
using Accounting.Desktop.Helpers;
using Accounting.Desktop.Services;
using Accounting.Desktop.Shell;

namespace Accounting.Desktop.Views;

public partial class FiscalPeriodsView : UserControl
{
    private readonly AccountingApiClient _api;

    public FiscalPeriodsView(AccountingApiClient api)
    {
        _api = api;
        InitializeComponent();
        Loaded += OnLoaded;
        CompanyContext.Changed += OnCompanyChanged;
        Unloaded += OnUnloaded;
        BuildColumns();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => CompanyContext.Changed -= OnCompanyChanged;

    private void OnCompanyChanged() => Dispatcher.InvokeAsync(() => _ = LoadAsync());

    private async void OnLoaded(object sender, RoutedEventArgs e) => await LoadAsync().ConfigureAwait(true);

    private void BuildColumns()
    {
        Grid.Columns.Clear();
        Grid.Columns.Add(new DataGridTextColumn { Header = "Year", Binding = new System.Windows.Data.Binding("Year"), Width = 60 });
        Grid.Columns.Add(new DataGridTextColumn { Header = "P#", Binding = new System.Windows.Data.Binding("PeriodNumber"), Width = 50 });
        Grid.Columns.Add(new DataGridTextColumn { Header = "Start", Binding = new System.Windows.Data.Binding("StartDate") { StringFormat = "yyyy-MM-dd" }, Width = 100 });
        Grid.Columns.Add(new DataGridTextColumn { Header = "End", Binding = new System.Windows.Data.Binding("EndDate") { StringFormat = "yyyy-MM-dd" }, Width = 100 });
        Grid.Columns.Add(new DataGridCheckBoxColumn { Header = "Closed", Binding = new System.Windows.Data.Binding("IsClosed"), Width = 70 });
        Grid.Columns.Add(new DataGridCheckBoxColumn { Header = "FY closed", Binding = new System.Windows.Data.Binding("FiscalYearClosed"), Width = 80 });
    }

    private async Task LoadAsync()
    {
        if (CompanyContext.SelectedCompanyId is not int cid)
        {
            StatusText.Text = "Select a company in the top bar.";
            Grid.ItemsSource = null;
            return;
        }

        StatusText.Text = "Loading…";
        var list = await _api.GetFromJsonAsync<List<FiscalPeriodDto>>($"api/companies/{cid}/FiscalPeriods").ConfigureAwait(true);
        Grid.ItemsSource = list ?? new List<FiscalPeriodDto>();
        StatusText.Text = list == null ? "Request failed." : $"{list.Count} period(s).";
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync().ConfigureAwait(true);

    private async void ClosePeriod_Click(object sender, RoutedEventArgs e)
    {
        if (CompanyContext.SelectedCompanyId is not int cid)
        {
            MessageBox.Show("Select a company first.", "Periods", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (Grid.SelectedItem is not FiscalPeriodDto row)
        {
            MessageBox.Show("Select a period.", "Periods", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (row.IsClosed)
        {
            MessageBox.Show("This period is already closed.", "Periods", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var r = await _api.PostEmptyAsync($"api/companies/{cid}/FiscalPeriods/{row.Id}/close").ConfigureAwait(true);
        if (!r.Ok)
            MessageBox.Show(r.ErrorMessage ?? "Close failed.", "Periods", MessageBoxButton.OK, MessageBoxImage.Warning);
        else
            await LoadAsync().ConfigureAwait(true);
    }

    private async void ReopenPeriod_Click(object sender, RoutedEventArgs e)
    {
        if (CompanyContext.SelectedCompanyId is not int cid)
        {
            MessageBox.Show("Select a company first.", "Periods", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (Grid.SelectedItem is not FiscalPeriodDto row)
        {
            MessageBox.Show("Select a period.", "Periods", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!row.IsClosed)
        {
            MessageBox.Show("This period is not closed.", "Periods", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var r = await _api.PostEmptyAsync($"api/companies/{cid}/FiscalPeriods/{row.Id}/reopen").ConfigureAwait(true);
        if (!r.Ok)
            MessageBox.Show(r.ErrorMessage ?? "Reopen failed.", "Periods", MessageBoxButton.OK, MessageBoxImage.Warning);
        else
            await LoadAsync().ConfigureAwait(true);
    }

    private void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        var w = Window.GetWindow(this);
        if (w != null)
            DataGridExportHelper.PromptExportCsv(Grid, w, "fiscal-periods");
    }

    private void ExportPdf_Click(object sender, RoutedEventArgs e)
    {
        var w = Window.GetWindow(this);
        if (w != null)
            DataGridExportHelper.PromptExportPdf(Grid, w, "fiscal-periods", "Fiscal periods");
    }
}
