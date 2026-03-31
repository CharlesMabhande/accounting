using Accounting.Application.Abstractions;
using Accounting.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ProjectJobsController : ControllerBase
{
    private readonly IProjectJobService _service;

    public ProjectJobsController(IProjectJobService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProjectJobRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
