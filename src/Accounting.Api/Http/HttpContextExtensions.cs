namespace Accounting.Api.Http;

public static class HttpContextExtensions
{
    public const string PermissionSetKey = "PermissionSet";

    public static bool HasPermission(this HttpContext http, string permission)
    {
        return http.Items[PermissionSetKey] is HashSet<string> hs &&
               hs.Contains(permission, StringComparer.OrdinalIgnoreCase);
    }
}
