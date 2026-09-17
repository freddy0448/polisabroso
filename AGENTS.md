# Repository Guidelines

## Project Structure & Module Organization

`Polisabroso.sln` contains the ASP.NET Core Blazor Server application in `src/Polisabroso.App`. Startup and routing live under `Host/`; reusable layouts and pages belong in `Shared/UI/`; business features are grouped by module under `Modules/<Feature>/` (for example, `Modules/Auth/UI`). Static files live in `wwwroot/`, with Tailwind input at `wwwroot/styles/tailwind.input.css` and generated CSS at `wwwroot/css/tailwind.css`. Design references are stored in `docs/design/`.

Keep module internals private. Cross-module behavior should be exposed through a small public contract; shared read models may be used for queries but must not become writable back doors into another module.

## Build, Test, and Development Commands

Run commands from the repository root unless noted:

- `dotnet restore Polisabroso.sln` restores .NET dependencies.
- `npm ci --prefix src/Polisabroso.App` installs the pinned Tailwind toolchain.
- `npm run css:build --prefix src/Polisabroso.App` generates minified CSS once.
- `npm run css:watch --prefix src/Polisabroso.App` watches styles during UI development; keep it in a separate terminal.
- `dotnet build Polisabroso.sln -c Release` compiles the solution without starting the Debug CSS watcher.
- `dotnet run --project src/Polisabroso.App -c Release` runs the application locally.

## Coding Style & Naming Conventions

Use four-space indentation, file-scoped namespaces, nullable reference types, and the existing C# brace style. Name types, components, and public members in `PascalCase`; use `camelCase` for locals and parameters. Keep Razor markup in `.razor` and substantial component logic in a matching `.razor.cs`. Code and identifiers are English; user-facing text is Spanish. Run `dotnet format Polisabroso.sln` before submitting broad C# changes.

## Testing Guidelines

No automated test project or coverage threshold is configured yet. New business rules should include a test project under `tests/`, named after the module (for example, `Polisabroso.Auth.Tests`). Use descriptive names such as `Login_InvalidCredentials_ShowsError`. Run future suites with `dotnet test Polisabroso.sln -c Release`.

## Commit & Pull Request Guidelines

Recent history favors concise Conventional Commit subjects such as `feat: add ...`, `refactor: remove ...`, and `docs: update ...`. Keep commits focused and use an imperative summary. Pull requests should explain the behavior and architecture impact, link relevant issues, list verification commands, and include screenshots for visible UI changes. Never commit secrets; keep local overrides in development configuration or environment variables.
