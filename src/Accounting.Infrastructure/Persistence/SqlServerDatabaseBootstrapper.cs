using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace Accounting.Infrastructure.Persistence;

/// <summary>
/// Creates the SQL Server database if missing (connects to <c>master</c> first).
/// Skips Azure SQL and connections without an initial catalog.
/// </summary>
public static class SqlServerDatabaseBootstrapper
{
    private static readonly Regex SafeDbName = new("^[A-Za-z0-9_]+$", RegexOptions.Compiled);

    public static async Task EnsureDatabaseExistsAsync(string? connectionString, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        var builder = new SqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(builder.DataSource))
            return;

        if (builder.DataSource.Contains("database.windows.net", StringComparison.OrdinalIgnoreCase))
            return;

        var databaseName = builder.InitialCatalog;
        if (string.IsNullOrWhiteSpace(databaseName))
            return;

        if (!SafeDbName.IsMatch(databaseName))
            throw new InvalidOperationException(
                "Database name may only contain letters, digits, and underscore.");

        var escaped = databaseName.Replace("]", "]]");
        var literal = databaseName.Replace("'", "''");

        builder.InitialCatalog = "master";
        builder.Pooling = true;

        await using var conn = new SqlConnection(builder.ConnectionString);
        await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"""
            IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = N'{literal}')
                CREATE DATABASE [{escaped}];
            """;
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}
