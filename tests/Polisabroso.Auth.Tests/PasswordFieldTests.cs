using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polisabroso.Shared.UI.Components;

namespace Polisabroso.Auth.Tests;

public class PasswordFieldTests
{
    [Fact]
    public async Task Render_EventHandlers_AreRegisteredInsteadOfRenderedAsLiteralAttributes()
    {
        using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var component = await renderer.RenderComponentAsync<PasswordField>(ParameterView.FromDictionary(
                new Dictionary<string, object?>
                {
                    [nameof(PasswordField.Id)] = "password",
                    [nameof(PasswordField.Name)] = "password",
                    [nameof(PasswordField.Label)] = "Contraseña"
                }));
            return component.ToHtmlString();
        });

        Assert.DoesNotContain("@onclick", html);
        Assert.DoesNotContain("@oninput", html);
    }
}
