namespace eSanta.Features.FamilyWishlists;

public sealed record FamilyMembersResult(string? FamilyName, IReadOnlyList<FamilyMemberDto> Members)
{
    public bool HasFamily => FamilyName is not null;

    public static FamilyMembersResult NoFamily() => new(null, []);
}
