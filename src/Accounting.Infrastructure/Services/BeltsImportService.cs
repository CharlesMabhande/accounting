using Accounting.Application.DTOs;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Accounting.Infrastructure.Services;

/// <summary>
/// Imports master data from a legacy SQL database (e.g. <c>belts</c>) into AccountingDb.
/// Maps <c>StkItem</c>, <c>Client</c>, and <c>Vendor</c> fields only—no wholesale copy of external schemas.
/// </summary>
public sealed class BeltsImportService
{
    private readonly AccountingDbContext _db;
    private readonly IConfiguration _configuration;

    public BeltsImportService(AccountingDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    public async Task<BeltsImportResultDto> ImportAsync(int companyId, BeltsImportRequest request, CancellationToken cancellationToken = default)
    {
        var cs = _configuration["Belts:SourceConnectionString"];
        if (string.IsNullOrWhiteSpace(cs))
            cs = "Server=.\\SQLEXPRESS;Database=belts;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

        if (!await _db.Companies.AsNoTracking().AnyAsync(c => c.Id == companyId, cancellationToken))
            return new BeltsImportResultDto { Message = "Company not found." };

        var inv = await _db.LedgerAccounts.AsNoTracking()
            .Where(a => a.CompanyId == companyId && a.Code == "1300")
            .Select(a => a.Id)
            .FirstOrDefaultAsync(cancellationToken);
        var cos = await _db.LedgerAccounts.AsNoTracking()
            .Where(a => a.CompanyId == companyId && a.Code == "5000")
            .Select(a => a.Id)
            .FirstOrDefaultAsync(cancellationToken);
        var ar = await _db.LedgerAccounts.AsNoTracking()
            .Where(a => a.CompanyId == companyId && a.Code == "1200")
            .Select(a => a.Id)
            .FirstOrDefaultAsync(cancellationToken);
        var ap = await _db.LedgerAccounts.AsNoTracking()
            .Where(a => a.CompanyId == companyId && a.Code == "2000")
            .Select(a => a.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (request.ImportStockItems && (inv == 0 || cos == 0))
            return new BeltsImportResultDto { Message = "Company must have ledger accounts 1300 (Inventory) and 5000 (COS) for stock import." };

        var result = new ImportCounters();
        await using var conn = new SqlConnection(cs);
        await conn.OpenAsync(cancellationToken);

        if (request.ImportStockItems)
            await ImportStkItemsAsync(companyId, conn, inv, cos, request.OverwriteExisting, result, cancellationToken);

        if (request.ImportCustomers && ar != 0)
            await ImportClientsAsync(companyId, conn, ar, request.OverwriteExisting, result, cancellationToken);

        if (request.ImportSuppliers && ap != 0)
            await ImportVendorsAsync(companyId, conn, ap, request.OverwriteExisting, result, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        return new BeltsImportResultDto
        {
            StockItemsInserted = result.StockIns,
            StockItemsUpdated = result.StockUpd,
            StockItemsSkipped = result.StockSkip,
            CustomersInserted = result.CustIns,
            CustomersUpdated = result.CustUpd,
            CustomersSkipped = result.CustSkip,
            SuppliersInserted = result.SuppIns,
            SuppliersUpdated = result.SuppUpd,
            SuppliersSkipped = result.SuppSkip,
            Errors = result.Errors,
            Message = "Import completed."
        };
    }

    private sealed class ImportCounters
    {
        public int StockIns, StockUpd, StockSkip, CustIns, CustUpd, CustSkip, SuppIns, SuppUpd, SuppSkip, Errors;
    }

    private async Task ImportStkItemsAsync(int companyId, SqlConnection conn, int invId, int cosId, bool overwrite, ImportCounters r, CancellationToken ct)
    {
        const string sql = """
            SELECT Code, Description_1, Description_2, Description_3, cExtDescription, cSimpleCode,
                   ItemActive, ServiceItem, fStockGPPercent,
                   fBuyLength, fBuyWidth, fBuyHeight, fSellLength, fSellWidth, fSellHeight,
                   cBuyWeight, cBuyUnit, cMeasurement
            FROM StkItem
            """;

        await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 120 };
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var existing = await _db.StockItems.Where(s => s.CompanyId == companyId).ToDictionaryAsync(s => s.Code, StringComparer.OrdinalIgnoreCase, ct);

        while (await reader.ReadAsync(ct))
        {
            try
            {
                var code = Truncate(NormalizeCode(reader["Code"]), 80);
                if (string.IsNullOrEmpty(code))
                {
                    r.StockSkip++;
                    continue;
                }

                var d1 = reader["Description_1"] as string ?? "";
                var d2 = reader["Description_2"] as string ?? "";
                var d3 = reader["Description_3"] as string ?? "";
                var desc = string.Join(" ", new[] { d1, d2, d3 }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
                if (desc.Length == 0)
                    desc = code;

                var longDesc = reader["cExtDescription"] as string;
                var alt = reader["cSimpleCode"] as string;
                var active = ReadBool(reader["ItemActive"], true);
                var service = ReadBool(reader["ServiceItem"], false);
                decimal? gp = reader["fStockGPPercent"] is DBNull ? null : Convert.ToDecimal(reader["fStockGPPercent"]);
                decimal? bl = ReadDecimal(reader["fBuyLength"]);
                decimal? bw = ReadDecimal(reader["fBuyWidth"]);
                decimal? bh = ReadDecimal(reader["fBuyHeight"]);
                decimal? sl = ReadDecimal(reader["fSellLength"]);
                decimal? sw = ReadDecimal(reader["fSellWidth"]);
                decimal? sh = ReadDecimal(reader["fSellHeight"]);
                decimal? w = ReadDecimal(reader["cBuyWeight"]);
                var wu = reader["cBuyUnit"] as string;
                var meas = reader["cMeasurement"] as string;

                if (existing.TryGetValue(code, out var row))
                {
                    if (!overwrite)
                    {
                        r.StockSkip++;
                        continue;
                    }

                    row.Description = Truncate(desc, 500) ?? desc;
                    row.LongDescription = Truncate(longDesc, 500);
                    row.AlternateCode = Truncate(alt, 40);
                    row.IsActive = active;
                    row.IsServiceItem = service;
                    row.TargetGpPercent = gp;
                    row.BuyLength = bl;
                    row.BuyWidth = bw;
                    row.BuyHeight = bh;
                    row.SellLength = sl;
                    row.SellWidth = sw;
                    row.SellHeight = sh;
                    row.Weight = w;
                    row.WeightUnit = Truncate(wu, 10);
                    row.MeasurementNotes = Truncate(meas, 200);
                    row.ModifiedAtUtc = DateTime.UtcNow;
                    r.StockUpd++;
                }
                else
                {
                    var e = new StockItem
                    {
                        CompanyId = companyId,
                        Code = code,
                        Description = Truncate(desc, 500) ?? desc,
                        LongDescription = Truncate(longDesc, 500),
                        AlternateCode = Truncate(alt, 40),
                        UnitOfMeasure = "EA",
                        InventoryAccountId = invId,
                        CostOfSalesAccountId = cosId,
                        IsActive = active,
                        IsServiceItem = service,
                        TargetGpPercent = gp,
                        BuyLength = bl,
                        BuyWidth = bw,
                        BuyHeight = bh,
                        SellLength = sl,
                        SellWidth = sw,
                        SellHeight = sh,
                        Weight = w,
                        WeightUnit = Truncate(wu, 10),
                        MeasurementNotes = Truncate(meas, 200),
                        CreatedAtUtc = DateTime.UtcNow
                    };
                    _db.StockItems.Add(e);
                    existing[code] = e;
                    r.StockIns++;
                }
            }
            catch
            {
                r.Errors++;
            }
        }
    }

    private async Task ImportClientsAsync(int companyId, SqlConnection conn, int arId, bool overwrite, ImportCounters r, CancellationToken ct)
    {
        const string sql = """
            SELECT Account, Name, Contact_Person, Physical1, Physical2, Physical3, Physical4, Physical5,
                   Post1, Post2, Post3, Post4, PostPC, Telephone, EMail, Tax_Number, Credit_Limit, On_Hold, Registration
            FROM Client
            """;

        await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 120 };
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var existing = await _db.Customers.Where(c => c.CompanyId == companyId).ToDictionaryAsync(c => c.Code, StringComparer.OrdinalIgnoreCase, ct);

        while (await reader.ReadAsync(ct))
        {
            try
            {
                var code = Truncate(NormalizeCode(reader["Account"]), 80);
                var name = Truncate(reader["Name"] as string ?? code, 300);
                if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(name))
                {
                    r.CustSkip++;
                    continue;
                }

                var contact = reader["Contact_Person"] as string;
                var phys1 = reader["Physical1"] as string;
                var phys2 = reader["Physical2"] as string;
                var phys3 = reader["Physical3"] as string;
                var phys4 = reader["Physical4"] as string;
                var phys5 = reader["Physical5"] as string;
                var post1 = reader["Post1"] as string;
                var post2 = reader["Post2"] as string;
                var post3 = reader["Post3"] as string;
                var postPc = reader["PostPC"] as string;
                var phone = reader["Telephone"] as string;
                var email = reader["EMail"] as string;
                var tax = reader["Tax_Number"] as string;
                var reg = reader["Registration"] as string;
                decimal? credit = reader["Credit_Limit"] is DBNull ? null : Convert.ToDecimal(reader["Credit_Limit"]);
                var onHold = ReadBool(reader["On_Hold"], false);

                if (existing.TryGetValue(code, out var row))
                {
                    if (!overwrite)
                    {
                        r.CustSkip++;
                        continue;
                    }

                    row.Name = name;
                    row.ContactName = Truncate(contact, 200);
                    row.Phone = Truncate(phone, 50);
                    row.Email = Truncate(email, 255);
                    row.PhysicalAddress1 = Truncate(phys1, 200);
                    row.PhysicalAddress2 = Truncate(phys2, 200);
                    row.PhysicalAddress3 = Truncate(phys3, 200);
                    row.PhysicalCity = Truncate(string.Join(" ", new[] { phys4, phys5 }.Where(s => !string.IsNullOrWhiteSpace(s))), 120);
                    row.PostalAddress1 = Truncate(post1, 200);
                    row.PostalAddress2 = Truncate(post2, 200);
                    row.PostalAddress3 = Truncate(post3, 200);
                    row.PostalCode = Truncate(postPc, 30);
                    row.TaxNumber = Truncate(tax, 80);
                    row.RegistrationNumber = Truncate(reg, 80);
                    row.CreditLimit = credit;
                    row.OnHold = onHold;
                    row.ModifiedAtUtc = DateTime.UtcNow;
                    r.CustUpd++;
                }
                else
                {
                    var e = new Customer
                    {
                        CompanyId = companyId,
                        Code = code,
                        Name = name,
                        AccountsReceivableAccountId = arId,
                        CurrencyCode = "USD",
                        IsActive = true,
                        ContactName = Truncate(contact, 200),
                        Phone = Truncate(phone, 50),
                        Email = Truncate(email, 255),
                        PhysicalAddress1 = Truncate(phys1, 200),
                        PhysicalAddress2 = Truncate(phys2, 200),
                        PhysicalAddress3 = Truncate(phys3, 200),
                        PhysicalCity = Truncate(string.Join(" ", new[] { phys4, phys5 }.Where(s => !string.IsNullOrWhiteSpace(s))), 120),
                        PostalAddress1 = Truncate(post1, 200),
                        PostalAddress2 = Truncate(post2, 200),
                        PostalAddress3 = Truncate(post3, 200),
                        PostalCode = Truncate(postPc, 30),
                        TaxNumber = Truncate(tax, 80),
                        RegistrationNumber = Truncate(reg, 80),
                        CreditLimit = credit,
                        OnHold = onHold,
                        CreatedAtUtc = DateTime.UtcNow
                    };
                    _db.Customers.Add(e);
                    existing[code] = e;
                    r.CustIns++;
                }
            }
            catch
            {
                r.Errors++;
            }
        }
    }

    private async Task ImportVendorsAsync(int companyId, SqlConnection conn, int apId, bool overwrite, ImportCounters r, CancellationToken ct)
    {
        const string sql = """
            SELECT Account, Name, Contact_Person, Physical1, Physical2, Physical3, Physical4,
                   Post1, Post2, Post3, PostPC, Telephone, EMail, Tax_Number, On_Hold, Registration
            FROM Vendor
            """;

        await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 120 };
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var existing = await _db.Suppliers.Where(s => s.CompanyId == companyId).ToDictionaryAsync(s => s.Code, StringComparer.OrdinalIgnoreCase, ct);

        while (await reader.ReadAsync(ct))
        {
            try
            {
                var code = Truncate(NormalizeCode(reader["Account"]), 80);
                var name = Truncate(reader["Name"] as string ?? code, 300);
                if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(name))
                {
                    r.SuppSkip++;
                    continue;
                }

                var contact = reader["Contact_Person"] as string;
                var phys1 = reader["Physical1"] as string;
                var phys2 = reader["Physical2"] as string;
                var phys3 = reader["Physical3"] as string;
                var phys4 = reader["Physical4"] as string;
                var post1 = reader["Post1"] as string;
                var post2 = reader["Post2"] as string;
                var post3 = reader["Post3"] as string;
                var postPc = reader["PostPC"] as string;
                var phone = reader["Telephone"] as string;
                var email = reader["EMail"] as string;
                var tax = reader["Tax_Number"] as string;
                var reg = reader["Registration"] as string;
                var onHold = ReadBool(reader["On_Hold"], false);

                if (existing.TryGetValue(code, out var row))
                {
                    if (!overwrite)
                    {
                        r.SuppSkip++;
                        continue;
                    }

                    row.Name = name;
                    row.ContactName = Truncate(contact, 200);
                    row.Phone = Truncate(phone, 50);
                    row.Email = Truncate(email, 255);
                    row.PhysicalAddress1 = Truncate(phys1, 200);
                    row.PhysicalAddress2 = Truncate(phys2, 200);
                    row.PhysicalAddress3 = Truncate(phys3, 200);
                    row.PhysicalCity = Truncate(phys4, 120);
                    row.PostalAddress1 = Truncate(post1, 200);
                    row.PostalAddress2 = Truncate(post2, 200);
                    row.PostalAddress3 = Truncate(post3, 200);
                    row.PostalCode = Truncate(postPc, 30);
                    row.TaxNumber = Truncate(tax, 80);
                    row.RegistrationNumber = Truncate(reg, 80);
                    row.OnHold = onHold;
                    row.ModifiedAtUtc = DateTime.UtcNow;
                    r.SuppUpd++;
                }
                else
                {
                    var e = new Supplier
                    {
                        CompanyId = companyId,
                        Code = code,
                        Name = name,
                        AccountsPayableAccountId = apId,
                        CurrencyCode = "USD",
                        IsActive = true,
                        ContactName = Truncate(contact, 200),
                        Phone = Truncate(phone, 50),
                        Email = Truncate(email, 255),
                        PhysicalAddress1 = Truncate(phys1, 200),
                        PhysicalAddress2 = Truncate(phys2, 200),
                        PhysicalAddress3 = Truncate(phys3, 200),
                        PhysicalCity = Truncate(phys4, 120),
                        PostalAddress1 = Truncate(post1, 200),
                        PostalAddress2 = Truncate(post2, 200),
                        PostalAddress3 = Truncate(post3, 200),
                        PostalCode = Truncate(postPc, 30),
                        TaxNumber = Truncate(tax, 80),
                        RegistrationNumber = Truncate(reg, 80),
                        OnHold = onHold,
                        CreatedAtUtc = DateTime.UtcNow
                    };
                    _db.Suppliers.Add(e);
                    existing[code] = e;
                    r.SuppIns++;
                }
            }
            catch
            {
                r.Errors++;
            }
        }
    }

    private static string? Truncate(string? s, int max)
    {
        if (string.IsNullOrEmpty(s))
            return null;
        s = s.Trim();
        return s.Length <= max ? s : s[..max];
    }

    private static string NormalizeCode(object? o)
    {
        if (o is null or DBNull)
            return "";
        return o.ToString()?.Trim() ?? "";
    }

    private static decimal? ReadDecimal(object? o)
    {
        if (o is null or DBNull)
            return null;
        try
        {
            return Convert.ToDecimal(o);
        }
        catch
        {
            return null;
        }
    }

    private static bool ReadBool(object? o, bool defaultValue)
    {
        if (o is null or DBNull)
            return defaultValue;
        if (o is bool b)
            return b;
        return Convert.ToInt32(o, System.Globalization.CultureInfo.InvariantCulture) != 0;
    }
}
