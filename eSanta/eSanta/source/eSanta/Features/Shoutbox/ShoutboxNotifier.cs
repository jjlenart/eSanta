namespace eSanta.Features.Shoutbox;

public sealed class ShoutboxNotifier(ILogger<ShoutboxNotifier> logger) : IShoutboxNotifier
{
    private readonly Lock _lock = new();
    private readonly Dictionary<int, List<Func<Task>>> _subscribers = [];

    public IDisposable Subscribe(int familyId, Func<Task> onMessagePosted)
    {
        lock (_lock)
        {
            if (!_subscribers.TryGetValue(familyId, out var handlers))
            {
                handlers = [];
                _subscribers[familyId] = handlers;
            }

            handlers.Add(onMessagePosted);
        }

        return new Subscription(() => Unsubscribe(familyId, onMessagePosted));
    }

    public async Task NotifyMessagePostedAsync(int familyId)
    {
        Func<Task>[] handlers;
        lock (_lock)
        {
            handlers = _subscribers.TryGetValue(familyId, out var list) ? [.. list] : [];
        }

        await Task.WhenAll(handlers.Select(InvokeSafelyAsync));
    }

    private async Task InvokeSafelyAsync(Func<Task> handler)
    {
        try
        {
            await handler();
        }
        catch (Exception exception)
        {
            // One broken circuit must not prevent the others from being notified.
            logger.LogWarning(exception, "Shoutbox subscriber failed to handle a new message notification.");
        }
    }

    private void Unsubscribe(int familyId, Func<Task> handler)
    {
        lock (_lock)
        {
            if (_subscribers.TryGetValue(familyId, out var handlers))
            {
                handlers.Remove(handler);
                if (handlers.Count == 0)
                {
                    _subscribers.Remove(familyId);
                }
            }
        }
    }

    private sealed class Subscription(Action unsubscribe) : IDisposable
    {
        private Action? _unsubscribe = unsubscribe;

        public void Dispose() => Interlocked.Exchange(ref _unsubscribe, null)?.Invoke();
    }
}
