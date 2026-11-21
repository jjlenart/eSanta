using eSanta.Data;
using eSanta.Features.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace eSanta.Tests.Features.Account;

public sealed class AliasServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly string _databaseName = Guid.NewGuid().ToString();

    public AliasServiceTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        services.AddIdentityCore<ApplicationUser>(options => options.User.AllowedUserNameCharacters = string.Empty)
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddClaimsPrincipalFactory<AliasClaimsPrincipalFactory>();
        services.AddScoped<IAliasService, AliasService>();

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void Alias_WithoutDisplayName_FallsBackToUserName()
    {
        var user = new ApplicationUser { UserName = "Anna" };

        user.Alias.ShouldBe("Anna");
    }

    [Fact]
    public async Task ChangeAliasAsync_WithValidAlias_SavesTrimmedAlias()
    {
        await CreateUserAsync("Anna");

        var result = await ChangeAliasAsync("Anna", "  Ciocia Ania  ");

        result.Succeeded.ShouldBeTrue();
        (await FindUserAsync("Anna")).DisplayName.ShouldBe("Ciocia Ania");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ChangeAliasAsync_WithEmptyAlias_ReturnsFailureAndKeepsOldAlias(string alias)
    {
        await CreateUserAsync("Anna");

        var result = await ChangeAliasAsync("Anna", alias);

        result.Succeeded.ShouldBeFalse();
        result.Error.ShouldBe("Pole Alias jest wymagane.");
        (await FindUserAsync("Anna")).Alias.ShouldBe("Anna");
    }

    [Fact]
    public async Task ChangeAliasAsync_WithTooLongAlias_ReturnsFailure()
    {
        await CreateUserAsync("Anna");

        var result = await ChangeAliasAsync("Anna", new string('a', ApplicationUser.DisplayNameMaxLength + 1));

        result.Succeeded.ShouldBeFalse();
    }

    [Fact]
    public async Task ClaimsPrincipalFactory_AddsAliasClaim()
    {
        await CreateUserAsync("Anna");
        await ChangeAliasAsync("Anna", "Ciocia Ania");

        await using var scope = _serviceProvider.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var factory = scope.ServiceProvider.GetRequiredService<IUserClaimsPrincipalFactory<ApplicationUser>>();
        var principal = await factory.CreateAsync((await userManager.FindByNameAsync("Anna"))!);

        principal.FindFirst(AliasClaimsPrincipalFactory.AliasClaimType)?.Value.ShouldBe("Ciocia Ania");
    }

    public void Dispose() => _serviceProvider.Dispose();

    private async Task CreateUserAsync(string userName)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        (await userManager.CreateAsync(new ApplicationUser { UserName = userName })).Succeeded.ShouldBeTrue();
    }

    private async Task<eSanta.Common.ServiceResult> ChangeAliasAsync(string userName, string alias)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var service = scope.ServiceProvider.GetRequiredService<IAliasService>();
        var user = (await userManager.FindByNameAsync(userName))!;

        return await service.ChangeAliasAsync(user, new AliasModel { Alias = alias });
    }

    private async Task<ApplicationUser> FindUserAsync(string userName)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        return (await userManager.FindByNameAsync(userName))!;
    }
}
