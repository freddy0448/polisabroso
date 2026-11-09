using System.Security.Claims;
using FluentResults;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Polisabroso.Modules.Auth.Api;
using Polisabroso.Modules.Auth.Internal;

namespace Polisabroso.Auth.Tests;

public class AuthEndpointsTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [InlineData(null)]
    public async Task Login_ValidCredentials_IssuesCookieWithClaimsAndRequestedPersistence(bool? remember)
    {
        var auth = new Mock<IUserAuth>();
        var claims = new[]
            { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()), new Claim(ClaimTypes.Role, "Chef") };
        auth.Setup(service => service.SignIn("chef", " password "))
            .ReturnsAsync(Result.Ok(new AuthSession(Guid.NewGuid(), "chef", ["Chef"], claims)));
        var authentication = new Mock<IAuthenticationService>();
        using var services = new ServiceCollection().AddSingleton(authentication.Object).BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };

        var result = await AuthEndpoints.SignInAsync(" chef ", " password ", remember,
            auth.Object, context, Options.Create(new IdentityOptions()), NullLoggerFactory.Instance);

        Assert.Equal("~/", Assert.IsType<RedirectHttpResult>(result).Url);
        authentication.Verify(service => service.SignInAsync(context, AuthConstants.ApplicationScheme,
            It.Is<ClaimsPrincipal>(principal => principal.Identity!.IsAuthenticated && principal.IsInRole("Chef") &&
                                                principal.FindFirst(ClaimTypes.NameIdentifier)!.Value ==
                                                claims[0].Value),
            It.Is<AuthenticationProperties>(properties => properties.IsPersistent == (remember == true))), Times.Once);
    }

    [Theory]
    [InlineData(false, "credentials")]
    [InlineData(true, "unavailable")]
    public async Task Login_Failure_DoesNotIssueCookie(bool throws, string error)
    {
        var auth = new Mock<IUserAuth>();
        var setup = auth.Setup(service => service.SignIn("chef", "wrong"));
        if (throws)
        {
            setup.ThrowsAsync(new InvalidOperationException("Unavailable"));
        }
        else
        {
            setup.ReturnsAsync(Result.Fail<AuthSession>("Invalid credentials"));
        }

        var authentication = new Mock<IAuthenticationService>();
        using var services = new ServiceCollection().AddSingleton(authentication.Object).BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };

        var result = await AuthEndpoints.SignInAsync("chef", "wrong", true,
            auth.Object, context, Options.Create(new IdentityOptions()), NullLoggerFactory.Instance);

        Assert.Equal($"~/login?error={error}", Assert.IsType<RedirectHttpResult>(result).Url);
        authentication.VerifyNoOtherCalls();
    }
}