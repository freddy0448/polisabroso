## Validations
Apparently, every time navigation is performed in a **Blazor Server-Side** application, the authentication state is validated by the [`RevalidatingAuthenticationStateProvider`](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/?view=aspnetcore-10.0&tabs=visual-studio&source=docs#additional-security-abstractions) class.

## Authorization
So in authorization they are two main concepts that should be understood:

- Claims: defines the user identity, and tells the application all the information required about the user.
- Roles: defines what the user's can and cannot do in general. E.g. **Admins** can restore another user's credentials, can edit their information, cannot access the accountability module, etc.

Now there's a problem in that, if applications validate all the user can do would be pretty difficult to maintain, because many roles could mean that the user can do "X", and any change on how the application should behave depending on the logged in user's roles could be breaking and hard to maintain.

So that's where **Policies** come in. In `dotnet` you can define policies based on **Claim Values** or **Roles**, and only one place, e.g.:

```cs
builder.Services.AddAuthorization(options =>
{
    // Simple policy requiring a claim
    options.AddPolicy("AtLeast21", policy => 
        policy.RequireClaim("Age", "21", "22", "23")); // etc

    // Advanced policy requiring a custom requirement
    options.AddPolicy("MinimumSeniority", policy =>
        policy.Requirements.Add(new SeniorityRequirement(3))); // Needs 3 years
        
    options.AddPolicy("CanManageUsers", policy =>
        policy.RequireRole("Admin"));
});
```
