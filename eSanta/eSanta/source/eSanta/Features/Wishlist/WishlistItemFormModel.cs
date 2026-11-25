using System.ComponentModel.DataAnnotations;
using eSanta.Common;
using eSanta.Data;

namespace eSanta.Features.Wishlist;

public sealed class WishlistItemFormModel
{
    [Required(ErrorMessage = "Podaj nazwę prezentu.")]
    [StringLength(WishlistItem.NameMaxLength, ErrorMessage = "Nazwa może mieć maksymalnie {1} znaków.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(WishlistItem.DescriptionMaxLength, ErrorMessage = "Opis może mieć maksymalnie {1} znaków.")]
    public string? Description { get; set; }

    [StringLength(WishlistItem.LinkMaxLength, ErrorMessage = "Link może mieć maksymalnie {1} znaków.")]
    [HttpUrl(ErrorMessage = "Podaj poprawny adres zaczynający się od http:// lub https://.")]
    public string? Link { get; set; }

    public static WishlistItemFormModel FromDto(WishlistItemDto item) => new()
    {
        Name = item.Name,
        Description = item.Description,
        Link = item.Link
    };
}
