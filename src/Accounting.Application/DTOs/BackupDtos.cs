namespace Accounting.Application.DTOs;

public sealed class ImportBackupResponseDto
{
    public bool Ok { get; set; }
    public string? ErrorMessage { get; set; }
}
