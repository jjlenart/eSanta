namespace eSanta.Data;

public class Attendee
{
    public const int FullNameMaxLength = 100;

    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public bool IsPresentAtChristmasEve { get; set; }

    public bool ParticipatesInGiftExchange { get; set; }

    /// <summary>
    /// Set when the entry belongs to a registered user, who is the only one allowed to change it.
    /// Entries without a user are added manually and can be edited by anyone.
    /// </summary>
    public string? UserId { get; set; }

    public ApplicationUser? User { get; set; }
}
