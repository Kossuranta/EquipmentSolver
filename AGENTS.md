# AI Agent Guide (Equipment Solver)

This repository is a **full-stack web application** for optimizing equipment loadouts across different games.

## Tech Stack

| Layer | Technology |
|-------|-----------|
| **Backend** | C# / ASP.NET Core Web API (.NET 10) |
| **Frontend** | Angular (latest stable) with TypeScript, **Angular Material** for UI components |
| **Database** | PostgreSQL via **Entity Framework Core** (Npgsql) with code-first migrations |
| **Auth** | ASP.NET Identity + JWT tokens |
| **Game data** | IGDB API for game search/selection |
| **Logging** | Serilog (console + file) |
| **Testing** | xUnit (solver + critical business logic) |
| **Hosting** | Single Docker container on Unraid server (behind reverse proxy) |
| **CI/CD** | Manual build/deploy for now |

## Development Environment

- **OS**: Windows 11
- **Shell**: **PowerShell** — all terminal commands must use PowerShell syntax (not bash/sh).
  - Use `;` to chain commands, not `&&`.
  - Use backtick (`` ` ``) for line continuation, not `\`.
  - Use `Remove-Item` instead of `rm`, `Copy-Item` instead of `cp`, etc., or use the tool equivalents.
  - Heredocs are not supported — pass multi-line strings via PowerShell syntax or avoid them.

## Required Behavior

- **Before implementing features**, read the relevant spec documents in `spec/` and use them as the source of truth for features, terms, and architecture:
  - Start here: `spec/README.md`
  - Progress tracking: `spec/PROGRESS.md`
- **To find code quickly**, refer to `INDEX.md` — it maps every file in the project by feature area (controllers, services, components, etc.).
- If the developer gives new requirements, changes rules, or clarifies terminology, **proactively suggest edits** to the relevant file(s) in `spec/` so the spec stays current.
- If something in `spec/` is ambiguous or incomplete, **ask questions** rather than guessing.

## Collaboration Style

You are pair-programming with a professional developer (solo hobby project).
- Prefer **direct, high-signal communication**.
- Favor simple, low-maintenance solutions over enterprise patterns.
- When proposing changes, include:
  - **What to change in code** (backend and/or frontend)
  - **Any infrastructure or configuration changes** needed (Docker, database migrations, environment variables, etc.)
- If something is ambiguous, **ask the developer** and avoid making assumptions.

## C# / Backend Code Style

- Use modern C# (.NET 10) features where they improve clarity.
- Prefer the simplest available syntax when it stays clear (e.g., collection expressions like `[]` for empty collections).
- Omit braces for single-statement conditionals and loops only when the body fits on one line; use braces for multi-line bodies.
- Use concrete collection types (e.g., `List`, `Dictionary`, `HashSet`) in APIs when practical.
- LINQ is fine in backend/API code; prefer explicit loops only when performance is critical.
- All C# files must declare a **file-scoped namespace**.
- Mark methods as `static` when they do not access instance data.
- Add **XML doc summaries** for public methods and non-trivial private helpers:
  - Keep summaries **short**
  - Avoid stating the obvious
- Follow **RESTful API conventions**:
  - Use proper HTTP methods (GET, POST, PUT, DELETE)
  - Return appropriate status codes
  - Use consistent DTO patterns for request/response bodies
- Use **dependency injection** throughout; avoid service locator patterns.
- Keep controllers thin — business logic belongs in services.

## Angular / Frontend Code Style

- Use **Angular Material** as the component library.
- Use **standalone components** (no NgModules unless necessary).
- Use **Angular signals and reactive patterns** where appropriate.
- Follow Angular style guide naming conventions (kebab-case files, PascalCase classes).
- Use **TypeScript strict mode**.
- Keep components small and focused; extract reusable logic into services.
- Use **Angular Router** for navigation with lazy-loaded routes.
- Handle loading and error states in the UI.

## Entity Framework Core / Database

- Use **code-first** approach with migrations.
- Keep migration history clean — each migration should represent a coherent change.
- Use **Fluent API** for entity configuration over data annotations when both are viable.
- Always include a `DbContext` seed or migration step when new required data is introduced.
- Never hardcode connection strings — use configuration/environment variables.

## Docker / Infrastructure

- The entire app (backend + frontend static files) runs in a **single Docker container** on an Unraid server.
- Angular is built and served as static files from ASP.NET Core's `wwwroot/` with SPA fallback.
- Provide a `Dockerfile` and `docker-compose.yml` for the full stack (app + PostgreSQL).
- Use **environment variables** for all configuration (connection strings, JWT secrets, IGDB API credentials, etc.).
- The container runs **HTTP only** — TLS termination is handled by a reverse proxy (Nginx Proxy Manager or similar on Unraid).
- PostgreSQL runs in a separate container via docker-compose with a persistent volume.
- Backups via scheduled `pg_dump` (Unraid User Scripts plugin).

## Project Structure

```
EquipmentSolver/
├── src/
│   ├── EquipmentSolver.Api/            # ASP.NET Core Web API + serves Angular static files
│   ├── EquipmentSolver.Core/           # Domain models, interfaces, services, solver logic
│   ├── EquipmentSolver.Infrastructure/ # EF Core, repositories, external services (IGDB)
│   └── EquipmentSolver.Web/           # Angular frontend app (Angular Material)
├── tests/
│   └── EquipmentSolver.Tests/          # xUnit tests (solver, critical business logic)
├── spec/                               # Specifications and progress tracking
├── docker-compose.yml
├── Dockerfile
└── AGENTS.md
```

## When You Propose Work

When you suggest an implementation, format it like:
- **Goal** — what this change accomplishes
- **Code changes** — files/classes + what to add/change (backend and frontend)
- **Infrastructure steps** — Docker, migrations, config changes (if any)
- **Test plan** — how to validate the change works

## Project Constraints / Preferences

- No hardcoded configuration values — use `appsettings.json`, environment variables, or configuration objects.
- Keep the backend and frontend clearly separated.
- API endpoints should be versioned (e.g., `/api/v1/...`).
- All user-facing errors should return meaningful messages.
- Prefer solutions that are easy to debug and extend.
- This is a **solo hobby project** — favor simplicity and low maintenance over enterprise patterns.

## Safety / Footguns to Avoid

- Never store passwords in plain text — ASP.NET Identity handles hashing.
- Never expose internal IDs or stack traces in API error responses in production.
- Always validate and sanitize user input on the backend, regardless of frontend validation.
- Don't skip EF Core migrations — always keep the database schema in sync with the code.
- Avoid N+1 query patterns — use `.Include()` or projection where needed.
- Store IGDB API credentials in environment variables, never in source code.

---
If anything here conflicts with a specific request, follow the request and note the deviation.
