using System.ComponentModel.DataAnnotations;
using eSanta.Data;

namespace eSanta.Features.Account;

public sealed class AliasModel
{
    [Required(ErrorMessage = "Pole {0} jest wymagane.")]
    [StringLength(ApplicationUser.DisplayNameMaxLength, ErrorMessage = "Pole {0} może mieć maksymalnie {1} znaków.")]
    [Display(Name = "Alias")]
    public string Alias { get; set; } = "";
}
