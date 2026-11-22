using eSanta.Common;

namespace eSanta.Components.Shared;

public interface IOperationFeedback
{
    /// <summary>Runs the operation and reports its outcome to the user. Returns true when it succeeded.</summary>
    Task<bool> ExecuteAsync(Func<Task<ServiceResult>> operation, string? successMessage = null);
}
