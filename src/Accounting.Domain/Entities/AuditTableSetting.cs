namespace Accounting.Domain.Entities;

/// <summary>Per-entity-type data change audit (insert/update/delete) when enabled.</summary>
public class AuditTableSetting
{
    public int Id { get; set; }
    /// <summary>CLR type name, e.g. Customer, UserAccount.</summary>
    public string EntityTypeName { get; set; } = string.Empty;
    public bool AuditInserts { get; set; }
    public bool AuditUpdates { get; set; }
    public bool AuditDeletes { get; set; }
    public bool IsEnabled { get; set; }
}
