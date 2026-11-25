using eSanta.Common;
using eSanta.Data;
using eSanta.Features.ShoppingList;
using eSanta.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace eSanta.Tests.Features.ShoppingList;

public class ShoppingListServiceTests
{
    private const string Me = "me";
    private const string Jan = "jan";
    private const string Zofia = "zofia";
    private const string Stranger = "stranger";
    private const string Homeless = "homeless";
    private const int MyFamilyId = 1;
    private const int OtherFamilyId = 2;

    private readonly InMemoryDbContextFactory _dbContextFactory = new();
    private readonly ShoppingListService _service;

    public ShoppingListServiceTests()
    {
        _service = new ShoppingListService(_dbContextFactory, new DataAnnotationsModelValidator());

        using var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.Families.AddRange(
            new Family { Id = MyFamilyId, Name = "Kowalscy", NormalizedName = "KOWALSCY" },
            new Family { Id = OtherFamilyId, Name = "Nowakowie", NormalizedName = "NOWAKOWIE" });
        dbContext.Users.AddRange(
            new ApplicationUser { Id = Me, UserName = "Anna", FamilyId = MyFamilyId },
            new ApplicationUser { Id = Jan, UserName = "Jan", DisplayName = "Wujek Janek", FamilyId = MyFamilyId },
            new ApplicationUser { Id = Zofia, UserName = "Zofia", FamilyId = MyFamilyId },
            new ApplicationUser { Id = Stranger, UserName = "Piotr", FamilyId = OtherFamilyId },
            new ApplicationUser { Id = Homeless, UserName = "Bez rodziny" });
        dbContext.Attendees.AddRange(
            new Attendee { Id = 1, FullName = "Anna", UserId = Me },
            new Attendee { Id = 2, FullName = "Jan", UserId = Jan },
            new Attendee { Id = 3, FullName = "Babcia Hela", FamilyId = MyFamilyId },
            new Attendee { Id = 10, FullName = "Piotr", UserId = Stranger },
            new Attendee { Id = 11, FullName = "Dziadek Nowak", FamilyId = OtherFamilyId });
        dbContext.SaveChanges();
    }

    [Fact]
    public async Task GetAsync_ListsEveryoneFromAttendanceListExceptMe()
    {
        var rows = (await _service.GetAsync(Me)).Rows;

        rows.Select(row => row.RecipientName).ShouldBe(["Babcia Hela", "Wujek Janek"]);
        rows.ShouldAllBe(row => !row.IsReserved && !row.IsBought && row.GiftName == null && !row.Recipient.IsCustom);
    }

    [Fact]
    public async Task GetAsync_MyReservationsFillGiftNameAndAreMarkedReserved()
    {
        await SeedAsync(dbContext => dbContext.WishlistItems.AddRange(
            new WishlistItem { OwnerId = Jan, Name = "Książka", Description = "-", ReservedById = Me },
            new WishlistItem { OwnerId = Jan, Name = "Szalik", Description = "-", ReservedById = Me },
            new WishlistItem { OwnerId = Jan, Name = "Gra", Description = "-", ReservedById = Zofia }));

        var row = (await _service.GetAsync(Me)).Rows.Single(candidate => candidate.Recipient.UserId == Jan);

        row.IsReserved.ShouldBeTrue();
        row.GiftName.ShouldBe("Książka, Szalik");
    }

    [Fact]
    public async Task GetAsync_ReservationForPersonOutsideAttendanceList_StillAppears()
    {
        await SeedAsync(dbContext => dbContext.WishlistItems.Add(
            new WishlistItem { OwnerId = Zofia, Name = "Perfumy", Description = "-", ReservedById = Me }));

        var row = (await _service.GetAsync(Me)).Rows.Single(candidate => candidate.Recipient.UserId == Zofia);

        row.RecipientName.ShouldBe("Zofia");
        row.IsReserved.ShouldBeTrue();
        row.GiftName.ShouldBe("Perfumy");
    }

    [Fact]
    public async Task GetAsync_OnlyRegisteredRecipientsHaveLetterLink()
    {
        var result = await _service.GetAsync(Me);

        result.FamilyName.ShouldBe("Kowalscy");
        result.Rows.Single(row => row.Recipient.UserId == Jan).HasLetter.ShouldBeTrue();
        result.Rows.Single(row => row.Recipient.AttendeeId == 3).HasLetter.ShouldBeFalse();
    }

    [Fact]
    public async Task GetAsync_SkipsAttendeesAndReservationsFromOtherFamilies()
    {
        await SeedAsync(dbContext => dbContext.WishlistItems.Add(
            new WishlistItem { OwnerId = Stranger, Name = "Zegarek", Description = "-", ReservedById = Me }));

        var rows = (await _service.GetAsync(Me)).Rows;

        rows.ShouldNotContain(row => row.Recipient.UserId == Stranger);
        rows.ShouldNotContain(row => row.Recipient.AttendeeId == 11);
    }

    [Fact]
    public async Task GetAsync_WithoutFamily_ReturnsNoFamily()
    {
        var result = await _service.GetAsync(Homeless);

        result.HasFamily.ShouldBeFalse();
        result.Rows.ShouldBeEmpty();
    }

    [Fact]
    public async Task SaveAsync_ForAttendeeFromOtherFamily_ReturnsFailure()
    {
        var result = await _service.SaveAsync(Me, ShoppingRecipient.ForAttendee(11), new ShoppingEntryFormModel { GiftName = "Coś" });

        result.Succeeded.ShouldBeFalse();
        await using var dbContext = _dbContextFactory.CreateDbContext();
        (await dbContext.ShoppingListEntries.AnyAsync()).ShouldBeFalse();
    }

    [Fact]
    public async Task AddCustomAsync_WithoutFamily_ReturnsFailure()
    {
        var result = await _service.AddCustomAsync(Homeless, new ShoppingEntryFormModel { RecipientName = "Sąsiadka" });

        result.Succeeded.ShouldBeFalse();
    }

    [Fact]
    public async Task SaveAsync_ForRegisteredUser_StoresManualGiftBoughtAndNote()
    {
        var result = await _service.SaveAsync(Me, ShoppingRecipient.ForUser(Jan),
            new ShoppingEntryFormModel { GiftName = "  Kubek  ", IsBought = true, Note = "  Empik, 40 zł  " });

        result.Succeeded.ShouldBeTrue();
        var row = (await _service.GetAsync(Me)).Rows.Single(candidate => candidate.Recipient.UserId == Jan);
        row.GiftName.ShouldBe("Kubek");
        row.IsBought.ShouldBeTrue();
        row.Note.ShouldBe("Empik, 40 zł");
        row.IsReserved.ShouldBeFalse();
    }

    [Fact]
    public async Task SaveAsync_WhenReserved_ShowsReservationButKeepsBoughtAndNote()
    {
        await SeedAsync(dbContext => dbContext.WishlistItems.Add(
            new WishlistItem { OwnerId = Jan, Name = "Książka", Description = "-", ReservedById = Me }));

        await _service.SaveAsync(Me, ShoppingRecipient.ForUser(Jan), new ShoppingEntryFormModel { IsBought = true, Note = "Kupione w sobotę" });

        var row = (await _service.GetAsync(Me)).Rows.Single(candidate => candidate.Recipient.UserId == Jan);
        row.GiftName.ShouldBe("Książka");
        row.IsBought.ShouldBeTrue();
        row.Note.ShouldBe("Kupione w sobotę");
    }

    [Fact]
    public async Task SaveAsync_Twice_UpdatesSameEntry()
    {
        await _service.SaveAsync(Me, ShoppingRecipient.ForAttendee(3), new ShoppingEntryFormModel { GiftName = "Szal" });
        await _service.SaveAsync(Me, ShoppingRecipient.ForAttendee(3), new ShoppingEntryFormModel { GiftName = "Szal", IsBought = true });

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var entry = (await dbContext.ShoppingListEntries.ToListAsync()).ShouldHaveSingleItem();
        entry.AttendeeId.ShouldBe(3);
        entry.RecipientName.ShouldBe("Babcia Hela");
        entry.IsBought.ShouldBeTrue();
    }

    [Fact]
    public async Task SaveAsync_EntriesArePrivatePerBuyer()
    {
        await _service.SaveAsync(Jan, ShoppingRecipient.ForAttendee(3), new ShoppingEntryFormModel { GiftName = "Rękawiczki", IsBought = true });

        var myRow = (await _service.GetAsync(Me)).Rows.Single(candidate => candidate.Recipient.AttendeeId == 3);

        myRow.GiftName.ShouldBeNull();
        myRow.IsBought.ShouldBeFalse();
    }

    [Theory]
    [InlineData(Me)]
    [InlineData(Stranger)]
    [InlineData("missing")]
    public async Task SaveAsync_ForSelfOrUserNotOnListWithoutReservation_ReturnsFailure(string recipientUserId)
    {
        var result = await _service.SaveAsync(Me, ShoppingRecipient.ForUser(recipientUserId), new ShoppingEntryFormModel { GiftName = "Coś" });

        result.Succeeded.ShouldBeFalse();
        await using var dbContext = _dbContextFactory.CreateDbContext();
        (await dbContext.ShoppingListEntries.AnyAsync()).ShouldBeFalse();
    }

    [Fact]
    public async Task SaveAsync_ForRegisteredUsersAttendeeEntryById_ReturnsFailure()
    {
        var result = await _service.SaveAsync(Me, ShoppingRecipient.ForAttendee(2), new ShoppingEntryFormModel { GiftName = "Coś" });

        result.Succeeded.ShouldBeFalse();
    }

    [Fact]
    public async Task SaveAsync_WithTooLongNote_ReturnsFailure()
    {
        var result = await _service.SaveAsync(Me, ShoppingRecipient.ForAttendee(3),
            new ShoppingEntryFormModel { Note = new string('a', ShoppingListEntry.NoteMaxLength + 1) });

        result.Succeeded.ShouldBeFalse();
    }

    [Fact]
    public async Task AddCustomAsync_AddsRowAfterListedPeople()
    {
        var result = await _service.AddCustomAsync(Me, new ShoppingEntryFormModel { RecipientName = "  Sąsiadka  ", GiftName = "Ciasto" });

        result.Succeeded.ShouldBeTrue();
        var rows = (await _service.GetAsync(Me)).Rows;
        var custom = rows[^1];
        custom.Recipient.IsCustom.ShouldBeTrue();
        custom.RecipientName.ShouldBe("Sąsiadka");
        custom.GiftName.ShouldBe("Ciasto");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task AddCustomAsync_WithoutRecipientName_ReturnsFailure(string? recipientName)
    {
        var result = await _service.AddCustomAsync(Me, new ShoppingEntryFormModel { RecipientName = recipientName });

        result.Succeeded.ShouldBeFalse();
        result.Error.ShouldBe("Podaj imię osoby, dla której kupujesz prezent.");
    }

    [Fact]
    public async Task SaveAsync_ForCustomEntry_UpdatesNameAndFields()
    {
        await _service.AddCustomAsync(Me, new ShoppingEntryFormModel { RecipientName = "Sąsiadka" });
        var entryId = (await _service.GetAsync(Me)).Rows.Single(row => row.Recipient.IsCustom).Recipient.CustomEntryId!.Value;

        var result = await _service.SaveAsync(Me, ShoppingRecipient.ForCustomEntry(entryId),
            new ShoppingEntryFormModel { RecipientName = "Pani Basia", GiftName = "Kwiaty", IsBought = true });

        result.Succeeded.ShouldBeTrue();
        var custom = (await _service.GetAsync(Me)).Rows.Single(row => row.Recipient.IsCustom);
        custom.RecipientName.ShouldBe("Pani Basia");
        custom.IsBought.ShouldBeTrue();
    }

    [Fact]
    public async Task CustomEntries_OfAnotherBuyer_CannotBeEditedOrDeleted()
    {
        await _service.AddCustomAsync(Jan, new ShoppingEntryFormModel { RecipientName = "Kolega z pracy" });
        await using (var dbContext = _dbContextFactory.CreateDbContext())
        {
            var entryId = (await dbContext.ShoppingListEntries.SingleAsync()).Id;

            (await _service.SaveAsync(Me, ShoppingRecipient.ForCustomEntry(entryId), new ShoppingEntryFormModel { RecipientName = "Zmienione" })).Succeeded.ShouldBeFalse();
            (await _service.DeleteCustomAsync(Me, entryId)).Succeeded.ShouldBeFalse();
        }

        (await _service.GetAsync(Me)).Rows.ShouldNotContain(row => row.Recipient.IsCustom);
        await using var verifyContext = _dbContextFactory.CreateDbContext();
        (await verifyContext.ShoppingListEntries.SingleAsync()).RecipientName.ShouldBe("Kolega z pracy");
    }

    [Fact]
    public async Task DeleteCustomAsync_OwnCustomEntry_RemovesIt()
    {
        await _service.AddCustomAsync(Me, new ShoppingEntryFormModel { RecipientName = "Sąsiadka" });
        var entryId = (await _service.GetAsync(Me)).Rows.Single(row => row.Recipient.IsCustom).Recipient.CustomEntryId!.Value;

        var result = await _service.DeleteCustomAsync(Me, entryId);

        result.Succeeded.ShouldBeTrue();
        (await _service.GetAsync(Me)).Rows.ShouldNotContain(row => row.Recipient.IsCustom);
    }

    [Fact]
    public async Task DeleteCustomAsync_EntryForListedPerson_ReturnsFailure()
    {
        await _service.SaveAsync(Me, ShoppingRecipient.ForAttendee(3), new ShoppingEntryFormModel { GiftName = "Szal" });
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var entryId = (await dbContext.ShoppingListEntries.SingleAsync()).Id;

        var result = await _service.DeleteCustomAsync(Me, entryId);

        result.Succeeded.ShouldBeFalse();
    }

    private async Task SeedAsync(Action<ApplicationDbContext> seed)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        seed(dbContext);
        await dbContext.SaveChangesAsync();
    }
}
