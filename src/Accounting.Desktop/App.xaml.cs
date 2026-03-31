using System.Windows;
using System.Windows.Threading;
using Accounting.Desktop.Services;
using WpfApp = System.Windows.Application;

namespace Accounting.Desktop;

public partial class App : WpfApp
{
    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        base.OnStartup(e);
        // Default OnLastWindowClose exits the process when the login dialog closes (no window left). Keep running until we show the shell.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var api = new AccountingApiClient(AccountingApiSettings.LoadBaseUrl());
        var login = new LoginWindow(api);
        var ok = login.ShowDialog() == true && login.LoginResult is not null;
        if (!ok)
        {
            Shutdown();
            return;
        }

        var perms = login.LoginResult!.Permissions ?? Array.Empty<string>();
        try
        {
            var shell = new MainWindow(api, perms);
            MainWindow = shell;
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            shell.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "The main window could not open.\n\n" + ex.Message,
                "CharlzTech Accounting",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            MessageBox.Show(
                e.Exception.Message + "\n\n" + e.Exception.StackTrace,
                "Unhandled error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch
        {
            // ignore
        }

        e.Handled = true;
    }
}
