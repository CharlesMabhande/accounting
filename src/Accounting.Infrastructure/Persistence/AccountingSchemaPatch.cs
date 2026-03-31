using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Persistence;

/// <summary>
/// Additive schema updates for deployments that already have a database created with an older model
/// (<see cref="DbContext.Database.EnsureCreatedAsync"/> does not migrate existing schemas).
/// </summary>
public static class AccountingSchemaPatch
{
    public static async Task ApplyAsync(AccountingDbContext db, CancellationToken cancellationToken = default)
    {
        var provider = db.Database.ProviderName ?? "";
        if (provider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
            await ApplySqlServerAsync(db, cancellationToken).ConfigureAwait(false);
        else if (provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            await ApplySqliteAsync(db, cancellationToken).ConfigureAwait(false);
    }

    private static async Task ApplySqlServerAsync(AccountingDbContext db, CancellationToken cancellationToken)
    {
        await EnsureSecurityTablesSqlServerAsync(db, cancellationToken);

        await AddColumnSqlServer(db, "StockItems", "LongDescription", "nvarchar(500) NULL", cancellationToken);
        await AddColumnSqlServer(db, "StockItems", "AlternateCode", "nvarchar(40) NULL", cancellationToken);
        await AddColumnSqlServer(db, "StockItems", "IsServiceItem", "bit NOT NULL CONSTRAINT DF_StockItems_IsServiceItem DEFAULT 0", cancellationToken);
        await AddColumnSqlServer(db, "StockItems", "TargetGpPercent", "decimal(9,4) NULL", cancellationToken);
        await AddColumnSqlServer(db, "StockItems", "BuyLength", "decimal(18,4) NULL", cancellationToken);
        await AddColumnSqlServer(db, "StockItems", "BuyWidth", "decimal(18,4) NULL", cancellationToken);
        await AddColumnSqlServer(db, "StockItems", "BuyHeight", "decimal(18,4) NULL", cancellationToken);
        await AddColumnSqlServer(db, "StockItems", "SellLength", "decimal(18,4) NULL", cancellationToken);
        await AddColumnSqlServer(db, "StockItems", "SellWidth", "decimal(18,4) NULL", cancellationToken);
        await AddColumnSqlServer(db, "StockItems", "SellHeight", "decimal(18,4) NULL", cancellationToken);
        await AddColumnSqlServer(db, "StockItems", "Weight", "decimal(18,4) NULL", cancellationToken);
        await AddColumnSqlServer(db, "StockItems", "WeightUnit", "nvarchar(10) NULL", cancellationToken);
        await AddColumnSqlServer(db, "StockItems", "MeasurementNotes", "nvarchar(200) NULL", cancellationToken);

        await AddPartyColumnsSqlServer(db, "Customers", "DF_Customers_OnHold", cancellationToken);
        await AddPartyColumnsSqlServer(db, "Suppliers", "DF_Suppliers_OnHold", cancellationToken);

        await AddColumnSqlServer(db, "JournalEntries", "AuditTrailNumber", "nvarchar(40) NULL", cancellationToken);
        await AddColumnSqlServer(db, "CustomerInvoices", "AuditTrailNumber", "nvarchar(40) NULL", cancellationToken);
        await AddColumnSqlServer(db, "SupplierInvoices", "AuditTrailNumber", "nvarchar(40) NULL", cancellationToken);
        await AddColumnSqlServer(db, "CashbookTransactions", "AuditTrailNumber", "nvarchar(40) NULL", cancellationToken);
    }

    private static async Task AddPartyColumnsSqlServer(AccountingDbContext db, string table, string onHoldConstraintName, CancellationToken cancellationToken)
    {
        await AddColumnSqlServer(db, table, "ContactName", "nvarchar(200) NULL", cancellationToken);
        await AddColumnSqlServer(db, table, "Phone", "nvarchar(50) NULL", cancellationToken);
        await AddColumnSqlServer(db, table, "Email", "nvarchar(255) NULL", cancellationToken);
        await AddColumnSqlServer(db, table, "PhysicalAddress1", "nvarchar(200) NULL", cancellationToken);
        await AddColumnSqlServer(db, table, "PhysicalAddress2", "nvarchar(200) NULL", cancellationToken);
        await AddColumnSqlServer(db, table, "PhysicalAddress3", "nvarchar(200) NULL", cancellationToken);
        await AddColumnSqlServer(db, table, "PhysicalCity", "nvarchar(120) NULL", cancellationToken);
        await AddColumnSqlServer(db, table, "PostalAddress1", "nvarchar(200) NULL", cancellationToken);
        await AddColumnSqlServer(db, table, "PostalAddress2", "nvarchar(200) NULL", cancellationToken);
        await AddColumnSqlServer(db, table, "PostalAddress3", "nvarchar(200) NULL", cancellationToken);
        await AddColumnSqlServer(db, table, "PostalCode", "nvarchar(30) NULL", cancellationToken);
        await AddColumnSqlServer(db, table, "TaxNumber", "nvarchar(80) NULL", cancellationToken);
        await AddColumnSqlServer(db, table, "RegistrationNumber", "nvarchar(80) NULL", cancellationToken);
        await AddColumnSqlServer(db, table, "CreditLimit", "decimal(18,2) NULL", cancellationToken);
        await AddColumnSqlServer(db, table, "OnHold", $"bit NOT NULL CONSTRAINT {onHoldConstraintName} DEFAULT 0", cancellationToken);
    }

    private static async Task AddColumnSqlServer(AccountingDbContext db, string table, string column, string sqlType, CancellationToken cancellationToken)
    {
        var tBr = table.Replace("]", "]]", StringComparison.Ordinal);
        var cBr = column.Replace("]", "]]", StringComparison.Ordinal);
        var tLit = table.Replace("'", "''", StringComparison.Ordinal);
        var cLit = column.Replace("'", "''", StringComparison.Ordinal);
        var sql = $"""
            IF NOT EXISTS (
                SELECT 1 FROM sys.columns c
                INNER JOIN sys.tables tb ON c.object_id = tb.object_id
                INNER JOIN sys.schemas s ON tb.schema_id = s.schema_id
                WHERE s.name = N'dbo' AND tb.name = N'{tLit}' AND c.name = N'{cLit}')
            ALTER TABLE [dbo].[{tBr}] ADD [{cBr}] {sqlType};
            """;
        await db.Database.ExecuteSqlRawAsync(sql, cancellationToken).ConfigureAwait(false);
    }

    private static async Task ApplySqliteAsync(AccountingDbContext db, CancellationToken cancellationToken)
    {
        await EnsureSecurityTablesSqliteAsync(db, cancellationToken);

        await AddColumnSqlite(db, "StockItems", "LongDescription", "TEXT", cancellationToken);
        await AddColumnSqlite(db, "StockItems", "AlternateCode", "TEXT", cancellationToken);
        await AddColumnSqlite(db, "StockItems", "IsServiceItem", "INTEGER NOT NULL DEFAULT 0", cancellationToken);
        await AddColumnSqlite(db, "StockItems", "TargetGpPercent", "REAL", cancellationToken);
        await AddColumnSqlite(db, "StockItems", "BuyLength", "REAL", cancellationToken);
        await AddColumnSqlite(db, "StockItems", "BuyWidth", "REAL", cancellationToken);
        await AddColumnSqlite(db, "StockItems", "BuyHeight", "REAL", cancellationToken);
        await AddColumnSqlite(db, "StockItems", "SellLength", "REAL", cancellationToken);
        await AddColumnSqlite(db, "StockItems", "SellWidth", "REAL", cancellationToken);
        await AddColumnSqlite(db, "StockItems", "SellHeight", "REAL", cancellationToken);
        await AddColumnSqlite(db, "StockItems", "Weight", "REAL", cancellationToken);
        await AddColumnSqlite(db, "StockItems", "WeightUnit", "TEXT", cancellationToken);
        await AddColumnSqlite(db, "StockItems", "MeasurementNotes", "TEXT", cancellationToken);

        await AddPartyColumnsSqlite(db, "Customers", cancellationToken);
        await AddPartyColumnsSqlite(db, "Suppliers", cancellationToken);

        await AddColumnSqlite(db, "JournalEntries", "AuditTrailNumber", "TEXT", cancellationToken);
        await AddColumnSqlite(db, "CustomerInvoices", "AuditTrailNumber", "TEXT", cancellationToken);
        await AddColumnSqlite(db, "SupplierInvoices", "AuditTrailNumber", "TEXT", cancellationToken);
        await AddColumnSqlite(db, "CashbookTransactions", "AuditTrailNumber", "TEXT", cancellationToken);
    }

    private static async Task AddPartyColumnsSqlite(AccountingDbContext db, string table, CancellationToken cancellationToken)
    {
        await AddColumnSqlite(db, table, "ContactName", "TEXT", cancellationToken);
        await AddColumnSqlite(db, table, "Phone", "TEXT", cancellationToken);
        await AddColumnSqlite(db, table, "Email", "TEXT", cancellationToken);
        await AddColumnSqlite(db, table, "PhysicalAddress1", "TEXT", cancellationToken);
        await AddColumnSqlite(db, table, "PhysicalAddress2", "TEXT", cancellationToken);
        await AddColumnSqlite(db, table, "PhysicalAddress3", "TEXT", cancellationToken);
        await AddColumnSqlite(db, table, "PhysicalCity", "TEXT", cancellationToken);
        await AddColumnSqlite(db, table, "PostalAddress1", "TEXT", cancellationToken);
        await AddColumnSqlite(db, table, "PostalAddress2", "TEXT", cancellationToken);
        await AddColumnSqlite(db, table, "PostalAddress3", "TEXT", cancellationToken);
        await AddColumnSqlite(db, table, "PostalCode", "TEXT", cancellationToken);
        await AddColumnSqlite(db, table, "TaxNumber", "TEXT", cancellationToken);
        await AddColumnSqlite(db, table, "RegistrationNumber", "TEXT", cancellationToken);
        await AddColumnSqlite(db, table, "CreditLimit", "REAL", cancellationToken);
        await AddColumnSqlite(db, table, "OnHold", "INTEGER NOT NULL DEFAULT 0", cancellationToken);
    }

    private static async Task AddColumnSqlite(AccountingDbContext db, string table, string column, string sqlType, CancellationToken cancellationToken)
    {
        if (await ColumnExistsSqlite(db, table, column, cancellationToken).ConfigureAwait(false))
            return;
        var safeTable = table.Replace("\"", "\"\"", StringComparison.Ordinal);
        var safeCol = column.Replace("\"", "\"\"", StringComparison.Ordinal);
#pragma warning disable EF1002
        await db.Database.ExecuteSqlRawAsync($"""ALTER TABLE "{safeTable}" ADD COLUMN "{safeCol}" {sqlType};""", cancellationToken).ConfigureAwait(false);
#pragma warning restore EF1002
    }

    private static async Task<bool> ColumnExistsSqlite(AccountingDbContext db, string table, string column, CancellationToken cancellationToken)
    {
        var conn = db.Database.GetDbConnection();
        var openedHere = false;
        if (conn.State != System.Data.ConnectionState.Open)
        {
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
            openedHere = true;
        }

        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(1) FROM pragma_table_info('{table.Replace("'", "''", StringComparison.Ordinal)}') WHERE name = '{column.Replace("'", "''", StringComparison.Ordinal)}'";
            var result = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return Convert.ToInt64(result, System.Globalization.CultureInfo.InvariantCulture) > 0;
        }
        finally
        {
            if (openedHere)
                await conn.CloseAsync().ConfigureAwait(false);
        }
    }

    private static async Task EnsureSecurityTablesSqlServerAsync(AccountingDbContext db, CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            IF NOT EXISTS (SELECT 1 FROM sys.tables t INNER JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = N'dbo' AND t.name = N'Permissions')
            BEGIN
              CREATE TABLE [dbo].[Permissions] (
                [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                [Name] nvarchar(120) NOT NULL,
                [Description] nvarchar(500) NULL,
                [CreatedAtUtc] datetime2 NOT NULL,
                [ModifiedAtUtc] datetime2 NULL
              );
              CREATE UNIQUE INDEX [IX_Permissions_Name] ON [dbo].[Permissions]([Name]);
            END
            """, cancellationToken).ConfigureAwait(false);

        // EF Core many-to-many uses RolesId + PermissionsId (not RoleId + PermissionId).
        await db.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'dbo.RolePermissions', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.RolePermissions', N'RoleId') IS NOT NULL
              DROP TABLE [dbo].[RolePermissions];
            """, cancellationToken).ConfigureAwait(false);

        await db.Database.ExecuteSqlRawAsync(
            """
            IF NOT EXISTS (SELECT 1 FROM sys.tables t INNER JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = N'dbo' AND t.name = N'RolePermissions')
            BEGIN
              CREATE TABLE [dbo].[RolePermissions] (
                [RolesId] int NOT NULL,
                [PermissionsId] int NOT NULL,
                CONSTRAINT [PK_RolePermissions] PRIMARY KEY ([RolesId], [PermissionsId]),
                CONSTRAINT [FK_RolePermissions_Roles_RolesId] FOREIGN KEY ([RolesId]) REFERENCES [dbo].[Roles] ([Id]) ON DELETE CASCADE,
                CONSTRAINT [FK_RolePermissions_Permissions_PermissionsId] FOREIGN KEY ([PermissionsId]) REFERENCES [dbo].[Permissions] ([Id]) ON DELETE CASCADE
              );
            END
            """, cancellationToken).ConfigureAwait(false);

        await db.Database.ExecuteSqlRawAsync(
            """
            IF NOT EXISTS (SELECT 1 FROM sys.tables t INNER JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = N'dbo' AND t.name = N'UserSessions')
            BEGIN
              CREATE TABLE [dbo].[UserSessions] (
                [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                [Token] uniqueidentifier NOT NULL,
                [UserId] int NOT NULL,
                [ExpiresAtUtc] datetime2 NOT NULL,
                [CreatedAtUtc] datetime2 NOT NULL,
                CONSTRAINT [FK_UserSessions_UserAccounts_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[UserAccounts] ([Id]) ON DELETE CASCADE
              );
              CREATE UNIQUE INDEX [IX_UserSessions_Token] ON [dbo].[UserSessions]([Token]);
            END
            """, cancellationToken).ConfigureAwait(false);

        await db.Database.ExecuteSqlRawAsync(
            """
            IF NOT EXISTS (SELECT 1 FROM sys.tables t INNER JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = N'dbo' AND t.name = N'AuditTableSettings')
            BEGIN
              CREATE TABLE [dbo].[AuditTableSettings] (
                [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                [EntityTypeName] nvarchar(200) NOT NULL,
                [AuditInserts] bit NOT NULL,
                [AuditUpdates] bit NOT NULL,
                [AuditDeletes] bit NOT NULL,
                [IsEnabled] bit NOT NULL
              );
              CREATE UNIQUE INDEX [IX_AuditTableSettings_EntityTypeName] ON [dbo].[AuditTableSettings]([EntityTypeName]);
            END
            """, cancellationToken).ConfigureAwait(false);

        await AddColumnSqlServer(db, "UserAccounts", "AccountKind", "tinyint NOT NULL CONSTRAINT DF_UserAccounts_AccountKind DEFAULT 0", cancellationToken);
    }

    private static async Task EnsureSecurityTablesSqliteAsync(AccountingDbContext db, CancellationToken cancellationToken)
    {
#pragma warning disable EF1002
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "Permissions" (
              "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
              "Name" TEXT NOT NULL,
              "Description" TEXT NULL,
              "CreatedAtUtc" TEXT NOT NULL,
              "ModifiedAtUtc" TEXT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_Permissions_Name" ON "Permissions"("Name");
            """).ConfigureAwait(false);

        // EF Core many-to-many uses RolesId + PermissionsId (not RoleId + PermissionId).
        if (await ColumnExistsSqlite(db, "RolePermissions", "RoleId", cancellationToken).ConfigureAwait(false)
            && !await ColumnExistsSqlite(db, "RolePermissions", "RolesId", cancellationToken).ConfigureAwait(false))
        {
            await db.Database.ExecuteSqlRawAsync("""DROP TABLE IF EXISTS "RolePermissions";""").ConfigureAwait(false);
        }

        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "RolePermissions" (
              "RolesId" INTEGER NOT NULL,
              "PermissionsId" INTEGER NOT NULL,
              PRIMARY KEY ("RolesId", "PermissionsId"),
              FOREIGN KEY ("RolesId") REFERENCES "Roles" ("Id") ON DELETE CASCADE,
              FOREIGN KEY ("PermissionsId") REFERENCES "Permissions" ("Id") ON DELETE CASCADE
            );
            """).ConfigureAwait(false);

#pragma warning restore EF1002

        await AddColumnSqlite(db, "UserAccounts", "AccountKind", "INTEGER NOT NULL DEFAULT 0", cancellationToken);
    }
}
