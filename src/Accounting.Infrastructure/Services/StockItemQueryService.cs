using Accounting.Application.Abstractions;
using Accounting.Application.DTOs;
using Accounting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Services;

public sealed class StockItemQueryService : IStockItemQueryService
{
    private readonly AccountingDbContext _db;

    public StockItemQueryService(AccountingDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<StockItemQueryDto>> ListItemsAsync(int companyId, CancellationToken cancellationToken = default)
    {
        return await _db.StockItems.AsNoTracking()
            .Where(s => s.CompanyId == companyId)
            .OrderBy(s => s.Code)
            .Select(s => new StockItemQueryDto
            {
                Id = s.Id,
                Code = s.Code,
                Description = s.Description,
                LongDescription = s.LongDescription,
                AlternateCode = s.AlternateCode,
                UnitOfMeasure = s.UnitOfMeasure,
                InventoryAccountId = s.InventoryAccountId,
                CostOfSalesAccountId = s.CostOfSalesAccountId,
                TrackSerialNumbers = s.TrackSerialNumbers,
                TrackLots = s.TrackLots,
                IsActive = s.IsActive,
                IsServiceItem = s.IsServiceItem,
                TargetGpPercent = s.TargetGpPercent,
                BuyLength = s.BuyLength,
                BuyWidth = s.BuyWidth,
                BuyHeight = s.BuyHeight,
                SellLength = s.SellLength,
                SellWidth = s.SellWidth,
                SellHeight = s.SellHeight,
                Weight = s.Weight,
                WeightUnit = s.WeightUnit,
                MeasurementNotes = s.MeasurementNotes
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WarehouseStockQueryDto>> ListWarehouseStockAsync(int companyId, CancellationToken cancellationToken = default)
    {
        return await _db.WarehouseStocks.AsNoTracking()
            .Where(ws => ws.CompanyId == companyId)
            .OrderBy(ws => ws.Warehouse.Code)
            .ThenBy(ws => ws.StockItem.Code)
            .Select(ws => new WarehouseStockQueryDto
            {
                WarehouseId = ws.WarehouseId,
                WarehouseCode = ws.Warehouse.Code,
                StockItemId = ws.StockItemId,
                StockCode = ws.StockItem.Code,
                Quantity = ws.Quantity,
                LastUnitCost = ws.LastUnitCost
            })
            .ToListAsync(cancellationToken);
    }
}
