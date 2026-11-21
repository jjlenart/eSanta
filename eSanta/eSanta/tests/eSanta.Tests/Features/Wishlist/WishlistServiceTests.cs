using eSanta.Data;
using eSanta.Features.Wishlist;
using eSanta.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace eSanta.Tests.Features.Wishlist;

public class WishlistServiceTests
{
    private const string OwnerId = "owner";
    private const string OtherUserId = "other";

    private readonly InMemoryDbContextFactory _dbContextFactory = new();
    private readonly WishlistService _service;

    public WishlistServiceTests()
    {
        _service = new WishlistService(_dbContextFactory);
    }

    [Fact]
    public async Task GetItemsAsync_ReturnsOnlyOwnersItemsInInsertionOrder()
    {
        await SeedAsync(
            new WishlistItem { OwnerId = OwnerId, Name = "Książka", Description = "Kryminał" },
            new WishlistItem { OwnerId = OtherUserId, Name = "Rower", Description = "Górski" },
            new WishlistItem { OwnerId = OwnerId, Name = "Szalik", Description = "Ciepły" });

        var items = await _service.GetItemsAsync(OwnerId);

        items.Select(item => item.Name).ShouldBe(["Książka", "Szalik"]);
    }

    [Fact]
    public async Task AddAsync_WithValidModel_SavesTrimmedItem()
    {
        var model = new WishlistItemFormModel
        {
            Name = "  Książka  ",
            Description = "  Najnowszy kryminał  ",
            Link = "  https://example.com/ksiazka  "
        };

        var result = await _service.AddAsync(OwnerId, model);

        result.Succeeded.ShouldBeTrue();
        var saved = (await GetAllEntitiesAsync()).ShouldHaveSingleItem();
        saved.OwnerId.ShouldBe(OwnerId);
        saved.Name.ShouldBe("Książka");
        saved.Description.ShouldBe("Najnowszy kryminał");
        saved.Link.ShouldBe("https://example.com/ksiazka");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AddAsync_WithoutLink_SavesNullLink(string? link)
    {
        var result = await _service.AddAsync(OwnerId, new WishlistItemFormModel { Name = "Książka", Description = "Kryminał", Link = link });

        result.Succeeded.ShouldBeTrue();
        (await GetAllEntitiesAsync()).ShouldHaveSingleItem().Link.ShouldBeNull();
    }

    [Theory]
    [InlineData("", "Opis")]
    [InlineData("Nazwa", "")]
    [InlineData("   ", "Opis")]
    public async Task AddAsync_WithMissingRequiredField_ReturnsFailure(string name, string description)
    {
        var result = await _service.AddAsync(OwnerId, new WishlistItemFormModel { Name = name, Description = description });

        result.Succeeded.ShouldBeFalse();
        result.Error.ShouldNotBeNullOrWhiteSpace();
        (await GetAllEntitiesAsync()).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("example.com")]
    [InlineData("ftp://example.com")]
    [InlineData("javascript:alert(1)")]
    public async Task AddAsync_WithInvalidLink_ReturnsFailure(string link)
    {
        var result = await _service.AddAsync(OwnerId, new WishlistItemFormModel { Name = "Książka", Description = "Kryminał", Link = link });

        result.Succeeded.ShouldBeFalse();
        result.Error.ShouldBe("Podaj poprawny adres zaczynający się od http:// lub https://.");
        (await GetAllEntitiesAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task UpdateAsync_OwnItem_UpdatesFields()
    {
        var item = await SeedAsync(new WishlistItem { OwnerId = OwnerId, Name = "Książka", Description = "Kryminał", Link = "https://example.com" });

        var result = await _service.UpdateAsync(OwnerId, item.Id, new WishlistItemFormModel { Name = "Gra", Description = "Planszowa" });

        result.Succeeded.ShouldBeTrue();
        var saved = (await GetAllEntitiesAsync()).ShouldHaveSingleItem();
        saved.Name.ShouldBe("Gra");
        saved.Description.ShouldBe("Planszowa");
        saved.Link.ShouldBeNull();
    }

    [Fact]
    public async Task UpdateAsync_SomeoneElsesItem_ReturnsFailureAndDoesNotChangeIt()
    {
        var item = await SeedAsync(new WishlistItem { OwnerId = OtherUserId, Name = "Rower", Description = "Górski" });

        var result = await _service.UpdateAsync(OwnerId, item.Id, new WishlistItemFormModel { Name = "Hulajnoga", Description = "Elektryczna" });

        result.Succeeded.ShouldBeFalse();
        (await GetAllEntitiesAsync()).ShouldHaveSingleItem().Name.ShouldBe("Rower");
    }

    [Fact]
    public async Task DeleteAsync_OwnItem_RemovesIt()
    {
        var item = await SeedAsync(new WishlistItem { OwnerId = OwnerId, Name = "Książka", Description = "Kryminał" });

        var result = await _service.DeleteAsync(OwnerId, item.Id);

        result.Succeeded.ShouldBeTrue();
        (await GetAllEntitiesAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_SomeoneElsesItem_ReturnsFailureAndKeepsIt()
    {
        var item = await SeedAsync(new WishlistItem { OwnerId = OtherUserId, Name = "Rower", Description = "Górski" });

        var result = await _service.DeleteAsync(OwnerId, item.Id);

        result.Succeeded.ShouldBeFalse();
        (await GetAllEntitiesAsync()).ShouldHaveSingleItem();
    }

    private async Task<WishlistItem> SeedAsync(params WishlistItem[] items)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.WishlistItems.AddRange(items);
        await dbContext.SaveChangesAsync();

        return items[0];
    }

    private async Task<List<WishlistItem>> GetAllEntitiesAsync()
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();

        return await dbContext.WishlistItems.AsNoTracking().ToListAsync();
    }
}
