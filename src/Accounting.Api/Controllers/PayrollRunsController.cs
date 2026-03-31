using Accounting.Application.Abstractions;
using Accounting.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PayrollRunsController : ControllerBase
{
    private readonly IPayrollService _service;

    public PayrollRunsController(IPayrollService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePayrollRunRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateRunAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id:int}/post")]
    public async Task<IActionResult> Post(int id, [FromQuery] int? userId, CancellationToken cancellationToken)
    {
        var result = await _service.PostRunAsync(id, userId, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
