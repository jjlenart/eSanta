using System.ComponentModel.DataAnnotations;
using eSanta.Data;

namespace eSanta.Features.Attendance;

public sealed class AttendeeFormModel
{
    [Required(ErrorMessage = "Podaj imię i nazwisko.")]
    [StringLength(Attendee.FullNameMaxLength, ErrorMessage = "Imię i nazwisko może mieć maksymalnie {1} znaków.")]
    public string FullName { get; set; } = string.Empty;

    public bool IsPresentAtChristmasEve { get; set; }

    public bool ParticipatesInGiftExchange { get; set; }

    public static AttendeeFormModel FromDto(AttendeeDto attendee) => new()
    {
        FullName = attendee.FullName,
        IsPresentAtChristmasEve = attendee.IsPresentAtChristmasEve,
        ParticipatesInGiftExchange = attendee.ParticipatesInGiftExchange
    };
}
