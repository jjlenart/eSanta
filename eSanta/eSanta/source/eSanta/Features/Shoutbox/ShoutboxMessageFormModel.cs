using System.ComponentModel.DataAnnotations;
using eSanta.Data;

namespace eSanta.Features.Shoutbox;

public sealed class ShoutboxMessageFormModel
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "Wpisz treść wiadomości.")]
    [StringLength(ShoutboxMessage.ContentMaxLength, ErrorMessage = "Wiadomość może mieć maksymalnie {1} znaków.")]
    public string? Content { get; set; }

    public bool IsAnonymous { get; set; }
}
