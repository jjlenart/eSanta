namespace eSanta.Common;

public sealed record ServiceResult(bool Succeeded, string? Error)
{
    /// <summary>The failure should be shown to the user prominently (in the middle of the screen).</summary>
    public bool IsNotice { get; init; }

    public static ServiceResult Success() => new(true, null);

    public static ServiceResult Failure(string error) => new(false, error);

    public static ServiceResult Failure(IEnumerable<string> errors) => new(false, string.Join(" ", errors));

    public static ServiceResult Notice(string message) => new(false, message) { IsNotice = true };
}
