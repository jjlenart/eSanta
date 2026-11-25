using eSanta.Features.Shoutbox;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace eSanta.Tests.Features.Shoutbox;

public class ShoutboxNotifierTests
{
    private readonly ShoutboxNotifier _notifier = new(NullLogger<ShoutboxNotifier>.Instance);

    [Fact]
    public async Task Notify_ReachesOnlySubscribersOfThatFamily()
    {
        var calls = new List<string>();
        using var mine = _notifier.Subscribe(1, () => Record(calls, "mine"));
        using var other = _notifier.Subscribe(2, () => Record(calls, "other"));

        await _notifier.NotifyMessagePostedAsync(1);

        calls.ShouldBe(["mine"]);
    }

    [Fact]
    public async Task Notify_AfterDispose_IsNotDelivered()
    {
        var calls = new List<string>();
        var subscription = _notifier.Subscribe(1, () => Record(calls, "mine"));
        subscription.Dispose();

        await _notifier.NotifyMessagePostedAsync(1);

        calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task Notify_FailingSubscriber_DoesNotStopOthers()
    {
        var calls = new List<string>();
        using var broken = _notifier.Subscribe(1, () => throw new InvalidOperationException());
        using var working = _notifier.Subscribe(1, () => Record(calls, "working"));

        await Should.NotThrowAsync(() => _notifier.NotifyMessagePostedAsync(1));

        calls.ShouldBe(["working"]);
    }

    private static Task Record(List<string> calls, string name)
    {
        calls.Add(name);
        return Task.CompletedTask;
    }
}
