using Accounting.Api.Http;
using Accounting.Application.Abstractions;
using Accounting.Application.DTOs;
using Accounting.Application.Security;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UserAccountsController : ControllerBase
{
    private readonly IUserAccountAdminService _users;

    public UserAccountsController(IUserAccountAdminService users)
    {
        _users = users;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int? limit, CancellationToken cancellationToken)
    {
        if (!HttpContext.HasPermission(BuiltInPermissions.SecurityUsersManage))
            return Forbid();
        return Ok(await _users.ListAsync(limit, cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        if (!HttpContext.HasPermission(BuiltInPermissions.SecurityUsersManage))
            return Forbid();
        var u = await _users.GetAsync(id, cancellationToken);
        return u is null ? NotFound() : Ok(u);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserAccountRequest request, CancellationToken cancellationToken)
    {
        if (!HttpContext.HasPermission(BuiltInPermissions.SecurityUsersManage))
            return Forbid();
        var result = await _users.CreateAsync(request, cancellationToken);
        return result.Success ? CreatedAtAction(nameof(Get), new { id = result.Data!.Id }, result.Data) : BadRequest(new { errors = result.Errors });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserAccountRequest request, CancellationToken cancellationToken)
    {
        if (!HttpContext.HasPermission(BuiltInPermissions.SecurityUsersManage))
            return Forbid();
        var result = await _users.UpdateAsync(id, request, cancellationToken);
        return result.Success ? NoContent() : BadRequest(new { errors = result.Errors });
    }

    [HttpPost("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        if (!HttpContext.HasPermission(BuiltInPermissions.SecurityUsersManage))
            return Forbid();
        var result = await _users.DeactivateAsync(id, cancellationToken);
        return result.Success ? NoContent() : BadRequest(new { errors = result.Errors });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (!HttpContext.HasPermission(BuiltInPermissions.SecurityUsersManage))
            return Forbid();
        var result = await _users.DeleteAsync(id, cancellationToken);
        return result.Success ? NoContent() : BadRequest(new { errors = result.Errors });
    }
}
