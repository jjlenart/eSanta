using eSanta.Features.Shoutbox;
using Shouldly;

namespace eSanta.Tests.Features.Shoutbox;

public class MentionParserTests
{
    private static readonly MentionCandidate[] Candidates =
    [
        new("jan", "Jan"),
        new("janek", "Janek"),
        new("wujek", "Wujek"),
        new("wujek-janek", "Wujek Janek")
    ];

    private readonly MentionParser _parser = new();

    [Theory]
    [InlineData("@Jan cześć", "jan")]
    [InlineData("Cześć @jan!", "jan")]
    [InlineData("(@Janek)", "janek")]
    [InlineData("@Wujek, chodź", "wujek")]
    [InlineData("@Wujek Janek chodź", "wujek-janek")]
    public void FindsSingleMention(string content, string expectedUserId)
    {
        _parser.FindMentionedUserIds(content, Candidates).ShouldBe([expectedUserId]);
    }

    [Theory]
    [InlineData("Bez oznaczeń")]
    [InlineData("mail@Jan.pl")]
    [InlineData("@Jann")]
    [InlineData("@")]
    [InlineData("@ Jan")]
    public void IgnoresTextThatIsNotAMention(string content)
    {
        _parser.FindMentionedUserIds(content, Candidates).ShouldBeEmpty();
    }

    [Fact]
    public void FindsSeveralMentionsOnce()
    {
        _parser.FindMentionedUserIds("@Jan i @Janek, @Jan!", Candidates).ShouldBe(["jan", "janek"], ignoreOrder: true);
    }

    [Fact]
    public void SameAliasForTwoUsers_MentionsBoth()
    {
        MentionCandidate[] candidates = [new("a", "Ola"), new("b", "Ola")];

        _parser.FindMentionedUserIds("@Ola", candidates).ShouldBe(["a", "b"], ignoreOrder: true);
    }
}
