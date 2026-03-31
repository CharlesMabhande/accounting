using System.Linq;
using System.Windows.Controls;
using Accounting.Desktop.Services;

namespace Accounting.Desktop.Views;

public partial class TabbedWorkspaceView : UserControl
{
    /// <summary>Each tab is either a read-only data browser or any other control (e.g. editable master data).</summary>
    public TabbedWorkspaceView(params (string Header, UserControl Content)[] tabs)
    {
        InitializeComponent();
        foreach (var t in tabs)
        {
            Tabs.Items.Add(new TabItem
            {
                Header = t.Header,
                Content = t.Content
            });
        }
    }

    /// <summary>Convenience: all tabs use <see cref="DataBrowserView"/> with the given URL templates.</summary>
    public TabbedWorkspaceView(AccountingApiClient api, params (string Header, string Title, string Subtitle, string UrlTemplate)[] tabs)
        : this(tabs.Select(t => (t.Header, (UserControl)new DataBrowserView(api, t.Title, t.Subtitle, t.UrlTemplate))).ToArray())
    {
    }
}
