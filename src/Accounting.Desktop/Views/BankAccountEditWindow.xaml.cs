using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Accounting.Application.DTOs;

namespace Accounting.Desktop.Views;

public partial class BankAccountEditWindow : Window
{
    private sealed class LedgerPickerItem
    {
        public int Id { get; init; }
        public string Display { get; init; } = "";
    }

    public UpsertBankAccountRequest? Result { get; private set; }

    public BankAccountEditWindow(IReadOnlyList<LedgerAccountOptionDto> ledgers, BankAccountDto? existing)
    {
        InitializeComponent();
        var items = ledgers.Select(l => new LedgerPickerItem
        {
            Id = l.Id,
            Display = $"{l.Code} — {l.Name}"
        }).ToList();

        LedgerCombo.ItemsSource = items;

        if (existing != null)
        {
            Title = "Edit bank account";
            CodeBox.Text = existing.Code;
            NameBox.Text = existing.Name;
            CurrencyBox.Text = existing.CurrencyCode;
            ActiveBox.IsChecked = existing.IsActive;
            var match = items.FirstOrDefault(i => i.Id == existing.LedgerAccountId);
            LedgerCombo.SelectedItem = match;
        }
        else
        {
            LedgerCombo.SelectedIndex = 0;
            CurrencyBox.Text = "USD";
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (LedgerCombo.SelectedItem is not LedgerPickerItem ledger)
        {
            MessageBox.Show("Select a ledger account.", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Result = new UpsertBankAccountRequest
        {
            Code = CodeBox.Text.Trim(),
            Name = NameBox.Text.Trim(),
            LedgerAccountId = ledger.Id,
            CurrencyCode = string.IsNullOrWhiteSpace(CurrencyBox.Text) ? "USD" : CurrencyBox.Text.Trim().ToUpperInvariant(),
            IsActive = ActiveBox.IsChecked == true
        };

        if (Result.Code.Length == 0 || Result.Name.Length == 0)
        {
            MessageBox.Show("Code and name are required.", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
        Close();
    }
}
