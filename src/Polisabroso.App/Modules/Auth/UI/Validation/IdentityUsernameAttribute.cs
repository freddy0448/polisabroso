using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Polisabroso.Modules.Auth.UI.Validation;

public sealed class IdentityUsernameAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string username || string.IsNullOrWhiteSpace(username))
        {
            return ValidationResult.Success;
        }

        var options = validationContext.GetRequiredService<IOptions<IdentityOptions>>().Value.User;
        var allowed = options.AllowedUserNameCharacters;
        return !string.IsNullOrEmpty(allowed) && username.Any(character => !allowed.Contains(character))
            ? new ValidationResult($"El nombre de usuario solo puede contener estos caracteres: {allowed}.")
            : ValidationResult.Success;
    }
}
