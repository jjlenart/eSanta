namespace eSanta.Features.Shoutbox;

public interface IMentionParser
{
    /// <summary>Returns ids of candidates mentioned in the content as <c>@Alias</c>.</summary>
    IReadOnlySet<string> FindMentionedUserIds(string content, IReadOnlyCollection<MentionCandidate> candidates);
}
