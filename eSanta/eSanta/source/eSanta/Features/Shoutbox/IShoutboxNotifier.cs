namespace eSanta.Features.Shoutbox;

/// <summary>Lets open shoutboxes of one family know that a new message was posted.</summary>
public interface IShoutboxNotifier
{
    /// <returns>Dispose to stop receiving notifications.</returns>
    IDisposable Subscribe(int familyId, Func<Task> onMessagePosted);

    Task NotifyMessagePostedAsync(int familyId);
}
