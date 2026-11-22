using eSanta.Common;
using eSanta.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace eSanta.Features.Administration;

public sealed class UserAdministrationService(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    UserManager<ApplicationUser> userManager,
    IModelValidator modelValidator) : IUserAdministrationService
{
    public async Task<bool> IsAdminAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Users.AnyAsync(user => user.Id == userId && user.IsAdmin, cancellationToken);
    }

    public async Task<IReadOnlyList<ManagedUserDto>?> GetUsersAsync(string adminId, CancellationToken cancellationToken = default)
    {
        if (!await IsAdminAsync(adminId, cancellationToken))
        {
            return null;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var users = await dbContext.Users
            .AsNoTracking()
            .Select(user => new
            {
                user.Id,
                user.UserName,
                user.DisplayName,
                FamilyName = user.Family != null ? user.Family.Name : null,
                user.IsAdmin
            })
            .ToListAsync(cancellationToken);

        return users
            .Select(user => new ManagedUserDto(
                user.Id,
                user.UserName ?? string.Empty,
                ApplicationUser.ToAlias(user.DisplayName, user.UserName),
                user.FamilyName,
                user.IsAdmin,
                user.Id == adminId))
            .OrderBy(user => user.Alias, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public async Task<ServiceResult> SetPasswordAsync(string adminId, string userId, SetPasswordModel model, CancellationToken cancellationToken = default)
    {
        if (!await IsAdminAsync(adminId, cancellationToken))
        {
            return ServiceResult.Failure("Nie masz uprawnień administratora.");
        }

        if (userId == adminId)
        {
            return ServiceResult.Failure("Swoje hasło zmienisz w ustawieniach konta.");
        }

        var validationErrors = modelValidator.Validate(model);
        if (validationErrors.Count > 0)
        {
            return ServiceResult.Failure(validationErrors);
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return ServiceResult.Failure("Nie znaleziono tego użytkownika. Mógł zostać usunięty.");
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var resetResult = await userManager.ResetPasswordAsync(user, token, model.NewPassword);

        return resetResult.Succeeded
            ? ServiceResult.Success()
            : ServiceResult.Failure(resetResult.Errors.Select(error => error.Description));
    }
}
