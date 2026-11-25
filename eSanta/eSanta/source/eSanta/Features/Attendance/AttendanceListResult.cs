namespace eSanta.Features.Attendance;

public sealed record AttendanceListResult(string? FamilyName, IReadOnlyList<AttendeeDto> Attendees)
{
    public bool HasFamily => FamilyName is not null;

    public static AttendanceListResult NoFamily() => new(null, []);
}
