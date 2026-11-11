# Repository Guidelines

## Project Structure & Module Organization

`Polisabroso.sln` contains the ASP.NET Core Blazor Server application in `src/Polisabroso.App`. Startup and routing live under `Host/`; reusable layouts and pages belong in `Shared/UI/`; business features are grouped by module under `Modules/<Feature>/` (for example, `Modules/Auth/UI`). Static files live in `wwwroot/`, with Tailwind input at `wwwroot/styles/tailwind.input.css` and generated CSS at `wwwroot/css/tailwind.css`. **The color palette is defined in `src\Polisabroso.App\wwwroot\styles\tailwind.input.css`.** Design references are stored in `docs/design/`.

Keep module internals private. Cross-module behavior should be exposed through a small public contract; shared read models may be used for queries but must not become writable back doors into another module.

## Component Data Access

All database or external data-source queries and writes must run in services under the owning module's `Internal/` folder, exposed through contracts in `Api/`. Blazor components, including pages, layouts, code-behind files, and component JavaScript, must call those contracts and consume materialized results.

Components must not inject or directly use query-capable infrastructure such as `DbContext`, `DbSet`, repositories, database connections, `UserManager`, or `SignInManager`, or execute deferred `IQueryable` queries. This applies even when a method hides the query internally, such as user/email availability checks or password sign-in checks. Keep form state, local input validation, and result presentation in components; put validation requiring stored data in an internal validation service. When creating or modifying components, move any direct data access into the owning module's service before completing the change.

## Build, Test, and Development Commands

Run commands from the repository root unless noted:

- `dotnet restore Polisabroso.sln` restores .NET dependencies.
- `npm ci --prefix src/Polisabroso.App` installs the pinned Tailwind toolchain.
- `npm run css:build --prefix src/Polisabroso.App` generates minified CSS once.
- `npm run css:watch --prefix src/Polisabroso.App` watches styles during UI development; keep it in a separate terminal.
- `dotnet build Polisabroso.sln -c Release` compiles the solution without starting the Debug CSS watcher.
- `dotnet run --project src/Polisabroso.App -c Release` runs the application locally.

## Coding Style & Naming Conventions

Use four-space indentation, file-scoped namespaces, nullable reference types, and the existing C# brace style. Name types, components, and public members in `PascalCase`; use `camelCase` for locals and parameters. Code and identifiers are English; user-facing text is Spanish. Run `dotnet format Polisabroso.sln` before submitting broad C# changes.

Every Blazor component must be split into colocated files with the same base name:

- `Component.razor` for Razor markup.
- `Component.razor.cs` for the partial component class and all C# component logic.
- `Component.razor.css` for isolated component styles.
- `Component.razor.js` for component JavaScript.

Keep component logic, styles, and scripts in their respective files rather than inline `@code`, `<style>`, or `<script>` blocks in the `.razor` file. Apply this structure when creating or modifying any Blazor component, including pages and layouts.

## Testing Guidelines

No automated test project or coverage threshold is configured yet. New business rules should include a test project under `tests/`, named after the module (for example, `Polisabroso.Auth.Tests`). Use descriptive names such as `Login_InvalidCredentials_ShowsError`. Run future suites with `dotnet test Polisabroso.sln -c Release`.

## Commit & Pull Request Guidelines

Recent history favors concise Conventional Commit subjects such as `feat: add ...`, `refactor: remove ...`, and `docs: update ...`. Keep commits focused and use an imperative summary. Pull requests should explain the behavior and architecture impact, link relevant issues, list verification commands, and include screenshots for visible UI changes. Never commit secrets; keep local overrides in development configuration or environment variables.

## Agent skills

### Issue tracker

Issues and specs live in GitHub Issues for `freddy0448/polisabroso`.
Before tracker operations, read `docs/agents/issue-tracker.md`.

### Triage labels

Use the five default triage labels.
Before triaging issues, read `docs/agents/triage-labels.md`.

### Domain docs

Use a single-context layout: root `GLOSSARY.md` and `docs/adr/`.
Before exploring the codebase, read `docs/agents/domain.md`.
