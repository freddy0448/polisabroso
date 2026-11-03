using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Polisabroso.Modules.Auth.UI.Validation;

public sealed class IdentityPasswordAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string password || string.IsNullOrEmpty(password))
        {
            return ValidationResult.Success;
        }

        var options = validationContext.GetRequiredService<IOptions<IdentityOptions>>().Value.Password;
        var errors = GetMissingRequirements(password, options);
        return errors.Count == 0
            ? ValidationResult.Success
            : new ValidationResult($"La contraseña debe contener {string.Join(", ", errors)}.");
    }

    internal static List<string> GetMissingRequirements(string password, PasswordOptions options)
    {
        var errors = new List<string>();
        if (password.Length < options.RequiredLength)
        {
            errors.Add($"al menos {options.RequiredLength} caracteres");
        }
        if (options.RequireLowercase && !password.Any(character => character is >= 'a' and <= 'z'))
        {
            errors.Add("una letra minúscula (a-z)");
        }
        if (options.RequireUppercase && !password.Any(character => character is >= 'A' and <= 'Z'))
        {
            errors.Add("una letra mayúscula (A-Z)");
        }
        if (options.RequireDigit && !password.Any(character => character is >= '0' and <= '9'))
        {
            errors.Add("un número (0-9)");
        }
        if (options.RequireNonAlphanumeric && password.All(character =>
                character is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9')))
        {
            errors.Add("un símbolo");
        }
        if (password.Distinct().Count() < options.RequiredUniqueChars)
        {
            errors.Add($"al menos {options.RequiredUniqueChars} caracteres distintos");
        }

        return errors;
    }
}
