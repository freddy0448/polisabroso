using System.ComponentModel.DataAnnotations;

namespace Polisabroso.Modules.Auth.Api;

public sealed record CreateUserRequest(
    [property: Required(ErrorMessage = "Ingresa tu nombre.")] string FirstName,
    [property: Required(ErrorMessage = "Ingresa tu apellido.")] string LastName,
    [property: Required(ErrorMessage = "Ingresa tu nombre de usuario.")] string Username,
    [property: Required(ErrorMessage = "Ingresa tu correo electrónico.")]
    [property: EmailAddress(ErrorMessage = "Ingresa un correo electrónico válido.")] string Email,
    [property: Required(ErrorMessage = "Ingresa tu número de teléfono.")]
    [property: Phone(ErrorMessage = "Ingresa un número de teléfono válido.")] string PhoneNumber,
    [property: Required(ErrorMessage = "Ingresa una contraseña.")] string Password)
{
    // Record-generated ToString would otherwise include the plaintext password.
    public override string ToString() => nameof(CreateUserRequest);
}
