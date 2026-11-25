using eSanta.Common;

namespace eSanta.Features.Shoutbox;

public interface IShoutboxService
{
    Task<ShoutboxFeed> GetLatestAsync(string userId, CancellationToken cancellationToken = default);

    Task<ShoutboxPage> GetOlderAsync(string userId, long beforeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShoutboxMessageDto>> GetNewerAsync(string userId, long afterId, CancellationToken cancellationToken = default);

    Task<ServiceResult> SendAsync(string userId, ShoutboxMessageFormModel model, CancellationToken cancellationToken = default);
}
