using FluentResults;

namespace Polisabroso.Modules.Auth.Api;

public interface IUserAuth
{
    /// <summary>
    /// Creates an unconfirmed account without signing in and returns its user ID.
    /// </summary>
    Task<Result<Guid>> CreateUser(CreateUserRequest request);

    /// <summary>
    /// Validates a username and password and returns session data without issuing an authentication cookie.
    /// </summary>
    Task<Result<AuthSession>> SignIn(string username, string password);
}