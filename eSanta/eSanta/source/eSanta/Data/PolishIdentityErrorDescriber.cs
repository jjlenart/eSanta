using Microsoft.AspNetCore.Identity;

namespace eSanta.Data;

public sealed class PolishIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DefaultError() =>
        Create(nameof(DefaultError), "Wystąpił nieznany błąd.");

    public override IdentityError ConcurrencyFailure() =>
        Create(nameof(ConcurrencyFailure), "Dane zostały w międzyczasie zmienione. Spróbuj ponownie.");

    public override IdentityError PasswordMismatch() =>
        Create(nameof(PasswordMismatch), "Nieprawidłowe hasło.");

    public override IdentityError InvalidToken() =>
        Create(nameof(InvalidToken), "Nieprawidłowy lub wygasły token.");

    public override IdentityError RecoveryCodeRedemptionFailed() =>
        Create(nameof(RecoveryCodeRedemptionFailed), "Nie udało się użyć kodu odzyskiwania.");

    public override IdentityError LoginAlreadyAssociated() =>
        Create(nameof(LoginAlreadyAssociated), "Użytkownik z tym loginem już istnieje.");

    public override IdentityError InvalidUserName(string? userName) =>
        Create(nameof(InvalidUserName), $"Login '{userName}' jest nieprawidłowy.");

    public override IdentityError InvalidEmail(string? email) =>
        Create(nameof(InvalidEmail), $"Adres e-mail '{email}' jest nieprawidłowy.");

    public override IdentityError DuplicateUserName(string userName) =>
        Create(nameof(DuplicateUserName), $"Login '{userName}' jest już zajęty. Dodaj do imienia np. pierwszą literę nazwiska.");

    public override IdentityError DuplicateEmail(string email) =>
        Create(nameof(DuplicateEmail), $"Adres e-mail '{email}' jest już zajęty.");

    public override IdentityError InvalidRoleName(string? role) =>
        Create(nameof(InvalidRoleName), $"Nazwa roli '{role}' jest nieprawidłowa.");

    public override IdentityError DuplicateRoleName(string role) =>
        Create(nameof(DuplicateRoleName), $"Nazwa roli '{role}' jest już zajęta.");

    public override IdentityError UserAlreadyHasPassword() =>
        Create(nameof(UserAlreadyHasPassword), "Użytkownik ma już ustawione hasło.");

    public override IdentityError UserLockoutNotEnabled() =>
        Create(nameof(UserLockoutNotEnabled), "Blokada konta nie jest włączona dla tego użytkownika.");

    public override IdentityError UserAlreadyInRole(string role) =>
        Create(nameof(UserAlreadyInRole), $"Użytkownik ma już rolę '{role}'.");

    public override IdentityError UserNotInRole(string role) =>
        Create(nameof(UserNotInRole), $"Użytkownik nie ma roli '{role}'.");

    public override IdentityError PasswordTooShort(int length) =>
        Create(nameof(PasswordTooShort), $"Hasło musi mieć co najmniej {length} znaków.");

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
        Create(nameof(PasswordRequiresUniqueChars), $"Hasło musi zawierać co najmniej {uniqueChars} różnych znaków.");

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        Create(nameof(PasswordRequiresNonAlphanumeric), "Hasło musi zawierać co najmniej jeden znak specjalny.");

    public override IdentityError PasswordRequiresDigit() =>
        Create(nameof(PasswordRequiresDigit), "Hasło musi zawierać co najmniej jedną cyfrę (0-9).");

    public override IdentityError PasswordRequiresLower() =>
        Create(nameof(PasswordRequiresLower), "Hasło musi zawierać co najmniej jedną małą literę (a-z).");

    public override IdentityError PasswordRequiresUpper() =>
        Create(nameof(PasswordRequiresUpper), "Hasło musi zawierać co najmniej jedną wielką literę (A-Z).");

    private static IdentityError Create(string code, string description) => new()
    {
        Code = code,
        Description = description
    };
}
