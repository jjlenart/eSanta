using eSanta.Common;
using eSanta.Data;
using Microsoft.EntityFrameworkCore;

namespace eSanta.Features.Attendance;

public sealed class AttendeeService(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IModelValidator modelValidator) : IAttendeeService
{
    private const string NotFoundError = "Nie znaleziono osoby. Mogła zostać usunięta przez innego użytkownika.";
    private const string RegisteredUserError = "Tę pozycję może zmienić tylko jej właściciel.";

    public async Task<IReadOnlyList<AttendeeDto>> GetAllAsync(string currentUserId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var attendees = await dbContext.Attendees
            .AsNoTracking()
            .Select(attendee => new
            {
                attendee.Id,
                attendee.FullName,
                attendee.IsPresentAtChristmasEve,
                attendee.ParticipatesInGiftExchange,
                attendee.UserId,
                DisplayName = attendee.User != null ? attendee.User.DisplayName : null,
                UserName = attendee.User != null ? attendee.User.UserName : null
            })
            .ToListAsync(cancellationToken);

        return attendees
            .Select(attendee => new AttendeeDto(
                attendee.Id,
                attendee.UserId is null ? attendee.FullName : ApplicationUser.ToAlias(attendee.DisplayName, attendee.UserName, attendee.FullName),
                attendee.IsPresentAtChristmasEve,
                attendee.ParticipatesInGiftExchange,
                IsRegisteredUser: attendee.UserId is not null,
                IsCurrentUser: attendee.UserId == currentUserId))
            .OrderBy(attendee => attendee.FullName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public async Task<MyAttendanceDto> GetMyStatusAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var status = await dbContext.Attendees
            .AsNoTracking()
            .Where(attendee => attendee.UserId == userId)
            .Select(attendee => new MyAttendanceDto(attendee.IsPresentAtChristmasEve, attendee.ParticipatesInGiftExchange))
            .FirstOrDefaultAsync(cancellationToken);

        return status ?? new MyAttendanceDto(false, false);
    }

    public async Task<ServiceResult> SetMyStatusAsync(string userId, MyAttendanceDto status, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var user = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new { user.DisplayName, user.UserName })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return ServiceResult.Failure("Nie znaleziono Twojego konta. Zaloguj się ponownie.");
        }

        var attendee = await dbContext.Attendees.FirstOrDefaultAsync(attendee => attendee.UserId == userId, cancellationToken);
        if (attendee is null)
        {
            attendee = new Attendee { UserId = userId };
            dbContext.Attendees.Add(attendee);
        }

        var alias = ApplicationUser.ToAlias(user.DisplayName, user.UserName, fallback: "?");
        attendee.FullName = alias.Length > Attendee.FullNameMaxLength ? alias[..Attendee.FullNameMaxLength] : alias;
        attendee.IsPresentAtChristmasEve = status.IsPresentAtChristmasEve;
        attendee.ParticipatesInGiftExchange = status.ParticipatesInGiftExchange;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return ServiceResult.Failure("Nie udało się zapisać Twojej obecności. Odśwież stronę i spróbuj ponownie.");
        }

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> AddAsync(AttendeeFormModel model, CancellationToken cancellationToken = default)
    {
        var validationErrors = modelValidator.Validate(model);
        if (validationErrors.Count > 0)
        {
            return ServiceResult.Failure(validationErrors);
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var attendee = new Attendee();
        Apply(model, attendee);
        dbContext.Attendees.Add(attendee);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> UpdateAsync(int id, AttendeeFormModel model, CancellationToken cancellationToken = default)
    {
        var validationErrors = modelValidator.Validate(model);
        if (validationErrors.Count > 0)
        {
            return ServiceResult.Failure(validationErrors);
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var attendee = await dbContext.Attendees.FindAsync([id], cancellationToken);
        if (attendee is null)
        {
            return ServiceResult.Failure(NotFoundError);
        }

        if (attendee.UserId is not null)
        {
            return ServiceResult.Failure(RegisteredUserError);
        }

        Apply(model, attendee);

        return await SaveAsync(dbContext, cancellationToken);
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var attendee = await dbContext.Attendees.FindAsync([id], cancellationToken);
        if (attendee is null)
        {
            return ServiceResult.Failure(NotFoundError);
        }

        if (attendee.UserId is not null)
        {
            return ServiceResult.Failure(RegisteredUserError);
        }

        dbContext.Attendees.Remove(attendee);

        return await SaveAsync(dbContext, cancellationToken);
    }

    private static async Task<ServiceResult> SaveAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult.Failure(NotFoundError);
        }

        return ServiceResult.Success();
    }

    private static void Apply(AttendeeFormModel model, Attendee attendee)
    {
        attendee.FullName = model.FullName.Trim();
        attendee.IsPresentAtChristmasEve = model.IsPresentAtChristmasEve;
        attendee.ParticipatesInGiftExchange = model.ParticipatesInGiftExchange;
    }
}
