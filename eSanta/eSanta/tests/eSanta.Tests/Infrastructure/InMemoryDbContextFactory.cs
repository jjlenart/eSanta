using eSanta.Data;
using Microsoft.EntityFrameworkCore;

namespace eSanta.Tests.Infrastructure;

public sealed class InMemoryDbContextFactory : IDbContextFactory<ApplicationDbContext>
{
    private readonly DbContextOptions<ApplicationDbContext> _options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

    public ApplicationDbContext CreateDbContext() => new(_options);
}
