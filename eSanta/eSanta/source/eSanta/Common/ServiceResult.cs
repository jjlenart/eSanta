namespace eSanta.Common;

public sealed record ServiceResult(bool Succeeded, string? Error)
{
    public static ServiceResult Success() => new(true, null);

    public static ServiceResult Failure(string error) => new(false, error);
}
