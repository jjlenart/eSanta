using eSanta.Common;
using eSanta.Data;
using eSanta.Features.Registration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace eSanta.Tests.Features.Registration;

public sealed class RegistrationServiceTests : IDisposable
{
    private const string ValidPassword = "Haslo123!";

    private readonly ServiceProvider _serviceProvider;
    private readonly string _databaseName = Guid.NewGuid().ToString();

    public RegistrationServiceTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        services.AddIdentityCore<ApplicationUser>(options => options.User.AllowedUserNameCharacters = string.Empty)
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddErrorDescriber<PolishIdentityErrorDescriber>();
        services.AddSingleton<IModelValidator, DataAnnotationsModelValidator>();
        services.AddScoped<IRegistrationService, RegistrationService>();

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task RegisterAsync_WithNewFamilyName_CreatesFamilyAndUser()
    {
        var result = await RegisterAsync(CreateModel("Łucja", "  Kowalscy  "));

        result.Succeeded.ShouldBeTrue();
        await using var dbContext = CreateDbContext();
        var family = (await dbContext.Families.ToListAsync()).ShouldHaveSingleItem();
        family.Name.ShouldBe("Kowalscy");
        family.NormalizedName.ShouldBe("KOWALSCY");
        var user = (await dbContext.Users.ToListAsync()).ShouldHaveSingleItem();
        user.UserName.ShouldBe("Łucja");
        user.DisplayName.ShouldBe("Łucja");
        user.FamilyId.ShouldBe(family.Id);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingFamilyNameInDifferentCase_JoinsThatFamily()
    {
        await RegisterAsync(CreateModel("Anna", "Kowalscy"));

        var result = await RegisterAsync(CreateModel("Jan", "kowalscy"));

        result.Succeeded.ShouldBeTrue();
        await using var dbContext = CreateDbContext();
        var family = (await dbContext.Families.ToListAsync()).ShouldHaveSingleItem();
        (await dbContext.Users.CountAsync(user => user.FamilyId == family.Id)).ShouldBe(2);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateFirstName_ReturnsPolishError()
    {
        await RegisterAsync(CreateModel("Anna", "Kowalscy"));

        var result = await RegisterAsync(CreateModel("Anna", "Nowakowie"));

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.Contains("Login 'Anna' jest już zajęty"));
    }

    [Fact]
    public async Task RegisterAsync_WithWeakPassword_DoesNotCreateFamily()
    {
        var model = CreateModel("Anna", "Kowalscy");
        model.Password = "abcdef";
        model.ConfirmPassword = "abcdef";

        var result = await RegisterAsync(model);

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldNotBeEmpty();
        await using var dbContext = CreateDbContext();
        (await dbContext.Families.AnyAsync()).ShouldBeFalse();
        (await dbContext.Users.AnyAsync()).ShouldBeFalse();
    }

    [Fact]
    public async Task RegisterAsync_WithMismatchedPasswords_ReturnsValidationError()
    {
        var model = CreateModel("Anna", "Kowalscy");
        model.ConfirmPassword = "Inne123!";

        var result = await RegisterAsync(model);

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain("Hasło i jego powtórzenie nie są takie same.");
    }

    [Theory]
    [InlineData("", "Kowalscy")]
    [InlineData("Anna", "   ")]
    public async Task RegisterAsync_WithMissingRequiredField_ReturnsValidationError(string firstName, string familyName)
    {
        var result = await RegisterAsync(CreateModel(firstName, familyName));

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldNotBeEmpty();
    }

    public void Dispose() => _serviceProvider.Dispose();

    private async Task<RegistrationResult> RegisterAsync(RegistrationModel model)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IRegistrationService>();

        return await service.RegisterAsync(model);
    }

    private ApplicationDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(_databaseName).Options);

    private static RegistrationModel CreateModel(string firstName, string familyName) => new()
    {
        FirstName = firstName,
        Password = ValidPassword,
        ConfirmPassword = ValidPassword,
        FamilyName = familyName
    };
}
