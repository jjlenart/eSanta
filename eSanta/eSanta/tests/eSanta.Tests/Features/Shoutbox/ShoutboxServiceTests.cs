using eSanta.Common;
using eSanta.Data;
using eSanta.Features.Shoutbox;
using eSanta.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace eSanta.Tests.Features.Shoutbox;

public class ShoutboxServiceTests
{
    private const string Me = "me";
    private const string Jan = "jan";
    private const string Stranger = "stranger";
    private const string Homeless = "homeless";
    private const int MyFamilyId = 1;
    private const int OtherFamilyId = 2;

    private static readonly DateTimeOffset Now = new(2026, 12, 24, 18, 30, 0, TimeSpan.Zero);

    private readonly InMemoryDbContextFactory _dbContextFactory = new();
    private readonly RecordingNotifier _notifier = new();
    private readonly ShoutboxService _service;

    public ShoutboxServiceTests()
    {
        _service = new ShoutboxService(_dbContextFactory, new DataAnnotationsModelValidator(), new MentionParser(), _notifier, new FixedTimeProvider(Now));

        using var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.Families.AddRange(
            new Family { Id = MyFamilyId, Name = "Kowalscy", NormalizedName = "KOWALSCY" },
            new Family { Id = OtherFamilyId, Name = "Nowakowie", NormalizedName = "NOWAKOWIE" });
        dbContext.Users.AddRange(
            new ApplicationUser { Id = Me, UserName = "Anna", DisplayName = "Ciocia Ania", FamilyId = MyFamilyId },
            new ApplicationUser { Id = Jan, UserName = "Jan", FamilyId = MyFamilyId },
            new ApplicationUser { Id = Stranger, UserName = "Piotr", FamilyId = OtherFamilyId },
            new ApplicationUser { Id = Homeless, UserName = "Bez rodziny" });
        dbContext.SaveChanges();
    }

    [Fact]
    public async Task SendAsync_StoresTrimmedMessageWithDisplayNameAndDate()
    {
        var result = await _service.SendAsync(Me, new ShoutboxMessageFormModel { Content = "  Wesołych Świąt!  " });

        result.Succeeded.ShouldBeTrue();
        var message = (await _service.GetLatestAsync(Jan)).Page.Messages.ShouldHaveSingleItem();
        message.Content.ShouldBe("Wesołych Świąt!");
        message.AuthorName.ShouldBe("Ciocia Ania");
        message.SentAt.ShouldBe(Now);
    }

    [Fact]
    public async Task SendAsync_NotifiesOnlyAuthorsFamily()
    {
        await _service.SendAsync(Me, new ShoutboxMessageFormModel { Content = "Hej" });

        _notifier.NotifiedFamilies.ShouldBe([MyFamilyId]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SendAsync_EmptyContent_Fails(string? content)
    {
        var result = await _service.SendAsync(Me, new ShoutboxMessageFormModel { Content = content });

        result.Succeeded.ShouldBeFalse();
        await AssertNoMessagesAsync();
    }

    [Fact]
    public async Task SendAsync_TooLongContent_Fails()
    {
        var result = await _service.SendAsync(Me, new ShoutboxMessageFormModel { Content = new string('a', ShoutboxMessage.ContentMaxLength + 1) });

        result.Succeeded.ShouldBeFalse();
        await AssertNoMessagesAsync();
    }

    [Fact]
    public async Task SendAsync_UserWithoutFamily_Fails()
    {
        var result = await _service.SendAsync(Homeless, new ShoutboxMessageFormModel { Content = "Hej" });

        result.Succeeded.ShouldBeFalse();
        _notifier.NotifiedFamilies.ShouldBeEmpty();
        await AssertNoMessagesAsync();
    }

    [Fact]
    public async Task GetLatestAsync_ShowsOnlyMessagesFromMyFamily()
    {
        await _service.SendAsync(Me, new ShoutboxMessageFormModel { Content = "Nasza" });
        await _service.SendAsync(Stranger, new ShoutboxMessageFormModel { Content = "Obca" });

        var feed = await _service.GetLatestAsync(Jan);

        feed.FamilyId.ShouldBe(MyFamilyId);
        feed.Page.Messages.Select(message => message.Content).ShouldBe(["Nasza"]);
    }

    [Fact]
    public async Task GetLatestAsync_UserWithoutFamily_ReturnsNoFamily()
    {
        var feed = await _service.GetLatestAsync(Homeless);

        feed.HasFamily.ShouldBeFalse();
        feed.Page.Messages.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetLatestAsync_ReturnsNewestPageOrderedFromOldest()
    {
        await SeedMessagesAsync(ShoutboxService.PageSize + 5);

        var page = (await _service.GetLatestAsync(Me)).Page;

        page.HasOlder.ShouldBeTrue();
        page.Messages.Count.ShouldBe(ShoutboxService.PageSize);
        page.Messages[0].Content.ShouldBe("6");
        page.Messages[^1].Content.ShouldBe((ShoutboxService.PageSize + 5).ToString());
    }

    [Fact]
    public async Task GetOlderAsync_PagesBackToTheVeryFirstMessage()
    {
        await SeedMessagesAsync(ShoutboxService.PageSize * 2 + 3);

        var loaded = (await _service.GetLatestAsync(Me)).Page.Messages.ToList();
        var hasOlder = true;
        while (hasOlder)
        {
            var older = await _service.GetOlderAsync(Me, loaded[0].Id);
            loaded.InsertRange(0, older.Messages);
            hasOlder = older.HasOlder;
        }

        loaded.Select(message => message.Content)
            .ShouldBe(Enumerable.Range(1, ShoutboxService.PageSize * 2 + 3).Select(number => number.ToString()));
    }

    [Fact]
    public async Task GetOlderAsync_DoesNotLeakOtherFamilyMessages()
    {
        await _service.SendAsync(Stranger, new ShoutboxMessageFormModel { Content = "Obca" });

        var page = await _service.GetOlderAsync(Me, long.MaxValue);

        page.Messages.ShouldBeEmpty();
        page.HasOlder.ShouldBeFalse();
    }

    [Fact]
    public async Task GetNewerAsync_ReturnsMessagesAfterGivenIdFromMyFamily()
    {
        await _service.SendAsync(Me, new ShoutboxMessageFormModel { Content = "Pierwsza" });
        var firstId = (await _service.GetLatestAsync(Me)).Page.Messages.Single().Id;
        await _service.SendAsync(Jan, new ShoutboxMessageFormModel { Content = "Druga" });
        await _service.SendAsync(Stranger, new ShoutboxMessageFormModel { Content = "Obca" });

        var newer = await _service.GetNewerAsync(Me, firstId);

        newer.Select(message => message.Content).ShouldBe(["Druga"]);
        // Jan has no display name, so the login is shown instead.
        newer.Single().AuthorName.ShouldBe("Jan");
    }

    [Fact]
    public async Task SendAsync_MentionedFamilyMember_SeesMessageAsMentioningThem()
    {
        await _service.SendAsync(Me, new ShoutboxMessageFormModel { Content = "@jan przyniesiesz choinkę?" });

        (await _service.GetLatestAsync(Jan)).Page.Messages.Single().MentionsMe.ShouldBeTrue();
        (await _service.GetLatestAsync(Me)).Page.Messages.Single().MentionsMe.ShouldBeFalse();
    }

    [Fact]
    public async Task SendAsync_MentionOfUserFromOtherFamily_IsIgnored()
    {
        await _service.SendAsync(Me, new ShoutboxMessageFormModel { Content = "Cześć @Piotr" });

        await using var dbContext = _dbContextFactory.CreateDbContext();
        (await dbContext.ShoutboxMentions.CountAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task SendAsync_AuthorCannotMentionThemselves()
    {
        await _service.SendAsync(Me, new ShoutboxMessageFormModel { Content = "To ja, @Ciocia Ania" });

        await using var dbContext = _dbContextFactory.CreateDbContext();
        (await dbContext.ShoutboxMentions.CountAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task GetNewerAsync_MarksMessagesMentioningMe()
    {
        await _service.SendAsync(Jan, new ShoutboxMessageFormModel { Content = "Hej @Ciocia Ania!" });

        (await _service.GetNewerAsync(Me, 0)).Single().MentionsMe.ShouldBeTrue();
    }

    [Fact]
    public async Task GetLatestAsync_ReturnsAliasesOfOtherFamilyMembersForSuggestions()
    {
        (await _service.GetLatestAsync(Me)).MentionableAliases.ShouldBe(["Jan"]);
        (await _service.GetLatestAsync(Jan)).MentionableAliases.ShouldBe(["Ciocia Ania"]);
    }

    [Fact]
    public async Task SendAsync_Anonymous_HidesAuthorFromOthers()
    {
        await _service.SendAsync(Me, new ShoutboxMessageFormModel { Content = "Zgadnij kto", IsAnonymous = true });

        var seenByJan = (await _service.GetLatestAsync(Jan)).Page.Messages.Single();
        seenByJan.IsAnonymous.ShouldBeTrue();
        seenByJan.AuthorName.ShouldBeNull();
        seenByJan.IsMine.ShouldBeFalse();
    }

    [Fact]
    public async Task SendAsync_Anonymous_AuthorStillRecognisesOwnMessage()
    {
        await _service.SendAsync(Me, new ShoutboxMessageFormModel { Content = "Zgadnij kto", IsAnonymous = true });

        var seenByMe = (await _service.GetNewerAsync(Me, 0)).Single();
        seenByMe.IsAnonymous.ShouldBeTrue();
        seenByMe.IsMine.ShouldBeTrue();
    }

    [Fact]
    public async Task SendAsync_Anonymous_MentionsStillWork()
    {
        await _service.SendAsync(Me, new ShoutboxMessageFormModel { Content = "@Jan wiem, co dostaniesz", IsAnonymous = true });

        (await _service.GetLatestAsync(Jan)).Page.Messages.Single().MentionsMe.ShouldBeTrue();
    }

    [Fact]
    public async Task SendAsync_NotAnonymous_ShowsAuthor()
    {
        await _service.SendAsync(Me, new ShoutboxMessageFormModel { Content = "Hej" });

        var message = (await _service.GetLatestAsync(Jan)).Page.Messages.Single();
        message.IsAnonymous.ShouldBeFalse();
        message.AuthorName.ShouldBe("Ciocia Ania");
    }

    private async Task SeedMessagesAsync(int count)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.ShoutboxMessages.AddRange(Enumerable.Range(1, count).Select(number => new ShoutboxMessage
        {
            FamilyId = MyFamilyId,
            AuthorId = number % 2 == 0 ? Me : Jan,
            Content = number.ToString(),
            SentAt = Now.AddMinutes(number)
        }));
        await dbContext.SaveChangesAsync();
    }

    private async Task AssertNoMessagesAsync()
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        (await dbContext.ShoutboxMessages.CountAsync()).ShouldBe(0);
    }

    private sealed class RecordingNotifier : IShoutboxNotifier
    {
        public List<int> NotifiedFamilies { get; } = [];

        public IDisposable Subscribe(int familyId, Func<Task> onMessagePosted) => throw new NotSupportedException();

        public Task NotifyMessagePostedAsync(int familyId)
        {
            NotifiedFamilies.Add(familyId);
            return Task.CompletedTask;
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
