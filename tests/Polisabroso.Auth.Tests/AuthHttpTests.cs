using System.Net;
using System.Security.Claims;
using FluentResults;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Polisabroso.Modules.Auth.Api;
using Polisabroso.Modules.Auth.Internal;

namespace Polisabroso.Auth.Tests;

public class AuthHttpTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Login_FormPost_RequiresAntiforgeryAndAuthenticatesNextRequest(bool remember)
    {
        var auth = new Mock<IUserAuth>();
        auth.Setup(service => service.SignIn("chef", "Password1!"))
            .ReturnsAsync(Result.Ok(new AuthSession(Guid.NewGuid(), "chef", [],
                [new Claim(ClaimTypes.Name, "chef")])));
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddAntiforgery();
        builder.Services.AddAuthentication(AuthConstants.ApplicationScheme)
            .AddCookie(AuthConstants.ApplicationScheme);
        builder.Services.AddAuthorization();
        builder.Services.Configure<IdentityOptions>(_ => { });
        builder.Services.AddSingleton(auth.Object);

        await using var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();
        app.MapAuthEndpoints();
        app.MapGet("/token", (HttpContext context, IAntiforgery antiforgery) =>
            antiforgery.GetAndStoreTokens(context).RequestToken!);
        app.MapGet("/session", (HttpContext context) => context.User.Identity!.Name!).RequireAuthorization();
        await app.StartAsync();

        using var handler = new HttpClientHandler { AllowAutoRedirect = false };
        using var client = new HttpClient(handler) { BaseAddress = new Uri(app.Urls.Single()) };
        var fields = new Dictionary<string, string>
        {
            ["username"] = "chef",
            ["password"] = "Password1!"
        };
        if (remember)
        {
            fields["remember"] = "true";
        }

        using var rejected = await client.PostAsync("/auth/sign-in", new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        auth.Verify(service => service.SignIn(It.IsAny<string>(), It.IsAny<string>()), Times.Never);

        fields["__RequestVerificationToken"] = await client.GetStringAsync("/token");
        using var accepted = await client.PostAsync("/auth/sign-in", new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.Redirect, accepted.StatusCode);
        Assert.Equal("/", accepted.Headers.Location!.OriginalString);
        var cookie = Assert.Single(accepted.Headers.GetValues("Set-Cookie"),
            value => value.StartsWith(".AspNetCore.Identity.Application="));
        Assert.Equal(remember, cookie.Contains("expires=", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("chef", await client.GetStringAsync("/session"));
    }
}