namespace eSanta.Features.FamilyWishlists;

public sealed record ReservableItemDto(
    int Id,
    string Name,
    string? Description,
    string? Link,
    ReservationStatus Status);
