using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Accounting.Application.Common;
using Accounting.Application.DTOs;
using Accounting.Desktop.Helpers;
using Accounting.Desktop.Services;
using Accounting.Desktop.Shell;

namespace Accounting.Desktop.Views;

public partial class CurrenciesExchangeRatesView : UserControl
{
    private readonly AccountingApiClient _api;
    private int _currencyFormId;

    public CurrenciesExchangeRatesView(AccountingApiClient api)
    {
        _api = api;
        InitializeComponent();
        FxDateBox.Text = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        Loaded += OnLoaded;
        CompanyContext.Changed += OnCompanyChanged;
        Unloaded += OnUnloaded;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => CompanyContext.Changed -= OnCompanyChanged;

    private void OnCompanyChanged() => Dispatcher.InvokeAsync(() => _ = LoadFxAsync());

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await LoadCurrenciesAsync().ConfigureAwait(true);
        await LoadFxAsync().ConfigureAwait(true);
    }

    private async Task LoadCurrenciesAsync()
    {
        CurStatus.Text = "Loading…";
        var list = await _api.GetFromJsonAsync<List<CurrencyDto>>("api/erp/currencies").ConfigureAwait(true);
        CurrencyGrid.ItemsSource = list ?? new List<CurrencyDto>();
        CurStatus.Text = list == null ? "Request failed." : $"{list.Count} currency/currencies.";
    }

    private async Task LoadFxAsync()
    {
        if (CompanyContext.SelectedCompanyId is not int cid)
        {
            FxGrid.ItemsSource = null;
            FxStatus.Text = "Select a company for exchange rates.";
            return;
        }

        FxStatus.Text = "Loading…";
        var list = await _api.GetFromJsonAsync<List<ExchangeRateDto>>($"api/companies/{cid}/erp/exchange-rates").ConfigureAwait(true);
        FxGrid.ItemsSource = list ?? new List<ExchangeRateDto>();
        FxStatus.Text = list == null ? "Request failed." : $"{list.Count} rate(s).";
    }

    private void CurrencyGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CurrencyGrid.SelectedItem is not CurrencyDto c)
        {
            _currencyFormId = 0;
            return;
        }

        _currencyFormId = c.Id;
        CurCodeBox.Text = c.Code;
        CurCodeBox.IsReadOnly = true;
        CurNameBox.Text = c.Name;
        CurDecBox.Text = c.DecimalPlaces.ToString(CultureInfo.InvariantCulture);
    }

    private async void RefreshCurrencies_Click(object sender, RoutedEventArgs e) => await LoadCurrenciesAsync().ConfigureAwait(true);

    private void NewCurrency_Click(object sender, RoutedEventArgs e)
    {
        CurrencyGrid.SelectedItem = null;
        _currencyFormId = 0;
        CurCodeBox.Text = "";
        CurCodeBox.IsReadOnly = false;
        CurNameBox.Text = "";
        CurDecBox.Text = "2";
        CurStatus.Text = "New currency — enter code and name.";
    }

    private async void SaveCurrency_Click(object sender, RoutedEventArgs e)
    {
        var code = (CurCodeBox.Text ?? "").Trim();
        var name = (CurNameBox.Text ?? "").Trim();
        if (!int.TryParse((CurDecBox.Text ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var dp))
        {
            MessageBox.Show("Decimals must be a whole number (0–6).", "Currencies", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        dp = Math.Clamp(dp, 0, 6);
        var req = new UpsertCurrencyRequest
        {
            Id = _currencyFormId,
            Code = code,
            Name = string.IsNullOrEmpty(name) ? code : name,
            DecimalPlaces = dp
        };

        var result = await _api.PostJsonAsync<IdResponseDto, UpsertCurrencyRequest>("api/erp/currencies", req).ConfigureAwait(true);
        if (!result.Ok)
        {
            MessageBox.Show(result.ErrorMessage ?? "Save failed.", "Currencies", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await LoadCurrenciesAsync().ConfigureAwait(true);
        if (result.Value != null)
        {
            _currencyFormId = result.Value.Id;
            foreach (CurrencyDto row in CurrencyGrid.Items)
            {
                if (row.Id == _currencyFormId)
                {
                    CurrencyGrid.SelectedItem = row;
                    break;
                }
            }
        }

        CurStatus.Text = "Saved.";
    }

    private async void DeleteCurrency_Click(object sender, RoutedEventArgs e)
    {
        if (_currencyFormId <= 0)
        {
            MessageBox.Show("Select a saved currency to delete, or use Refresh after selecting a row.", "Currencies",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (MessageBox.Show($"Delete currency {CurCodeBox.Text}?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) !=
            MessageBoxResult.Yes)
            return;

        var r = await _api.DeleteAsync($"api/erp/currencies/{_currencyFormId}").ConfigureAwait(true);
        if (!r.Ok)
        {
            MessageBox.Show(r.ErrorMessage ?? "Delete failed.", "Currencies", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        NewCurrency_Click(sender, e);
        await LoadCurrenciesAsync().ConfigureAwait(true);
    }

    private async void RefreshFx_Click(object sender, RoutedEventArgs e) => await LoadFxAsync().ConfigureAwait(true);

    private async void SaveFx_Click(object sender, RoutedEventArgs e)
    {
        if (CompanyContext.SelectedCompanyId is not int cid)
        {
            MessageBox.Show("Select a company first.", "Exchange rates", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var from = (FxFromBox.Text ?? "").Trim();
        var to = (FxToBox.Text ?? "").Trim();
        if (!decimal.TryParse((FxRateBox.Text ?? "").Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var rate))
        {
            MessageBox.Show("Enter a valid rate.", "Exchange rates", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dateStr = (FxDateBox.Text ?? "").Trim();
        if (!DateOnly.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var eff))
        {
            MessageBox.Show("Enter effective date as yyyy-MM-dd.", "Exchange rates", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var req = new UpsertExchangeRateRequest
        {
            FromCurrencyCode = from,
            ToCurrencyCode = to,
            Rate = rate,
            EffectiveDate = eff
        };

        var result = await _api.PostJsonAsync<OperationResult<int>, UpsertExchangeRateRequest>(
                $"api/companies/{cid}/erp/exchange-rates", req)
            .ConfigureAwait(true);
        if (!result.Ok || result.Value is null || !result.Value.Success)
        {
            var err = result.ErrorMessage;
            if (result.Value?.Errors is { Count: > 0 } errors)
                err = string.Join("; ", errors);
            MessageBox.Show(err ?? "Save failed.", "Exchange rates", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await LoadFxAsync().ConfigureAwait(true);
    }

    private async void DeleteFx_Click(object sender, RoutedEventArgs e)
    {
        if (CompanyContext.SelectedCompanyId is not int cid)
        {
            MessageBox.Show("Select a company first.", "Exchange rates", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (FxGrid.SelectedItem is not ExchangeRateDto row)
        {
            MessageBox.Show("Select a rate row.", "Exchange rates", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var r = await _api.DeleteAsync($"api/companies/{cid}/erp/exchange-rates/{row.Id}").ConfigureAwait(true);
        if (!r.Ok)
        {
            MessageBox.Show(r.ErrorMessage ?? "Delete failed.", "Exchange rates", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await LoadFxAsync().ConfigureAwait(true);
    }

    private void ExportCurrenciesCsv_Click(object sender, RoutedEventArgs e)
    {
        var w = Window.GetWindow(this);
        if (w != null)
            DataGridExportHelper.PromptExportCsv(CurrencyGrid, w, "currencies");
    }

    private void ExportCurrenciesPdf_Click(object sender, RoutedEventArgs e)
    {
        var w = Window.GetWindow(this);
        if (w != null)
            DataGridExportHelper.PromptExportPdf(CurrencyGrid, w, "currencies", "Currencies");
    }

    private void ExportFxCsv_Click(object sender, RoutedEventArgs e)
    {
        var w = Window.GetWindow(this);
        if (w != null)
            DataGridExportHelper.PromptExportCsv(FxGrid, w, "exchange-rates");
    }

    private void ExportFxPdf_Click(object sender, RoutedEventArgs e)
    {
        var w = Window.GetWindow(this);
        if (w != null)
            DataGridExportHelper.PromptExportPdf(FxGrid, w, "exchange-rates", "Exchange rates");
    }
}
