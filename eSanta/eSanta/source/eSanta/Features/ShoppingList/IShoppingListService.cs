using eSanta.Common;

namespace eSanta.Features.ShoppingList;

public interface IShoppingListService
{
    Task<IReadOnlyList<ShoppingListRowDto>> GetAsync(string ownerId, CancellationToken cancellationToken = default);

    Task<ServiceResult> SaveAsync(string ownerId, ShoppingRecipient recipient, ShoppingEntryFormModel model, CancellationToken cancellationToken = default);

    Task<ServiceResult> AddCustomAsync(string ownerId, ShoppingEntryFormModel model, CancellationToken cancellationToken = default);

    Task<ServiceResult> DeleteCustomAsync(string ownerId, int entryId, CancellationToken cancellationToken = default);
}
