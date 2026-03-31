using System.Windows;
using System.Windows.Controls;
using Accounting.Desktop.Helpers;
using Accounting.Desktop.Services;
using Accounting.Desktop.Shell;

namespace Accounting.Desktop.Views;

public partial class DataBrowserView : UserControl
{
    private readonly AccountingApiClient _api;
    private readonly string _urlTemplate;

    public DataBrowserView(AccountingApiClient api, string title, string subtitle, string urlTemplateWithCompanyPlaceholder)
    {
        _api = api;
        _urlTemplate = urlTemplateWithCompanyPlaceholder;
        InitializeComponent();
        TitleText.Text = title;
        SubtitleText.Text = subtitle;
        Loaded += OnLoaded;
        CompanyContext.Changed += OnCompanyChanged;
        Unloaded += OnUnloaded;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        CompanyContext.Changed -= OnCompanyChanged;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e) => await LoadAsync();

    private void OnCompanyChanged() => Dispatcher.InvokeAsync(() => _ = LoadAsync());

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();

    private void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        var owner = Window.GetWindow(this);
        if (owner == null)
            return;
        var name = string.IsNullOrWhiteSpace(TitleText.Text) ? "data" : TitleText.Text.Trim();
        DataGridExportHelper.PromptExportCsv(Grid, owner, name);
    }

    private void ExportPdf_Click(object sender, RoutedEventArgs e)
    {
        var owner = Window.GetWindow(this);
        if (owner == null)
            return;
        var name = string.IsNullOrWhiteSpace(TitleText.Text) ? "data" : TitleText.Text.Trim();
        DataGridExportHelper.PromptExportPdf(Grid, owner, name, name);
    }

    private async Task LoadAsync()
    {
        string url;
        if (_urlTemplate.Contains("{companyId}", StringComparison.Ordinal))
        {
            if (CompanyContext.SelectedCompanyId is not int cid)
            {
                Grid.ItemsSource = null;
                StatusText.Text = "Select a company in the top bar.";
                return;
            }

            url = _urlTemplate.Replace("{companyId}", cid.ToString(), StringComparison.Ordinal);
        }
        else
        {
            url = _urlTemplate;
        }

        StatusText.Text = "Loading…";
        var json = await _api.GetJsonStringAsync(url);
        var view = JsonGridHelper.JsonArrayToView(json);
        Grid.ItemsSource = view;
        StatusText.Text = view == null ? "No data or request failed." : $"{view.Count} row(s).";
    }
}
