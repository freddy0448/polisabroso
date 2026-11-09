using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Polisabroso.Host.Blazor;
using Polisabroso.Modules.Auth.Api;
using Polisabroso.Modules.Auth.Domain;
using Polisabroso.Modules.Auth.Internal;
using Polisabroso.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var connString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddSqlServer<AppDbContext>(connString, null, contextActions =>
{
    contextActions.EnableDetailedErrors(builder.Environment.IsDevelopment());
    contextActions.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
});

builder.Services.AddCascadingAuthenticationState();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = AuthConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();
builder.Services.AddIdentityCore<AppUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;

        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;

        options.ClaimsIdentity.EmailClaimType = ClaimTypes.Email;
        options.ClaimsIdentity.RoleClaimType = ClaimTypes.Role;
        options.ClaimsIdentity.UserIdClaimType = ClaimTypes.NameIdentifier;
        options.ClaimsIdentity.UserNameClaimType = ClaimTypes.Name;

        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.Password.RequiredLength = 8;

        options.User.RequireUniqueEmail = true;
        options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    })
    .AddRoles<AppRole>()
    .AddRoleManager<RoleManager<AppRole>>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager<SignInManager<AppUser>>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<AuthValidationService>();
builder.Services.AddScoped<IUserAuth>(services => services.GetRequiredService<AuthValidationService>());
builder.Services.AddScoped<IAuthValidation>(services => services.GetRequiredService<AuthValidationService>());

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    app.Logger.LogInformation("Aplicando las migraciones pendientes de la base de datos...");
    try
    {
        await dbContext.Database.MigrateAsync();
        app.Logger.LogInformation("Base de datos actualizada. Todas las migraciones están aplicadas.");
    }
    catch (Exception exception)
    {
        app.Logger.LogCritical(exception,
            "No se pudieron aplicar las migraciones. Se detiene el inicio de la aplicación.");
        throw;
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapAuthEndpoints();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();