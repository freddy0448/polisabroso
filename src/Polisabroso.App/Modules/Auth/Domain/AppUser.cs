using Microsoft.AspNetCore.Identity;

namespace Polisabroso.Modules.Auth.Domain;

public class AppUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}
