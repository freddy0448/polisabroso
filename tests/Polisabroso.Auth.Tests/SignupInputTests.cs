using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Polisabroso.Modules.Auth.UI;

namespace Polisabroso.Auth.Tests;

public class SignupInputTests
{
    [Theory]
    [InlineData("Abc12!", false)]
    [InlineData("abcdefgh1!", false)]
    [InlineData("ABCDEFGH1!", false)]
    [InlineData("Abcdefgh!", false)]
    [InlineData("Abcdefgh1", false)]
    [InlineData("Abcdefg1!", true)]
    [InlineData(" Abcdef1 ", true)]
    [InlineData("Ábcdefg1!", false)]
    public void Password_ConfiguredRequirements_ValidatesEachRule(string password, bool expected)
    {
        Assert.Equal(expected, ValidateProperty(nameof(SignupInput.Password), password));
    }

    [Theory]
    [InlineData("chef", true)]
    [InlineData("Chef-01._@+", true)]
    [InlineData("chef name", false)]
    [InlineData("josé", false)]
    [InlineData("chef!", false)]
    [InlineData("", false)]
    public void Username_ConfiguredCharacters_RejectsDisallowedCharacters(string username, bool expected)
    {
        Assert.Equal(expected, ValidateProperty(nameof(SignupInput.Username), username));
    }

    [Theory]
    [InlineData("chef@example.com", true)]
    [InlineData("invalid", false)]
    [InlineData("", false)]
    public void Email_InvalidOrMissingAddress_ShowsError(string email, bool expected)
    {
        Assert.Equal(expected, ValidateProperty(nameof(SignupInput.Email), email));
    }

    [Fact]
    public void Validation_ChangedIdentityOptions_UsesConfiguredRules()
    {
        Assert.True(ValidateProperty(nameof(SignupInput.Password), "abcdefgh", options =>
        {
            options.Password.RequireUppercase = false;
            options.Password.RequireDigit = false;
            options.Password.RequireNonAlphanumeric = false;
        }));
        Assert.False(ValidateProperty(nameof(SignupInput.Password), "Abcdefg1!", options =>
            options.Password.RequiredLength = 12));
        Assert.False(ValidateProperty(nameof(SignupInput.Password), "Abcdefg1!", options =>
            options.Password.RequiredUniqueChars = 10));
        Assert.False(ValidateProperty(nameof(SignupInput.Username), "chef", options =>
            options.User.AllowedUserNameCharacters = "abc"));
    }

    private static bool ValidateProperty(string memberName, string value, Action<IdentityOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.Configure<IdentityOptions>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
            configure?.Invoke(options);
        });
        using var provider = services.BuildServiceProvider();
        var context = new ValidationContext(new SignupInput(), provider, null) { MemberName = memberName };
        return Validator.TryValidateProperty(value, context, new List<ValidationResult>());
    }
}
