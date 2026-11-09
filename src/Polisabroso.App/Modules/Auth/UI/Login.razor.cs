using Microsoft.AspNetCore.Components;

namespace Polisabroso.Modules.Auth.UI;

public partial class Login : ComponentBase
{
    private string _password = string.Empty;

    [SupplyParameterFromQuery(Name = "error")]
    public string? Error { get; set; }

    private string? ErrorMessage => Error switch
    {
        "credentials" =>
            "No se pudo iniciar sesión. Revisa tus credenciales y que tu cuenta esté habilitada y no esté bloqueada.",
        "unavailable" => "No se pudo iniciar sesión en este momento. Inténtalo de nuevo.",
        _ => null
    };

    private static void HandleRecoveryPlaceholder()
    {
    }
}