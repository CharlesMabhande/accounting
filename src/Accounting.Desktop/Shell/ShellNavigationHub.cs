namespace Accounting.Desktop.Shell;

/// <summary>Lets dashboard tiles request opening a module without coupling to <see cref="MainWindow"/>.</summary>
public static class ShellNavigationHub
{
    public static event Action<string>? ModuleRequested;

    public static void OpenModule(string moduleKey) =>
        ModuleRequested?.Invoke(moduleKey);
}
