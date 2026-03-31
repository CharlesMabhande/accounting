using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using Accounting.Application.DTOs;

namespace Accounting.Desktop.Views;

public partial class PartyEditWindow : Window
{
    public enum PartyMode
    {
        Customer,
        Supplier
    }

    private sealed class LedgerPickerItem
    {
        public int? Id { get; init; }
        public string Display { get; init; } = "";
    }

    private readonly PartyMode _mode;

    public UpsertCustomerRequest? CustomerResult { get; private set; }
    public UpsertSupplierRequest? SupplierResult { get; private set; }

    public PartyEditWindow(PartyMode mode, IReadOnlyList<LedgerAccountOptionDto> ledgers, CustomerDto? customer = null, SupplierDto? supplier = null)
    {
        _mode = mode;
        InitializeComponent();

        LedgerLabel.Text = mode == PartyMode.Customer
            ? "Accounts receivable ledger (optional)"
            : "Accounts payable ledger (optional)";

        Title = mode == PartyMode.Customer ? "Customer" : "Supplier";

        var items = new List<LedgerPickerItem>
        {
            new() { Id = null, Display = "— None —" }
        };
        items.AddRange(ledgers.Select(l => new LedgerPickerItem
        {
            Id = l.Id,
            Display = $"{l.Code} — {l.Name}"
        }));
        LedgerCombo.ItemsSource = items;

        if (mode == PartyMode.Customer && customer != null)
        {
            Title = "Edit customer";
            LoadCustomer(customer, items);
        }
        else if (mode == PartyMode.Supplier && supplier != null)
        {
            Title = "Edit supplier";
            LoadSupplier(supplier, items);
        }
        else
        {
            LedgerCombo.SelectedIndex = 0;
            CurrencyBox.Text = "USD";
        }
    }

    private void LoadCustomer(CustomerDto c, List<LedgerPickerItem> items)
    {
        CodeBox.Text = c.Code;
        NameBox.Text = c.Name;
        CurrencyBox.Text = c.CurrencyCode;
        ActiveBox.IsChecked = c.IsActive;
        OnHoldBox.IsChecked = c.OnHold;
        ContactBox.Text = c.ContactName ?? "";
        PhoneBox.Text = c.Phone ?? "";
        EmailBox.Text = c.Email ?? "";
        Phys1Box.Text = c.PhysicalAddress1 ?? "";
        Phys2Box.Text = c.PhysicalAddress2 ?? "";
        Phys3Box.Text = c.PhysicalAddress3 ?? "";
        PhysCityBox.Text = c.PhysicalCity ?? "";
        Post1Box.Text = c.PostalAddress1 ?? "";
        Post2Box.Text = c.PostalAddress2 ?? "";
        Post3Box.Text = c.PostalAddress3 ?? "";
        PostCodeBox.Text = c.PostalCode ?? "";
        TaxBox.Text = c.TaxNumber ?? "";
        RegBox.Text = c.RegistrationNumber ?? "";
        CreditLimitBox.Text = c.CreditLimit?.ToString(CultureInfo.CurrentCulture) ?? "";
        LedgerCombo.SelectedItem = items.FirstOrDefault(i => i.Id == c.AccountsReceivableAccountId) ?? items[0];
    }

    private void LoadSupplier(SupplierDto s, List<LedgerPickerItem> items)
    {
        CodeBox.Text = s.Code;
        NameBox.Text = s.Name;
        CurrencyBox.Text = s.CurrencyCode;
        ActiveBox.IsChecked = s.IsActive;
        OnHoldBox.IsChecked = s.OnHold;
        ContactBox.Text = s.ContactName ?? "";
        PhoneBox.Text = s.Phone ?? "";
        EmailBox.Text = s.Email ?? "";
        Phys1Box.Text = s.PhysicalAddress1 ?? "";
        Phys2Box.Text = s.PhysicalAddress2 ?? "";
        Phys3Box.Text = s.PhysicalAddress3 ?? "";
        PhysCityBox.Text = s.PhysicalCity ?? "";
        Post1Box.Text = s.PostalAddress1 ?? "";
        Post2Box.Text = s.PostalAddress2 ?? "";
        Post3Box.Text = s.PostalAddress3 ?? "";
        PostCodeBox.Text = s.PostalCode ?? "";
        TaxBox.Text = s.TaxNumber ?? "";
        RegBox.Text = s.RegistrationNumber ?? "";
        CreditLimitBox.Text = s.CreditLimit?.ToString(CultureInfo.CurrentCulture) ?? "";
        LedgerCombo.SelectedItem = items.FirstOrDefault(i => i.Id == s.AccountsPayableAccountId) ?? items[0];
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var ledgerItem = LedgerCombo.SelectedItem as LedgerPickerItem;
        int? ledgerId = ledgerItem?.Id;
        decimal? credit = null;
        if (!string.IsNullOrWhiteSpace(CreditLimitBox.Text)
            && decimal.TryParse(CreditLimitBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var cr))
            credit = cr;

        if (_mode == PartyMode.Customer)
        {
            CustomerResult = new UpsertCustomerRequest
            {
                Code = CodeBox.Text.Trim(),
                Name = NameBox.Text.Trim(),
                AccountsReceivableAccountId = ledgerId,
                CurrencyCode = string.IsNullOrWhiteSpace(CurrencyBox.Text) ? "USD" : CurrencyBox.Text.Trim().ToUpperInvariant(),
                IsActive = ActiveBox.IsChecked == true,
                OnHold = OnHoldBox.IsChecked == true,
                ContactName = NullIfEmpty(ContactBox.Text),
                Phone = NullIfEmpty(PhoneBox.Text),
                Email = NullIfEmpty(EmailBox.Text),
                PhysicalAddress1 = NullIfEmpty(Phys1Box.Text),
                PhysicalAddress2 = NullIfEmpty(Phys2Box.Text),
                PhysicalAddress3 = NullIfEmpty(Phys3Box.Text),
                PhysicalCity = NullIfEmpty(PhysCityBox.Text),
                PostalAddress1 = NullIfEmpty(Post1Box.Text),
                PostalAddress2 = NullIfEmpty(Post2Box.Text),
                PostalAddress3 = NullIfEmpty(Post3Box.Text),
                PostalCode = NullIfEmpty(PostCodeBox.Text),
                TaxNumber = NullIfEmpty(TaxBox.Text),
                RegistrationNumber = NullIfEmpty(RegBox.Text),
                CreditLimit = credit
            };
            if (CustomerResult.Code.Length == 0 || CustomerResult.Name.Length == 0)
            {
                MessageBox.Show("Code and name are required.", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }
        else
        {
            SupplierResult = new UpsertSupplierRequest
            {
                Code = CodeBox.Text.Trim(),
                Name = NameBox.Text.Trim(),
                AccountsPayableAccountId = ledgerId,
                CurrencyCode = string.IsNullOrWhiteSpace(CurrencyBox.Text) ? "USD" : CurrencyBox.Text.Trim().ToUpperInvariant(),
                IsActive = ActiveBox.IsChecked == true,
                OnHold = OnHoldBox.IsChecked == true,
                ContactName = NullIfEmpty(ContactBox.Text),
                Phone = NullIfEmpty(PhoneBox.Text),
                Email = NullIfEmpty(EmailBox.Text),
                PhysicalAddress1 = NullIfEmpty(Phys1Box.Text),
                PhysicalAddress2 = NullIfEmpty(Phys2Box.Text),
                PhysicalAddress3 = NullIfEmpty(Phys3Box.Text),
                PhysicalCity = NullIfEmpty(PhysCityBox.Text),
                PostalAddress1 = NullIfEmpty(Post1Box.Text),
                PostalAddress2 = NullIfEmpty(Post2Box.Text),
                PostalAddress3 = NullIfEmpty(Post3Box.Text),
                PostalCode = NullIfEmpty(PostCodeBox.Text),
                TaxNumber = NullIfEmpty(TaxBox.Text),
                RegistrationNumber = NullIfEmpty(RegBox.Text),
                CreditLimit = credit
            };
            if (SupplierResult.Code.Length == 0 || SupplierResult.Name.Length == 0)
            {
                MessageBox.Show("Code and name are required.", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        DialogResult = true;
        Close();
    }

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
