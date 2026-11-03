using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Polisabroso.Shared.UI.Components;

public partial class PasswordField : ComponentBase
{
    private bool _visible;

    [CascadingParameter] private EditContext? EditContext { get; set; }
    [Parameter, EditorRequired] public string Id { get; set; } = string.Empty;
    [Parameter, EditorRequired] public string Name { get; set; } = string.Empty;
    [Parameter, EditorRequired] public string Label { get; set; } = string.Empty;
    [Parameter] public string Placeholder { get; set; } = string.Empty;
    [Parameter] public string Autocomplete { get; set; } = "new-password";
    [Parameter] public string? Hint { get; set; }
    [Parameter] public string Value { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> ValueChanged { get; set; }
    [Parameter] public Expression<Func<string>>? ValueExpression { get; set; }

    private bool HasErrors => EditContext is not null && ValueExpression is not null &&
        EditContext.GetValidationMessages(FieldIdentifier.Create(ValueExpression)).Any();
    private string? DescriptionIds
    {
        get
        {
            var hintId = string.IsNullOrEmpty(Hint) ? null : $"{Id}-hint";
            var errorId = EditContext is not null && ValueExpression is not null ? $"{Id}-error" : null;
            var ids = string.Join(" ", new[] { hintId, errorId }.Where(id => id is not null));
            return ids.Length > 0 ? ids : null;
        }
    }
    private string ToggleLabel => $"{(_visible ? "Ocultar" : "Mostrar")} {Label.ToLowerInvariant()}";

    private void ToggleVisibility() => _visible = !_visible;

    private async Task HandleInput(ChangeEventArgs args)
    {
        await ValueChanged.InvokeAsync(args.Value?.ToString() ?? string.Empty);
        if (EditContext is not null && ValueExpression is not null)
        {
            EditContext.NotifyFieldChanged(FieldIdentifier.Create(ValueExpression));
        }
    }
}
