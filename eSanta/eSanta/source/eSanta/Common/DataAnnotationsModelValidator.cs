using System.ComponentModel.DataAnnotations;

namespace eSanta.Common;

public sealed class DataAnnotationsModelValidator : IModelValidator
{
    public IReadOnlyList<string> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);

        return results
            .Select(result => result.ErrorMessage)
            .OfType<string>()
            .ToList();
    }
}
