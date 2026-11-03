# Masked text input

`MaskedInput` is a shared Blazor input in `Polisabroso.Shared.UI.Components`.
It uses `InputBase<string>` and supports `EditForm`, `ValidationMessage`,
`@bind-Value`, and standard HTML attributes such as `id`, `name`, `class`,
`placeholder`, `autocomplete`, `inputmode`, `required`, `disabled`, and `readonly`.

```razor
@using Polisabroso.Shared.UI.Components

<label for="phone">Teléfono</label>
<MaskedInput id="phone" name="phone" Mask="0000 0000"
             inputmode="tel" autocomplete="tel" placeholder="7123 4567"
             aria-describedby="phone-error" @bind-Value="_input.PhoneNumber" />
<div id="phone-error">
    <ValidationMessage For="() => _input.PhoneNumber" />
</div>
```

Typing `71234567` displays `7123 4567`; the bound value remains `71234567`.
The surrounding page supplies the label, validation messages, and visual styling.

| Pattern | Meaning |
| --- | --- |
| `0` | Digit (`0–9`) |
| `A` | Letter (`a–z`, `A–Z`) |
| `*` | ASCII letter or digit |
| Other characters | Literal separators or prefixes |
| `\` before a character | Treat the following character as a literal |

Examples: `00/00/0000`, `AA-000`, `*** ***`, and `+591 0000 0000`.
In Razor markup, `Mask="\A-000"` displays a literal `A` prefix.

Formatting happens in the browser as the user types, pastes, or deletes.
Initial and programmatic values are formatted during Blazor rendering.
Interactive rendering is required for live masking. Without interactivity,
only the initial formatted value is displayed.

Masks control character shape, not business validity: partial input is allowed,
excess characters are discarded, and a date mask does not establish a valid date.
Use model validation for required lengths and business rules. The model should
contain raw slot characters, not formatted text. Apply masks to identifiers,
phone numbers, and similar strings, rather than free-form names or email addresses.
