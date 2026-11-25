namespace eSanta.Features.Shoutbox;

/// <param name="MentionableAliases">Aliases of the other family members, used for mention suggestions.</param>
public sealed record ShoutboxFeed(int? FamilyId, ShoutboxPage Page, IReadOnlyList<string> MentionableAliases)
{
    public bool HasFamily => FamilyId is not null;

    public static ShoutboxFeed NoFamily() => new(null, ShoutboxPage.Empty, []);
}
