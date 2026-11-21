using eSanta.Common;
using eSanta.Data;

namespace eSanta.Features.Account;

public interface IAliasService
{
    Task<ServiceResult> ChangeAliasAsync(ApplicationUser user, AliasModel model);
}
