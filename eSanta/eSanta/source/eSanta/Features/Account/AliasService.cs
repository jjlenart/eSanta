using eSanta.Common;
using eSanta.Data;
using Microsoft.AspNetCore.Identity;

namespace eSanta.Features.Account;

public sealed class AliasService(
    UserManager<ApplicationUser> userManager,
    IModelValidator modelValidator) : IAliasService
{
    public async Task<ServiceResult> ChangeAliasAsync(ApplicationUser user, AliasModel model)
    {
        var validationErrors = modelValidator.Validate(model);
        if (validationErrors.Count > 0)
        {
            return ServiceResult.Failure(validationErrors);
        }

        user.DisplayName = model.Alias.Trim();
        var updateResult = await userManager.UpdateAsync(user);

        return updateResult.Succeeded
            ? ServiceResult.Success()
            : ServiceResult.Failure(updateResult.Errors.Select(error => error.Description));
    }
}
