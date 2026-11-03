using System.Security.Claims;

namespace Polisabroso.Modules.Auth.Api;

public sealed record AuthSession(
    Guid UserId,
    string UserName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<Claim> Claims);
