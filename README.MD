# Polisabroso

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

### Transactions and `DbContext`

Since this is a **Blazor Server** project, a `DbContext` instantiated in, the `customers` module, can also be present at
the products module, becase a `User Circuit` can be considered as a `scope` and the context will exist in the circuit's
lifecycle.

So when performing `DML` operations, a new instance of the `DbContext` should be created, using
`IDbContextFactory<DbContext>`.

> Note: in the future it's possible to change the database structure. Each module could represent a **Database Scheme**.

### Folder Structure

Feature Groups. Every folder represents a **Feature**, and each of those folders has a similar sub-folder structure,
isually including UI, API, Services, etc.

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