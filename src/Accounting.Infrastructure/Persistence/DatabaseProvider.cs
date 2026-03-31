using Microsoft.Extensions.Configuration;

namespace Accounting.Infrastructure.Persistence;

public static class DatabaseProvider
{
    public const string Sqlite = "Sqlite";
    public const string SqlServer = "SqlServer";

    public static bool IsSqlite(IConfiguration configuration) =>
        string.Equals(
            configuration["Database:Provider"],
            Sqlite,
            StringComparison.OrdinalIgnoreCase);

    public static bool IsSqlServer(IConfiguration configuration) =>
        string.Equals(
            configuration["Database:Provider"],
            SqlServer,
            StringComparison.OrdinalIgnoreCase);
}
