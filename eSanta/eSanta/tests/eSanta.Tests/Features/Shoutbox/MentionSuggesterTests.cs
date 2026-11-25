using eSanta.Features.Shoutbox;
using Shouldly;

namespace eSanta.Tests.Features.Shoutbox;

public class MentionSuggesterTests
{
    private static readonly string[] Aliases = ["Zofia", "Jan", "Janek", "Wujek Janek"];

    private readonly MentionSuggester _suggester = new();

    [Fact]
    public void JustTypedAt_SuggestsEveryoneAlphabetically()
    {
        _suggester.Suggest("Cześć @", Aliases).ShouldBe(["Jan", "Janek", "Wujek Janek", "Zofia"]);
    }

    [Fact]
    public void FiltersByTypedPrefixIgnoringCase()
    {
        _suggester.Suggest("@ja", Aliases).ShouldBe(["Jan", "Janek"]);
    }

    [Fact]
    public void PrefixMayContainSpaces()
    {
        _suggester.Suggest("@Wujek J", Aliases).ShouldBe(["Wujek Janek"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Bez małpy")]
    [InlineData("mail@ja")]
    [InlineData("@Jan ")]
    [InlineData("@Zofia")]
    public void NoMentionBeingTyped_SuggestsNothing(string? text)
    {
        _suggester.Suggest(text, Aliases).ShouldBeEmpty();
    }

    [Fact]
    public void LimitsNumberOfSuggestions()
    {
        var many = Enumerable.Range(1, 10).Select(number => $"Osoba {number:00}").ToList();

        _suggester.Suggest("@", many).Count.ShouldBe(MentionSuggester.MaxSuggestions);
    }

    [Theory]
    [InlineData("Cześć @ja", "Cześć @Janek ")]
    [InlineData("@", "@Janek ")]
    public void Complete_ReplacesTypedMention(string text, string expected)
    {
        _suggester.Complete(text, "Janek").ShouldBe(expected);
    }
}
