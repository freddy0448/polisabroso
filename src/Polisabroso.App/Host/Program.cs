using Polisabroso.Host.Blazor;
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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();