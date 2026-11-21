using eSanta.Data;

namespace eSanta.Features.Registration;

public sealed record RegistrationResult(ApplicationUser? User, IReadOnlyList<string> Errors)
{
    public bool Succeeded => User is not null;

    public static RegistrationResult Success(ApplicationUser user) => new(user, []);

    public static RegistrationResult Failure(IEnumerable<string> errors) => new(null, errors.ToList());
}
