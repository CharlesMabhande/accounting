using System.Diagnostics;
using Accounting.Api.Middleware;
using Accounting.Infrastructure;
using Accounting.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;

if (IsDevelopmentEnvironment())
    ReleaseDevPortFromStaleProcesses();

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

if (builder.Environment.IsDevelopment())
{
    if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
        builder.WebHost.UseUrls("http://localhost:5151");
}
else
{
    builder.WebHost.UseUrls("http://0.0.0.0:8080");
}

if (DatabaseProvider.IsSqlServer(builder.Configuration))
{
    var resolved = await SqlServerRuntimeProvisioner.ResolveWorkingConnectionAsync(builder.Configuration);
    if (!string.IsNullOrWhiteSpace(resolved))
    {
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = resolved
        });
    }

    var sqlConnection = builder.Configuration.GetConnectionString("DefaultConnection");
    await SqlServerDatabaseBootstrapper.EnsureDatabaseExistsAsync(sqlConnection);
}

builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 524_288_000);
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 524_288_000);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAccountingInfrastructure(builder.Configuration);

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AccountingDbContext>();
    // SQL Server database is created (empty) by SqlServerDatabaseBootstrapper,
    // then EF builds the schema here. Same logical model for SQLite (single-file) for portable installs.
    await db.Database.EnsureCreatedAsync();
    await AccountingSchemaPatch.ApplyAsync(db);
    await AccountingDbSeeder.SeedAsync(db);
}

var enableSwagger = app.Environment.IsDevelopment()
    || app.Configuration.GetValue("Api:EnableSwagger", false);
if (enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<SessionAuthMiddleware>();
app.MapControllers();
await app.RunAsync();

static bool IsDevelopmentEnvironment() =>
    string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase);

static void ReleaseDevPortFromStaleProcesses()
{
    var currentPid = Environment.ProcessId;
    foreach (var p in Process.GetProcessesByName("Accounting.Api"))
    {
        if (p.Id == currentPid)
            continue;
        try
        {
            p.Kill(entireProcessTree: true);
            p.WaitForExit(milliseconds: 5000);
        }
        catch (InvalidOperationException)
        {
        }
        catch (NotSupportedException)
        {
        }
        finally
        {
            p.Dispose();
        }
    }
}
