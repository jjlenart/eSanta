namespace eSanta.Features.Shoutbox;

public interface IMentionSuggester
{
    /// <summary>Aliases matching the mention being typed at the end of the text, or empty when no mention is being typed.</summary>
    IReadOnlyList<string> Suggest(string? text, IReadOnlyList<string> aliases);

    /// <summary>Replaces the mention being typed at the end of the text with the chosen alias.</summary>
    string Complete(string? text, string alias);
}
