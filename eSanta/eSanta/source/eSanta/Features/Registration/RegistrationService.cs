using System.ComponentModel.DataAnnotations;
using eSanta.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace eSanta.Features.Registration;

public sealed class RegistrationService(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext) : IRegistrationService
{
    public async Task<RegistrationResult> RegisterAsync(RegistrationModel model, CancellationToken cancellationToken = default)
    {
        var validationErrors = Validate(model);
        if (validationErrors.Count > 0)
        {
            return RegistrationResult.Failure(validationErrors);
        }

        var firstName = model.FirstName.Trim();
        var user = new ApplicationUser
        {
            UserName = firstName,
            DisplayName = firstName,
            Family = await FindOrCreateFamilyAsync(model.FamilyName, cancellationToken)
        };

        // The user and a newly created family are saved together, so a failed
        // registration never leaves an empty family behind.
        var result = await userManager.CreateAsync(user, model.Password);

        return result.Succeeded
            ? RegistrationResult.Success(user)
            : RegistrationResult.Failure(result.Errors.Select(error => error.Description));
    }

    private async Task<Family> FindOrCreateFamilyAsync(string familyName, CancellationToken cancellationToken)
    {
        var normalizedName = Family.Normalize(familyName);

        var existingFamily = await dbContext.Families
            .FirstOrDefaultAsync(family => family.NormalizedName == normalizedName, cancellationToken);

        return existingFamily ?? new Family
        {
            Name = familyName.Trim(),
            NormalizedName = normalizedName
        };
    }

    private static List<string> Validate(RegistrationModel model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);

        return results
            .Select(result => result.ErrorMessage)
            .OfType<string>()
            .ToList();
    }
}
