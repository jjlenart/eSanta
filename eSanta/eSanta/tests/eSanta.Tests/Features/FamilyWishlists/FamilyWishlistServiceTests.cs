using eSanta.Data;
using eSanta.Features.FamilyWishlists;
using eSanta.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace eSanta.Tests.Features.FamilyWishlists;

public class FamilyWishlistServiceTests
{
    private const string Anna = "anna";
    private const string Jan = "jan";
    private const string Zofia = "zofia";
    private const string Outsider = "outsider";
    private const string Homeless = "homeless";

    private readonly InMemoryDbContextFactory _dbContextFactory = new();
    private readonly FamilyWishlistService _service;

    public FamilyWishlistServiceTests()
    {
        _service = new FamilyWishlistService(_dbContextFactory);

        using var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.Families.AddRange(
            new Family { Id = 1, Name = "Kowalscy", NormalizedName = "KOWALSCY" },
            new Family { Id = 2, Name = "Nowakowie", NormalizedName = "NOWAKOWIE" });
        dbContext.Users.AddRange(
            new ApplicationUser { Id = Anna, UserName = "Anna", DisplayName = "Ciocia Ania", FamilyId = 1 },
            new ApplicationUser { Id = Jan, UserName = "Jan", FamilyId = 1 },
            new ApplicationUser { Id = Zofia, UserName = "Zofia", DisplayName = "Babcia", FamilyId = 1 },
            new ApplicationUser { Id = Outsider, UserName = "Piotr", FamilyId = 2 },
            new ApplicationUser { Id = Homeless, UserName = "Ewa" });
        dbContext.SaveChanges();
    }

    [Fact]
    public async Task GetFamilyMembersAsync_ReturnsOtherMembersSortedByAliasWithLoginFallback()
    {
        var result = await _service.GetFamilyMembersAsync(Anna);

        result.HasFamily.ShouldBeTrue();
        result.FamilyName.ShouldBe("Kowalscy");
        result.Members.Select(member => member.Alias).ShouldBe(["Babcia", "Jan"]);
        result.Members.ShouldNotContain(member => member.Id == Anna || member.Id == Outsider);
    }

    [Fact]
    public async Task GetFamilyMembersAsync_ForUserWithoutFamily_ReturnsNoFamily()
    {
        var result = await _service.GetFamilyMembersAsync(Homeless);

        result.HasFamily.ShouldBeFalse();
        result.Members.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetMemberWishlistAsync_ShowsReservationStatusFromViewersPerspective()
    {
        await SeedItemsAsync(
            new WishlistItem { Id = 1, OwnerId = Jan, Name = "Książka", Description = "Kryminał" },
            new WishlistItem { Id = 2, OwnerId = Jan, Name = "Szalik", Description = "Ciepły", ReservedById = Anna },
            new WishlistItem { Id = 3, OwnerId = Jan, Name = "Gra", Description = "Planszowa", ReservedById = Zofia });

        var wishlist = await _service.GetMemberWishlistAsync(Anna, Jan);

        wishlist.ShouldNotBeNull();
        wishlist.Alias.ShouldBe("Jan");
        wishlist.Items.Select(item => item.Status).ShouldBe(
            [ReservationStatus.Available, ReservationStatus.ReservedByMe, ReservationStatus.ReservedByOther]);
    }

    [Theory]
    [InlineData(Outsider)]
    [InlineData(Anna)]
    [InlineData("missing")]
    public async Task GetMemberWishlistAsync_ForSelfOrPersonOutsideFamily_ReturnsNull(string memberId)
    {
        var wishlist = await _service.GetMemberWishlistAsync(Anna, memberId);

        wishlist.ShouldBeNull();
    }

    [Fact]
    public async Task ReserveAsync_AvailableItem_ReservesForCurrentUser()
    {
        await SeedOwnItemsAsync(Anna, FamilyWishlistService.MinimumOwnItemsToReserve);
        await SeedItemsAsync(new WishlistItem { Id = 1, OwnerId = Jan, Name = "Książka", Description = "Kryminał" });

        var result = await _service.ReserveAsync(Anna, 1);

        result.Succeeded.ShouldBeTrue();
        (await GetItemAsync(1)).ReservedById.ShouldBe(Anna);
    }

    [Fact]
    public async Task ReserveAsync_ItemReservedBySomeoneElse_ReturnsFailureAndKeepsReservation()
    {
        await SeedOwnItemsAsync(Anna, FamilyWishlistService.MinimumOwnItemsToReserve);
        await SeedItemsAsync(new WishlistItem { Id = 1, OwnerId = Jan, Name = "Książka", Description = "Kryminał", ReservedById = Zofia });

        var result = await _service.ReserveAsync(Anna, 1);

        result.Succeeded.ShouldBeFalse();
        result.Error.ShouldBe("Ten prezent został już zarezerwowany przez kogoś innego.");
        (await GetItemAsync(1)).ReservedById.ShouldBe(Zofia);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public async Task ReserveAsync_WhenOwnWishlistHasTooFewItems_ReturnsFailure(int ownItemsCount)
    {
        await SeedOwnItemsAsync(Anna, ownItemsCount);
        await SeedItemsAsync(new WishlistItem { Id = 1, OwnerId = Jan, Name = "Książka", Description = "Kryminał" });

        var result = await _service.ReserveAsync(Anna, 1);

        result.Succeeded.ShouldBeFalse();
        result.Error.ShouldBe("A czy Ty już uzupełniłeś swój list?");
        result.IsNotice.ShouldBeTrue();
        (await GetItemAsync(1)).ReservedById.ShouldBeNull();
    }

    [Fact]
    public async Task ReserveAsync_OwnItem_ReturnsFailure()
    {
        await SeedItemsAsync(new WishlistItem { Id = 1, OwnerId = Anna, Name = "Książka", Description = "Kryminał" });

        var result = await _service.ReserveAsync(Anna, 1);

        result.Succeeded.ShouldBeFalse();
        (await GetItemAsync(1)).ReservedById.ShouldBeNull();
    }

    [Fact]
    public async Task ReserveAsync_ItemOfPersonOutsideFamily_ReturnsFailure()
    {
        await SeedItemsAsync(new WishlistItem { Id = 1, OwnerId = Outsider, Name = "Rower", Description = "Górski" });

        var result = await _service.ReserveAsync(Anna, 1);

        result.Succeeded.ShouldBeFalse();
        (await GetItemAsync(1)).ReservedById.ShouldBeNull();
    }

    [Fact]
    public async Task CancelReservationAsync_OwnReservation_ClearsIt()
    {
        await SeedItemsAsync(new WishlistItem { Id = 1, OwnerId = Jan, Name = "Książka", Description = "Kryminał", ReservedById = Anna });

        var result = await _service.CancelReservationAsync(Anna, 1);

        result.Succeeded.ShouldBeTrue();
        (await GetItemAsync(1)).ReservedById.ShouldBeNull();
    }

    [Fact]
    public async Task CancelReservationAsync_SomeoneElsesReservation_ReturnsFailureAndKeepsIt()
    {
        await SeedItemsAsync(new WishlistItem { Id = 1, OwnerId = Jan, Name = "Książka", Description = "Kryminał", ReservedById = Zofia });

        var result = await _service.CancelReservationAsync(Anna, 1);

        result.Succeeded.ShouldBeFalse();
        (await GetItemAsync(1)).ReservedById.ShouldBe(Zofia);
    }

    [Fact]
    public async Task ConcurrentReservations_SecondSaveIsRejected()
    {
        await SeedItemsAsync(new WishlistItem { Id = 1, OwnerId = Jan, Name = "Książka", Description = "Kryminał" });

        await using var annaContext = _dbContextFactory.CreateDbContext();
        await using var zofiaContext = _dbContextFactory.CreateDbContext();
        var itemSeenByAnna = await annaContext.WishlistItems.SingleAsync(item => item.Id == 1);
        var itemSeenByZofia = await zofiaContext.WishlistItems.SingleAsync(item => item.Id == 1);

        itemSeenByZofia.ReservedById = Zofia;
        await zofiaContext.SaveChangesAsync();

        itemSeenByAnna.ReservedById = Anna;
        await Should.ThrowAsync<DbUpdateConcurrencyException>(() => annaContext.SaveChangesAsync());
        (await GetItemAsync(1)).ReservedById.ShouldBe(Zofia);
    }

    private async Task SeedItemsAsync(params WishlistItem[] items)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.WishlistItems.AddRange(items);
        await dbContext.SaveChangesAsync();
    }

    private Task SeedOwnItemsAsync(string ownerId, int count) =>
        SeedItemsAsync(Enumerable.Range(1, count)
            .Select(index => new WishlistItem { Id = 100 + index, OwnerId = ownerId, Name = $"Prezent {index}" })
            .ToArray());

    private async Task<WishlistItem> GetItemAsync(int id)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();

        return await dbContext.WishlistItems.AsNoTracking().SingleAsync(item => item.Id == id);
    }
}
