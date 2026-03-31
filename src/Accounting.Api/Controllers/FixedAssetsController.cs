using Accounting.Application.Abstractions;
using Accounting.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class FixedAssetsController : ControllerBase
{
    private readonly IFixedAssetService _service;

    public FixedAssetsController(IFixedAssetService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFixedAssetRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("depreciation")]
    public async Task<IActionResult> PostDepreciation([FromBody] PostDepreciationRequest request, [FromQuery] int? userId, CancellationToken cancellationToken)
    {
        var result = await _service.PostDepreciationAsync(request, userId, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
