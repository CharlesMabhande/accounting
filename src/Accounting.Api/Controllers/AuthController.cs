using Accounting.Application.Abstractions;
using Accounting.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _auth.LoginAsync(request, cancellationToken);
        return result.Success ? Ok(result.Data) : Unauthorized(new { errors = result.Errors });
    }

    [HttpGet("session")]
    public async Task<IActionResult> GetSession(
        [FromHeader(Name = "X-Session-Token")] string? token,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Unauthorized();
        var result = await _auth.GetSessionAsync(token, cancellationToken);
        return result.Success ? Ok(result.Data) : Unauthorized(new { errors = result.Errors });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        [FromHeader(Name = "X-Session-Token")] string? token,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Unauthorized();
        await _auth.LogoutAsync(token, cancellationToken);
        return NoContent();
    }
}
