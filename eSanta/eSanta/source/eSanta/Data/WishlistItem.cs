namespace eSanta.Data;

public class WishlistItem
{
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 2000;
    public const int LinkMaxLength = 2000;

    public int Id { get; set; }

    public string OwnerId { get; set; } = string.Empty;

    public ApplicationUser? Owner { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Link { get; set; }

    public string? ReservedById { get; set; }

    public ApplicationUser? ReservedBy { get; set; }
}
