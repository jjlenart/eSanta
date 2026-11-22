using eSanta.Common;
using MudBlazor;

namespace eSanta.Components.Shared;

public sealed class SnackbarOperationFeedback(ISnackbar snackbar) : IOperationFeedback
{
    public async Task<bool> ExecuteAsync(Func<Task<ServiceResult>> operation, string? successMessage = null)
    {
        try
        {
            var result = await operation();
            if (result.Succeeded)
            {
                if (successMessage is not null)
                {
                    snackbar.Add(successMessage, Severity.Success);
                }

                return true;
            }

            snackbar.Add(result.Error ?? "Operacja nie powiodła się.", Severity.Error);
        }
        catch (Exception)
        {
            snackbar.Add("Nie udało się zapisać zmian. Spróbuj ponownie.", Severity.Error);
        }

        return false;
    }
}
