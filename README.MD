# Polisabroso

The [login workflow](docs/auth-login.md) documents the username form, POST endpoint, authentication cookie, and
remember-me behavior.

## Architecture

This project is based on the **Modular Monolithic Architecture**. Monolithic means that the whole application is
contained in a single project/executable, and requires only one deploy. Modular means the application is separated by
modules and all the `host` knows it's their modules, not their dependencies.

E.g, in order to register the whole module in the application's DI container, there has ta be a sort of abstraction like
this:

```csharp
namespace Ecommerce.Modules.Catalog.Internal;

public static class CatalogModuleExtensions
{
    public static IServiceCollection AddCatalogModule(this IServiceCollection services, IConfiguration config)
    {
        // Register private DB Context and Services
        services.AddDbContext<CatalogDbContext>(options => ...);
        
        // Register the public interface implementation
        services.AddScoped<ICatalogApi, CatalogApi>(); 

        return services;
    }
}
```

### Why this architecture

#### Advantages

- Single deploy.
- Easier maintaince in smaller projects.
- Loose coupling.
- High cohesion.
- In-memory/process calls (does not require external services like an API.)

#### Known disadvantages

- Conceptual boundaries, requires clear and strict rules.

### Rules

In order to comunicate one module with another, the **dependent** module has to **inject** a clean public
implementation, made with the purpose of external modules do not call or reference any internal module class or service.

Sometimes it's ok to fetch data from a different module, e.g: a complex `SELECT` statement with many `JOINS`, this could
be set up as a view, DTO, etc., and only applies to consult or fetch data, it should never be manipulated. In this
project, all the modules share a database.

| NEED                                               | APPROACH                                                                                          |
|----------------------------------------------------|---------------------------------------------------------------------------------------------------|
| Execute another's module rule, process, flow, etc. | Public contract                                                                                   |
| Show a notification from one module to another     | Use the domain or a integration services, **e.g.** a services independent of modules  (event bus) |
| Show data from another module                      | Use Views, DTOs, readonly models, etc.                                                            |

#### Choosing the boundary

`Api` means the **public contract of a module**, not an HTTP API and not a place for service implementations. The
implementation, domain model, persistence, and private UI logic belong to the owning module. Create a public contract
only when a consumer outside that module needs one; two components inside the same module can use a private service.

| Consumer and need                                     | Put it here                                                           | Example                                                                    |
|-------------------------------------------------------|-----------------------------------------------------------------------|----------------------------------------------------------------------------|
| UI and service in the same module                     | `Modules/<Feature>/UI` and `Modules/<Feature>/Internal`               | Auth login uses its own credential-validation service.                     |
| Another module must run a use case                    | Contract in the owner's `Api`; implementation in its `Internal`       | Orders calls an Inventory reservation operation.                           |
| Another module only needs information                 | A read-only query contract/DTO in the owner's `Api`                   | Billing requests a product's current price.                                |
| Another module needs to change owned data             | A command on the owner's public contract                              | Orders asks Inventory to reserve stock; it does not edit Inventory tables. |
| Many modules react to an occurrence                   | An integration event with a documented publisher and subscribers      | Billing reacts after an order is confirmed.                                |
| The host needs to configure a module                  | One module registration extension; internal dependencies stay private | `Program.cs` calls `AddAuthModule(...)`.                                   |
| A reusable visual component needs no feature behavior | `Shared/UI`                                                           | A form field or dialog.                                                    |
| Application chrome needs feature behavior             | Compose it in `Host` or expose a feature-owned UI component/contract  | The app shell displays an Auth logout control.                             |

A shared database and a single executable do not remove these boundaries. Do not inject another module's private
service or `DbContext`, reference its domain entities, or write to its tables from a consumer. Public DTOs should
contain only the fields consumers need; never expose a tracked entity as a writable shortcut. Keep contracts small and
name operations by the owner's use cases rather than exposing a generic repository.

#### Example: Orders needs Inventory

The public contract describes the operation without exposing how Inventory stores stock:

```csharp
// Modules/Inventory/Api/IInventoryApi.cs
namespace Polisabroso.Modules.Inventory.Api;

public interface IInventoryApi
{
    Task<bool> ReserveAsync(Guid productId, int quantity, CancellationToken cancellationToken);
}
```

Inventory implements and registers that contract inside its own module. Orders injects `IInventoryApi` and never
references `InventoryDbContext` or an Inventory entity. If a reservation and order creation must commit atomically,
define the transaction boundary deliberately in the use case; an in-process call alone does not make two writes atomic.

#### Example: Auth and the application layout

Auth owns credential validation, sign-in, sign-out, and the authentication cookie. A possible layout is:

```text
Modules/Auth/
├── Api/                    # Only contracts needed outside Auth
├── Internal/               # Identity adapter, session flow, persistence
├── Domain/                 # Auth-owned types
└── UI/                     # Login and a reusable logout control
Host/                       # Module registration and application composition
Shared/UI/                  # Feature-independent components and layouts
```

`Login` can call a private Auth service because both are in the same module. A logout control used by the application
shell can remain in `Auth/UI`; the host composes it into the shell. If another module must initiate a session operation,
publish a narrow contract such as `IAuthSession` in `Auth/Api` and keep its implementation in `Auth/Internal`. Avoid
putting the implementation in `Api` or making `Shared/UI` depend on Auth's private classes. If the current
`Shared/UI/Layout/MainLayout.razor` grows feature-specific behavior, move that composition to the host or have it use
only an intentional public contract.

`AuthenticationStateProvider` reports the current principal to Blazor components; it is not the service that validates
passwords or persists a session. With ASP.NET Core Identity, sign-in and sign-out must set or clear a cookie during an
HTTP request. Route the login/logout forms through Auth-owned HTTP handlers or static-rendered Identity pages, then
reload/navigate so the Blazor circuit receives the updated principal. Do not call cookie-writing `SignInManager`
operations from an interactive component event. Protect HTTP endpoints and use antiforgery for state-changing forms.

### Transactions and `DbContext`

Since this is a **Blazor Server** project, a `DbContext` instantiated in, the `customers` module, can also be present at
the products module, because a `User Circuit` can be considered as a `scope` and the context will exist in the circuit's
lifecycle.

So when performing `DML` operations, a new instance of the `DbContext` should be created, using
`IDbContextFactory<DbContext>`.

> Note: in the future it's possible to change the database structure. Each module could represent a **Database Scheme**.

### Folder Structure

Feature Groups. Every folder represents a **Feature**, and each of those folders has a similar sub-folder structure,
usually including UI, API, Services, etc. The following diagram shows a possible future split into separate projects;
today Polisabroso keeps these boundaries as folders in `src/Polisabroso.App`. Since folders within one C# project do not
enforce module visibility, review dependencies against the public-contract rule above. If modules become separate
projects, reference their `Api` projects from consumers and keep their `Internal` projects private.

```
EcommerceMonolith/
├── Ecommerce.Host/               # Blazor Web App (App.razor, Program.cs)
│   └── ...                       # References all module API projects
├── Ecommerce.Shared/             # Common abstractions (IEventBus, Base DTOs)
│
├── Ecommerce.Modules.Catalog/    # CATALOG MODULE
│   ├── Ecommerce.Modules.Catalog.Api/     # Public interfaces & Contracts
│   ├── Ecommerce.Modules.Catalog.Internal/# Private implementation (Entities, DB Context)
│   └── Ecommerce.Modules.Catalog.UI/       # Blazor UI components (.razor files)
│
└── Ecommerce.Modules.Orders/     # ORDERS MODULE
    ├── Ecommerce.Modules.Orders.Api/
    ├── Ecommerce.Modules.Orders.Internal/
    └── Ecommerce.Modules.Orders.UI/
```

## Minimal Valuable Product (MVP)

Modules:

- [ ] Authentication
- [ ] Authorization
- [ ] Billing
- [ ] Inventory
- [ ] Accounts receivable
- [ ] Accounts payable

## Language

- **Display Language:** Spanish
- **Code Language:** English

## Modules

- Orders: Iventory, bills, sales.
- Accountability: Accounts payable, accounts receivable.
- Human resources
- Reports
- Security: authentication, authorization.

# TBD:

- Business and accounting rules. (fiscal periods, sequences)
- Auth and security. (roles, permissions, sensitive-data handling)
- Development conventions. (branches, naming, commits, pull requests)

# Future

- Menus: public menu of the day, frequent options, recepies catalog, etc.
- Receips and costs: rations per plate/recipe, cost per plate/recipe, income per plate/recipe, etc.

---

- Tips

# Notes

- The application is meant to initialize in `/login`, this is defined in `launchSettings.json` and has to be considered
  in deploy.
