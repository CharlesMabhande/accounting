using System.Collections.ObjectModel;
using Accounting.Application.Security;

namespace Accounting.Desktop.Shell;

public static class NavigationPermissionFilter
{
    public static ObservableCollection<ShellNavNode> FilterByPermissions(
        ObservableCollection<ShellNavNode> roots,
        HashSet<string> permissions)
    {
        var list = new ObservableCollection<ShellNavNode>();
        foreach (var n in roots)
        {
            var f = FilterNode(n, permissions);
            if (f != null)
                list.Add(f);
        }

        return list;
    }

    private static ShellNavNode? FilterNode(ShellNavNode n, HashSet<string> permissions)
    {
        if (n.Children is { Count: > 0 } ch)
        {
            var kids = new ObservableCollection<ShellNavNode>();
            foreach (var c in ch)
            {
                var fc = FilterNode(c, permissions);
                if (fc != null)
                    kids.Add(fc);
            }

            if (kids.Count > 0)
            {
                return new ShellNavNode
                {
                    Title = n.Title,
                    Caption = n.Caption,
                    IconGlyph = n.IconGlyph,
                    ModuleKey = "",
                    RequiredPermission = n.RequiredPermission,
                    Children = kids
                };
            }
        }

        if (n.CreateView == null)
            return null;

        var req = n.RequiredPermission ??
                  (string.IsNullOrEmpty(n.ModuleKey) ? null : BuiltInPermissions.Nav(n.ModuleKey));
        if (req is not null && !permissions.Contains(req))
            return null;

        return CloneFull(n);
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
}
