using System.ComponentModel.DataAnnotations;
using eSanta.Common;
using Shouldly;

namespace eSanta.Tests.Common;

public class DataAnnotationsModelValidatorTests
{
    private readonly DataAnnotationsModelValidator _validator = new();

    [Fact]
    public void Validate_ValidModel_ReturnsNoErrors()
    {
        _validator.Validate(new SampleModel { Name = "Anna", Note = "ok" }).ShouldBeEmpty();
    }

    [Fact]
    public void Validate_InvalidModel_ReturnsEveryErrorMessage()
    {
        var errors = _validator.Validate(new SampleModel { Name = "", Note = "za długa" });

        errors.ShouldBe(["Imię jest wymagane.", "Notatka jest za długa."], ignoreOrder: true);
    }

    [Fact]
    public void ServiceResultFailure_WithManyErrors_JoinsThem()
    {
        var result = ServiceResult.Failure(["Pierwszy.", "Drugi."]);

        result.Succeeded.ShouldBeFalse();
        result.Error.ShouldBe("Pierwszy. Drugi.");
    }

    private sealed class SampleModel
    {
        [Required(ErrorMessage = "Imię jest wymagane.")]
        public string Name { get; set; } = "";

        [StringLength(5, ErrorMessage = "Notatka jest za długa.")]
        public string? Note { get; set; }
    }
}
