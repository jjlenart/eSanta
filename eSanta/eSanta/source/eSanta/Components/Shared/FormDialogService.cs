using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace eSanta.Components.Shared;

public sealed class FormDialogService(IDialogService dialogService, ISnackbar snackbar) : IFormDialogService
{
    public async Task<bool> ShowAsync<TDialog>(string title, DialogParameters<TDialog> parameters, string successMessage, MaxWidth maxWidth = MaxWidth.Small)
        where TDialog : ComponentBase
    {
        var options = new DialogOptions
        {
            MaxWidth = maxWidth,
            FullWidth = true,
            CloseOnEscapeKey = true
        };

        var dialog = await dialogService.ShowAsync<TDialog>(title, parameters, options);
        var result = await dialog.Result;

        if (result is not { Canceled: false })
        {
            return false;
        }

        snackbar.Add(successMessage, Severity.Success);

        return true;
    }

    public async Task<bool> ConfirmDeleteAsync(string title, string message) =>
        await dialogService.ShowMessageBoxAsync(title, message, yesText: "Usuń", cancelText: "Anuluj") == true;
}
