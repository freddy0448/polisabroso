using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Polisabroso.Modules.Auth.Api;
using Polisabroso.Modules.Auth.Domain;
using Polisabroso.Modules.Auth.Internal;

namespace Polisabroso.Auth.Tests;

public class AuthValidationServiceTests
{
    private readonly AppUser _user = new() { Id = Guid.NewGuid(), UserName = "chef" };
    private readonly Mock<UserManager<AppUser>> _users;
    private readonly Mock<RoleManager<AppRole>> _roles;
    private readonly Mock<SignInManager<AppUser>> _signIn;
    private readonly AuthValidationService _session;

    public AuthValidationServiceTests()
    {
        var options = Options.Create(new IdentityOptions());
        _users = new Mock<UserManager<AppUser>>(
            Mock.Of<IUserStore<AppUser>>(), options, new PasswordHasher<AppUser>(),
            Array.Empty<IUserValidator<AppUser>>(), Array.Empty<IPasswordValidator<AppUser>>(),
            new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(),
            Mock.Of<IServiceProvider>(), Mock.Of<ILogger<UserManager<AppUser>>>());
        _roles = new Mock<RoleManager<AppRole>>(
            Mock.Of<IRoleStore<AppRole>>(), Array.Empty<IRoleValidator<AppRole>>(),
            new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(),
            Mock.Of<ILogger<RoleManager<AppRole>>>());

        var factory = new UserClaimsPrincipalFactory<AppUser, AppRole>(_users.Object, _roles.Object, options);
        _signIn = new Mock<SignInManager<AppUser>>(
                _users.Object, new HttpContextAccessor(), factory, options,
                Mock.Of<ILogger<SignInManager<AppUser>>>(), Mock.Of<IAuthenticationSchemeProvider>(),
                Mock.Of<IUserConfirmation<AppUser>>())
            { CallBase = true };

        _users.Setup(manager => manager.FindByNameAsync("chef")).ReturnsAsync(_user);
        _signIn.Setup(manager => manager.CheckPasswordSignInAsync(_user, It.IsAny<string>(), true))
            .ReturnsAsync(SignInResult.Success);
        _signIn.Setup(manager => manager.IsTwoFactorEnabledAsync(_user)).ReturnsAsync(false);
        _session = new AuthValidationService(_signIn.Object);
    }

    [Fact]
    public async Task CreateUser_ValidRequest_PersistsProfileAndReturnsIdWithoutSigningIn()
    {
        var request =
            new CreateUserRequest(" Ana ", " Pérez ", "ana", "ana@example.com", "+591 71234567", " Password1! ");
        _signIn.Invocations.Clear();
        AppUser? createdUser = null;
        _users.Setup(manager => manager.CreateAsync(It.IsAny<AppUser>(), request.Password))
            .Callback<AppUser, string>((user, _) => createdUser = user)
            .ReturnsAsync(IdentityResult.Success);

        var result = await _session.CreateUser(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(createdUser);
        Assert.NotEqual(Guid.Empty, result.Value);
        Assert.Equal(createdUser.Id, result.Value);
        Assert.Equal("Ana", createdUser.FirstName);
        Assert.Equal("Pérez", createdUser.LastName);
        Assert.Equal(request.Username, createdUser.UserName);
        Assert.Equal(request.Email, createdUser.Email);
        Assert.Equal(request.PhoneNumber, createdUser.PhoneNumber);
        Assert.False(createdUser.EmailConfirmed);
        Assert.False(createdUser.PhoneNumberConfirmed);
        _users.Verify(manager => manager.CreateAsync(It.IsAny<AppUser>(), request.Password), Times.Once);
        _signIn.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("DuplicateUserName", "Este nombre de usuario ya está en uso.")]
    [InlineData("DuplicateEmail", "Este correo electrónico ya está registrado.")]
    [InlineData("PasswordRequiresDigit", "La contraseña debe contener un número (0-9).")]
    public async Task CreateUser_IdentityRejectsRequest_ReturnsTranslatedError(string code, string message)
    {
        _users.Setup(manager => manager.CreateAsync(It.IsAny<AppUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = code, Description = "Identity rejection" }));

        var result =
            await _session.CreateUser(new CreateUserRequest("Ana", "Pérez", "ana", "ana@example.com", "71234567",
                "Password1!"));

        Assert.True(result.IsFailed);
        Assert.Equal(message, result.Errors.Single().Message);
        Assert.Equal(code, result.Errors.Single().Metadata["Code"]);
    }

    [Theory]
    [InlineData("", "ana@example.com", "71234567", "Password1!")]
    [InlineData("Ana", "invalid", "71234567", "Password1!")]
    [InlineData("Ana", "ana@example.com", "invalid", "Password1!")]
    [InlineData("Ana", "ana@example.com", "71234567", "")]
    public async Task CreateUser_InvalidProfile_DoesNotCallIdentity(string firstName, string email, string phone,
        string password)
    {
        var result =
            await _session.CreateUser(new CreateUserRequest(firstName, "Pérez", "ana", email, phone, password));

        Assert.True(result.IsFailed);
        _users.Verify(manager => manager.CreateAsync(It.IsAny<AppUser>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task Signup_ExistingUsernameOrEmail_ReturnsAvailability(bool usernameExists, bool emailExists)
    {
        _users.Object.Options.User.RequireUniqueEmail = true;
        _users.Setup(manager => manager.FindByNameAsync("chef"))
            .ReturnsAsync(usernameExists ? _user : null);
        _users.Setup(manager => manager.FindByEmailAsync("chef@example.com"))
            .ReturnsAsync(emailExists ? _user : null);

        var result = await _session.CheckSignupAvailability("chef", "chef@example.com");

        Assert.Equal(!usernameExists, result.UsernameAvailable);
        Assert.Equal(!emailExists, result.EmailAvailable);
        _signIn.Verify(manager => manager.CheckPasswordSignInAsync(
            It.IsAny<AppUser>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task Signup_UniqueEmailDisabled_SkipsEmailLookup()
    {
        _users.Object.Options.User.RequireUniqueEmail = false;

        var result = await _session.CheckSignupAvailability("new-chef", "chef@example.com");

        Assert.True(result.UsernameAvailable);
        Assert.True(result.EmailAvailable);
        _users.Verify(manager => manager.FindByEmailAsync(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("", "password")]
    [InlineData(" ", "password")]
    [InlineData("chef", "")]
    public async Task Login_EmptyCredentials_ReturnsFailureWithoutCheckingPassword(string username, string password)
    {
        var result = await _session.SignIn(username, password);

        Assert.True(result.IsFailed);
        _signIn.Verify(manager => manager.CheckPasswordSignInAsync(
            It.IsAny<AppUser>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task Login_UnknownUser_ReturnsSameErrorAsIncorrectPassword()
    {
        _signIn.Setup(manager => manager.CheckPasswordSignInAsync(_user, "wrong", true))
            .ReturnsAsync(SignInResult.Failed);

        var unknownUser = await _session.SignIn("unknown", "wrong");
        var incorrectPassword = await _session.SignIn("chef", "wrong");

        Assert.True(unknownUser.IsFailed);
        Assert.True(incorrectPassword.IsFailed);
        Assert.Equal(unknownUser.Errors.Single().Message, incorrectPassword.Errors.Single().Message);
        _signIn.Verify(manager => manager.CreateUserPrincipalAsync(It.IsAny<AppUser>()), Times.Never);
    }

    [Theory]
    [InlineData(true, "La cuenta está bloqueada temporalmente. Inténtalo más tarde.")]
    [InlineData(false, "La cuenta no está habilitada para iniciar sesión.")]
    public async Task Login_RestrictedAccount_ReturnsFailureWithoutClaims(bool lockedOut, string message)
    {
        _signIn.Setup(manager => manager.CheckPasswordSignInAsync(_user, "password", true))
            .ReturnsAsync(lockedOut ? SignInResult.LockedOut : SignInResult.NotAllowed);

        var result = await _session.SignIn("chef", "password");

        Assert.True(result.IsFailed);
        Assert.Equal(message, result.Errors.Single().Message);
        _signIn.Verify(manager => manager.CreateUserPrincipalAsync(It.IsAny<AppUser>()), Times.Never);
    }

    [Fact]
    public async Task Login_TwoFactorEnabled_ReturnsFailureWithoutClaims()
    {
        _signIn.Setup(manager => manager.IsTwoFactorEnabledAsync(_user)).ReturnsAsync(true);

        var result = await _session.SignIn("chef", "password");

        Assert.True(result.IsFailed);
        Assert.Equal("Se requiere autenticación de dos factores.", result.Errors.Single().Message);
        _signIn.Verify(manager => manager.CreateUserPrincipalAsync(It.IsAny<AppUser>()), Times.Never);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Login_EmailForExistingAccount_DoesNotAuthenticateByEmail(bool requireUniqueEmail)
    {
        _users.Object.Options.User.RequireUniqueEmail = requireUniqueEmail;
        _users.Setup(manager => manager.FindByEmailAsync("chef@example.com")).ReturnsAsync(_user);

        var result = await _session.SignIn("chef@example.com", "password");

        Assert.True(result.IsFailed);
        _users.Verify(manager => manager.FindByNameAsync("chef@example.com"), Times.Once);
        _users.Verify(manager => manager.FindByEmailAsync(It.IsAny<string>()), Times.Never);
        _signIn.Verify(manager => manager.CheckPasswordSignInAsync(
            It.IsAny<AppUser>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsAllRolesAndUserAndRoleClaims()
    {
        _users.Setup(manager => manager.SupportsUserRole).Returns(true);
        _users.Setup(manager => manager.SupportsUserClaim).Returns(true);
        _users.Setup(manager => manager.GetUserIdAsync(_user)).ReturnsAsync(_user.Id.ToString());
        _users.Setup(manager => manager.GetUserNameAsync(_user)).ReturnsAsync(_user.UserName);
        _users.Setup(manager => manager.GetRolesAsync(_user)).ReturnsAsync(new[] { "Chef", "Manager" });
        _users.Setup(manager => manager.GetClaimsAsync(_user))
            .ReturnsAsync(new[] { new Claim("location", "La Paz"), new Claim("permission", "orders.read") });
        _roles.Setup(manager => manager.SupportsRoleClaims).Returns(true);
        foreach (var name in new[] { "Chef", "Manager" })
        {
            var role = new AppRole { Name = name };
            _roles.Setup(manager => manager.FindByNameAsync(name)).ReturnsAsync(role);
            _roles.Setup(manager => manager.GetClaimsAsync(role))
                .ReturnsAsync(new[] { new Claim("permission", $"{name}.access") });
        }

        // Passwords must be passed unchanged, including whitespace.
        var result = await _session.SignIn("chef", " password ");

        Assert.True(result.IsSuccess);
        Assert.Equal(_user.Id, result.Value.UserId);
        Assert.Equal("chef", result.Value.UserName);
        Assert.Equal(new[] { "Chef", "Manager" }, result.Value.Roles);
        Assert.Contains(result.Value.Claims,
            claim => claim.Type == ClaimTypes.NameIdentifier && claim.Value == _user.Id.ToString());
        Assert.Contains(result.Value.Claims, claim => claim.Type == "location" && claim.Value == "La Paz");
        Assert.Contains(result.Value.Claims, claim => claim.Type == "permission" && claim.Value == "orders.read");
        Assert.Contains(result.Value.Claims, claim => claim.Type == "permission" && claim.Value == "Chef.access");
        Assert.Contains(result.Value.Claims, claim => claim.Type == "permission" && claim.Value == "Manager.access");
        Assert.Equal(2, result.Value.Claims.Count(claim => claim.Type == ClaimTypes.Role));
        _signIn.Verify(manager => manager.CheckPasswordSignInAsync(_user, " password ", true), Times.Once);
    }
}