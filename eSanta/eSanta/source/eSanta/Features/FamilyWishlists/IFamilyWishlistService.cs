using eSanta.Common;

namespace eSanta.Features.FamilyWishlists;

public interface IFamilyWishlistService
{
    Task<FamilyMembersResult> GetFamilyMembersAsync(string currentUserId, CancellationToken cancellationToken = default);

    /// <returns>The member's wishlist, or <c>null</c> when the member is not in the current user's family.</returns>
    Task<MemberWishlistDto?> GetMemberWishlistAsync(string currentUserId, string memberId, CancellationToken cancellationToken = default);

    Task<ServiceResult> ReserveAsync(string currentUserId, int itemId, CancellationToken cancellationToken = default);

    Task<ServiceResult> CancelReservationAsync(string currentUserId, int itemId, CancellationToken cancellationToken = default);
}
