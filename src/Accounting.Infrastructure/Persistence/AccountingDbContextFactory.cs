using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Accounting.Infrastructure.Persistence;

/// <summary>Design-time factory for EF Core tools (<c>dotnet ef migrations add</c>) — uses LocalDB by default.</summary>
public sealed class AccountingDbContextFactory : IDesignTimeDbContextFactory<AccountingDbContext>
{
    public AccountingDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AccountingDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\mssqllocaldb;Database=AccountingDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true")
            .Options;
        return new AccountingDbContext(options);
    }
}
