using Accounting.Application.Common;

namespace Accounting.Application.Abstractions;

/// <summary>Manufacturing: issue BOM components to WIP for a work order.</summary>
public interface IWorkOrderManufacturingService
{
    Task<OperationResult> IssueMaterialsAsync(int companyId, int workOrderId, CancellationToken cancellationToken = default);
}
