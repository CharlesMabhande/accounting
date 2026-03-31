using System.Windows.Controls;
using Accounting.Desktop.Services;

namespace Accounting.Desktop.Views;

public partial class GeneralLedgerView : UserControl
{
    public GeneralLedgerView(AccountingApiClient api)
    {
        InitializeComponent();
        Tabs.Items.Add(new TabItem
        {
            Header = "Chart of accounts",
            Content = new DataBrowserView(api, "Chart of accounts", "Ledger accounts for the selected company.", "api/companies/{companyId}/ChartOfAccounts")
        });
        Tabs.Items.Add(new TabItem
        {
            Header = "Journal entries",
            Content = new DataBrowserView(api, "Journal entries", "Recent journal batches.", "api/companies/{companyId}/inquiry/journal-entries")
        });
        Tabs.Items.Add(new TabItem
        {
            Header = "Trial balance",
            Content = new TrialBalanceView(api)
        });
    }
}
