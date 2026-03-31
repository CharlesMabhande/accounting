using Accounting.Application.Abstractions;
using Accounting.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class JournalsController : ControllerBase
{
    private readonly IJournalPostingService _posting;

    public JournalsController(IJournalPostingService posting)
    {
        _posting = posting;
    }

    [HttpPost]
    [ProducesResponseType(typeof(JournalPostingResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(JournalPostingResult), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<JournalPostingResult>> Post([FromBody] PostJournalRequest request, CancellationToken cancellationToken)
    {
        var result = await _posting.PostJournalAsync(request, cancellationToken);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }
}
