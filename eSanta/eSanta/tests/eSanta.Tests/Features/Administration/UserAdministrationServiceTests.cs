using eSanta.Common;
using System.Security.Claims;
using eSanta.Data;
using eSanta.Features.Administration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace eSanta.Tests.Features.Administration;

public sealed class UserAdministrationServiceTests : IDisposable
{
    private const string OldPassword = "Stare123!";
    private const string NewPassword = "Nowe456!";

    private readonly ServiceProvider _serviceProvider;
    private readonly string _databaseName = Guid.NewGuid().ToString();

    public UserAdministrationServiceTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddDbContextFactory<ApplicationDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        services.AddIdentityCore<ApplicationUser>(options => options.User.AllowedUserNameCharacters = string.Empty)
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
        services.AddSingleton<IModelValidator, DataAnnotationsModelValidator>();
        services.AddScoped<IUserAdministrationService, UserAdministrationService>();

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task IsAdminAsync_ReflectsFlag()
    {
        var adminId = await CreateUserAsync("Admin", isAdmin: true);
        var userId = await CreateUserAsync("Anna");

        await UseServiceAsync(async service =>
        {
            (await service.IsAdminAsync(adminId)).ShouldBeTrue();
            (await service.IsAdminAsync(userId)).ShouldBeFalse();
            (await service.IsAdminAsync("missing")).ShouldBeFalse();
        });
    }

    [Fact]
    public async Task GetUsersAsync_AsAdmin_ListsAllUsersWithFamilyAndMarksCurrentUser()
    {
        var adminId = await CreateUserAsync("Admin", isAdmin: true);
        await CreateUserAsync("Anna", displayName: "Ciocia Ania", familyName: "Kowalscy");

        var users = await UseServiceAsync(service => service.GetUsersAsync(adminId));

        users.ShouldNotBeNull();
        users.Select(user => user.Alias).ShouldBe(["Admin", "Ciocia Ania"]);
        users.Single(user => user.UserName == "Anna").FamilyName.ShouldBe("Kowalscy");
        users.Single(user => user.UserName == "Admin").IsCurrentUser.ShouldBeTrue();
        users.Single(user => user.UserName == "Admin").IsAdmin.ShouldBeTrue();
    }

    [Fact]
    public async Task GetUsersAsync_AsRegularUser_ReturnsNull()
    {
        var userId = await CreateUserAsync("Anna");

        (await UseServiceAsync(service => service.GetUsersAsync(userId))).ShouldBeNull();
    }

    [Fact]
    public async Task SetPasswordAsync_AsAdmin_ChangesPassword()
    {
        var adminId = await CreateUserAsync("Admin", isAdmin: true);
        var userId = await CreateUserAsync("Anna");

        var result = await UseServiceAsync(service => service.SetPasswordAsync(adminId, userId, ValidModel()));

        result.Succeeded.ShouldBeTrue();
        (await CheckPasswordAsync("Anna", NewPassword)).ShouldBeTrue();
        (await CheckPasswordAsync("Anna", OldPassword)).ShouldBeFalse();
    }

    [Fact]
    public async Task SetPasswordAsync_AsRegularUser_ReturnsFailureAndKeepsPassword()
    {
        var userId = await CreateUserAsync("Anna");
        var otherId = await CreateUserAsync("Jan");

        var result = await UseServiceAsync(service => service.SetPasswordAsync(userId, otherId, ValidModel()));

        result.Succeeded.ShouldBeFalse();
        result.Error.ShouldBe("Nie masz uprawnień administratora.");
        (await CheckPasswordAsync("Jan", OldPassword)).ShouldBeTrue();
    }

    [Fact]
    public async Task SetPasswordAsync_ForSelf_ReturnsFailure()
    {
        var adminId = await CreateUserAsync("Admin", isAdmin: true);

        var result = await UseServiceAsync(service => service.SetPasswordAsync(adminId, adminId, ValidModel()));

        result.Succeeded.ShouldBeFalse();
        (await CheckPasswordAsync("Admin", OldPassword)).ShouldBeTrue();
    }

    [Fact]
    public async Task SetPasswordAsync_ForMissingUser_ReturnsFailure()
    {
        var adminId = await CreateUserAsync("Admin", isAdmin: true);

        var result = await UseServiceAsync(service => service.SetPasswordAsync(adminId, "missing", ValidModel()));

        result.Succeeded.ShouldBeFalse();
    }

    [Theory]
    [InlineData("Nowe456!", "Inne456!")]
    [InlineData("abc", "abc")]
    [InlineData("", "")]
    public async Task SetPasswordAsync_WithInvalidModel_ReturnsFailureAndKeepsPassword(string newPassword, string confirmPassword)
    {
        var adminId = await CreateUserAsync("Admin", isAdmin: true);
        var userId = await CreateUserAsync("Anna");

        var result = await UseServiceAsync(service => service.SetPasswordAsync(adminId, userId,
            new SetPasswordModel { NewPassword = newPassword, ConfirmPassword = confirmPassword }));

        result.Succeeded.ShouldBeFalse();
        (await CheckPasswordAsync("Anna", OldPassword)).ShouldBeTrue();
    }

    [Fact]
    public async Task SetPasswordAsync_WithPasswordRejectedByIdentity_ReturnsIdentityError()
    {
        var adminId = await CreateUserAsync("Admin", isAdmin: true);
        var userId = await CreateUserAsync("Anna");

        var result = await UseServiceAsync(service => service.SetPasswordAsync(adminId, userId,
            new SetPasswordModel { NewPassword = "abcdef", ConfirmPassword = "abcdef" }));

        result.Succeeded.ShouldBeFalse();
        result.Error.ShouldNotBeNullOrWhiteSpace();
        (await CheckPasswordAsync("Anna", OldPassword)).ShouldBeTrue();
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task AdminAuthorizationHandler_SucceedsOnlyForAdmins(bool isAdmin, bool expected)
    {
        var userId = await CreateUserAsync("Anna", isAdmin: isAdmin);
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId)], "Test"));
        var requirement = new AdminRequirement();
        var context = new AuthorizationHandlerContext([requirement], principal, resource: null);

        await UseServiceAsync(async service =>
        {
            await new AdminAuthorizationHandler(service).HandleAsync(context);
        });

        context.HasSucceeded.ShouldBe(expected);
    }

    public void Dispose() => _serviceProvider.Dispose();

    private static SetPasswordModel ValidModel() => new() { NewPassword = NewPassword, ConfirmPassword = NewPassword };

    private async Task<string> CreateUserAsync(string userName, bool isAdmin = false, string? displayName = null, string? familyName = null)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            UserName = userName,
            DisplayName = displayName,
            IsAdmin = isAdmin,
            Family = familyName is null ? null : new Family { Name = familyName, NormalizedName = familyName.ToUpperInvariant() }
        };

        (await userManager.CreateAsync(user, OldPassword)).Succeeded.ShouldBeTrue();

        return user.Id;
    }

    private async Task<bool> CheckPasswordAsync(string userName, string password)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        return await userManager.CheckPasswordAsync((await userManager.FindByNameAsync(userName))!, password);
    }

    private async Task UseServiceAsync(Func<IUserAdministrationService, Task> action)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        await action(scope.ServiceProvider.GetRequiredService<IUserAdministrationService>());
    }

    private async Task<T> UseServiceAsync<T>(Func<IUserAdministrationService, Task<T>> action)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<IUserAdministrationService>());
    }
}
