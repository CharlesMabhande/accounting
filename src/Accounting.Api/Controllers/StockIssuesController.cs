using Accounting.Application.Abstractions;
using Accounting.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class StockIssuesController : ControllerBase
{
    private readonly IStockService _service;

    public StockIssuesController(IStockService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> PostIssue([FromBody] PostStockIssueRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.PostIssueAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
