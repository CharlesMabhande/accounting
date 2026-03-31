namespace Accounting.Application.DTOs;

public sealed class CreateCustomerInvoiceLineDto
{
    public string Description { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public int? StockItemId { get; init; }
    public int? RevenueAccountId { get; init; }
}

public sealed class CreateCustomerInvoiceRequest
{
    public int CompanyId { get; init; }
    public int CustomerId { get; init; }
    public DateOnly DocumentDate { get; init; }
    public int? TaxCodeId { get; init; }
    public IReadOnlyList<CreateCustomerInvoiceLineDto> Lines { get; init; } = Array.Empty<CreateCustomerInvoiceLineDto>();
}

public sealed class CreateSupplierInvoiceLineDto
{
    public string Description { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public decimal UnitCost { get; init; }
    public int? StockItemId { get; init; }
    public int? ExpenseAccountId { get; init; }
}

public sealed class CreateSupplierInvoiceRequest
{
    public int CompanyId { get; init; }
    public int SupplierId { get; init; }
    public DateOnly DocumentDate { get; init; }
    public int? TaxCodeId { get; init; }
    public IReadOnlyList<CreateSupplierInvoiceLineDto> Lines { get; init; } = Array.Empty<CreateSupplierInvoiceLineDto>();
}
