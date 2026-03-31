using Accounting.Domain.Enums;

namespace Accounting.Domain.Evolution;

/// <summary>
/// Describes a subsystem folder from a common ERP product installer layout
/// (e.g. AP, CM, RetailPOS). Used for documentation and API mapping only.
/// </summary>
public sealed record EvolutionInstallerModule(
    string FolderCode,
    string DisplayName,
    string TypicalArea,
    ModuleCode? MappedModule);
