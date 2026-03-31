using Accounting.Application.Abstractions;
using Accounting.Application.DTOs;
using Accounting.Domain.Enums;
using Accounting.Infrastructure.Persistence;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Services;

public sealed class ReportingService : IReportingService
{
    private readonly AccountingDbContext _db;

    public ReportingService(AccountingDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<TrialBalanceLineDto>> GetTrialBalanceAsync(
        int companyId,
        DateOnly asOfDate,
        CancellationToken cancellationToken = default)
    {
        var rows = await (
                from jl in _db.JournalLines.AsNoTracking()
                join je in _db.JournalEntries.AsNoTracking() on jl.JournalEntryId equals je.Id
                join la in _db.LedgerAccounts.AsNoTracking() on jl.LedgerAccountId equals la.Id
                where je.CompanyId == companyId
                      && je.Status == DocumentStatus.Posted
                      && je.EntryDate <= asOfDate
                group new { jl, la } by new { jl.LedgerAccountId, la.Code, la.Name, la.AccountType }
                into g
                select new
                {
                    g.Key.LedgerAccountId,
                    g.Key.Code,
                    g.Key.Name,
                    g.Key.AccountType,
                    Debit = g.Sum(x => x.jl.Debit),
                    Credit = g.Sum(x => x.jl.Credit)
                })
            .OrderBy(x => x.Code)
            .ToListAsync(cancellationToken);

        return rows.Select(r => new TrialBalanceLineDto
        {
            LedgerAccountId = r.LedgerAccountId,
            Code = r.Code,
            Name = r.Name,
            AccountType = r.AccountType.ToString(),
            Debit = r.Debit,
            Credit = r.Credit,
            Balance = r.Debit - r.Credit
        }).ToList();
    }

    public async Task<IReadOnlyList<LedgerLineDto>> GetLedgerAsync(
        int companyId,
        int ledgerAccountId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        var opening = await (
                from jl in _db.JournalLines.AsNoTracking()
                join je in _db.JournalEntries.AsNoTracking() on jl.JournalEntryId equals je.Id
                where je.CompanyId == companyId
                      && je.Status == DocumentStatus.Posted
                      && jl.LedgerAccountId == ledgerAccountId
                      && je.EntryDate < fromDate
                select jl)
            .SumAsync(jl => jl.Debit - jl.Credit, cancellationToken);

        var lines = await (
                from jl in _db.JournalLines.AsNoTracking()
                join je in _db.JournalEntries.AsNoTracking() on jl.JournalEntryId equals je.Id
                where je.CompanyId == companyId
                      && je.Status == DocumentStatus.Posted
                      && jl.LedgerAccountId == ledgerAccountId
                      && je.EntryDate >= fromDate
                      && je.EntryDate <= toDate
                orderby je.EntryDate, je.EntryNumber, jl.LineNumber
                select new { je.EntryDate, je.EntryNumber, je.Reference, je.Description, jl.Debit, jl.Credit })
            .ToListAsync(cancellationToken);

        var result = new List<LedgerLineDto>();
        var running = opening;
        foreach (var x in lines)
        {
            running += x.Debit - x.Credit;
            result.Add(new LedgerLineDto
            {
                EntryDate = x.EntryDate,
                EntryNumber = x.EntryNumber,
                Reference = x.Reference,
                Description = x.Description,
                Debit = x.Debit,
                Credit = x.Credit,
                RunningBalance = running
            });
        }

        return result;
    }

    public async Task<byte[]> ExportTrialBalanceExcelAsync(
        int companyId,
        DateOnly asOfDate,
        CancellationToken cancellationToken = default)
    {
        var rows = await GetTrialBalanceAsync(companyId, asOfDate, cancellationToken);
        using var stream = new MemoryStream();
        using (var wb = new XLWorkbook())
        {
            var ws = wb.Worksheets.Add("Trial Balance");
            ws.Cell(1, 1).Value = "Trial Balance";
            ws.Cell(2, 1).Value = "Company ID";
            ws.Cell(2, 2).Value = companyId;
            ws.Cell(3, 1).Value = "As of";
            ws.Cell(3, 2).Value = asOfDate.ToString("yyyy-MM-dd");
            var r = 5;
            ws.Cell(r, 1).Value = "Code";
            ws.Cell(r, 2).Value = "Name";
            ws.Cell(r, 3).Value = "Type";
            ws.Cell(r, 4).Value = "Debit";
            ws.Cell(r, 5).Value = "Credit";
            ws.Cell(r, 6).Value = "Balance";
            r++;
            foreach (var line in rows)
            {
                ws.Cell(r, 1).Value = line.Code;
                ws.Cell(r, 2).Value = line.Name;
                ws.Cell(r, 3).Value = line.AccountType;
                ws.Cell(r, 4).Value = line.Debit;
                ws.Cell(r, 5).Value = line.Credit;
                ws.Cell(r, 6).Value = line.Balance;
                r++;
            }

            ws.Columns().AdjustToContents();
            wb.SaveAs(stream);
        }

        return stream.ToArray();
    }

    public async Task<CustomerStatementDto?> GetCustomerStatementAsync(
        int companyId,
        int customerId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        var company = await _db.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);
        var customer = await _db.Customers.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == customerId && c.CompanyId == companyId, cancellationToken);
        if (company == null || customer == null)
            return null;

        if (to < from)
            (from, to) = (to, from);

        var openingInvoices = await _db.CustomerInvoices.AsNoTracking()
            .Where(i => i.CompanyId == companyId && i.CustomerId == customerId && i.Status == DocumentStatus.Posted &&
                        i.DocumentDate < from)
            .SumAsync(i => i.Total, cancellationToken);

        var openingReceipts = await _db.CashbookTransactions.AsNoTracking()
            .Where(t => t.CompanyId == companyId && t.CustomerId == customerId && t.Status == DocumentStatus.Posted &&
                        t.IsReceipt && t.TransactionDate < from)
            .SumAsync(t => t.Amount, cancellationToken);

        var openingBalance = openingInvoices - openingReceipts;

        var invoices = await _db.CustomerInvoices.AsNoTracking()
            .Where(i => i.CompanyId == companyId && i.CustomerId == customerId && i.Status == DocumentStatus.Posted &&
                        i.DocumentDate >= from && i.DocumentDate <= to)
            .OrderBy(i => i.DocumentDate).ThenBy(i => i.DocumentNumber)
            .Select(i => new { i.DocumentDate, i.DocumentNumber, i.Total })
            .ToListAsync(cancellationToken);

        var receipts = await _db.CashbookTransactions.AsNoTracking()
            .Where(t => t.CompanyId == companyId && t.CustomerId == customerId && t.Status == DocumentStatus.Posted &&
                        t.IsReceipt && t.TransactionDate >= from && t.TransactionDate <= to)
            .OrderBy(t => t.TransactionDate).ThenBy(t => t.Reference)
            .Select(t => new { t.TransactionDate, t.Reference, t.Description, t.Amount })
            .ToListAsync(cancellationToken);

        var events = new List<(DateOnly Date, string Type, string Ref, string Desc, decimal Dr, decimal Cr)>();
        foreach (var i in invoices)
            events.Add((i.DocumentDate, "Invoice", i.DocumentNumber, "Customer invoice", i.Total, 0));
        foreach (var r in receipts)
            events.Add((r.TransactionDate, "Receipt", r.Reference, r.Description ?? "", 0, r.Amount));

        events.Sort((a, b) =>
        {
            var c = a.Date.CompareTo(b.Date);
            return c != 0 ? c : string.Compare(a.Ref, b.Ref, StringComparison.Ordinal);
        });

        var lines = new List<CustomerStatementLineDto>();
        var balance = openingBalance;
        foreach (var e in events)
        {
            balance += e.Dr - e.Cr;
            lines.Add(new CustomerStatementLineDto
            {
                Date = e.Date,
                DocumentType = e.Type,
                Reference = e.Ref,
                Description = e.Desc,
                Debit = e.Dr,
                Credit = e.Cr,
                Balance = balance
            });
        }

        var addr = string.Join(", ", new[]
        {
            customer.PhysicalAddress1, customer.PhysicalAddress2, customer.PhysicalAddress3, customer.PhysicalCity,
            customer.PostalCode
        }.Where(s => !string.IsNullOrWhiteSpace(s)));

        return new CustomerStatementDto
        {
            CompanyCode = company.Code,
            CompanyName = company.Name,
            CustomerCode = customer.Code,
            CustomerName = customer.Name,
            CustomerAddress = string.IsNullOrWhiteSpace(addr) ? null : addr,
            PeriodFrom = from,
            PeriodTo = to,
            StatementDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            OpeningBalance = openingBalance,
            ClosingBalance = balance,
            Lines = lines
        };
    }
}
