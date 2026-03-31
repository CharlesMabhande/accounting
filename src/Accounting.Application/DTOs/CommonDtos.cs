namespace Accounting.Application.DTOs;

/// <summary>API response for simple id returns (e.g. POST upsert).</summary>
public sealed class IdResponseDto
{
    public int Id { get; set; }
}

/// <summary>GET api/database-backup/folder — server backup directory.</summary>
public sealed class BackupFolderDto
{
    public string Path { get; init; } = "";
}
