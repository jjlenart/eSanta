namespace eSanta.Features.Attendance;

public sealed record AttendeeDto(
    int Id,
    string FullName,
    bool IsPresentAtChristmasEve,
    bool ParticipatesInGiftExchange,
    bool IsRegisteredUser = false,
    bool IsCurrentUser = false)
{
    public bool CanEdit => !IsRegisteredUser;
}
