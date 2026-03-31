using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Win32;

namespace Accounting.Desktop.Helpers;

/// <summary>Save plain text (e.g. import logs) to disk.</summary>
public static class TextExportHelper
{
    public static void PromptSaveText(Window owner, string content, string defaultFileNameBase)
    {
        var dlg = new SaveFileDialog
        {
            Filter = "Text (*.txt)|*.txt|All files (*.*)|*.*",
            FileName = $"{defaultFileNameBase}-{DateTime.Now:yyyyMMdd-HHmmss}.txt"
        };
        if (dlg.ShowDialog() != true)
            return;
        try
        {
            File.WriteAllText(dlg.FileName, content ?? "", new UTF8Encoding(true));
            MessageBox.Show($"Saved:\n{dlg.FileName}", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Export failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
