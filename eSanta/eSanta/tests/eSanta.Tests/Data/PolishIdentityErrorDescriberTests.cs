using System.Reflection;
using eSanta.Data;
using Microsoft.AspNetCore.Identity;
using Shouldly;

namespace eSanta.Tests.Data;

public class PolishIdentityErrorDescriberTests
{
    [Fact]
    public void OverridesEveryErrorMessage()
    {
        var baseMethods = typeof(IdentityErrorDescriber)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(method => method.IsVirtual && method.ReturnType == typeof(IdentityError));

        var notOverridden = baseMethods
            .Where(method => typeof(PolishIdentityErrorDescriber)
                .GetMethod(method.Name, method.GetParameters().Select(parameter => parameter.ParameterType).ToArray())!
                .DeclaringType != typeof(PolishIdentityErrorDescriber))
            .Select(method => method.Name)
            .ToList();

        notOverridden.ShouldBeEmpty();
    }

    [Fact]
    public void PasswordTooShort_ReturnsPolishMessageWithLength()
    {
        var error = new PolishIdentityErrorDescriber().PasswordTooShort(6);

        error.Code.ShouldBe(nameof(IdentityErrorDescriber.PasswordTooShort));
        error.Description.ShouldBe("Hasło musi mieć co najmniej 6 znaków.");
    }
}
