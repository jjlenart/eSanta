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
    private const string NoFamilyError = "Twoje konto nie jest przypisane do żadnej rodziny, więc nie możesz korzystać z listy obecności.";

    public async Task<AttendanceListResult> GetAllAsync(string currentUserId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var family = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == currentUserId && user.Family != null)
            .Select(user => new { user.Family!.Id, user.Family.Name })
            .FirstOrDefaultAsync(cancellationToken);

        if (family is null)
        {
            return AttendanceListResult.NoFamily();
        }

        var attendees = await dbContext.Attendees
            .AsNoTracking()
            .Where(Attendee.BelongsToFamily(family.Id))
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

        var attendeeDtos = attendees
            .Select(attendee => new AttendeeDto(
                attendee.Id,
                attendee.UserId is null ? attendee.FullName : ApplicationUser.ToAlias(attendee.DisplayName, attendee.UserName, attendee.FullName),
                attendee.IsPresentAtChristmasEve,
                attendee.ParticipatesInGiftExchange,
                IsRegisteredUser: attendee.UserId is not null,
                IsCurrentUser: attendee.UserId == currentUserId))
            .OrderBy(attendee => attendee.FullName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        return new AttendanceListResult(family.Name, attendeeDtos);
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
            .Select(user => new { user.DisplayName, user.UserName, user.FamilyId })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return ServiceResult.Failure("Nie znaleziono Twojego konta. Zaloguj się ponownie.");
        }

        if (user.FamilyId is null)
        {
            return ServiceResult.Failure(NoFamilyError);
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

    public async Task<ServiceResult> AddAsync(string currentUserId, AttendeeFormModel model, CancellationToken cancellationToken = default)
    {
        var validationErrors = modelValidator.Validate(model);
        if (validationErrors.Count > 0)
        {
            return ServiceResult.Failure(validationErrors);
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var familyId = await GetFamilyIdAsync(dbContext, currentUserId, cancellationToken);
        if (familyId is null)
        {
            return ServiceResult.Failure(NoFamilyError);
        }

        var attendee = new Attendee { FamilyId = familyId };
        Apply(model, attendee);
        dbContext.Attendees.Add(attendee);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> UpdateAsync(string currentUserId, int id, AttendeeFormModel model, CancellationToken cancellationToken = default)
    {
        var validationErrors = modelValidator.Validate(model);
        if (validationErrors.Count > 0)
        {
            return ServiceResult.Failure(validationErrors);
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var (attendee, error) = await FindEditableAsync(dbContext, currentUserId, id, cancellationToken);
        if (attendee is null)
        {
            return ServiceResult.Failure(error!);
        }

        Apply(model, attendee);

        return await SaveAsync(dbContext, cancellationToken);
    }

    public async Task<ServiceResult> DeleteAsync(string currentUserId, int id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var (attendee, error) = await FindEditableAsync(dbContext, currentUserId, id, cancellationToken);
        if (attendee is null)
        {
            return ServiceResult.Failure(error!);
        }

        dbContext.Attendees.Remove(attendee);

        return await SaveAsync(dbContext, cancellationToken);
    }

    private static async Task<(Attendee? Attendee, string? Error)> FindEditableAsync(ApplicationDbContext dbContext, string currentUserId, int id, CancellationToken cancellationToken)
    {
        var familyId = await GetFamilyIdAsync(dbContext, currentUserId, cancellationToken);
        if (familyId is null)
        {
            return (null, NoFamilyError);
        }

        var attendee = await dbContext.Attendees
            .Where(attendee => attendee.Id == id)
            .Where(Attendee.BelongsToFamily(familyId.Value))
            .FirstOrDefaultAsync(cancellationToken);

        return attendee switch
        {
            null => (null, NotFoundError),
            { UserId: not null } => (null, RegisteredUserError),
            _ => (attendee, null)
        };
    }

    private static Task<int?> GetFamilyIdAsync(ApplicationDbContext dbContext, string userId, CancellationToken cancellationToken) =>
        dbContext.Users
            .Where(user => user.Id == userId)
            .Select(user => user.FamilyId)
            .FirstOrDefaultAsync(cancellationToken);

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
