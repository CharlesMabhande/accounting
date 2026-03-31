using Accounting.Application.Abstractions;
using Accounting.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class CashbookTransactionsController : ControllerBase
{
    private readonly ICashbookService _service;

    public CashbookTransactionsController(ICashbookService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCashbookRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id:int}/post")]
    public async Task<IActionResult> Post(int id, [FromQuery] int? userId, CancellationToken cancellationToken)
    {
        var result = await _service.PostAsync(id, userId, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
