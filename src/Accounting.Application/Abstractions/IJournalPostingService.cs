using Accounting.Application.DTOs;

namespace Accounting.Application.Abstractions;

public interface IJournalPostingService
{
    Task<JournalPostingResult> PostJournalAsync(PostJournalRequest request, CancellationToken cancellationToken = default);
}
