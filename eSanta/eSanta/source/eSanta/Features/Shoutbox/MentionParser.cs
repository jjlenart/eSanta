namespace eSanta.Features.Shoutbox;

public sealed class MentionParser : IMentionParser
{
    public IReadOnlySet<string> FindMentionedUserIds(string content, IReadOnlyCollection<MentionCandidate> candidates)
    {
        var mentioned = new HashSet<string>();
        var usableCandidates = candidates.Where(candidate => !string.IsNullOrWhiteSpace(candidate.Alias)).ToList();

        for (var index = content.IndexOf('@'); index >= 0; index = content.IndexOf('@', index + 1))
        {
            if (index > 0 && char.IsLetterOrDigit(content[index - 1]))
            {
                continue;
            }

            var start = index + 1;
            var matches = usableCandidates
                .Where(candidate => IsMentionedAt(content.AsSpan(start), candidate.Alias))
                .ToList();

            if (matches.Count == 0)
            {
                continue;
            }

            // Aliases may contain spaces, so "@Wujek Janek" must win over "@Wujek".
            var longest = matches.Max(candidate => candidate.Alias.Length);
            mentioned.UnionWith(matches.Where(candidate => candidate.Alias.Length == longest).Select(candidate => candidate.UserId));
        }

        return mentioned;
    }

    private static bool IsMentionedAt(ReadOnlySpan<char> text, string alias) =>
        text.StartsWith(alias, StringComparison.CurrentCultureIgnoreCase)
        && (text.Length == alias.Length || !char.IsLetterOrDigit(text[alias.Length]));
}
