using eSanta.Common;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace eSanta.Components.Shared;

public abstract class FormDialogBase<TModel> : ComponentBase
    where TModel : class
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter, EditorRequired]
    public TModel Model { get; set; } = default!;

    [Parameter, EditorRequired]
    public Func<TModel, Task<ServiceResult>> OnSave { get; set; } = default!;

    protected string? Error { get; private set; }

    protected bool IsSaving { get; private set; }

    protected virtual string UnexpectedErrorMessage => "Nie udało się zapisać zmian. Spróbuj ponownie.";

    protected async Task SubmitAsync()
    {
        IsSaving = true;
        Error = null;

        try
        {
            var result = await OnSave(Model);
            if (result.Succeeded)
            {
                MudDialog.Close(DialogResult.Ok(true));
                return;
            }

            Error = result.Error;
        }
        catch (Exception)
        {
            Error = UnexpectedErrorMessage;
        }
        finally
        {
            IsSaving = false;
        }
    }

    protected void Cancel() => MudDialog.Cancel();
}
