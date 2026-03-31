using Accounting.Api.Http;
using Accounting.Application.Abstractions;

namespace Accounting.Api.Middleware;

public sealed class SessionAuthMiddleware
{
    private static readonly HashSet<string> PublicPrefixes =
    [
        "/api/health",
        "/api/auth/login",
        "/api/auth/session",
        "/swagger"
    ];

    private readonly RequestDelegate _next;

    public SessionAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAuthService auth, ICurrentSessionContext sessionCtx)
    {
        var path = context.Request.Path.Value ?? "";
        if (IsPublic(path))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("X-Session-Token", out var tokenValues) ||
            string.IsNullOrWhiteSpace(tokenValues.ToString()))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var token = tokenValues.ToString();
        var result = await auth.GetSessionAsync(token, context.RequestAborted).ConfigureAwait(false);
        if (!result.Success || result.Data is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        sessionCtx.UserId = result.Data.UserId;
        context.Items[HttpContextExtensions.PermissionSetKey] =
            new HashSet<string>(result.Data.Permissions ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
        await _next(context);
    }

    private static bool IsPublic(string path)
    {
        if (string.Equals(path, "/", StringComparison.Ordinal))
            return false;
        return PublicPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
    }
}
