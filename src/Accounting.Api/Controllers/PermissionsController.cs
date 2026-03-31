using Accounting.Api.Http;
using Accounting.Application.Security;
using Accounting.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PermissionsController : ControllerBase
{
    private readonly AccountingDbContext _db;

    public PermissionsController(AccountingDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        if (!HttpContext.HasPermission(BuiltInPermissions.SecurityRolesManage) &&
            !HttpContext.HasPermission(BuiltInPermissions.SecurityUsersManage))
            return Forbid();
        var rows = await _db.Permissions.AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new { p.Id, p.Name, p.Description })
            .ToListAsync(cancellationToken);
        return Ok(rows);
    }
}
