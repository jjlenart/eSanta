namespace eSanta.Components.Shared;

public interface ICurrentUser
{
    Task<string?> GetIdAsync();
}
