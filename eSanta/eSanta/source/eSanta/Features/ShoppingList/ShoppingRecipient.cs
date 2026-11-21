namespace eSanta.Features.ShoppingList;

/// <summary>
/// Identifies who a shopping list row is for. Exactly one of the values is set.
/// </summary>
public sealed record ShoppingRecipient(string? UserId, int? AttendeeId, int? CustomEntryId)
{
    public bool IsCustom => CustomEntryId is not null;

    public static ShoppingRecipient ForUser(string userId) => new(userId, null, null);

    public static ShoppingRecipient ForAttendee(int attendeeId) => new(null, attendeeId, null);

    public static ShoppingRecipient ForCustomEntry(int entryId) => new(null, null, entryId);
}
