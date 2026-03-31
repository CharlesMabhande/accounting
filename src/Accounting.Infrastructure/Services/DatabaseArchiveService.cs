using System.IO.Compression;
using System.Text.Json;
using Accounting.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.SqlServer.Metadata;
using Microsoft.Extensions.Configuration;

namespace Accounting.Infrastructure.Services;

/// <summary>Logical backup/restore via JSON in a ZIP, plus native file backup for SQLite / SQL Server .bak.</summary>
public sealed class DatabaseArchiveService
{
    private readonly AccountingDbContext _db;
    private readonly IConfiguration _configuration;

    public DatabaseArchiveService(AccountingDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    public bool IsSqlite =>
        DatabaseProvider.IsSqlite(_configuration);

    public async Task ExportJsonZipAsync(Stream outputStream, CancellationToken cancellationToken = default)
    {
        var model = _db.Model;
        var insertOrder = GetInsertOrder(model);
        using var zip = new ZipArchive(outputStream, ZipArchiveMode.Create, leaveOpen: true);
        var manifest = new Dictionary<string, object?>
        {
            ["version"] = 1,
            ["provider"] = IsSqlite ? "Sqlite" : "SqlServer",
            ["exportedAtUtc"] = DateTime.UtcNow
        };
        await WriteJsonEntryAsync(zip, "manifest.json", manifest, cancellationToken).ConfigureAwait(false);

        var conn = _db.Database.GetDbConnection();
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            foreach (var entityType in insertOrder)
            {
                var (sql, fileKey) = BuildSelectForEntity(entityType);
                if (sql is null || fileKey is null)
                    continue;

                var rows = await ReadRowsAsync(conn, sql, cancellationToken).ConfigureAwait(false);
                var payload = new Dictionary<string, object?>
                {
                    ["table"] = fileKey,
                    ["rows"] = rows
                };
                await WriteJsonEntryAsync(zip, $"Data/{SanitizeFileName(fileKey)}.json", payload, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        finally
        {
            await conn.CloseAsync().ConfigureAwait(false);
        }
    }

    public async Task<ImportResult> ImportJsonZipAsync(Stream zipStream, CancellationToken cancellationToken = default)
    {
        using var zip = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);
        var manifestEntry = zip.GetEntry("manifest.json");
        if (manifestEntry is null)
            return new ImportResult(false, "Archive missing manifest.json.");

        await using (var ms = manifestEntry.Open())
        {
            using var doc = await JsonDocument.ParseAsync(ms, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (doc.RootElement.TryGetProperty("provider", out var p))
            {
                var expected = IsSqlite ? "Sqlite" : "SqlServer";
                if (!string.Equals(p.GetString(), expected, StringComparison.OrdinalIgnoreCase))
                    return new ImportResult(false,
                        $"Archive provider '{p.GetString()}' does not match current database ({expected}).");
            }
        }

        var model = _db.Model;
        var insertOrder = GetInsertOrder(model);
        var deleteOrder = insertOrder.AsEnumerable().Reverse().ToList();

        var tableData = new Dictionary<string, List<Dictionary<string, object?>>>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in zip.Entries)
        {
            if (!entry.FullName.StartsWith("Data/", StringComparison.OrdinalIgnoreCase) ||
                !entry.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                continue;

            await using var es = entry.Open();
            using var doc = await JsonDocument.ParseAsync(es, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            var root = doc.RootElement;
            if (!root.TryGetProperty("table", out var tableEl) || !root.TryGetProperty("rows", out var rowsEl))
                continue;
            var tableKey = tableEl.GetString() ?? "";
            var rows = new List<Dictionary<string, object?>>();
            foreach (var row in rowsEl.EnumerateArray())
            {
                var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var prop in row.EnumerateObject())
                    dict[prop.Name] = JsonElementToValue(prop.Value);
                rows.Add(dict);
            }

            tableData[tableKey] = rows;
        }

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var conn = _db.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

            if (IsSqlite)
            {
                await ExecuteNonQueryAsync(conn, "PRAGMA foreign_keys = OFF;", cancellationToken).ConfigureAwait(false);
            }

            foreach (var entityType in deleteOrder)
            {
                var del = BuildDeleteAll(entityType);
                if (del is not null)
                    await ExecuteNonQueryAsync(conn, del, cancellationToken).ConfigureAwait(false);
            }

            foreach (var entityType in insertOrder)
            {
                var key = BuildTableKey(entityType);
                if (key is null || !tableData.TryGetValue(key, out var rows) || rows.Count == 0)
                    continue;

                await InsertRowsAsync(conn, entityType, rows, cancellationToken).ConfigureAwait(false);
            }

            if (IsSqlite)
            {
                await ExecuteNonQueryAsync(conn, "PRAGMA foreign_keys = ON;", cancellationToken).ConfigureAwait(false);
            }

            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new ImportResult(true, null);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return new ImportResult(false, ex.Message);
        }
    }

    public async Task<byte[]?> TryCreateNativeBackupAsync(CancellationToken cancellationToken = default)
    {
        if (IsSqlite)
        {
            string? path = null;
            var cs = _configuration.GetConnectionString("DefaultConnection");
            if (!string.IsNullOrWhiteSpace(cs))
            {
                var b = new SqliteConnectionStringBuilder(cs);
                if (!string.IsNullOrEmpty(b.DataSource))
                {
                    path = Path.IsPathRooted(b.DataSource)
                        ? b.DataSource
                        : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, b.DataSource));
                }
            }

            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                var relativePath = _configuration["Database:SqlitePath"] ?? Path.Combine("Data", "Accounting.db");
                var combined = Path.Combine(AppContext.BaseDirectory,
                    relativePath.Replace('/', Path.DirectorySeparatorChar));
                path = Path.GetFullPath(combined);
            }

            if (!File.Exists(path))
                return null;
            return await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        }

        var sqlCs = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(sqlCs))
            return null;

        var scb = new SqlConnectionStringBuilder(sqlCs);
        var dbName = scb.InitialCatalog;
        if (string.IsNullOrEmpty(dbName))
            return null;

        var backupPath = Path.Combine(Path.GetTempPath(), $"Accounting_{Guid.NewGuid():N}.bak");
        var safeDb = dbName.Replace("]", "]]", StringComparison.Ordinal);
        var safePath = backupPath.Replace("'", "''", StringComparison.Ordinal);
        var sql = $"BACKUP DATABASE [{safeDb}] TO DISK = N'{safePath}' WITH FORMAT, INIT, NAME = N'CharlzTechAccounting', SKIP, NOREWIND, NOUNLOAD, STATS = 10";

        await _db.Database.ExecuteSqlRawAsync(sql, cancellationToken).ConfigureAwait(false);
        try
        {
            return await File.ReadAllBytesAsync(backupPath, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            try
            {
                File.Delete(backupPath);
            }
            catch
            {
                // ignore
            }
        }
    }

    private static async Task WriteJsonEntryAsync(
        ZipArchive zip,
        string path,
        object data,
        CancellationToken cancellationToken)
    {
        var entry = zip.CreateEntry(path);
        await using var s = entry.Open();
        await JsonSerializer.SerializeAsync(s, data,
            new JsonSerializerOptions { WriteIndented = false }, cancellationToken).ConfigureAwait(false);
    }

    private static List<IEntityType> GetInsertOrder(IModel model)
    {
        var entities = model.GetEntityTypes()
            .Where(e => e.BaseType == null && e.GetTableName() != null && !e.IsOwned())
            .ToList();

        var indegree = entities.ToDictionary(e => e, _ => 0);
        var adj = entities.ToDictionary(e => e, _ => new List<IEntityType>());

        foreach (var e in entities)
        {
            foreach (var fk in e.GetForeignKeys())
            {
                var principal = fk.PrincipalEntityType;
                var dependent = fk.DeclaringEntityType;
                if (!entities.Contains(principal) || !entities.Contains(dependent))
                    continue;
                if (principal == dependent)
                    continue;
                adj[principal].Add(dependent);
                indegree[dependent]++;
            }
        }

        var queue = new Queue<IEntityType>(indegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
        var order = new List<IEntityType>();
        while (queue.Count > 0)
        {
            var n = queue.Dequeue();
            order.Add(n);
            foreach (var m in adj[n])
            {
                indegree[m]--;
                if (indegree[m] == 0)
                    queue.Enqueue(m);
            }
        }

        if (order.Count != entities.Count)
            order = entities;

        return order;
    }

    private string? BuildTableKey(IEntityType entityType)
    {
        var schema = entityType.GetSchema() ?? "dbo";
        var table = entityType.GetTableName();
        if (table is null)
            return null;
        return IsSqlite ? table : $"{schema}.{table}";
    }

    private (string? Sql, string? FileKey) BuildSelectForEntity(IEntityType entityType)
    {
        var schema = entityType.GetSchema() ?? "dbo";
        var table = entityType.GetTableName();
        if (table is null)
            return (null, null);

        if (IsSqlite)
            return ($"SELECT * FROM \"{table.Replace("\"", "\"\"", StringComparison.Ordinal)}\"", table);

        var s = schema.Replace("]", "]]", StringComparison.Ordinal);
        var t = table.Replace("]", "]]", StringComparison.Ordinal);
        return ($"SELECT * FROM [{s}].[{t}]", $"{schema}.{table}");
    }

    private string? BuildDeleteAll(IEntityType entityType)
    {
        var schema = entityType.GetSchema() ?? "dbo";
        var table = entityType.GetTableName();
        if (table is null)
            return null;

        if (IsSqlite)
            return $"DELETE FROM \"{table.Replace("\"", "\"\"", StringComparison.Ordinal)}\";";

        var s = schema.Replace("]", "]]", StringComparison.Ordinal);
        var t = table.Replace("]", "]]", StringComparison.Ordinal);
        return $"DELETE FROM [{s}].[{t}];";
    }

    private async Task InsertRowsAsync(
        System.Data.Common.DbConnection conn,
        IEntityType entityType,
        IReadOnlyList<Dictionary<string, object?>> rows,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
            return;

        var schema = entityType.GetSchema() ?? "dbo";
        var table = entityType.GetTableName();
        if (table is null)
            return;

        var cols = rows[0].Keys.ToList();
        if (cols.Count == 0)
            return;

        var hasIdentity = !IsSqlite && entityType.GetProperties().Any(p =>
            SqlServerPropertyExtensions.GetValueGenerationStrategy(p) ==
            SqlServerValueGenerationStrategy.IdentityColumn);

        if (!IsSqlite && hasIdentity)
        {
            var s = schema.Replace("]", "]]", StringComparison.Ordinal);
            var t = table.Replace("]", "]]", StringComparison.Ordinal);
            await ExecuteNonQueryAsync(conn, $"SET IDENTITY_INSERT [{s}].[{t}] ON;", cancellationToken)
                .ConfigureAwait(false);
        }

        foreach (var row in rows)
        {
            var insert = BuildInsertStatement(entityType, schema, table, cols, row);
            await ExecuteNonQueryWithParamsAsync(conn, insert.sql, insert.parameters, cancellationToken)
                .ConfigureAwait(false);
        }

        if (!IsSqlite && hasIdentity)
        {
            var s = schema.Replace("]", "]]", StringComparison.Ordinal);
            var t = table.Replace("]", "]]", StringComparison.Ordinal);
            await ExecuteNonQueryAsync(conn, $"SET IDENTITY_INSERT [{s}].[{t}] OFF;", cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private (string sql, List<(string Name, object? Value)> parameters) BuildInsertStatement(
        IEntityType entityType,
        string schema,
        string table,
        IReadOnlyList<string> columns,
        IReadOnlyDictionary<string, object?> row)
    {
        var parameters = new List<(string Name, object? Value)>();
        var paramNames = new List<string>();
        var colQuoted = new List<string>();
        var i = 0;
        foreach (var c in columns)
        {
            if (!row.TryGetValue(c, out var val))
                continue;
            var p = $"@p{i++}";
            paramNames.Add(p);
            parameters.Add((p, val));
            if (IsSqlite)
                colQuoted.Add($"\"{c.Replace("\"", "\"\"", StringComparison.Ordinal)}\"");
            else
                colQuoted.Add($"[{c.Replace("]", "]]", StringComparison.Ordinal)}]");
        }

        if (IsSqlite)
        {
            var tq = table.Replace("\"", "\"\"", StringComparison.Ordinal);
            var sql =
                $"INSERT INTO \"{tq}\" ({string.Join(", ", colQuoted)}) VALUES ({string.Join(", ", paramNames)});";
            return (sql, parameters);
        }

        var sch = schema.Replace("]", "]]", StringComparison.Ordinal);
        var tbl = table.Replace("]", "]]", StringComparison.Ordinal);
        var sqlServer =
            $"INSERT INTO [{sch}].[{tbl}] ({string.Join(", ", colQuoted)}) VALUES ({string.Join(", ", paramNames)});";
        return (sqlServer, parameters);
    }

    private static async Task ExecuteNonQueryAsync(
        System.Data.Common.DbConnection conn,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task ExecuteNonQueryWithParamsAsync(
        System.Data.Common.DbConnection conn,
        string sql,
        IReadOnlyList<(string Name, object? Value)> parameters,
        CancellationToken cancellationToken)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }

        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<List<Dictionary<string, object?>>> ReadRowsAsync(
        System.Data.Common.DbConnection conn,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        var rows = new List<Dictionary<string, object?>>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var name = reader.GetName(i);
                row[name] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }

            rows.Add(row);
        }

        return rows;
    }

    private static object? JsonElementToValue(JsonElement el)
    {
        return el.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.String => el.GetString(),
            JsonValueKind.Number => el.TryGetDecimal(out var dec)
                ? dec
                : el.TryGetInt64(out var l)
                    ? l
                    : el.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Array => el,
            JsonValueKind.Object => el,
            _ => el.ToString()
        };
    }

    private static string SanitizeFileName(string key) =>
        key.Replace(".", "_", StringComparison.Ordinal).Replace("\\", "_", StringComparison.Ordinal);

    public sealed record ImportResult(bool Ok, string? ErrorMessage);
}
