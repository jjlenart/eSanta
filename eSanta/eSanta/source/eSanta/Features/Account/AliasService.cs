using System.ComponentModel.DataAnnotations;
using eSanta.Common;
using eSanta.Data;
using Microsoft.AspNetCore.Identity;

namespace eSanta.Features.Account;

public sealed class AliasService(UserManager<ApplicationUser> userManager) : IAliasService
{
    public async Task<ServiceResult> ChangeAliasAsync(ApplicationUser user, AliasModel model)
    {
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true))
        {
            return ServiceResult.Failure(string.Join(" ", results.Select(result => result.ErrorMessage)));
        }

        user.DisplayName = model.Alias.Trim();
        var updateResult = await userManager.UpdateAsync(user);

        return updateResult.Succeeded
            ? ServiceResult.Success()
            : ServiceResult.Failure(string.Join(" ", updateResult.Errors.Select(error => error.Description)));
    }
}
