using eSanta.Common;
using eSanta.Data;
using Microsoft.EntityFrameworkCore;

namespace eSanta.Features.Shoutbox;

public sealed class ShoutboxService(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IModelValidator modelValidator,
    IMentionParser mentionParser,
    IShoutboxNotifier notifier,
    TimeProvider timeProvider) : IShoutboxService
{
    public const int PageSize = 30;

    private const string NoFamilyError = "Twoje konto nie jest przypisane do żadnej rodziny, więc nie możesz pisać w czacie „Pogadajmy”.";

    public async Task<ShoutboxFeed> GetLatestAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        if (await GetFamilyIdAsync(dbContext, userId, cancellationToken) is not { } familyId)
        {
            return ShoutboxFeed.NoFamily();
        }

        var page = await GetPageAsync(dbContext.ShoutboxMessages.Where(message => message.FamilyId == familyId), userId, cancellationToken);
        var candidates = await GetMentionCandidatesAsync(dbContext, familyId, userId, cancellationToken);

        return new ShoutboxFeed(familyId, page, candidates.Select(candidate => candidate.Alias).ToList());
    }

    public async Task<ShoutboxPage> GetOlderAsync(string userId, long beforeId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        if (await GetFamilyIdAsync(dbContext, userId, cancellationToken) is not { } familyId)
        {
            return ShoutboxPage.Empty;
        }

        return await GetPageAsync(
            dbContext.ShoutboxMessages.Where(message => message.FamilyId == familyId && message.Id < beforeId),
            userId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<ShoutboxMessageDto>> GetNewerAsync(string userId, long afterId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        if (await GetFamilyIdAsync(dbContext, userId, cancellationToken) is not { } familyId)
        {
            return [];
        }

        return await Project(dbContext.ShoutboxMessages
                .Where(message => message.FamilyId == familyId && message.Id > afterId)
                .OrderBy(message => message.Id), userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceResult> SendAsync(string userId, ShoutboxMessageFormModel model, CancellationToken cancellationToken = default)
    {
        var validationErrors = modelValidator.Validate(model);
        if (validationErrors.Count > 0)
        {
            return ServiceResult.Failure(validationErrors);
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        if (await GetFamilyIdAsync(dbContext, userId, cancellationToken) is not { } familyId)
        {
            return ServiceResult.Failure(NoFamilyError);
        }

        var content = model.Content!.Trim();
        var candidates = await GetMentionCandidatesAsync(dbContext, familyId, userId, cancellationToken);

        dbContext.ShoutboxMessages.Add(new ShoutboxMessage
        {
            FamilyId = familyId,
            AuthorId = userId,
            Content = content,
            SentAt = timeProvider.GetUtcNow(),
            IsAnonymous = model.IsAnonymous,
            Mentions = mentionParser.FindMentionedUserIds(content, candidates)
                .Select(mentionedUserId => new ShoutboxMention { UserId = mentionedUserId })
                .ToList()
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return ServiceResult.Failure("Nie udało się wysłać wiadomości. Spróbuj ponownie.");
        }

        await notifier.NotifyMessagePostedAsync(familyId);

        return ServiceResult.Success();
    }

    private static async Task<ShoutboxPage> GetPageAsync(IQueryable<ShoutboxMessage> messages, string userId, CancellationToken cancellationToken)
    {
        // One extra row tells whether anything older remains.
        var newestFirst = await Project(messages.OrderByDescending(message => message.Id).Take(PageSize + 1), userId)
            .ToListAsync(cancellationToken);

        var hasOlder = newestFirst.Count > PageSize;

        return new ShoutboxPage(newestFirst.Take(PageSize).Reverse().ToList(), hasOlder);
    }

    private static IQueryable<ShoutboxMessageDto> Project(IQueryable<ShoutboxMessage> messages, string userId) =>
        messages.Select(message => new ShoutboxMessageDto(
            message.Id,
            message.IsAnonymous ? null : ApplicationUser.ToAlias(message.Author!.DisplayName, message.Author.UserName, "?"),
            message.Content,
            message.SentAt,
            message.AuthorId == userId,
            message.Mentions.Any(mention => mention.UserId == userId)));

    /// <summary>Other members of the family; the author cannot mention themselves.</summary>
    private static async Task<IReadOnlyList<MentionCandidate>> GetMentionCandidatesAsync(ApplicationDbContext dbContext, int familyId, string userId, CancellationToken cancellationToken)
    {
        var members = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.FamilyId == familyId && user.Id != userId)
            .Select(user => new { user.Id, user.DisplayName, user.UserName })
            .ToListAsync(cancellationToken);

        return members
            .Select(member => new MentionCandidate(member.Id, ApplicationUser.ToAlias(member.DisplayName, member.UserName)))
            .Where(candidate => candidate.Alias.Length > 0)
            .OrderBy(candidate => candidate.Alias, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static Task<int?> GetFamilyIdAsync(ApplicationDbContext dbContext, string userId, CancellationToken cancellationToken) =>
        dbContext.Users
            .Where(user => user.Id == userId)
            .Select(user => user.FamilyId)
            .FirstOrDefaultAsync(cancellationToken);
}
