using eSanta.Common;
using eSanta.Data;
using Microsoft.EntityFrameworkCore;

namespace eSanta.Features.FamilyWishlists;

public sealed class FamilyWishlistService(IDbContextFactory<ApplicationDbContext> dbContextFactory) : IFamilyWishlistService
{
    private const string NotFoundError = "Nie znaleziono prezentu. Mógł zostać usunięty przez właściciela.";
    private const string AlreadyReservedError = "Ten prezent został już zarezerwowany przez kogoś innego.";

    public async Task<FamilyMembersResult> GetFamilyMembersAsync(string currentUserId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var family = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == currentUserId && user.Family != null)
            .Select(user => new { user.Family!.Id, user.Family.Name })
            .FirstOrDefaultAsync(cancellationToken);

        if (family is null)
        {
            return FamilyMembersResult.NoFamily();
        }

        var members = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.FamilyId == family.Id && user.Id != currentUserId)
            .Select(user => new { user.Id, user.DisplayName, user.UserName })
            .ToListAsync(cancellationToken);

        var memberDtos = members
            .Select(member => new FamilyMemberDto(member.Id, ApplicationUser.ToAlias(member.DisplayName, member.UserName)))
            .OrderBy(member => member.Alias, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        return new FamilyMembersResult(family.Name, memberDtos);
    }

    public async Task<MemberWishlistDto?> GetMemberWishlistAsync(string currentUserId, string memberId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        if (!await AreInSameFamilyAsync(dbContext, currentUserId, memberId, cancellationToken))
        {
            return null;
        }

        var member = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == memberId)
            .Select(user => new { user.DisplayName, user.UserName })
            .FirstAsync(cancellationToken);

        var items = await dbContext.WishlistItems
            .AsNoTracking()
            .Where(item => item.OwnerId == memberId)
            .OrderBy(item => item.Id)
            .Select(item => new { item.Id, item.Name, item.Description, item.Link, item.ReservedById })
            .ToListAsync(cancellationToken);

        var itemDtos = items
            .Select(item => new ReservableItemDto(
                item.Id,
                item.Name,
                item.Description,
                item.Link,
                ToStatus(item.ReservedById, currentUserId)))
            .ToList();

        return new MemberWishlistDto(memberId, ApplicationUser.ToAlias(member.DisplayName, member.UserName), itemDtos);
    }

    public async Task<ServiceResult> ReserveAsync(string currentUserId, int itemId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var item = await dbContext.WishlistItems.FirstOrDefaultAsync(item => item.Id == itemId, cancellationToken);
        if (item is null || !await AreInSameFamilyAsync(dbContext, currentUserId, item.OwnerId, cancellationToken))
        {
            return ServiceResult.Failure(NotFoundError);
        }

        if (item.ReservedById == currentUserId)
        {
            return ServiceResult.Success();
        }

        if (item.ReservedById is not null)
        {
            return ServiceResult.Failure(AlreadyReservedError);
        }

        item.ReservedById = currentUserId;

        return await SaveAsync(dbContext, AlreadyReservedError, cancellationToken);
    }

    public async Task<ServiceResult> CancelReservationAsync(string currentUserId, int itemId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var item = await dbContext.WishlistItems.FirstOrDefaultAsync(item => item.Id == itemId, cancellationToken);
        if (item is null)
        {
            return ServiceResult.Failure(NotFoundError);
        }

        if (item.ReservedById != currentUserId)
        {
            return ServiceResult.Failure("Możesz cofnąć tylko własną rezerwację.");
        }

        item.ReservedById = null;

        return await SaveAsync(dbContext, "Rezerwacja została w międzyczasie zmieniona. Odśwież stronę.", cancellationToken);
    }

    private static async Task<bool> AreInSameFamilyAsync(ApplicationDbContext dbContext, string currentUserId, string otherUserId, CancellationToken cancellationToken)
    {
        if (currentUserId == otherUserId)
        {
            return false;
        }

        var familyIds = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == currentUserId || user.Id == otherUserId)
            .Select(user => user.FamilyId)
            .ToListAsync(cancellationToken);

        return familyIds.Count == 2 && familyIds[0] is not null && familyIds[0] == familyIds[1];
    }

    private static async Task<ServiceResult> SaveAsync(ApplicationDbContext dbContext, string concurrencyError, CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult.Failure(concurrencyError);
        }

        return ServiceResult.Success();
    }

    private static ReservationStatus ToStatus(string? reservedById, string currentUserId) => reservedById switch
    {
        null => ReservationStatus.Available,
        _ when reservedById == currentUserId => ReservationStatus.ReservedByMe,
        _ => ReservationStatus.ReservedByOther
    };
}
