using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace eSanta.Components.Shared;

public sealed class CurrentUser(AuthenticationStateProvider authenticationStateProvider) : ICurrentUser
{
    public async Task<string?> GetIdAsync()
    {
        var authenticationState = await authenticationStateProvider.GetAuthenticationStateAsync();

        return authenticationState.User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
