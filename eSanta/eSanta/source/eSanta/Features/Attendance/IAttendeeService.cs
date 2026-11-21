using eSanta.Common;

namespace eSanta.Features.Attendance;

public interface IAttendeeService
{
    Task<IReadOnlyList<AttendeeDto>> GetAllAsync(string currentUserId, CancellationToken cancellationToken = default);

    Task<MyAttendanceDto> GetMyStatusAsync(string userId, CancellationToken cancellationToken = default);

    Task<ServiceResult> SetMyStatusAsync(string userId, MyAttendanceDto status, CancellationToken cancellationToken = default);

    Task<ServiceResult> AddAsync(AttendeeFormModel model, CancellationToken cancellationToken = default);

    Task<ServiceResult> UpdateAsync(int id, AttendeeFormModel model, CancellationToken cancellationToken = default);

    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
