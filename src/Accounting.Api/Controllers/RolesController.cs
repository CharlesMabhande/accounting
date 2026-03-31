using Accounting.Api.Http;
using Accounting.Application.Abstractions;
using Accounting.Application.DTOs;
using Accounting.Application.Security;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class RolesController : ControllerBase
{
    private readonly IRoleAdminService _roles;

    public RolesController(IRoleAdminService roles)
    {
        _roles = roles;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        if (!HttpContext.HasPermission(BuiltInPermissions.SecurityRolesManage))
            return Forbid();
        return Ok(await _roles.ListAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        if (!HttpContext.HasPermission(BuiltInPermissions.SecurityRolesManage))
            return Forbid();
        var r = await _roles.GetAsync(id, cancellationToken);
        return r is null ? NotFound() : Ok(r);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequest request, CancellationToken cancellationToken)
    {
        if (!HttpContext.HasPermission(BuiltInPermissions.SecurityRolesManage))
            return Forbid();
        var result = await _roles.CreateAsync(request, cancellationToken);
        return result.Success ? CreatedAtAction(nameof(Get), new { id = result.Data!.Id }, result.Data) : BadRequest(new { errors = result.Errors });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        if (!HttpContext.HasPermission(BuiltInPermissions.SecurityRolesManage))
            return Forbid();
        var result = await _roles.UpdateAsync(id, request, cancellationToken);
        return result.Success ? NoContent() : BadRequest(new { errors = result.Errors });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (!HttpContext.HasPermission(BuiltInPermissions.SecurityRolesManage))
            return Forbid();
        var result = await _roles.DeleteAsync(id, cancellationToken);
        return result.Success ? NoContent() : BadRequest(new { errors = result.Errors });
    }
}
