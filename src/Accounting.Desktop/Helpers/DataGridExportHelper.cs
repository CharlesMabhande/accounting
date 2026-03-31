using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace Accounting.Desktop.Helpers;

/// <summary>CSV and PDF export for WPF <see cref="DataGrid"/> (DataView-backed inquiry grids and DTO-bound master screens).</summary>
public static class DataGridExportHelper
{
    public static void PromptExportCsv(DataGrid grid, Window owner, string defaultFileNameBase)
    {
        if (!HasRows(grid))
        {
            MessageBox.Show("Nothing to export.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dlg = new SaveFileDialog
        {
            Filter = "CSV (*.csv)|*.csv|All files (*.*)|*.*",
            FileName = $"{SanitizeFileName(defaultFileNameBase)}-{DateTime.Now:yyyyMMdd-HHmmss}.csv"
        };
        if (dlg.ShowDialog() != true)
            return;
        try
        {
            ExportToCsv(grid, dlg.FileName);
            MessageBox.Show($"Saved:\n{dlg.FileName}", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Export failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    public static void PromptExportPdf(DataGrid grid, Window owner, string defaultFileNameBase, string title)
    {
        if (!HasRows(grid))
        {
            MessageBox.Show("Nothing to export.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dlg = new SaveFileDialog
        {
            Filter = "PDF (*.pdf)|*.pdf|All files (*.*)|*.*",
            FileName = $"{SanitizeFileName(defaultFileNameBase)}-{DateTime.Now:yyyyMMdd-HHmmss}.pdf"
        };
        if (dlg.ShowDialog() != true)
            return;
        try
        {
            ExportToPdf(grid, dlg.FileName, title);
            MessageBox.Show($"Saved:\n{dlg.FileName}", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Export failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    public static void ExportToCsv(DataGrid grid, string path)
    {
        using var sw = new StreamWriter(path, false, new UTF8Encoding(true));
        var table = TryGetDataTable(grid);
        if (table != null)
        {
            WriteDataTableCsv(sw, table);
            return;
        }

        var paths = GetOrderedBindingPaths(grid);
        if (paths.Count == 0)
            throw new InvalidOperationException("No exportable columns (add explicit columns or use a data inquiry screen).");

        for (var i = 0; i < paths.Count; i++)
        {
            if (i > 0)
                sw.Write(',');
            sw.Write(EscapeCsv(paths[i].Header));
        }

        sw.WriteLine();
        foreach (var item in grid.Items)
        {
            if (item is not { } row)
                continue;
            for (var i = 0; i < paths.Count; i++)
            {
                if (i > 0)
                    sw.Write(',');
                var v = GetNestedValue(row, paths[i].Path);
                sw.Write(EscapeCsv(FormatCell(v)));
            }

            sw.WriteLine();
        }
    }

    public static void ExportToPdf(DataGrid grid, string path, string title)
    {
        var table = TryGetDataTable(grid);
        if (table != null)
        {
            WriteDataTablePdf(path, title, table);
            return;
        }

        var paths = GetOrderedBindingPaths(grid);
        if (paths.Count == 0)
            throw new InvalidOperationException("No exportable columns.");

        var headers = paths.Select(p => p.Header).ToList();
        var rows = new List<string[]>();
        foreach (var item in grid.Items)
        {
            if (item is not { } row)
                continue;
            var cells = new string[paths.Count];
            for (var i = 0; i < paths.Count; i++)
                cells[i] = FormatCell(GetNestedValue(row, paths[i].Path));
            rows.Add(cells);
        }

        WriteRowsPdf(path, title, headers, rows);
    }

    private static bool HasRows(DataGrid grid) => grid.Items != null && grid.Items.Count > 0;

    private static DataTable? TryGetDataTable(DataGrid grid)
    {
        switch (grid.ItemsSource)
        {
            case DataView dv:
                return dv.Table;
            case ICollectionView cv when cv.SourceCollection is DataView dv2:
                return dv2.Table;
            default:
                return null;
        }
    }

    private sealed record ColumnSpec(string Header, string Path);

    private static List<ColumnSpec> GetOrderedBindingPaths(DataGrid grid)
    {
        var list = new List<(int DisplayIndex, ColumnSpec Spec)>();
        foreach (var col in grid.Columns.OrderBy(c => c.DisplayIndex))
        {
            var path = GetBindingPath(col);
            if (string.IsNullOrEmpty(path))
                continue;
            var header = col.Header?.ToString() ?? path;
            list.Add((col.DisplayIndex, new ColumnSpec(header, path)));
        }

        return list.OrderBy(x => x.DisplayIndex).Select(x => x.Spec).ToList();
    }

    private static string? GetBindingPath(DataGridColumn col)
    {
        switch (col)
        {
            case DataGridBoundColumn b when b.Binding is Binding bb:
                return bb.Path.Path;
            case DataGridCheckBoxColumn cb when cb.Binding is Binding cbb:
                return cbb.Path.Path;
            default:
                return null;
        }
    }

    private static object? GetNestedValue(object obj, string path)
    {
        object? current = obj;
        foreach (var part in path.Split('.'))
        {
            if (current == null)
                return null;
            var t = current.GetType();
            var p = t.GetProperty(part);
            current = p?.GetValue(current);
        }

        return current;
    }

    private static string FormatCell(object? v) =>
        v switch
        {
            null => "",
            IFormattable f => f.ToString(null, CultureInfo.CurrentCulture),
            _ => v.ToString() ?? ""
        };

    private static void WriteDataTableCsv(TextWriter sw, DataTable table)
    {
        for (var c = 0; c < table.Columns.Count; c++)
        {
            if (c > 0)
                sw.Write(',');
            sw.Write(EscapeCsv(table.Columns[c].ColumnName));
        }

        sw.WriteLine();
        foreach (DataRow row in table.Rows)
        {
            for (var c = 0; c < table.Columns.Count; c++)
            {
                if (c > 0)
                    sw.Write(',');
                sw.Write(EscapeCsv(row[c]?.ToString() ?? ""));
            }

            sw.WriteLine();
        }
    }

    private static void WriteDataTablePdf(string path, string title, DataTable table)
    {
        var headers = table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
        var rows = new List<string[]>();
        foreach (DataRow row in table.Rows)
        {
            var cells = new string[table.Columns.Count];
            for (var c = 0; c < table.Columns.Count; c++)
                cells[c] = row[c]?.ToString() ?? "";
            rows.Add(cells);
        }

        WriteRowsPdf(path, title, headers, rows);
    }

    private static void WriteRowsPdf(string path, string title, IReadOnlyList<string> headers, IReadOnlyList<string[]> rows)
    {
        var doc = new PdfDocument();
        var page = doc.AddPage();
        page.Width = XUnit.FromPoint(595);
        page.Height = XUnit.FromPoint(842);
        var gfx = XGraphics.FromPdfPage(page);
        var titleFont = new XFont("Arial", 12, XFontStyleEx.Bold);
        var headFont = new XFont("Arial", 8, XFontStyleEx.Bold);
        var bodyFont = new XFont("Arial", 7, XFontStyleEx.Regular);

        const double margin = 40;
        double y = margin;
        gfx.DrawString(title, titleFont, XBrushes.DarkBlue, new XRect(margin, y, page.Width - 2 * margin, 20),
            XStringFormats.TopLeft);
        y += 28;

        var colCount = Math.Max(1, headers.Count);
        var usable = page.Width.Point - 2 * margin;
        var colW = usable / colCount;
        const double rowH = 12;
        const int maxCellChars = 40;

        void NewPage()
        {
            gfx.Dispose();
            page = doc.AddPage();
            page.Width = XUnit.FromPoint(595);
            page.Height = XUnit.FromPoint(842);
            gfx = XGraphics.FromPdfPage(page);
            y = margin;
        }

        void DrawHeaderRow()
        {
            double x = margin;
            for (var i = 0; i < headers.Count; i++)
            {
                var h = Truncate(headers[i], maxCellChars);
                gfx.DrawString(h, headFont, XBrushes.Black, new XRect(x, y, colW - 2, rowH), XStringFormats.TopLeft);
                x += colW;
            }

            y += rowH + 2;
        }

        DrawHeaderRow();

        foreach (var row in rows)
        {
            if (y > page.Height.Point - margin - rowH)
            {
                NewPage();
                DrawHeaderRow();
            }

            double x = margin;
            for (var i = 0; i < colCount; i++)
            {
                var cell = i < row.Length ? Truncate(row[i], maxCellChars) : "";
                gfx.DrawString(cell, bodyFont, XBrushes.Black, new XRect(x, y, colW - 2, rowH), XStringFormats.TopLeft);
                x += colW;
            }

            y += rowH;
        }

        gfx.Dispose();
        doc.Save(path);
    }

    private static string Truncate(string s, int maxChars)
    {
        if (string.IsNullOrEmpty(s))
            return "";
        return s.Length <= maxChars ? s : s[..maxChars] + "…";
    }

    private static string EscapeCsv(string? s)
    {
        if (string.IsNullOrEmpty(s))
            return "";
        if (s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r'))
            return "\"" + s.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
        return s;
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(name.Length);
        foreach (var c in name)
            sb.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
        var t = sb.ToString().Trim();
        return string.IsNullOrEmpty(t) ? "export" : t;
    }
}
