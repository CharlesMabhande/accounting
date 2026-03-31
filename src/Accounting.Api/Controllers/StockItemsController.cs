using Accounting.Application.Abstractions;
using Accounting.Application.DTOs;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Api.Controllers;

[ApiController]
[Route("api/companies/{companyId:int}/[controller]")]
public sealed class StockItemsController : ControllerBase
{
    private readonly IStockItemQueryService _query;
    private readonly AccountingDbContext _db;

    public StockItemsController(IStockItemQueryService query, AccountingDbContext db)
    {
        _query = query;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> List(int companyId, CancellationToken cancellationToken)
    {
        return Ok(await _query.ListItemsAsync(companyId, cancellationToken));
    }

    [HttpGet("warehouse-stock")]
    public async Task<IActionResult> WarehouseStock(int companyId, CancellationToken cancellationToken)
    {
        return Ok(await _query.ListWarehouseStockAsync(companyId, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<StockItemQueryDto>> Create(int companyId, [FromBody] UpsertStockItemRequest request, CancellationToken cancellationToken)
    {
        if (!await _db.Companies.AsNoTracking().AnyAsync(c => c.Id == companyId, cancellationToken))
            return NotFound("Company not found.");

        var code = (request.Code ?? "").Trim();
        var desc = (request.Description ?? "").Trim();
        if (code.Length == 0 || desc.Length == 0)
            return BadRequest("Code and description are required.");

        if (await _db.StockItems.AnyAsync(x => x.CompanyId == companyId && x.Code == code, cancellationToken))
            return Conflict("A stock item with this code already exists.");

        if (!await LedgerOkAsync(companyId, request.InventoryAccountId, cancellationToken)
            || !await LedgerOkAsync(companyId, request.CostOfSalesAccountId, cancellationToken))
            return BadRequest("Inventory and COS ledger accounts must exist for this company and be postable.");

        var entity = new StockItem
        {
            CompanyId = companyId,
            Code = code,
            Description = desc,
            LongDescription = NullIfEmpty(request.LongDescription),
            AlternateCode = NullIfEmpty(request.AlternateCode),
            UnitOfMeasure = string.IsNullOrWhiteSpace(request.UnitOfMeasure) ? "EA" : request.UnitOfMeasure.Trim(),
            InventoryAccountId = request.InventoryAccountId,
            CostOfSalesAccountId = request.CostOfSalesAccountId,
            TrackSerialNumbers = request.TrackSerialNumbers,
            TrackLots = request.TrackLots,
            IsActive = request.IsActive,
            IsServiceItem = request.IsServiceItem,
            TargetGpPercent = request.TargetGpPercent,
            BuyLength = request.BuyLength,
            BuyWidth = request.BuyWidth,
            BuyHeight = request.BuyHeight,
            SellLength = request.SellLength,
            SellWidth = request.SellWidth,
            SellHeight = request.SellHeight,
            Weight = request.Weight,
            WeightUnit = NullIfEmpty(request.WeightUnit),
            MeasurementNotes = NullIfEmpty(request.MeasurementNotes),
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.StockItems.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return StatusCode(StatusCodes.Status201Created, await MapDtoAsync(entity.Id, cancellationToken));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<StockItemQueryDto>> Update(int companyId, int id, [FromBody] UpsertStockItemRequest request, CancellationToken cancellationToken)
    {
        var entity = await _db.StockItems.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, cancellationToken);
        if (entity == null)
            return NotFound();

        var code = (request.Code ?? "").Trim();
        var desc = (request.Description ?? "").Trim();
        if (code.Length == 0 || desc.Length == 0)
            return BadRequest("Code and description are required.");

        if (await _db.StockItems.AnyAsync(x => x.CompanyId == companyId && x.Code == code && x.Id != id, cancellationToken))
            return Conflict("A stock item with this code already exists.");

        if (!await LedgerOkAsync(companyId, request.InventoryAccountId, cancellationToken)
            || !await LedgerOkAsync(companyId, request.CostOfSalesAccountId, cancellationToken))
            return BadRequest("Inventory and COS ledger accounts must exist for this company and be postable.");

        entity.Code = code;
        entity.Description = desc;
        entity.LongDescription = NullIfEmpty(request.LongDescription);
        entity.AlternateCode = NullIfEmpty(request.AlternateCode);
        entity.UnitOfMeasure = string.IsNullOrWhiteSpace(request.UnitOfMeasure) ? "EA" : request.UnitOfMeasure.Trim();
        entity.InventoryAccountId = request.InventoryAccountId;
        entity.CostOfSalesAccountId = request.CostOfSalesAccountId;
        entity.TrackSerialNumbers = request.TrackSerialNumbers;
        entity.TrackLots = request.TrackLots;
        entity.IsActive = request.IsActive;
        entity.IsServiceItem = request.IsServiceItem;
        entity.TargetGpPercent = request.TargetGpPercent;
        entity.BuyLength = request.BuyLength;
        entity.BuyWidth = request.BuyWidth;
        entity.BuyHeight = request.BuyHeight;
        entity.SellLength = request.SellLength;
        entity.SellWidth = request.SellWidth;
        entity.SellHeight = request.SellHeight;
        entity.Weight = request.Weight;
        entity.WeightUnit = NullIfEmpty(request.WeightUnit);
        entity.MeasurementNotes = NullIfEmpty(request.MeasurementNotes);
        entity.ModifiedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(await MapDtoAsync(id, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int companyId, int id, CancellationToken cancellationToken)
    {
        var entity = await _db.StockItems.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, cancellationToken);
        if (entity == null)
            return NotFound();

        var hasRefs = await _db.WarehouseStocks.AnyAsync(w => w.StockItemId == id, cancellationToken)
            || await _db.SalesOrderLines.AnyAsync(l => l.StockItemId == id, cancellationToken)
            || await _db.PurchaseOrderLines.AnyAsync(l => l.StockItemId == id, cancellationToken)
            || await _db.CustomerInvoiceLines.AnyAsync(l => l.StockItemId == id, cancellationToken)
            || await _db.SupplierInvoiceLines.AnyAsync(l => l.StockItemId == id, cancellationToken);

        if (hasRefs)
        {
            entity.IsActive = false;
            entity.ModifiedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return Ok(new { deactivated = true, message = "Stock item has movements or documents; deactivated instead of deleted." });
        }

        _db.StockItems.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<bool> LedgerOkAsync(int companyId, int ledgerId, CancellationToken cancellationToken)
    {
        return await _db.LedgerAccounts.AsNoTracking()
            .AnyAsync(a => a.Id == ledgerId && a.CompanyId == companyId && a.IsPostable, cancellationToken);
    }

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private async Task<StockItemQueryDto> MapDtoAsync(int id, CancellationToken cancellationToken)
    {
        return await _db.StockItems.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new StockItemQueryDto
            {
                Id = x.Id,
                Code = x.Code,
                Description = x.Description,
                LongDescription = x.LongDescription,
                AlternateCode = x.AlternateCode,
                UnitOfMeasure = x.UnitOfMeasure,
                InventoryAccountId = x.InventoryAccountId,
                CostOfSalesAccountId = x.CostOfSalesAccountId,
                TrackSerialNumbers = x.TrackSerialNumbers,
                TrackLots = x.TrackLots,
                IsActive = x.IsActive,
                IsServiceItem = x.IsServiceItem,
                TargetGpPercent = x.TargetGpPercent,
                BuyLength = x.BuyLength,
                BuyWidth = x.BuyWidth,
                BuyHeight = x.BuyHeight,
                SellLength = x.SellLength,
                SellWidth = x.SellWidth,
                SellHeight = x.SellHeight,
                Weight = x.Weight,
                WeightUnit = x.WeightUnit,
                MeasurementNotes = x.MeasurementNotes
            })
            .FirstAsync(cancellationToken);
    }
}
