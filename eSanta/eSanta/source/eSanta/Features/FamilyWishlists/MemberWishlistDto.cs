namespace eSanta.Features.FamilyWishlists;

public sealed record MemberWishlistDto(string MemberId, string Alias, IReadOnlyList<ReservableItemDto> Items);
