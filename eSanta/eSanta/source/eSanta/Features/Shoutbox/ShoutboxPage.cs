namespace eSanta.Features.Shoutbox;

/// <param name="Messages">Ordered from the oldest to the newest.</param>
/// <param name="HasOlder">True when there are older messages that were not loaded yet.</param>
public sealed record ShoutboxPage(IReadOnlyList<ShoutboxMessageDto> Messages, bool HasOlder)
{
    public static ShoutboxPage Empty { get; } = new([], false);
}
