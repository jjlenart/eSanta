namespace eSanta.Data;

public class ShoutboxMention
{
    public long MessageId { get; set; }

    public ShoutboxMessage? Message { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }
}
