using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace eSanta.Components.Shared;

public interface IFormDialogService
{
    /// <summary>Shows a form dialog, announces the success message once it is saved and returns true in that case.</summary>
    Task<bool> ShowAsync<TDialog>(string title, DialogParameters<TDialog> parameters, string successMessage, MaxWidth maxWidth = MaxWidth.Small)
        where TDialog : ComponentBase;

    Task<bool> ConfirmDeleteAsync(string title, string message);
}
