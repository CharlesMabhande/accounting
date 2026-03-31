using Accounting.Api.Http;
using Accounting.Application.DTOs;
using Accounting.Application.Security;
using Accounting.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Accounting.Api.Controllers;

[ApiController]
[Route("api/database-backup")]
public sealed class DatabaseBackupController : ControllerBase
{
    private readonly DatabaseArchiveService _archive;
    private readonly IConfiguration _configuration;

    public DatabaseBackupController(DatabaseArchiveService archive, IConfiguration configuration)
    {
        _archive = archive;
        _configuration = configuration;
    }

    /// <summary>Returns the configured backup folder (created if missing).</summary>
    [HttpGet("folder")]
    public ActionResult<object> GetBackupFolder()
    {
        if (!HttpContext.HasPermission(BuiltInPermissions.SecurityBackupManage))
            return Forbid();
        var path = BackupPathResolver.EnsureBackupRootExists(_configuration);
        return Ok(new { path });
    }

    /// <summary>Portable ZIP: manifest + JSON per table (logical export).</summary>
    [HttpGet("export-json")]
    public async Task<IActionResult> ExportJsonZip(CancellationToken cancellationToken)
    {
        if (!HttpContext.HasPermission(BuiltInPermissions.SecurityBackupManage))
            return Forbid();

        var name = $"accounting-export-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip";
        var root = BackupPathResolver.EnsureBackupRootExists(_configuration);
        var savedPath = Path.Combine(root, name);
        await using (var fs = System.IO.File.Create(savedPath))
            await _archive.ExportJsonZipAsync(fs, cancellationToken).ConfigureAwait(false);

        Response.Headers.Append("X-Backup-Saved-Path", savedPath);
        var bytes = await System.IO.File.ReadAllBytesAsync(savedPath, cancellationToken).ConfigureAwait(false);
        return File(bytes, "application/zip", name);
    }

    /// <summary>Replace data from a ZIP produced by export-json (same database provider).</summary>
    [HttpPost("import-json")]
    [RequestSizeLimit(524_288_000)]
    public async Task<IActionResult> ImportJsonZip(IFormFile? file, CancellationToken cancellationToken)
    {
        if (!HttpContext.HasPermission(BuiltInPermissions.SecurityBackupManage))
            return Forbid();
        if (file is null || file.Length == 0)
            return BadRequest(new ImportBackupResponseDto { Ok = false, ErrorMessage = "No file uploaded." });

        var root = BackupPathResolver.EnsureBackupRootExists(_configuration);
        var stamp = $"import-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.zip";
        var savedPath = Path.Combine(root, stamp);
        await using (var readStream = file.OpenReadStream())
        {
            await using var save = System.IO.File.Create(savedPath);
            await readStream.CopyToAsync(save, cancellationToken).ConfigureAwait(false);
        }

        await using var importStream = System.IO.File.OpenRead(savedPath);
        var result = await _archive.ImportJsonZipAsync(importStream, cancellationToken).ConfigureAwait(false);
        Response.Headers.Append("X-Backup-Import-Staging-Path", savedPath);
        return result.Ok
            ? Ok(new ImportBackupResponseDto { Ok = true })
            : BadRequest(new ImportBackupResponseDto { Ok = false, ErrorMessage = result.ErrorMessage });
    }

    /// <summary>Native file: SQLite .db bytes or SQL Server .bak.</summary>
    [HttpGet("native")]
    public async Task<IActionResult> NativeFile(CancellationToken cancellationToken)
    {
        if (!HttpContext.HasPermission(BuiltInPermissions.SecurityBackupManage))
            return Forbid();

        var bytes = await _archive.TryCreateNativeBackupAsync(cancellationToken).ConfigureAwait(false);
        if (bytes is null || bytes.Length == 0)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "Native backup is not available for this database configuration." });
        }

        var ext = _archive.IsSqlite ? ".db" : ".bak";
        var name = $"accounting-native-{DateTime.UtcNow:yyyyMMdd-HHmmss}{ext}";
        var root = BackupPathResolver.EnsureBackupRootExists(_configuration);
        var savedPath = Path.Combine(root, name);
        await System.IO.File.WriteAllBytesAsync(savedPath, bytes, cancellationToken).ConfigureAwait(false);
        Response.Headers.Append("X-Backup-Saved-Path", savedPath);
        return File(bytes, "application/octet-stream", name);
    }
}
