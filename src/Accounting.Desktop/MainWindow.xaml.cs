using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Accounting.Application.DTOs;
using Accounting.Desktop.Services;
using Accounting.Desktop.Shell;
using Accounting.Desktop.Views;

namespace Accounting.Desktop;

public partial class MainWindow : Window
{
    private readonly AccountingApiClient _api;
    private readonly Dictionary<string, ShellNavNode> _moduleIndex = new(StringComparer.OrdinalIgnoreCase);
    private readonly ObservableCollection<ShellNavNode> _fullNavRoots;
    private readonly ObservableCollection<ShellNavNode> _permissionFilteredRoots;
    private readonly DispatcherTimer _clockTimer;
    private bool _companyComboProgrammatic;

    public ObservableCollection<RecentModuleEntry> RecentModules { get; } = new();

    public MainWindow(AccountingApiClient api, IReadOnlyList<string> permissions)
    {
        _api = api;
        _fullNavRoots = EvolutionNavigationCatalog.Build(_api);
        var permSet = new HashSet<string>(permissions ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
        _permissionFilteredRoots = NavigationPermissionFilter.FilterByPermissions(_fullNavRoots, permSet);
        IndexModules(_permissionFilteredRoots);
        InitializeComponent();
        DataContext = this;
        NavTree.ItemsSource = _permissionFilteredRoots;
        ToolbarApiHint.Text = _api.BaseUrl;

        var (sqlServer, dbName) = AccountingApiSettings.LoadConnectionDisplay();
        StatusServer.Text = $"Server: {sqlServer}";
        StatusDatabase.Text = $"Database: {dbName}";

        ShellNavigationHub.ModuleRequested += OnModuleRequested;

        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) => StatusClock.Text = DateTime.Now.ToString("dddd, dd MMMM yyyy HH:mm:ss", System.Globalization.CultureInfo.CurrentCulture);
        _clockTimer.Start();
        StatusClock.Text = DateTime.Now.ToString("dddd, dd MMMM yyyy HH:mm:ss", System.Globalization.CultureInfo.CurrentCulture);

        if (_moduleIndex.TryGetValue("dashboard", out var dash))
            OpenModule(dash, addRecent: false);
    }

    private void MainWindow_OnLoaded(object sender, RoutedEventArgs e) => _ = LoadCompaniesAsync();

    private async Task LoadCompaniesAsync()
    {
        try
        {
            var companies = await _api.GetCompaniesAsync().ConfigureAwait(true);
            _companyComboProgrammatic = true;
            CompanyCombo.ItemsSource = companies;
            if (companies.Count > 0)
            {
                CompanyCombo.SelectedIndex = 0;
                CompanyContext.Set(companies[0].Id);
                StatusCompany.Text = $"Company: {companies[0].Code} — {companies[0].Name}";
            }
            else
            {
                CompanyContext.Clear();
                StatusCompany.Text = "Company: —";
            }
        }
        catch (Exception ex)
        {
            CompanyContext.Clear();
            StatusCompany.Text = "Company: —";
            MessageBox.Show(
                "Could not load companies. The main window will stay open; try Maintenance → Refresh company list.\n\n" + ex.Message,
                "Companies",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        finally
        {
            _companyComboProgrammatic = false;
        }
    }

    private void IndexModules(IEnumerable<ShellNavNode> nodes)
    {
        foreach (var n in nodes)
        {
            if (!string.IsNullOrEmpty(n.ModuleKey) && n.CreateView != null)
                _moduleIndex[n.ModuleKey] = n;
            if (n.Children != null)
                IndexModules(n.Children);
        }
    }

    private void OnModuleRequested(string moduleKey)
    {
        if (_moduleIndex.TryGetValue(moduleKey, out var node))
            Dispatcher.Invoke(() => OpenModule(node));
    }

    private void NavTree_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is ShellNavNode node && node.CreateView != null)
            OpenModule(node);
    }

    private void OpenModule(ShellNavNode node, bool addRecent = true)
    {
        if (node.CreateView == null)
            return;

        foreach (TabItem existing in WorkspaceTabs.Items.OfType<TabItem>())
        {
            if (existing.Tag is string tag && tag == node.ModuleKey)
            {
                WorkspaceTabs.SelectedItem = existing;
                if (addRecent)
                    PushRecent(node);
                return;
            }
        }

        var tab = new TabItem
        {
            Header = node.Title,
            Tag = node.ModuleKey,
            Content = node.CreateView(),
            ToolTip = node.Caption
        };
        WorkspaceTabs.Items.Add(tab);
        WorkspaceTabs.SelectedItem = tab;
        if (addRecent)
            PushRecent(node);
    }

    private void PushRecent(ShellNavNode node)
    {
        if (string.IsNullOrEmpty(node.ModuleKey))
            return;
        for (var i = RecentModules.Count - 1; i >= 0; i--)
        {
            if (RecentModules[i].ModuleKey == node.ModuleKey)
                RecentModules.RemoveAt(i);
        }

        RecentModules.Insert(0, new RecentModuleEntry(node.ModuleKey, node.Title));
        while (RecentModules.Count > 14)
            RecentModules.RemoveAt(RecentModules.Count - 1);
    }

    private void NavFilterBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var q = NavFilterBox.Text?.Trim() ?? "";
        NavTree.ItemsSource = string.IsNullOrEmpty(q)
            ? _permissionFilteredRoots
            : FilterRoots(_permissionFilteredRoots, q);
    }

    private static ObservableCollection<ShellNavNode> FilterRoots(ObservableCollection<ShellNavNode> roots, string q)
    {
        var list = new ObservableCollection<ShellNavNode>();
        foreach (var n in roots)
        {
            var f = FilterNode(n, q);
            if (f != null)
                list.Add(f);
        }

        return list;
    }

    private static ShellNavNode? FilterNode(ShellNavNode n, string q)
    {
        var childMatches = new List<ShellNavNode>();
        if (n.Children != null)
        {
            foreach (var c in n.Children)
            {
                var fc = FilterNode(c, q);
                if (fc != null)
                    childMatches.Add(fc);
            }
        }

        var self = n.Title.Contains(q, StringComparison.OrdinalIgnoreCase)
            || (n.Caption?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false);

        if (self)
            return CloneFull(n);
        if (childMatches.Count > 0)
        {
            return new ShellNavNode
            {
                Title = n.Title,
                Caption = n.Caption,
                IconGlyph = n.IconGlyph,
                ModuleKey = "",
                RequiredPermission = n.RequiredPermission,
                Children = new ObservableCollection<ShellNavNode>(childMatches)
            };
        }

        return null;
    }

    private static ShellNavNode CloneFull(ShellNavNode n)
    {
        ObservableCollection<ShellNavNode>? kids = null;
        if (n.Children is { Count: > 0 } ch)
            kids = new ObservableCollection<ShellNavNode>(ch.Select(CloneFull));
        return new ShellNavNode
        {
            ModuleKey = n.ModuleKey,
            Title = n.Title,
            Caption = n.Caption,
            IconGlyph = n.IconGlyph,
            RequiredPermission = n.RequiredPermission,
            CreateView = n.CreateView,
            Children = kids
        };
    }

    private void CompanyCombo_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_companyComboProgrammatic)
            return;
        if (CompanyCombo.SelectedItem is CompanyQueryDto c)
        {
            CompanyContext.Set(c.Id);
            StatusCompany.Text = $"Company: {c.Code} — {c.Name}";
        }
        else
        {
            CompanyContext.Clear();
            StatusCompany.Text = "Company: —";
        }
    }

    private void MenuExit_Click(object sender, RoutedEventArgs e) => Close();

    private void MenuHome_Click(object sender, RoutedEventArgs e)
    {
        if (_moduleIndex.TryGetValue("dashboard", out var n))
            OpenModule(n);
    }

    private async void MenuRefreshCompanies_Click(object sender, RoutedEventArgs e) => await LoadCompaniesAsync();

    private void MenuAbout_Click(object sender, RoutedEventArgs e) => OpenOrFocusAboutTab();

    private void OpenOrFocusAboutTab()
    {
        foreach (TabItem existing in WorkspaceTabs.Items.OfType<TabItem>())
        {
            if (existing.Tag is string tag && tag == "about")
            {
                WorkspaceTabs.SelectedItem = existing;
                return;
            }
        }

        var tab = new TabItem
        {
            Header = "About",
            Tag = "about",
            Content = new AboutView(),
            ToolTip = "About CharlzTech Accounting"
        };
        WorkspaceTabs.Items.Add(tab);
        WorkspaceTabs.SelectedItem = tab;
    }

    private void MenuOpenModule_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: string key } && !string.IsNullOrWhiteSpace(key) &&
            _moduleIndex.TryGetValue(key, out var node))
            OpenModule(node);
    }

    private void MenuOpenSwagger_Click(object sender, RoutedEventArgs e)
    {
        var url = _api.BaseUrl.TrimEnd('/') + "/swagger";
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch
        {
            MessageBox.Show($"Open in a browser: {url}", "Swagger", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void BackTab_Click(object sender, RoutedEventArgs e)
    {
        var i = WorkspaceTabs.SelectedIndex;
        if (i > 0)
            WorkspaceTabs.SelectedIndex = i - 1;
    }

    private void MainWindow_OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        ShellNavigationHub.ModuleRequested -= OnModuleRequested;
        _clockTimer.Stop();
        _api.Dispose();
    }
}
