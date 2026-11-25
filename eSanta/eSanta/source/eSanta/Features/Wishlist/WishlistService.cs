using eSanta.Common;
using eSanta.Data;
using Microsoft.EntityFrameworkCore;

namespace eSanta.Features.Wishlist;

public sealed class WishlistService(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IModelValidator modelValidator) : IWishlistService
{
    private const string NotFoundError = "Nie znaleziono prezentu. Mógł zostać już usunięty.";
    private const string ConcurrencyError = "Prezent został w międzyczasie zmieniony. Odśwież stronę i spróbuj ponownie.";

    public async Task<IReadOnlyList<WishlistItemDto>> GetItemsAsync(string ownerId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.WishlistItems
            .AsNoTracking()
            .Where(item => item.OwnerId == ownerId)
            .OrderBy(item => item.Id)
            .Select(item => new WishlistItemDto(item.Id, item.Name, item.Description, item.Link))
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceResult> AddAsync(string ownerId, WishlistItemFormModel model, CancellationToken cancellationToken = default)
    {
        var validationErrors = modelValidator.Validate(model);
        if (validationErrors.Count > 0)
        {
            return ServiceResult.Failure(validationErrors);
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var item = new WishlistItem { OwnerId = ownerId };
        Apply(model, item);
        dbContext.WishlistItems.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> UpdateAsync(string ownerId, int itemId, WishlistItemFormModel model, CancellationToken cancellationToken = default)
    {
        var validationErrors = modelValidator.Validate(model);
        if (validationErrors.Count > 0)
        {
            return ServiceResult.Failure(validationErrors);
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var item = await FindOwnedItemAsync(dbContext, ownerId, itemId, cancellationToken);
        if (item is null)
        {
            return ServiceResult.Failure(NotFoundError);
        }

        Apply(model, item);

        return await SaveAsync(dbContext, cancellationToken);
    }

    public async Task<ServiceResult> DeleteAsync(string ownerId, int itemId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var item = await FindOwnedItemAsync(dbContext, ownerId, itemId, cancellationToken);
        if (item is null)
        {
            return ServiceResult.Failure(NotFoundError);
        }

        dbContext.WishlistItems.Remove(item);

        return await SaveAsync(dbContext, cancellationToken);
    }

    private static Task<WishlistItem?> FindOwnedItemAsync(ApplicationDbContext dbContext, string ownerId, int itemId, CancellationToken cancellationToken) =>
        dbContext.WishlistItems.FirstOrDefaultAsync(item => item.Id == itemId && item.OwnerId == ownerId, cancellationToken);

    private static async Task<ServiceResult> SaveAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult.Failure(ConcurrencyError);
        }

        return ServiceResult.Success();
    }

    private static void Apply(WishlistItemFormModel model, WishlistItem item)
    {
        item.Name = model.Name.Trim();
        item.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        item.Link = string.IsNullOrWhiteSpace(model.Link) ? null : model.Link.Trim();
    }
}
