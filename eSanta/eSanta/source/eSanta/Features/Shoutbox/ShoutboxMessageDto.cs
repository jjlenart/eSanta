namespace eSanta.Features.Shoutbox;

/// <param name="AuthorName">Null when the message was posted anonymously.</param>
/// <param name="IsMine">True when the user who loaded the message is its author.</param>
/// <param name="MentionsMe">True when the user who loaded the message is mentioned in it.</param>
public sealed record ShoutboxMessageDto(long Id, string? AuthorName, string Content, DateTimeOffset SentAt, bool IsMine, bool MentionsMe)
{
    public bool IsAnonymous => AuthorName is null;
}
