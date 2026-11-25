using System.Linq.Expressions;

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
    /// Entries without a user are added manually and can be edited by anyone from the same family.
    /// </summary>
    public string? UserId { get; set; }

    public ApplicationUser? User { get; set; }

    /// <summary>
    /// Family of a manually added entry. A registered user's entry always follows the user's own family.
    /// </summary>
    public int? FamilyId { get; set; }

    public Family? Family { get; set; }

    public static Expression<Func<Attendee, bool>> BelongsToFamily(int familyId) =>
        attendee => attendee.UserId != null
            ? attendee.User!.FamilyId == familyId
            : attendee.FamilyId == familyId;
}
