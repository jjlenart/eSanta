using Microsoft.EntityFrameworkCore;

namespace eSanta.Data;

/// <summary>
/// Applies pending EF Core migrations on startup. Hosted services start before the web server,
/// so no request is handled against an outdated schema.
/// </summary>
public sealed class DatabaseMigrationService(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    ILogger<DatabaseMigrationService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        if (pendingMigrations.Count == 0)
        {
            logger.LogInformation("Database schema is up to date.");
            return;
        }

        logger.LogInformation("Applying {Count} pending migration(s): {Migrations}", pendingMigrations.Count, string.Join(", ", pendingMigrations));
        await dbContext.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Database migrations applied.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
