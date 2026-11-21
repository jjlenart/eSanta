namespace eSanta.Data;

public class Family
{
    public const int NameMaxLength = 100;

    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string NormalizedName { get; set; } = string.Empty;

    public ICollection<ApplicationUser> Members { get; set; } = [];

    public static string Normalize(string name) => name.Trim().ToUpperInvariant();
}
