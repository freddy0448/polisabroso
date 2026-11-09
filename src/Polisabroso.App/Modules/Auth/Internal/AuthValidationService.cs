using System.ComponentModel.DataAnnotations;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Polisabroso.Modules.Auth.Api;
using Polisabroso.Modules.Auth.Domain;

namespace Polisabroso.Modules.Auth.Internal;

internal sealed class AuthValidationService(SignInManager<AppUser> signInManager) : IUserAuth, IAuthValidation
{
    private readonly SignInManager<AppUser> _signInManager = signInManager;

    public async Task<Result<Guid>> CreateUser(CreateUserRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validationResults = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, new ValidationContext(request), validationResults,
                validateAllProperties: true))
        {
            return Result.Fail<Guid>(validationResults.Select(result =>
                new Error(result.ErrorMessage!).WithMetadata("Fields", result.MemberNames.ToArray())));
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            UserName = request.Username,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber
        };

        // Identity validates username/email uniqueness and password policy, then hashes the password.
        var result = await _signInManager.UserManager.CreateAsync(user, request.Password);
        return result.Succeeded
            ? Result.Ok(user.Id)
            : Result.Fail<Guid>(result.Errors.Select(error => new Error(error.Code switch
            {
                "DuplicateUserName" => "Este nombre de usuario ya está en uso.",
                "DuplicateEmail" => "Este correo electrónico ya está registrado.",
                "InvalidUserName" => "El nombre de usuario contiene caracteres no permitidos.",
                "InvalidEmail" => "Ingresa un correo electrónico válido.",
                "PasswordTooShort" =>
                    $"La contraseña debe tener al menos {_signInManager.UserManager.Options.Password.RequiredLength} caracteres.",
                "PasswordRequiresDigit" => "La contraseña debe contener un número (0-9).",
                "PasswordRequiresLower" => "La contraseña debe contener una letra minúscula (a-z).",
                "PasswordRequiresUpper" => "La contraseña debe contener una letra mayúscula (A-Z).",
                "PasswordRequiresNonAlphanumeric" => "La contraseña debe contener un símbolo.",
                "PasswordRequiresUniqueChars" =>
                    $"La contraseña debe contener al menos {_signInManager.UserManager.Options.Password.RequiredUniqueChars} caracteres distintos.",
                _ => "No se pudo crear la cuenta. Inténtalo de nuevo."
            }).WithMetadata("Code", error.Code)));
    }

    public async Task<SignupAvailability> CheckSignupAvailability(string username, string email)
    {
        var userManager = _signInManager.UserManager;
        // The Identity store shares a scoped DbContext, so queries must run sequentially.
        var existingUsername = await userManager.FindByNameAsync(username);
        var existingEmail = userManager.Options.User.RequireUniqueEmail
            ? await userManager.FindByEmailAsync(email)
            : null;

        return new SignupAvailability(existingUsername is null, existingEmail is null);
    }

    public async Task<Result<AuthSession>> SignIn(string username, string password)
    {
        const string invalidCredentials = "Usuario o contraseña incorrectos.";

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
        {
            return Result.Fail<AuthSession>(invalidCredentials);
        }

        var userManager = _signInManager.UserManager;
        var user = await userManager.FindByNameAsync(username);
        if (user is null)
        {
            return Result.Fail<AuthSession>(invalidCredentials);
        }

        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        if (signInResult.IsLockedOut)
        {
            return Result.Fail<AuthSession>("La cuenta está bloqueada temporalmente. Inténtalo más tarde.");
        }

        if (signInResult.IsNotAllowed)
        {
            return Result.Fail<AuthSession>("La cuenta no está habilitada para iniciar sesión.");
        }

        if (!signInResult.Succeeded)
        {
            return Result.Fail<AuthSession>(invalidCredentials);
        }

        // Password validation alone must not bypass an account's second factor.
        if (await _signInManager.IsTwoFactorEnabledAsync(user))
        {
            return Result.Fail<AuthSession>("Se requiere autenticación de dos factores.");
        }

        var roles = await userManager.GetRolesAsync(user);
        // Identity's role-aware factory includes user claims and claims inherited from roles.
        var principal = await _signInManager.CreateUserPrincipalAsync(user);

        return Result.Ok(new AuthSession(
            user.Id,
            user.UserName!,
            [.. roles],
            [.. principal.Claims]));
    }
}