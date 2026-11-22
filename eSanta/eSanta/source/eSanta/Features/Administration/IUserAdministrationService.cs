using eSanta.Common;

namespace eSanta.Features.Administration;

public interface IUserAdministrationService
{
    Task<bool> IsAdminAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Returns null when the caller is not an administrator.</summary>
    Task<IReadOnlyList<ManagedUserDto>?> GetUsersAsync(string adminId, CancellationToken cancellationToken = default);

    Task<ServiceResult> SetPasswordAsync(string adminId, string userId, SetPasswordModel model, CancellationToken cancellationToken = default);
}
