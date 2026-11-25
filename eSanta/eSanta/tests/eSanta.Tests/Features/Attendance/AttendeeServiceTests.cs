using eSanta.Common;
using eSanta.Data;
using eSanta.Features.Attendance;
using eSanta.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace eSanta.Tests.Features.Attendance;

public class AttendeeServiceTests
{
    private const string CurrentUserId = "current";
    private const string OtherUserId = "other";
    private const string StrangerId = "stranger";
    private const string HomelessId = "homeless";
    private const int MyFamilyId = 1;
    private const int OtherFamilyId = 2;

    private readonly InMemoryDbContextFactory _dbContextFactory = new();
    private readonly AttendeeService _service;

    public AttendeeServiceTests()
    {
        _service = new AttendeeService(_dbContextFactory, new DataAnnotationsModelValidator());

        using var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.Families.AddRange(
            new Family { Id = MyFamilyId, Name = "Kowalscy", NormalizedName = "KOWALSCY" },
            new Family { Id = OtherFamilyId, Name = "Nowakowie", NormalizedName = "NOWAKOWIE" });
        dbContext.Users.AddRange(
            new ApplicationUser { Id = CurrentUserId, UserName = "Anna", DisplayName = "Ciocia Ania", FamilyId = MyFamilyId },
            new ApplicationUser { Id = OtherUserId, UserName = "Jan", FamilyId = MyFamilyId },
            new ApplicationUser { Id = StrangerId, UserName = "Piotr", FamilyId = OtherFamilyId },
            new ApplicationUser { Id = HomelessId, UserName = "Bez rodziny" });
        dbContext.SaveChanges();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAttendeesOrderedByFullName()
    {
        await SeedAsync(
            new Attendee { FullName = "Zofia Nowak", FamilyId = MyFamilyId },
            new Attendee { FullName = "Adam Kowalski", FamilyId = MyFamilyId, IsPresentAtChristmasEve = true });

        var result = await _service.GetAllAsync(CurrentUserId);

        result.FamilyName.ShouldBe("Kowalscy");
        result.Attendees.Select(attendee => attendee.FullName).ShouldBe(["Adam Kowalski", "Zofia Nowak"]);
        result.Attendees[0].IsPresentAtChristmasEve.ShouldBeTrue();
        result.Attendees.ShouldAllBe(attendee => attendee.CanEdit && !attendee.IsRegisteredUser);
    }

    [Fact]
    public async Task GetAllAsync_ShowsRegisteredUsersByCurrentAliasAsReadOnly()
    {
        await SeedAsync(
            new Attendee { FullName = "stary alias", UserId = CurrentUserId, IsPresentAtChristmasEve = true },
            new Attendee { FullName = "Jan", UserId = OtherUserId },
            new Attendee { FullName = "Bartek", FamilyId = MyFamilyId });

        var attendees = (await _service.GetAllAsync(CurrentUserId)).Attendees;

        attendees.Select(attendee => attendee.FullName).ShouldBe(["Bartek", "Ciocia Ania", "Jan"]);
        var me = attendees.Single(attendee => attendee.FullName == "Ciocia Ania");
        me.IsRegisteredUser.ShouldBeTrue();
        me.IsCurrentUser.ShouldBeTrue();
        me.CanEdit.ShouldBeFalse();
        var other = attendees.Single(attendee => attendee.FullName == "Jan");
        other.IsCurrentUser.ShouldBeFalse();
        other.CanEdit.ShouldBeFalse();
    }

    [Fact]
    public async Task GetAllAsync_SkipsAttendeesFromOtherFamilies()
    {
        await SeedAsync(
            new Attendee { FullName = "Bartek", FamilyId = MyFamilyId },
            new Attendee { FullName = "Piotr", UserId = StrangerId },
            new Attendee { FullName = "Dziadek Nowak", FamilyId = OtherFamilyId },
            new Attendee { FullName = "Bez rodziny" });

        var attendees = (await _service.GetAllAsync(CurrentUserId)).Attendees;

        attendees.Select(attendee => attendee.FullName).ShouldBe(["Bartek"]);
    }

    [Fact]
    public async Task GetAllAsync_WithoutFamily_ReturnsNoFamily()
    {
        await SeedAsync(new Attendee { FullName = "Bartek", FamilyId = MyFamilyId });

        var result = await _service.GetAllAsync(HomelessId);

        result.HasFamily.ShouldBeFalse();
        result.Attendees.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetMyStatusAsync_WithoutEntry_ReturnsBothUnchecked()
    {
        var status = await _service.GetMyStatusAsync(CurrentUserId);

        status.ShouldBe(new MyAttendanceDto(false, false));
    }

    [Fact]
    public async Task SetMyStatusAsync_FirstTime_CreatesOwnEntryWithAlias()
    {
        var result = await _service.SetMyStatusAsync(CurrentUserId, new MyAttendanceDto(true, false));

        result.Succeeded.ShouldBeTrue();
        var saved = (await GetAllEntitiesAsync()).ShouldHaveSingleItem();
        saved.UserId.ShouldBe(CurrentUserId);
        saved.FullName.ShouldBe("Ciocia Ania");
        saved.IsPresentAtChristmasEve.ShouldBeTrue();
        saved.ParticipatesInGiftExchange.ShouldBeFalse();
        (await _service.GetMyStatusAsync(CurrentUserId)).ShouldBe(new MyAttendanceDto(true, false));
    }

    [Fact]
    public async Task SetMyStatusAsync_SecondTime_UpdatesExistingEntry()
    {
        await _service.SetMyStatusAsync(CurrentUserId, new MyAttendanceDto(true, false));

        var result = await _service.SetMyStatusAsync(CurrentUserId, new MyAttendanceDto(false, true));

        result.Succeeded.ShouldBeTrue();
        var saved = (await GetAllEntitiesAsync()).ShouldHaveSingleItem();
        saved.IsPresentAtChristmasEve.ShouldBeFalse();
        saved.ParticipatesInGiftExchange.ShouldBeTrue();
    }

    [Theory]
    [InlineData("missing")]
    [InlineData(HomelessId)]
    public async Task SetMyStatusAsync_ForUnknownUserOrUserWithoutFamily_ReturnsFailure(string userId)
    {
        var result = await _service.SetMyStatusAsync(userId, new MyAttendanceDto(true, true));

        result.Succeeded.ShouldBeFalse();
        (await GetAllEntitiesAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task UpdateAsync_RegisteredUsersEntry_ReturnsFailureAndKeepsIt()
    {
        var entry = await SeedAsync(new Attendee { FullName = "Jan", UserId = OtherUserId });

        var result = await _service.UpdateAsync(CurrentUserId, entry.Id, new AttendeeFormModel { FullName = "Zmieniony", IsPresentAtChristmasEve = true });

        result.Succeeded.ShouldBeFalse();
        var saved = (await GetAllEntitiesAsync()).ShouldHaveSingleItem();
        saved.FullName.ShouldBe("Jan");
        saved.IsPresentAtChristmasEve.ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteAsync_RegisteredUsersEntry_ReturnsFailureAndKeepsIt()
    {
        var entry = await SeedAsync(new Attendee { FullName = "Jan", UserId = OtherUserId });

        var result = await _service.DeleteAsync(CurrentUserId, entry.Id);

        result.Succeeded.ShouldBeFalse();
        (await GetAllEntitiesAsync()).ShouldHaveSingleItem();
    }

    [Fact]
    public async Task AddAsync_WithValidModel_SavesTrimmedAttendeeInMyFamily()
    {
        var model = new AttendeeFormModel
        {
            FullName = "  Jan Kowalski  ",
            IsPresentAtChristmasEve = true,
            ParticipatesInGiftExchange = true
        };

        var result = await _service.AddAsync(CurrentUserId, model);

        result.Succeeded.ShouldBeTrue();
        var saved = (await GetAllEntitiesAsync()).ShouldHaveSingleItem();
        saved.FullName.ShouldBe("Jan Kowalski");
        saved.FamilyId.ShouldBe(MyFamilyId);
        saved.IsPresentAtChristmasEve.ShouldBeTrue();
        saved.ParticipatesInGiftExchange.ShouldBeTrue();
    }

    [Fact]
    public async Task AddAsync_WithoutFamily_ReturnsFailureAndSavesNothing()
    {
        var result = await _service.AddAsync(HomelessId, new AttendeeFormModel { FullName = "Jan Kowalski" });

        result.Succeeded.ShouldBeFalse();
        (await GetAllEntitiesAsync()).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AddAsync_WithEmptyFullName_ReturnsFailureAndSavesNothing(string fullName)
    {
        var result = await _service.AddAsync(CurrentUserId, new AttendeeFormModel { FullName = fullName });

        result.Succeeded.ShouldBeFalse();
        result.Error.ShouldNotBeNullOrWhiteSpace();
        (await GetAllEntitiesAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task AddAsync_WithTooLongFullName_ReturnsFailure()
    {
        var model = new AttendeeFormModel { FullName = new string('a', Attendee.FullNameMaxLength + 1) };

        var result = await _service.AddAsync(CurrentUserId, model);

        result.Succeeded.ShouldBeFalse();
        (await GetAllEntitiesAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task UpdateAsync_WithExistingAttendee_UpdatesAllFields()
    {
        var attendee = await SeedAsync(new Attendee { FullName = "Jan Kowalski", FamilyId = MyFamilyId });
        var model = new AttendeeFormModel
        {
            FullName = "Jan Nowak",
            IsPresentAtChristmasEve = true,
            ParticipatesInGiftExchange = true
        };

        var result = await _service.UpdateAsync(CurrentUserId, attendee.Id, model);

        result.Succeeded.ShouldBeTrue();
        var saved = (await GetAllEntitiesAsync()).ShouldHaveSingleItem();
        saved.FullName.ShouldBe("Jan Nowak");
        saved.IsPresentAtChristmasEve.ShouldBeTrue();
        saved.ParticipatesInGiftExchange.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateAsync_AttendeeFromOtherFamily_ReturnsFailureAndKeepsIt()
    {
        var attendee = await SeedAsync(new Attendee { FullName = "Dziadek Nowak", FamilyId = OtherFamilyId });

        var result = await _service.UpdateAsync(CurrentUserId, attendee.Id, new AttendeeFormModel { FullName = "Zmieniony" });

        result.Succeeded.ShouldBeFalse();
        (await GetAllEntitiesAsync()).ShouldHaveSingleItem().FullName.ShouldBe("Dziadek Nowak");
    }

    [Fact]
    public async Task UpdateAsync_WithMissingAttendee_ReturnsFailure()
    {
        var result = await _service.UpdateAsync(CurrentUserId, 999, new AttendeeFormModel { FullName = "Jan Kowalski" });

        result.Succeeded.ShouldBeFalse();
        result.Error.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidModel_DoesNotChangeAttendee()
    {
        var attendee = await SeedAsync(new Attendee { FullName = "Jan Kowalski", FamilyId = MyFamilyId });

        var result = await _service.UpdateAsync(CurrentUserId, attendee.Id, new AttendeeFormModel { FullName = "" });

        result.Succeeded.ShouldBeFalse();
        (await GetAllEntitiesAsync()).ShouldHaveSingleItem().FullName.ShouldBe("Jan Kowalski");
    }

    [Fact]
    public async Task DeleteAsync_WithExistingAttendee_RemovesAttendee()
    {
        var attendee = await SeedAsync(new Attendee { FullName = "Jan Kowalski", FamilyId = MyFamilyId });

        var result = await _service.DeleteAsync(CurrentUserId, attendee.Id);

        result.Succeeded.ShouldBeTrue();
        (await GetAllEntitiesAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_AttendeeFromOtherFamily_ReturnsFailureAndKeepsIt()
    {
        var attendee = await SeedAsync(new Attendee { FullName = "Dziadek Nowak", FamilyId = OtherFamilyId });

        var result = await _service.DeleteAsync(CurrentUserId, attendee.Id);

        result.Succeeded.ShouldBeFalse();
        (await GetAllEntitiesAsync()).ShouldHaveSingleItem();
    }

    [Fact]
    public async Task DeleteAsync_WithMissingAttendee_ReturnsFailure()
    {
        var result = await _service.DeleteAsync(CurrentUserId, 999);

        result.Succeeded.ShouldBeFalse();
        result.Error.ShouldNotBeNullOrWhiteSpace();
    }

    private async Task<Attendee> SeedAsync(params Attendee[] attendees)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.Attendees.AddRange(attendees);
        await dbContext.SaveChangesAsync();

        return attendees[0];
    }

    private async Task<List<Attendee>> GetAllEntitiesAsync()
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();

        return await dbContext.Attendees.AsNoTracking().ToListAsync();
    }
}
