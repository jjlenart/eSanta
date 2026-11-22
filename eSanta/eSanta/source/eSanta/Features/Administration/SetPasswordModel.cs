using System.ComponentModel.DataAnnotations;

namespace eSanta.Features.Administration;

public sealed class SetPasswordModel
{
    [Required(ErrorMessage = "Pole {0} jest wymagane.")]
    [StringLength(100, ErrorMessage = "Pole {0} musi mieć od {2} do {1} znaków.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Nowe hasło")]
    public string NewPassword { get; set; } = "";

    [DataType(DataType.Password)]
    [Display(Name = "Powtórz hasło")]
    [Compare(nameof(NewPassword), ErrorMessage = "Hasło i jego powtórzenie nie są takie same.")]
    public string ConfirmPassword { get; set; } = "";
}
