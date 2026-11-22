using eSanta.Common;
using eSanta.Data;
using Microsoft.EntityFrameworkCore;

namespace eSanta.Features.ShoppingList;

public sealed class ShoppingListService(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IModelValidator modelValidator) : IShoppingListService
{
    private const string RecipientNotFoundError = "Nie znaleziono tej osoby. Mogła zostać usunięta z listy obecności.";
    private const string RecipientNameRequiredError = "Podaj imię osoby, dla której kupujesz prezent.";

    public async Task<IReadOnlyList<ShoppingListRowDto>> GetAsync(string ownerId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var attendees = await dbContext.Attendees
            .AsNoTracking()
            .Where(attendee => attendee.UserId != ownerId)
            .Select(attendee => new
            {
                attendee.Id,
                attendee.FullName,
                attendee.UserId,
                DisplayName = attendee.User != null ? attendee.User.DisplayName : null,
                UserName = attendee.User != null ? attendee.User.UserName : null
            })
            .ToListAsync(cancellationToken);

        var reservations = await dbContext.WishlistItems
            .AsNoTracking()
            .Where(item => item.ReservedById == ownerId && item.OwnerId != ownerId)
            .OrderBy(item => item.Id)
            .Select(item => new { item.OwnerId, item.Name, item.Owner!.DisplayName, item.Owner.UserName })
            .ToListAsync(cancellationToken);

        var entries = await dbContext.ShoppingListEntries
            .AsNoTracking()
            .Where(entry => entry.OwnerId == ownerId)
            .ToListAsync(cancellationToken);

        var ownerFamilyId = await dbContext.Users
            .Where(user => user.Id == ownerId)
            .Select(user => user.FamilyId)
            .FirstOrDefaultAsync(cancellationToken);

        HashSet<string> familyMemberIds = ownerFamilyId is null
            ? []
            : (await dbContext.Users
                .Where(user => user.FamilyId == ownerFamilyId && user.Id != ownerId)
                .Select(user => user.Id)
                .ToListAsync(cancellationToken))
                .ToHashSet();

        var registeredRecipients = attendees
            .Where(attendee => attendee.UserId is not null)
            .Select(attendee => (UserId: attendee.UserId!, Name: ApplicationUser.ToAlias(attendee.DisplayName, attendee.UserName, attendee.FullName)))
            .Concat(reservations.Select(reservation => (UserId: reservation.OwnerId, Name: ApplicationUser.ToAlias(reservation.DisplayName, reservation.UserName, "?"))))
            .DistinctBy(recipient => recipient.UserId);

        var rows = new List<ShoppingListRowDto>();

        foreach (var recipient in registeredRecipients)
        {
            var entry = entries.FirstOrDefault(candidate => candidate.RecipientUserId == recipient.UserId);
            var reservedNames = reservations
                .Where(reservation => reservation.OwnerId == recipient.UserId)
                .Select(reservation => reservation.Name)
                .ToList();

            rows.Add(CreateRow(ShoppingRecipient.ForUser(recipient.UserId), recipient.Name, entry, reservedNames)
                with { HasLetter = familyMemberIds.Contains(recipient.UserId) });
        }

        foreach (var attendee in attendees.Where(attendee => attendee.UserId is null))
        {
            var entry = entries.FirstOrDefault(candidate => candidate.AttendeeId == attendee.Id);
            rows.Add(CreateRow(ShoppingRecipient.ForAttendee(attendee.Id), attendee.FullName, entry, []));
        }

        var listedRows = rows.OrderBy(row => row.RecipientName, StringComparer.CurrentCultureIgnoreCase);

        var customRows = entries
            .Where(entry => entry.RecipientUserId is null && entry.AttendeeId is null)
            .Select(entry => CreateRow(ShoppingRecipient.ForCustomEntry(entry.Id), entry.RecipientName, entry, []))
            .OrderBy(row => row.RecipientName, StringComparer.CurrentCultureIgnoreCase);

        return listedRows.Concat(customRows).ToList();
    }

    public async Task<ServiceResult> SaveAsync(string ownerId, ShoppingRecipient recipient, ShoppingEntryFormModel model, CancellationToken cancellationToken = default)
    {
        var validationErrors = modelValidator.Validate(model);
        if (validationErrors.Count > 0)
        {
            return ServiceResult.Failure(validationErrors);
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        ShoppingListEntry? entry;
        if (recipient.CustomEntryId is { } entryId)
        {
            if (string.IsNullOrWhiteSpace(model.RecipientName))
            {
                return ServiceResult.Failure(RecipientNameRequiredError);
            }

            entry = await FindCustomEntryAsync(dbContext, ownerId, entryId, cancellationToken);
            if (entry is null)
            {
                return ServiceResult.Failure("Nie znaleziono tego wpisu. Mógł zostać już usunięty.");
            }

            entry.RecipientName = model.RecipientName.Trim();
        }
        else if (recipient.UserId is { } recipientUserId)
        {
            var recipientName = await FindRegisteredRecipientNameAsync(dbContext, ownerId, recipientUserId, cancellationToken);
            if (recipientName is null)
            {
                return ServiceResult.Failure(RecipientNotFoundError);
            }

            entry = await dbContext.ShoppingListEntries
                .FirstOrDefaultAsync(candidate => candidate.OwnerId == ownerId && candidate.RecipientUserId == recipientUserId, cancellationToken);
            entry ??= AddEntry(dbContext, new ShoppingListEntry { OwnerId = ownerId, RecipientUserId = recipientUserId });
            entry.RecipientName = recipientName;
        }
        else if (recipient.AttendeeId is { } attendeeId)
        {
            var attendeeName = await dbContext.Attendees
                .AsNoTracking()
                .Where(attendee => attendee.Id == attendeeId && attendee.UserId == null)
                .Select(attendee => attendee.FullName)
                .FirstOrDefaultAsync(cancellationToken);
            if (attendeeName is null)
            {
                return ServiceResult.Failure(RecipientNotFoundError);
            }

            entry = await dbContext.ShoppingListEntries
                .FirstOrDefaultAsync(candidate => candidate.OwnerId == ownerId && candidate.AttendeeId == attendeeId, cancellationToken);
            entry ??= AddEntry(dbContext, new ShoppingListEntry { OwnerId = ownerId, AttendeeId = attendeeId });
            entry.RecipientName = attendeeName;
        }
        else
        {
            return ServiceResult.Failure(RecipientNotFoundError);
        }

        Apply(model, entry);

        return await SaveChangesAsync(dbContext, cancellationToken);
    }

    public async Task<ServiceResult> AddCustomAsync(string ownerId, ShoppingEntryFormModel model, CancellationToken cancellationToken = default)
    {
        var validationErrors = modelValidator.Validate(model);
        if (validationErrors.Count > 0)
        {
            return ServiceResult.Failure(validationErrors);
        }

        if (string.IsNullOrWhiteSpace(model.RecipientName))
        {
            return ServiceResult.Failure(RecipientNameRequiredError);
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var entry = AddEntry(dbContext, new ShoppingListEntry { OwnerId = ownerId, RecipientName = model.RecipientName.Trim() });
        Apply(model, entry);

        return await SaveChangesAsync(dbContext, cancellationToken);
    }

    public async Task<ServiceResult> DeleteCustomAsync(string ownerId, int entryId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var entry = await FindCustomEntryAsync(dbContext, ownerId, entryId, cancellationToken);
        if (entry is null)
        {
            return ServiceResult.Failure("Można usuwać tylko własne wpisy dla osób spoza listy.");
        }

        dbContext.ShoppingListEntries.Remove(entry);

        return await SaveChangesAsync(dbContext, cancellationToken);
    }

    /// <summary>
    /// A registered user can be a recipient when they are on the attendance list or when the owner reserved one of their gifts.
    /// </summary>
    private static async Task<string?> FindRegisteredRecipientNameAsync(ApplicationDbContext dbContext, string ownerId, string recipientUserId, CancellationToken cancellationToken)
    {
        if (recipientUserId == ownerId)
        {
            return null;
        }

        var isListed = await dbContext.Attendees.AnyAsync(attendee => attendee.UserId == recipientUserId, cancellationToken)
            || await dbContext.WishlistItems.AnyAsync(item => item.OwnerId == recipientUserId && item.ReservedById == ownerId, cancellationToken);
        if (!isListed)
        {
            return null;
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == recipientUserId)
            .Select(user => new { user.DisplayName, user.UserName })
            .FirstOrDefaultAsync(cancellationToken);

        return user is null ? null : Truncate(ApplicationUser.ToAlias(user.DisplayName, user.UserName, "?"), ShoppingListEntry.RecipientNameMaxLength);
    }

    private static Task<ShoppingListEntry?> FindCustomEntryAsync(ApplicationDbContext dbContext, string ownerId, int entryId, CancellationToken cancellationToken) =>
        dbContext.ShoppingListEntries.FirstOrDefaultAsync(
            entry => entry.Id == entryId && entry.OwnerId == ownerId && entry.RecipientUserId == null && entry.AttendeeId == null,
            cancellationToken);

    private static ShoppingListEntry AddEntry(ApplicationDbContext dbContext, ShoppingListEntry entry)
    {
        dbContext.ShoppingListEntries.Add(entry);
        return entry;
    }

    private static ShoppingListRowDto CreateRow(ShoppingRecipient recipient, string recipientName, ShoppingListEntry? entry, IReadOnlyList<string> reservedNames) =>
        new(
            recipient,
            recipientName,
            reservedNames.Count > 0 ? string.Join(", ", reservedNames) : null,
            entry?.GiftName,
            entry?.IsBought ?? false,
            entry?.Note);

    private static async Task<ServiceResult> SaveChangesAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return ServiceResult.Failure("Nie udało się zapisać zmian. Odśwież stronę i spróbuj ponownie.");
        }

        return ServiceResult.Success();
    }

    private static void Apply(ShoppingEntryFormModel model, ShoppingListEntry entry)
    {
        entry.GiftName = string.IsNullOrWhiteSpace(model.GiftName) ? null : model.GiftName.Trim();
        entry.IsBought = model.IsBought;
        entry.Note = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note.Trim();
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length > maxLength ? value[..maxLength] : value;
}
