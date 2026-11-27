using eSanta.Common;
using MudBlazor;

namespace eSanta.Components.Shared;

public sealed class SnackbarOperationFeedback(ISnackbar snackbar, IDialogService dialogService) : IOperationFeedback
{
    public async Task<bool> ExecuteAsync(Func<Task<ServiceResult>> operation, string? successMessage = null)
    {
        ServiceResult result;

        try
        {
            result = await operation();
        }
        catch (Exception)
        {
            snackbar.Add("Nie udało się zapisać zmian. Spróbuj ponownie.", Severity.Error);
            return false;
        }

        if (result.Succeeded)
        {
            if (successMessage is not null)
            {
                snackbar.Add(successMessage, Severity.Success);
            }

            return true;
        }

        var error = result.Error ?? "Operacja nie powiodła się.";
        if (result.IsNotice)
        {
            await dialogService.ShowMessageBoxAsync(string.Empty, error, yesText: "OK");
        }
        else
        {
            snackbar.Add(error, Severity.Error);
        }

        return false;
    }
}
