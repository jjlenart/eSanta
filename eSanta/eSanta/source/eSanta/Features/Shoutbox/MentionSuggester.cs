namespace eSanta.Features.Shoutbox;

public sealed class MentionSuggester : IMentionSuggester
{
    public const int MaxSuggestions = 5;

    public IReadOnlyList<string> Suggest(string? text, IReadOnlyList<string> aliases)
    {
        if (FindMentionStart(text) is not { } start)
        {
            return [];
        }

        var query = text![(start + 1)..];

        return aliases
            .Where(alias => alias.StartsWith(query, StringComparison.CurrentCultureIgnoreCase)
                && !alias.Equals(query, StringComparison.CurrentCultureIgnoreCase))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .Order(StringComparer.CurrentCultureIgnoreCase)
            .Take(MaxSuggestions)
            .ToList();
    }

    public string Complete(string? text, string alias)
    {
        text ??= string.Empty;
        var prefix = FindMentionStart(text) is { } start ? text[..start] : text;

        return $"{prefix}@{alias} ";
    }

    /// <summary>The mention being typed starts at the last "@" that opens a word.</summary>
    private static int? FindMentionStart(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        var start = text.LastIndexOf('@');
        if (start < 0 || (start > 0 && char.IsLetterOrDigit(text[start - 1])))
        {
            return null;
        }

        return start;
    }
}
