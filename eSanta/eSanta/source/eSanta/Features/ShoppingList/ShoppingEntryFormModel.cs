using System.ComponentModel.DataAnnotations;
using eSanta.Data;

namespace eSanta.Features.ShoppingList;

public sealed class ShoppingEntryFormModel
{
    /// <summary>Used only for people from outside the attendance list.</summary>
    [StringLength(ShoppingListEntry.RecipientNameMaxLength, ErrorMessage = "Imię może mieć maksymalnie {1} znaków.")]
    public string? RecipientName { get; set; }

    [StringLength(ShoppingListEntry.GiftNameMaxLength, ErrorMessage = "Nazwa prezentu może mieć maksymalnie {1} znaków.")]
    public string? GiftName { get; set; }

    public bool IsBought { get; set; }

    [StringLength(ShoppingListEntry.NoteMaxLength, ErrorMessage = "Notatka może mieć maksymalnie {1} znaków.")]
    public string? Note { get; set; }

    public static ShoppingEntryFormModel FromRow(ShoppingListRowDto row) => new()
    {
        RecipientName = row.Recipient.IsCustom ? row.RecipientName : null,
        GiftName = row.ManualGiftName,
        IsBought = row.IsBought,
        Note = row.Note
    };
}
