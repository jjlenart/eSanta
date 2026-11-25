namespace eSanta.Data;

public class ShoutboxMessage
{
    public const int ContentMaxLength = 500;

    public long Id { get; set; }

    public int FamilyId { get; set; }

    public Family? Family { get; set; }

    public string AuthorId { get; set; } = string.Empty;

    public ApplicationUser? Author { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTimeOffset SentAt { get; set; }

    /// <summary>The author is still stored, but never shown to other family members.</summary>
    public bool IsAnonymous { get; set; }

    public ICollection<ShoutboxMention> Mentions { get; set; } = [];
}
