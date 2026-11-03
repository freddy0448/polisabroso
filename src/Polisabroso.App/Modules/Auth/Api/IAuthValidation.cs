namespace Polisabroso.Modules.Auth.Api;

public interface IAuthValidation
{
    Task<SignupAvailability> CheckSignupAvailability(string username, string email);
}
