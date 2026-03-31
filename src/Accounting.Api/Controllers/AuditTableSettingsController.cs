using Accounting.Api.Http;
using Accounting.Application.Abstractions;
using Accounting.Application.DTOs;
using Accounting.Application.Security;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

[ApiController]
[Route("api/audit")]
public sealed class AuditTableSettingsController : ControllerBase
{
    private readonly IAuditTableSettingsService _settings;

    public AuditTableSettingsController(IAuditTableSettingsService settings)
    {
        _settings = settings;
    }

    [HttpGet("table-settings")]
    public async Task<IActionResult> GetTableSettings(CancellationToken cancellationToken)
    {
        if (!HttpContext.HasPermission(BuiltInPermissions.SecurityAuditSettings))
            return Forbid();
        return Ok(await _settings.ListAsync(cancellationToken));
    }

    [HttpPut("table-settings")]
    public async Task<IActionResult> SaveTableSettings([FromBody] SaveAuditTableSettingsRequest request, CancellationToken cancellationToken)
    {
        if (!HttpContext.HasPermission(BuiltInPermissions.SecurityAuditSettings))
            return Forbid();
        await _settings.SaveAsync(request, cancellationToken);
        return NoContent();
    }
}
