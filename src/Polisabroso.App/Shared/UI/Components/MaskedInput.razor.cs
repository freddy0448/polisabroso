using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Polisabroso.Shared.UI.Components;

public partial class MaskedInput : IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <summary>
    /// Mask slots: 0 = ASCII digit, A = ASCII letter, * = either.
    /// Other characters are literals; prefix a slot character with a backslash to display it literally.
    /// Value contains only the characters entered into slots, without mask literals.
    /// </summary>
    [Parameter, EditorRequired] public string Mask { get; set; } = string.Empty;

    private ElementReference _inputElement;
    private IJSObjectReference? _module;
    private IJSObjectReference? _controller;
    private string? _appliedMask;
    private bool _disposed;
    private string InputCssClass => $"masked-input {CssClass}".TrimEnd();

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        TextMask.Validate(Mask);
    }

    protected override string FormatValueAsString(string? value) => TextMask.Format(value ?? string.Empty, Mask);

    protected override bool TryParseValueFromString(string? value, out string result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        result = TextMask.Unmask(value ?? string.Empty, Mask);
        validationErrorMessage = null;
        return true;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_disposed)
        {
            return;
        }

        _module ??= await JS.InvokeAsync<IJSObjectReference>("import", "./Shared/UI/Components/MaskedInput.razor.js");
        if (_disposed)
        {
            await _module.DisposeAsync();
            return;
        }

        if (_controller is null)
        {
            _controller = await _module.InvokeAsync<IJSObjectReference>("attach", _inputElement, Mask);
            _appliedMask = Mask;
        }
        else if (_appliedMask != Mask)
        {
            await _controller.InvokeVoidAsync("setMask", Mask);
            _appliedMask = Mask;
        }
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        ((IDisposable)this).Dispose();
        try
        {
            if (_controller is not null)
            {
                await _controller.InvokeVoidAsync("dispose");
                await _controller.DisposeAsync();
            }
            if (_module is not null)
            {
                await _module.DisposeAsync();
            }
        }
        catch (JSDisconnectedException)
        {
            // The circuit has already disconnected.
        }
    }
}
