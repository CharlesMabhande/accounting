namespace Accounting.Desktop.Shell;

public sealed class RecentModuleEntry
{
    public RecentModuleEntry(string moduleKey, string title)
    {
        ModuleKey = moduleKey;
        Title = title;
    }

    public string ModuleKey { get; }
    public string Title { get; }
}
