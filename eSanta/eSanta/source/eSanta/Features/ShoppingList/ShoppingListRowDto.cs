namespace eSanta.Features.ShoppingList;

public sealed record ShoppingListRowDto(
    ShoppingRecipient Recipient,
    string RecipientName,
    string? ReservedGiftNames,
    string? ManualGiftName,
    bool IsBought,
    string? Note)
{
    public bool IsReserved => ReservedGiftNames is not null;

    public string? GiftName => ReservedGiftNames ?? ManualGiftName;
}
