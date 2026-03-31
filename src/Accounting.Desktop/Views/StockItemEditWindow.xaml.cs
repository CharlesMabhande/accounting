using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using Accounting.Application.DTOs;

namespace Accounting.Desktop.Views;

public partial class StockItemEditWindow : Window
{
    private sealed class LedgerPickerItem
    {
        public int Id { get; init; }
        public string Display { get; init; } = "";
    }

    public UpsertStockItemRequest? Result { get; private set; }

    public StockItemEditWindow(IReadOnlyList<LedgerAccountOptionDto> ledgers, StockItemQueryDto? existing = null)
    {
        InitializeComponent();
        var items = ledgers.Select(l => new LedgerPickerItem
        {
            Id = l.Id,
            Display = $"{l.Code} — {l.Name}"
        }).ToList();
        InvCombo.ItemsSource = items;
        CosCombo.ItemsSource = items;

        if (existing != null)
        {
            Title = "Edit stock item";
            CodeBox.Text = existing.Code;
            DescBox.Text = existing.Description;
            LongDescBox.Text = existing.LongDescription ?? "";
            AltCodeBox.Text = existing.AlternateCode ?? "";
            UomBox.Text = existing.UnitOfMeasure;
            InvCombo.SelectedItem = items.FirstOrDefault(i => i.Id == existing.InventoryAccountId);
            CosCombo.SelectedItem = items.FirstOrDefault(i => i.Id == existing.CostOfSalesAccountId);
            BuyLBox.Text = existing.BuyLength?.ToString(CultureInfo.CurrentCulture) ?? "";
            BuyWBox.Text = existing.BuyWidth?.ToString(CultureInfo.CurrentCulture) ?? "";
            BuyHBox.Text = existing.BuyHeight?.ToString(CultureInfo.CurrentCulture) ?? "";
            SellLBox.Text = existing.SellLength?.ToString(CultureInfo.CurrentCulture) ?? "";
            SellWBox.Text = existing.SellWidth?.ToString(CultureInfo.CurrentCulture) ?? "";
            SellHBox.Text = existing.SellHeight?.ToString(CultureInfo.CurrentCulture) ?? "";
            WeightBox.Text = existing.Weight?.ToString(CultureInfo.CurrentCulture) ?? "";
            WeightUnitBox.Text = existing.WeightUnit ?? "";
            MeasBox.Text = existing.MeasurementNotes ?? "";
            GpBox.Text = existing.TargetGpPercent?.ToString(CultureInfo.CurrentCulture) ?? "";
            ActiveBox.IsChecked = existing.IsActive;
            ServiceBox.IsChecked = existing.IsServiceItem;
            SerialBox.IsChecked = existing.TrackSerialNumbers;
            LotsBox.IsChecked = existing.TrackLots;
        }
        else
        {
            UomBox.Text = "EA";
            if (items.Count > 0)
            {
                InvCombo.SelectedIndex = 0;
                CosCombo.SelectedIndex = System.Math.Min(1, items.Count - 1);
            }
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (InvCombo.SelectedItem is not LedgerPickerItem inv || CosCombo.SelectedItem is not LedgerPickerItem cos)
        {
            MessageBox.Show("Select inventory and COS ledger accounts.", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Result = new UpsertStockItemRequest
        {
            Code = CodeBox.Text.Trim(),
            Description = DescBox.Text.Trim(),
            LongDescription = NullIfEmpty(LongDescBox.Text),
            AlternateCode = NullIfEmpty(AltCodeBox.Text),
            UnitOfMeasure = string.IsNullOrWhiteSpace(UomBox.Text) ? "EA" : UomBox.Text.Trim(),
            InventoryAccountId = inv.Id,
            CostOfSalesAccountId = cos.Id,
            TrackSerialNumbers = SerialBox.IsChecked == true,
            TrackLots = LotsBox.IsChecked == true,
            IsActive = ActiveBox.IsChecked == true,
            IsServiceItem = ServiceBox.IsChecked == true,
            TargetGpPercent = ParseDec(GpBox.Text),
            BuyLength = ParseDec(BuyLBox.Text),
            BuyWidth = ParseDec(BuyWBox.Text),
            BuyHeight = ParseDec(BuyHBox.Text),
            SellLength = ParseDec(SellLBox.Text),
            SellWidth = ParseDec(SellWBox.Text),
            SellHeight = ParseDec(SellHBox.Text),
            Weight = ParseDec(WeightBox.Text),
            WeightUnit = NullIfEmpty(WeightUnitBox.Text),
            MeasurementNotes = NullIfEmpty(MeasBox.Text)
        };

        if (Result.Code.Length == 0 || Result.Description.Length == 0)
        {
            MessageBox.Show("Code and description are required.", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
        Close();
    }

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static decimal? ParseDec(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return null;
        return decimal.TryParse(s, NumberStyles.Number, CultureInfo.CurrentCulture, out var d) ? d : null;
    }
}
