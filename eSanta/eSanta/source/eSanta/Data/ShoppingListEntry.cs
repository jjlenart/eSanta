namespace eSanta.Data;

/// <summary>
/// A buyer's private note about a gift for one recipient. The recipient is either a registered user,
/// a manually added attendee, or (when both are null) a person from outside the attendance list.
/// </summary>
public class ShoppingListEntry
{
    public const int RecipientNameMaxLength = 100;
    public const int GiftNameMaxLength = 200;
    public const int NoteMaxLength = 1000;

    public int Id { get; set; }

    public string OwnerId { get; set; } = string.Empty;

    public ApplicationUser? Owner { get; set; }

    public string? RecipientUserId { get; set; }

    public ApplicationUser? RecipientUser { get; set; }

    public int? AttendeeId { get; set; }

    public Attendee? Attendee { get; set; }

    /// <summary>
    /// Snapshot of the recipient's name; it is what remains visible if the recipient is later removed.
    /// </summary>
    public string RecipientName { get; set; } = string.Empty;

    public string? GiftName { get; set; }

    public bool IsBought { get; set; }

    public string? Note { get; set; }
}
