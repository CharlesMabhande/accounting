using Accounting.Application.Abstractions;

namespace Accounting.Infrastructure.Security;

public sealed class CurrentSessionContext : ICurrentSessionContext
{
    public int? UserId { get; set; }
}
