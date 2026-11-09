using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Polisabroso.Modules.Auth.Api;

namespace Polisabroso.Modules.Auth.Internal;

internal static class AuthEndpoints
{
    internal static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Form binding requires antiforgery validation before the handler runs.
        endpoints.MapPost("/auth/sign-in", SignInAsync).AllowAnonymous();
        return endpoints;
    }

    internal static async Task<IResult> SignInAsync(
        [FromForm] string username,
        [FromForm] string password,
        [FromForm] bool? remember,
        IUserAuth userAuth,
        HttpContext context,
        IOptions<IdentityOptions> options,
        ILoggerFactory loggerFactory)
    {
        try
        {
            var result = await userAuth.SignIn(username.Trim(), password);
            if (result.IsFailed)
            {
                return Results.LocalRedirect("~/login?error=credentials");
            }

            var claimsOptions = options.Value.ClaimsIdentity;
            var identity = new ClaimsIdentity(result.Value.Claims, AuthConstants.ApplicationScheme,
                claimsOptions.UserNameClaimType, claimsOptions.RoleClaimType);
            await context.SignInAsync(AuthConstants.ApplicationScheme, new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = remember == true });

            // A new document load starts a Blazor circuit with the authenticated principal.
            return Results.LocalRedirect("~/");
        }
        catch (Exception exception)
        {
            loggerFactory.CreateLogger("Polisabroso.Modules.Auth.SignIn")
                .LogError(exception, "Unable to complete sign-in.");
            return Results.LocalRedirect("~/login?error=unavailable");
        }
    }
}