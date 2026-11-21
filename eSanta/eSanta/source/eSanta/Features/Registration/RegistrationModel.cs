using System.ComponentModel.DataAnnotations;
using eSanta.Data;

namespace eSanta.Features.Registration;

public sealed class RegistrationModel
{
    public const int FirstNameMaxLength = ApplicationUser.DisplayNameMaxLength;

    [Required(ErrorMessage = "Pole {0} jest wymagane.")]
    [StringLength(FirstNameMaxLength, ErrorMessage = "Pole {0} może mieć maksymalnie {1} znaków.")]
    [Display(Name = "Imię")]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Pole {0} jest wymagane.")]
    [StringLength(100, ErrorMessage = "Pole {0} musi mieć od {2} do {1} znaków.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Hasło")]
    public string Password { get; set; } = "";

    [DataType(DataType.Password)]
    [Display(Name = "Powtórz hasło")]
    [Compare(nameof(Password), ErrorMessage = "Hasło i jego powtórzenie nie są takie same.")]
    public string ConfirmPassword { get; set; } = "";

    [Required(ErrorMessage = "Pole {0} jest wymagane.")]
    [StringLength(Family.NameMaxLength, ErrorMessage = "Pole {0} może mieć maksymalnie {1} znaków.")]
    [Display(Name = "Nazwa rodziny")]
    public string FamilyName { get; set; } = "";
}
