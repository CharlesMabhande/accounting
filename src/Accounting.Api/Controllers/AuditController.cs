using Accounting.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuditController : ControllerBase
{
    private readonly IAuditService _audit;

    public AuditController(IAuditService audit)
    {
        _audit = audit;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int? limit, CancellationToken cancellationToken)
    {
        return Ok(await _audit.ListAsync(limit, cancellationToken));
    }

    [HttpPost("log")]
    public async Task<IActionResult> Log([FromQuery] int? userId, [FromBody] AuditLogRequest body, CancellationToken cancellationToken)
    {
        await _audit.LogAsync(userId, body.Action, body.EntityName, body.EntityKey, body.Details, cancellationToken);
        return Ok();
    }
}

public sealed class AuditLogRequest
{
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityKey { get; set; }
    public string Details { get; set; } = string.Empty;
}
