using Accounting.Domain.Enums;
using Accounting.Domain.Evolution;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>Module catalogue for this application’s subsystems.</summary>
[ApiController]
[Route("api/[controller]")]
public sealed class ModulesController : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyList<object>> Get()
    {
        var modules = Enum.GetValues<ModuleCode>()
            .Select(m => new { Code = m.ToString(), Id = (int)m })
            .ToList();
        return Ok(modules);
    }

    /// <summary>Maps common third-party installer folder names (e.g. AP, CM) to this system's modules. Informational only.</summary>
    [HttpGet("evolution")]
    public ActionResult<IReadOnlyList<object>> GetEvolutionInstallerMap()
    {
        var rows = EvolutionInstallerCatalog.Modules
            .Select(m => new
            {
                m.FolderCode,
                m.DisplayName,
                m.TypicalArea,
                MappedModuleId = m.MappedModule.HasValue ? (int?)m.MappedModule.Value : null,
                MappedModuleCode = m.MappedModule?.ToString()
            })
            .ToList();
        return Ok(rows);
    }
}
