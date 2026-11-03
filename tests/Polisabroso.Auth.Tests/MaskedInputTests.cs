using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Moq;
using Polisabroso.Shared.UI.Components;

namespace Polisabroso.Auth.Tests;

// The test harness dispatches real Blazor events through a minimal renderer.
#pragma warning disable BL0006
public class MaskedInputTests
{
    [Theory]
    [InlineData("7123 4567", "71234567")]
    [InlineData("71", "71")]
    [InlineData("", "")]
    public async Task InputEvent_UpdatesBoundModel_WithRawValue(string displayedValue, string expectedValue)
    {
        var model = new PhoneModel { Phone = "1234" };
        var controller = new Mock<IJSObjectReference>();
        var module = new Mock<IJSObjectReference>();
        module.Setup(reference => reference.InvokeAsync<IJSObjectReference>("attach", It.IsAny<object?[]>()))
            .ReturnsAsync(controller.Object);
        var js = new Mock<IJSRuntime>();
        js.Setup(runtime => runtime.InvokeAsync<IJSObjectReference>("import", It.IsAny<object?[]>()))
            .ReturnsAsync(module.Object);
        using var services = new ServiceCollection().AddLogging().AddSingleton(js.Object).BuildServiceProvider();
        await using var renderer = new InputRenderer(services);

        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var id = await renderer.RenderInputAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(MaskedInput.Mask)] = "0000 0000",
                [nameof(MaskedInput.Value)] = model.Phone,
                [nameof(MaskedInput.ValueChanged)] = EventCallback.Factory.Create<string>(model, value => model.Phone = value),
                [nameof(MaskedInput.ValueExpression)] = (Expression<Func<string>>)(() => model.Phone)
            }));

            await renderer.DispatchEventAsync(renderer.InputHandler(id), null, new ChangeEventArgs { Value = displayedValue });

            Assert.Equal(expectedValue, model.Phone);
        });
    }

    [Fact]
    public async Task Render_BoundValue_FormatsValueAndRegistersInputBinding()
    {
        var model = new PhoneModel { Phone = "71234567" };
        using var services = new ServiceCollection().AddLogging()
            .AddSingleton(Mock.Of<IJSRuntime>()).BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var component = await renderer.RenderComponentAsync<MaskedInput>(ParameterView.FromDictionary(
                new Dictionary<string, object?>
                {
                    [nameof(MaskedInput.Mask)] = "0000 0000",
                    [nameof(MaskedInput.Value)] = model.Phone,
                    [nameof(MaskedInput.ValueExpression)] = (Expression<Func<string>>)(() => model.Phone)
                }));
            return component.ToHtmlString();
        });

        Assert.Contains("value=\"7123 4567\"", html);
        Assert.DoesNotContain("@bind", html);
    }

    private sealed class PhoneModel
    {
        public string Phone { get; set; } = string.Empty;
    }

    // Dispatch the real rendered Blazor event without starting the application or a database.
    private sealed class InputRenderer(IServiceProvider services)
        : Renderer(services, services.GetRequiredService<ILoggerFactory>())
    {
        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

        public async Task<int> RenderInputAsync(ParameterView parameters)
        {
            var id = AssignRootComponentId(InstantiateComponent(typeof(MaskedInput)));
            await RenderRootComponentAsync(id, parameters);
            return id;
        }

        public ulong InputHandler(int componentId)
        {
            var frames = GetCurrentRenderTreeFrames(componentId);
            return frames.Array.Take(frames.Count).Single(frame =>
                frame.FrameType == RenderTreeFrameType.Attribute && frame.AttributeName == "oninput").AttributeEventHandlerId;
        }

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) => Task.CompletedTask;
        protected override void HandleException(Exception exception) =>
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception).Throw();
    }
}
#pragma warning restore BL0006
