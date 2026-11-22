namespace eSanta.Features.Administration;

public sealed record ManagedUserDto(
    string Id,
    string UserName,
    string Alias,
    string? FamilyName,
    bool IsAdmin,
    bool IsCurrentUser);
