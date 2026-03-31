using Accounting.Application.DTOs;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace Accounting.Desktop.Helpers;

public static class CustomerStatementPdfExporter
{
    public static void Save(CustomerStatementDto d, string filePath)
    {
        var doc = new PdfDocument();
        var page = doc.AddPage();
        page.Width = XUnit.FromPoint(595);
        page.Height = XUnit.FromPoint(842);
        var gfx = XGraphics.FromPdfPage(page);
        var titleFont = new XFont("Arial", 16, XFontStyleEx.Bold);
        var headFont = new XFont("Arial", 11, XFontStyleEx.Bold);
        var bodyFont = new XFont("Arial", 9, XFontStyleEx.Regular);
        var smallFont = new XFont("Arial", 8, XFontStyleEx.Regular);

        double x = 40, y = 40;
        gfx.DrawString("Customer statement", titleFont, XBrushes.DarkBlue, new XRect(x, y, page.Width - 80, 24), XStringFormats.TopLeft);
        y += 28;
        gfx.DrawString($"{d.CompanyName} · {d.CompanyCode}", bodyFont, XBrushes.Gray, new XRect(x, y, page.Width - 80, 16), XStringFormats.TopLeft);
        y += 20;
        gfx.DrawString($"{d.CustomerCode} — {d.CustomerName}", headFont, XBrushes.Black, new XRect(x, y, page.Width - 80, 18), XStringFormats.TopLeft);
        y += 22;
        if (!string.IsNullOrWhiteSpace(d.CustomerAddress))
        {
            gfx.DrawString(d.CustomerAddress!, smallFont, XBrushes.Gray, new XRect(x, y, page.Width - 80, 40), XStringFormats.TopLeft);
            y += 36;
        }

        gfx.DrawString(
            $"Period: {d.PeriodFrom:dd MMM yyyy} to {d.PeriodTo:dd MMM yyyy}   ·   Statement date: {d.StatementDate:dd MMM yyyy}",
            bodyFont, XBrushes.Black, new XRect(x, y, page.Width - 80, 16), XStringFormats.TopLeft);
        y += 22;
        gfx.DrawString($"Opening balance: {d.OpeningBalance:N2}", headFont, XBrushes.Black, new XRect(x, y, page.Width - 80, 16), XStringFormats.TopLeft);
        y += 22;

        gfx.DrawString("Date", headFont, XBrushes.Black, x, y);
        gfx.DrawString("Type", headFont, XBrushes.Black, x + 70, y);
        gfx.DrawString("Reference", headFont, XBrushes.Black, x + 130, y);
        gfx.DrawString("Debit", headFont, XBrushes.Black, x + 300, y);
        gfx.DrawString("Credit", headFont, XBrushes.Black, x + 360, y);
        gfx.DrawString("Balance", headFont, XBrushes.Black, x + 420, y);
        y += 16;

        foreach (var line in d.Lines)
        {
            if (y > page.Height - 100)
            {
                page = doc.AddPage();
                page.Width = XUnit.FromPoint(595);
                page.Height = XUnit.FromPoint(842);
                gfx = XGraphics.FromPdfPage(page);
                y = 40;
            }

            gfx.DrawString(line.Date.ToString("dd/MM/yyyy"), bodyFont, XBrushes.Black, x, y);
            gfx.DrawString(line.DocumentType, bodyFont, XBrushes.Black, x + 70, y);
            gfx.DrawString(line.Reference, bodyFont, XBrushes.Black, x + 130, y);
            gfx.DrawString(line.Debit != 0 ? line.Debit.ToString("N2") : "", bodyFont, XBrushes.Black, x + 300, y);
            gfx.DrawString(line.Credit != 0 ? line.Credit.ToString("N2") : "", bodyFont, XBrushes.Black, x + 360, y);
            gfx.DrawString(line.Balance.ToString("N2"), bodyFont, XBrushes.Black, x + 420, y);
            y += 14;
        }

        y += 10;
        gfx.DrawString($"Closing balance: {d.ClosingBalance:N2}", headFont, XBrushes.Black, new XRect(x, y, page.Width - 80, 18), XStringFormats.TopLeft);

        doc.Save(filePath);
    }
}
