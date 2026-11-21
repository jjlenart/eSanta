using eSanta.Data;
using eSanta.Features.Attendance;
using eSanta.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace eSanta.Tests.Features.Attendance;

public class AttendeeServiceTests
{
    private readonly InMemoryDbContextFactory _dbContextFactory = new();
    private readonly AttendeeService _service;

    public AttendeeServiceTests()
    {
        _service = new AttendeeService(_dbContextFactory);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAttendeesOrderedByFullName()
    {
        await SeedAsync(
            new Attendee { FullName = "Zofia Nowak" },
            new Attendee { FullName = "Adam Kowalski", IsPresentAtChristmasEve = true });

        var attendees = await _service.GetAllAsync(CurrentUserId);

        attendees.Select(attendee => attendee.FullName).ShouldBe(["Adam Kowalski", "Zofia Nowak"]);
        attendees[0].IsPresentAtChristmasEve.ShouldBeTrue();
        attendees.ShouldAllBe(attendee => attendee.CanEdit && !attendee.IsRegisteredUser);
    }

    [Fact]
    public async Task GetAllAsync_ShowsRegisteredUsersByCurrentAliasAsReadOnly()
    {
        await SeedUsersAsync();
        await SeedAsync(
            new Attendee { FullName = "stary alias", UserId = CurrentUserId, IsPresentAtChristmasEve = true },
            new Attendee { FullName = "Jan", UserId = OtherUserId },
            new Attendee { FullName = "Bartek" });

        var attendees = await _service.GetAllAsync(CurrentUserId);

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
    public async Task GetMyStatusAsync_WithoutEntry_ReturnsBothUnchecked()
    {
        await SeedUsersAsync();

        var status = await _service.GetMyStatusAsync(CurrentUserId);

        status.ShouldBe(new MyAttendanceDto(false, false));
    }

    [Fact]
    public async Task SetMyStatusAsync_FirstTime_CreatesOwnEntryWithAlias()
    {
        await SeedUsersAsync();

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
        await SeedUsersAsync();
        await _service.SetMyStatusAsync(CurrentUserId, new MyAttendanceDto(true, false));

        var result = await _service.SetMyStatusAsync(CurrentUserId, new MyAttendanceDto(false, true));

        result.Succeeded.ShouldBeTrue();
        var saved = (await GetAllEntitiesAsync()).ShouldHaveSingleItem();
        saved.IsPresentAtChristmasEve.ShouldBeFalse();
        saved.ParticipatesInGiftExchange.ShouldBeTrue();
    }

    [Fact]
    public async Task SetMyStatusAsync_ForUnknownUser_ReturnsFailure()
    {
        var result = await _service.SetMyStatusAsync("missing", new MyAttendanceDto(true, true));

        result.Succeeded.ShouldBeFalse();
        (await GetAllEntitiesAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task UpdateAsync_RegisteredUsersEntry_ReturnsFailureAndKeepsIt()
    {
        await SeedUsersAsync();
        var entry = await SeedAsync(new Attendee { FullName = "Jan", UserId = OtherUserId });

        var result = await _service.UpdateAsync(entry.Id, new AttendeeFormModel { FullName = "Zmieniony", IsPresentAtChristmasEve = true });

        result.Succeeded.ShouldBeFalse();
        var saved = (await GetAllEntitiesAsync()).ShouldHaveSingleItem();
        saved.FullName.ShouldBe("Jan");
        saved.IsPresentAtChristmasEve.ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteAsync_RegisteredUsersEntry_ReturnsFailureAndKeepsIt()
    {
        await SeedUsersAsync();
        var entry = await SeedAsync(new Attendee { FullName = "Jan", UserId = OtherUserId });

        var result = await _service.DeleteAsync(entry.Id);

        result.Succeeded.ShouldBeFalse();
        (await GetAllEntitiesAsync()).ShouldHaveSingleItem();
    }

    private const string CurrentUserId = "current";
    private const string OtherUserId = "other";

    private async Task SeedUsersAsync()
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.Users.AddRange(
            new ApplicationUser { Id = CurrentUserId, UserName = "Anna", DisplayName = "Ciocia Ania" },
            new ApplicationUser { Id = OtherUserId, UserName = "Jan" });
        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task AddAsync_WithValidModel_SavesTrimmedAttendee()
    {
        var model = new AttendeeFormModel
        {
            FullName = "  Jan Kowalski  ",
            IsPresentAtChristmasEve = true,
            ParticipatesInGiftExchange = true
        };

        var result = await _service.AddAsync(model);

        result.Succeeded.ShouldBeTrue();
        var saved = (await GetAllEntitiesAsync()).ShouldHaveSingleItem();
        saved.FullName.ShouldBe("Jan Kowalski");
        saved.IsPresentAtChristmasEve.ShouldBeTrue();
        saved.ParticipatesInGiftExchange.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AddAsync_WithEmptyFullName_ReturnsFailureAndSavesNothing(string fullName)
    {
        var result = await _service.AddAsync(new AttendeeFormModel { FullName = fullName });

        result.Succeeded.ShouldBeFalse();
        result.Error.ShouldNotBeNullOrWhiteSpace();
        (await GetAllEntitiesAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task AddAsync_WithTooLongFullName_ReturnsFailure()
    {
        var model = new AttendeeFormModel { FullName = new string('a', Attendee.FullNameMaxLength + 1) };

        var result = await _service.AddAsync(model);

        result.Succeeded.ShouldBeFalse();
        (await GetAllEntitiesAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task UpdateAsync_WithExistingAttendee_UpdatesAllFields()
    {
        var attendee = await SeedAsync(new Attendee { FullName = "Jan Kowalski" });
        var model = new AttendeeFormModel
        {
            FullName = "Jan Nowak",
            IsPresentAtChristmasEve = true,
            ParticipatesInGiftExchange = true
        };

        var result = await _service.UpdateAsync(attendee.Id, model);

        result.Succeeded.ShouldBeTrue();
        var saved = (await GetAllEntitiesAsync()).ShouldHaveSingleItem();
        saved.FullName.ShouldBe("Jan Nowak");
        saved.IsPresentAtChristmasEve.ShouldBeTrue();
        saved.ParticipatesInGiftExchange.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateAsync_WithMissingAttendee_ReturnsFailure()
    {
        var result = await _service.UpdateAsync(999, new AttendeeFormModel { FullName = "Jan Kowalski" });

        result.Succeeded.ShouldBeFalse();
        result.Error.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidModel_DoesNotChangeAttendee()
    {
        var attendee = await SeedAsync(new Attendee { FullName = "Jan Kowalski" });

        var result = await _service.UpdateAsync(attendee.Id, new AttendeeFormModel { FullName = "" });

        result.Succeeded.ShouldBeFalse();
        (await GetAllEntitiesAsync()).ShouldHaveSingleItem().FullName.ShouldBe("Jan Kowalski");
    }

    [Fact]
    public async Task DeleteAsync_WithExistingAttendee_RemovesAttendee()
    {
        var attendee = await SeedAsync(new Attendee { FullName = "Jan Kowalski" });

        var result = await _service.DeleteAsync(attendee.Id);

        result.Succeeded.ShouldBeTrue();
        (await GetAllEntitiesAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_WithMissingAttendee_ReturnsFailure()
    {
        var result = await _service.DeleteAsync(999);

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
