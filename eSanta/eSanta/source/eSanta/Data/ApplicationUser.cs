using Microsoft.AspNetCore.Identity;

namespace eSanta.Data;

public class ApplicationUser : IdentityUser
{
    public const int DisplayNameMaxLength = 50;

    public string? DisplayName { get; set; }

    public int? FamilyId { get; set; }

    public Family? Family { get; set; }

    public bool IsAdmin { get; set; }

    public string Alias => ToAlias(DisplayName, UserName);

    public static string ToAlias(string? displayName, string? userName, string fallback = "") =>
        !string.IsNullOrWhiteSpace(displayName) ? displayName
        : !string.IsNullOrWhiteSpace(userName) ? userName
        : fallback;
}
