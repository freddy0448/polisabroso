using System.ComponentModel.DataAnnotations;
using Polisabroso.Modules.Auth.UI.Validation;

namespace Polisabroso.Modules.Auth.UI;

public sealed class SignupInput
{
    [Required(ErrorMessage = "Ingresa tu nombre.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa tu apellido.")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa tu nombre de usuario.")]
    [IdentityUsername]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa tu correo electrónico.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo electrónico válido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa tu número de teléfono.")]
    [Phone(ErrorMessage = "Ingresa un número de teléfono válido.")]
    [MaxLength(15)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa una contraseña.")]
    [IdentityPassword]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirma tu contraseña.")]
    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden. Revisa la confirmación.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}