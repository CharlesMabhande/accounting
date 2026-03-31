using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace Accounting.Desktop.Shell;

/// <summary>Tree node for Explorer-style navigation (folder or module).</summary>
public sealed class ShellNavNode
{
    public string ModuleKey { get; init; } = "";
    public string Title { get; init; } = "";
    public string? Caption { get; init; }
    /// <summary>Single Segoe MDL2 Assets character (see <see cref="NavIcons"/>).</summary>
    public string IconGlyph { get; init; } = "\uE8A5";
    /// <summary>Permission name (e.g. nav.dashboard). If null, derived from <see cref="ModuleKey"/> when filtering.</summary>
    public string? RequiredPermission { get; init; }
    public ObservableCollection<ShellNavNode>? Children { get; init; }
    public Func<UserControl>? CreateView { get; init; }
}
