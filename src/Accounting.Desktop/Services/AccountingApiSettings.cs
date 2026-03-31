using System.IO;
using System.Text.Json;

namespace Accounting.Desktop.Services;

internal static class AccountingApiSettings
{
    public static string LoadBaseUrl()
    {
        var env = Environment.GetEnvironmentVariable("ACCOUNTING_API_URL");
        if (!string.IsNullOrWhiteSpace(env))
            return env.TrimEnd('/');

        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (!File.Exists(path))
                return "http://localhost:5151";

            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            if (!doc.RootElement.TryGetProperty("AccountingApi", out var api))
                return "http://localhost:5151";
            if (!api.TryGetProperty("BaseUrl", out var url))
                return "http://localhost:5151";
            var s = url.GetString();
            return string.IsNullOrWhiteSpace(s) ? "http://localhost:5151" : s.TrimEnd('/');
        }
        catch
        {
            return "http://localhost:5151";
        }
    }

    /// <summary>Optional labels for the connection status bar (SQL side — not read from the API).</summary>
    public static (string Server, string Database) LoadConnectionDisplay()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (!File.Exists(path))
                return (".\\SQLEXPRESS", "AccountingDb");

            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            if (!doc.RootElement.TryGetProperty("ConnectionDisplay", out var cd))
                return (".\\SQLEXPRESS", "AccountingDb");

            static string S(JsonElement e, string name) =>
                e.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() ?? "" : "";

            var server = S(cd, "SqlServerInstance");
            var db = S(cd, "DatabaseName");
            return (
                string.IsNullOrWhiteSpace(server) ? ".\\SQLEXPRESS" : server.Trim(),
                string.IsNullOrWhiteSpace(db) ? "AccountingDb" : db.Trim());
        }
        catch
        {
            return (".\\SQLEXPRESS", "AccountingDb");
        }
    }
}
