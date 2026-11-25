using eSanta.Common;

namespace eSanta.Features.Attendance;

public interface IAttendeeService
{
    Task<AttendanceListResult> GetAllAsync(string currentUserId, CancellationToken cancellationToken = default);

    Task<MyAttendanceDto> GetMyStatusAsync(string userId, CancellationToken cancellationToken = default);

    Task<ServiceResult> SetMyStatusAsync(string userId, MyAttendanceDto status, CancellationToken cancellationToken = default);

    Task<ServiceResult> AddAsync(string currentUserId, AttendeeFormModel model, CancellationToken cancellationToken = default);

    Task<ServiceResult> UpdateAsync(string currentUserId, int id, AttendeeFormModel model, CancellationToken cancellationToken = default);

    Task<ServiceResult> DeleteAsync(string currentUserId, int id, CancellationToken cancellationToken = default);
}
