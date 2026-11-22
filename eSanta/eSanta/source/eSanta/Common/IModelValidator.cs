namespace eSanta.Common;

public interface IModelValidator
{
    IReadOnlyList<string> Validate(object model);
}
