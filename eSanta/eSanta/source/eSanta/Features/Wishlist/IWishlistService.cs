using eSanta.Common;

namespace eSanta.Features.Wishlist;

public interface IWishlistService
{
    Task<IReadOnlyList<WishlistItemDto>> GetItemsAsync(string ownerId, CancellationToken cancellationToken = default);

    Task<ServiceResult> AddAsync(string ownerId, WishlistItemFormModel model, CancellationToken cancellationToken = default);

    Task<ServiceResult> UpdateAsync(string ownerId, int itemId, WishlistItemFormModel model, CancellationToken cancellationToken = default);

    Task<ServiceResult> DeleteAsync(string ownerId, int itemId, CancellationToken cancellationToken = default);
}
