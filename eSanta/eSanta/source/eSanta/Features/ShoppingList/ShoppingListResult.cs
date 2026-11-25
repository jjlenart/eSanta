namespace eSanta.Features.ShoppingList;

public sealed record ShoppingListResult(string? FamilyName, IReadOnlyList<ShoppingListRowDto> Rows)
{
    public bool HasFamily => FamilyName is not null;

    public static ShoppingListResult NoFamily() => new(null, []);
}
