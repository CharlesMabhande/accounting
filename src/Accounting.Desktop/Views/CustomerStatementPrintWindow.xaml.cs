using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Accounting.Application.DTOs;
using Accounting.Desktop.Helpers;
using Microsoft.Win32;

namespace Accounting.Desktop.Views;

public partial class CustomerStatementPrintWindow
{
    private readonly CustomerStatementDto _data;

    public CustomerStatementPrintWindow(CustomerStatementDto data)
    {
        _data = data;
        InitializeComponent();
        Viewer.Document = BuildDocument(data);
    }

    private void SavePdf_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog
        {
            Filter = "PDF (*.pdf)|*.pdf",
            FileName = $"statement-{_data.CustomerCode}-{DateTime.Now:yyyyMMdd}.pdf"
        };
        if (dlg.ShowDialog() != true)
            return;
        try
        {
            CustomerStatementPdfExporter.Save(_data, dlg.FileName);
            MessageBox.Show($"Saved:\n{dlg.FileName}", "PDF", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "PDF export failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Print_Click(object sender, RoutedEventArgs e)
    {
        var pd = new PrintDialog();
        if (pd.ShowDialog() != true)
            return;
        var doc = Viewer.Document;
        if (doc == null)
            return;
        pd.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, "Customer statement");
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private static FlowDocument BuildDocument(CustomerStatementDto d)
    {
        var doc = new FlowDocument
        {
            PagePadding = new Thickness(40),
            ColumnWidth = double.PositiveInfinity,
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 11,
            TextAlignment = TextAlignment.Left
        };

        doc.Blocks.Add(new Paragraph(new Run("Customer statement"))
        {
            FontSize = 22,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 6)
        });
        doc.Blocks.Add(new Paragraph(new Run($"{d.CompanyName}  ·  {d.CompanyCode}"))
        {
            TextAlignment = TextAlignment.Center,
            Foreground = Brushes.DimGray,
            Margin = new Thickness(0, 0, 0, 16)
        });

        doc.Blocks.Add(new Paragraph(new Run($"{d.CustomerCode} — {d.CustomerName}"))
        {
            FontSize = 14,
            FontWeight = FontWeights.SemiBold
        });
        if (!string.IsNullOrWhiteSpace(d.CustomerAddress))
        {
            doc.Blocks.Add(new Paragraph(new Run(d.CustomerAddress))
            {
                Foreground = Brushes.DimGray
            });
        }

        doc.Blocks.Add(new Paragraph(new Run(
                $"Period: {d.PeriodFrom:dd MMM yyyy} to {d.PeriodTo:dd MMM yyyy}   ·   Printed: {d.StatementDate:dd MMM yyyy}"))
        {
            Margin = new Thickness(0, 12, 0, 8)
        });

        doc.Blocks.Add(new Paragraph(new Run($"Opening balance: {d.OpeningBalance:N2}"))
        {
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 8)
        });

        var table = new Table { CellSpacing = 0 };
        for (var i = 0; i < 6; i++)
            table.Columns.Add(new TableColumn());

        var rowGroup = new TableRowGroup();
        var headerRow = new TableRow();
        foreach (var h in new[] { "Date", "Type", "Reference", "Debit", "Credit", "Balance" })
        {
            headerRow.Cells.Add(new TableCell(new Paragraph(new Run(h))
            {
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4)
            }));
        }

        rowGroup.Rows.Add(headerRow);

        foreach (var line in d.Lines)
        {
            var r = new TableRow();
            r.Cells.Add(Cell(line.Date.ToString("dd/MM/yyyy")));
            r.Cells.Add(Cell(line.DocumentType));
            r.Cells.Add(Cell(line.Reference));
            r.Cells.Add(Cell(line.Debit != 0 ? line.Debit.ToString("N2") : ""));
            r.Cells.Add(Cell(line.Credit != 0 ? line.Credit.ToString("N2") : ""));
            r.Cells.Add(Cell(line.Balance.ToString("N2")));
            rowGroup.Rows.Add(r);
        }

        table.RowGroups.Add(rowGroup);
        doc.Blocks.Add(table);

        doc.Blocks.Add(new Paragraph(new Run($"Closing balance: {d.ClosingBalance:N2}"))
        {
            FontWeight = FontWeights.SemiBold,
            FontSize = 13,
            Margin = new Thickness(0, 16, 0, 0)
        });

        doc.Blocks.Add(new Paragraph(new Run("Amounts reflect posted customer invoices and customer receipts in cashbook for the selected period."))
        {
            Foreground = Brushes.Gray,
            FontSize = 10,
            Margin = new Thickness(0, 20, 0, 0)
        });

        return doc;
    }

    private static TableCell Cell(string text) =>
        new(new Paragraph(new Run(text ?? "")));
}
