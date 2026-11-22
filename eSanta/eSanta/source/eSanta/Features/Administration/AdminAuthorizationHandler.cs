using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace eSanta.Features.Administration;

/// <summary>
/// Checks the flag in the database on every request, so setting or clearing it manually takes effect without signing in again.
/// </summary>
public sealed class AdminAuthorizationHandler(IUserAdministrationService userAdministrationService)
    : AuthorizationHandler<AdminRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminRequirement requirement)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is not null && await userAdministrationService.IsAdminAsync(userId))
        {
            context.Succeed(requirement);
        }
    }
}
