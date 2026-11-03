using FluentResults;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Polisabroso.Modules.Auth.Api;
using Polisabroso.Modules.Auth.UI.Validation;

namespace Polisabroso.Modules.Auth.UI.Components;

public partial class SignupForm : ComponentBase, IDisposable
{
    [Inject] private IAuthValidation AuthValidation { get; set; } = default!;
    [Inject] private IUserAuth UserAuth { get; set; } = default!;
    [Inject] private IOptions<IdentityOptions> IdentityOptions { get; set; } = default!;
    [Inject] private ILogger<SignupForm> Logger { get; set; } = default!;

    private string PasswordHint => "La contraseña debe contener " +
        string.Join(", ", IdentityPasswordAttribute.GetMissingRequirements(string.Empty, IdentityOptions.Value.Password)) + ".";

    private readonly SignupInput _input = new();
    private EditContext _editContext = default!; //todo check class
    private ElementReference _feedbackElement;
    private string _feedback = string.Empty;
    private FeedbackKind _feedbackKind = FeedbackKind.Info;
    private string FeedbackClass => $"signup-form__feedback signup-form__feedback--{_feedbackKind.ToString().ToLowerInvariant()}";
    private string FeedbackTitle => _feedbackKind switch
    {
        FeedbackKind.Warning => "Revisa tus datos",
        FeedbackKind.Error => "No pudimos continuar",
        FeedbackKind.Success => "Cuenta creada",
        _ => "Información"
    };
    private bool _focusFeedback;
    private ValidationMessageStore _availabilityMessages = default!; //todo check class
    private bool _isSubmitting;

    protected override void OnInitialized()
    {
        _editContext = new EditContext(_input);
        _availabilityMessages = new ValidationMessageStore(_editContext);
        _editContext.OnFieldChanged += HandleFieldChanged;
    }

    private string InvalidState(string propertyName) =>
        _editContext.GetValidationMessages(_editContext.Field(propertyName)).Any() ? "true" : "false";

    private void HandleFieldChanged(object? sender, FieldChangedEventArgs args)
    {
        _feedback = string.Empty;
        _availabilityMessages.Clear(args.FieldIdentifier);
        _editContext.NotifyValidationStateChanged();
        // Changing the original password must also revalidate its confirmation.
        if (args.FieldIdentifier.FieldName == nameof(SignupInput.Password) && _input.ConfirmPassword.Length > 0)
        {
            _editContext.NotifyFieldChanged(_editContext.Field(nameof(SignupInput.ConfirmPassword)));
        }
    }

    private async Task HandleValidSubmit()
    {
        if (_isSubmitting)
        {
            return;
        }

        _isSubmitting = true;
        _availabilityMessages.Clear();
        var username = _input.Username;
        var email = _input.Email;
        try
        {
            var availability = await AuthValidation.CheckSignupAvailability(username, email);

            // Ignore results for values edited while the database queries were running.
            if (username != _input.Username || email != _input.Email)
            {
                return;
            }

            if (!availability.UsernameAvailable)
            {
                _availabilityMessages.Add(_editContext.Field(nameof(SignupInput.Username)), "Este nombre de usuario ya está en uso.");
            }
            if (!availability.EmailAvailable)
            {
                _availabilityMessages.Add(_editContext.Field(nameof(SignupInput.Email)), "Este correo electrónico ya está registrado.");
            }
            _editContext.NotifyValidationStateChanged();

            if (!_editContext.Validate())
            {
                HandleInvalidSubmit();
                return;
            }

            var request = new CreateUserRequest(
                _input.FirstName, _input.LastName, username, email, _input.PhoneNumber, _input.Password);
            var result = await UserAuth.CreateUser(request);
            if (result.IsFailed)
            {
                foreach (var error in result.Errors)
                {
                    AddCreationError(error);
                }
                _editContext.NotifyValidationStateChanged();
                _feedback = string.Join(" ", result.Errors.Select(error => error.Message).Distinct());
                _feedbackKind = FeedbackKind.Error;
                _focusFeedback = true;
                return;
            }

            _input.Password = string.Empty;
            _input.ConfirmPassword = string.Empty;
            _feedback = "Tu cuenta se creó correctamente. Debe confirmarse antes de que puedas iniciar sesión.";
            _feedbackKind = FeedbackKind.Success;
            _focusFeedback = true;
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Unable to complete signup.");
            _feedback = "No se pudo completar el registro. Inténtalo de nuevo.";
            _feedbackKind = FeedbackKind.Error;
            _focusFeedback = true;
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private void HandleInvalidSubmit()
    {
        _feedback = "Revisa los campos indicados antes de crear tu cuenta.";
        _feedbackKind = FeedbackKind.Warning;
        _focusFeedback = true;
    }

    private void AddCreationError(IError error)
    {
        if (error.Metadata.TryGetValue("Fields", out var fields) && fields is IEnumerable<string> fieldNames)
        {
            foreach (var fieldName in fieldNames)
            {
                _availabilityMessages.Add(_editContext.Field(fieldName), error.Message);
            }
        }

        if (!error.Metadata.TryGetValue("Code", out var code))
        {
            return;
        }

        var field = (code as string) switch
        {
            "DuplicateUserName" or "InvalidUserName" => nameof(SignupInput.Username),
            "DuplicateEmail" or "InvalidEmail" => nameof(SignupInput.Email),
            string passwordCode when passwordCode.StartsWith("Password", StringComparison.Ordinal) => nameof(SignupInput.Password),
            _ => null
        };
        if (field is not null)
        {
            _availabilityMessages.Add(_editContext.Field(field), error.Message);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_focusFeedback)
        {
            _focusFeedback = false;
            await _feedbackElement.FocusAsync();
        }
    }

    public void Dispose() => _editContext.OnFieldChanged -= HandleFieldChanged;

    private enum FeedbackKind
    {
        Info,
        Warning,
        Error,
        Success
    }
}
