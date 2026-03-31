using Accounting.Application.DTOs;

namespace Accounting.Application.Abstractions;

public interface IStockItemQueryService
{
    Task<IReadOnlyList<StockItemQueryDto>> ListItemsAsync(int companyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WarehouseStockQueryDto>> ListWarehouseStockAsync(int companyId, CancellationToken cancellationToken = default);
}
