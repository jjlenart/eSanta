using Microsoft.AspNetCore.Authorization;

namespace eSanta.Features.Administration;

public sealed class AdminRequirement : IAuthorizationRequirement
{
    public const string PolicyName = "Admin";
}
