# Codebase Index

Quick reference for finding code by feature area. All paths are relative to the repo root.

---

## Root / Infrastructure

| File | Purpose |
|------|---------|
| `Dockerfile` | Multi-stage build: Angular → .NET → runtime, serves SPA from wwwroot |
| `docker-compose.yml` | App + PostgreSQL services, env vars, volumes, health checks |
| `EquipmentSolver.slnx` | .NET solution file |
| `add-migration.cmd` | Adds a new EF Core migration |
| `run-backend.cmd` | Runs the ASP.NET Core API (`dotnet run`) |
| `run-frontend.cmd` | Runs the Angular dev server (`ng serve`) |
| `run-migration.cmd` | Applies EF Core migrations |
| `spec/README.md` | Full project specification (features, architecture, decisions) |
| `spec/PROGRESS.md` | Progress tracker (phase status, decisions log) |
| `AGENTS.md` | AI agent guide (tech stack, code style, conventions) |

---

## Backend — `src/EquipmentSolver.Api/`

Entry point and HTTP layer.

### Startup

| File | Purpose |
|------|---------|
| `Program.cs` | App entry point, middleware pipeline, DI registration |
| `appsettings.json` | Production configuration |
| `appsettings.Development.json` | Development overrides |

### Controllers

| File | Purpose |
|------|---------|
| `Controllers/AuthController.cs` | Register, login, token refresh, account delete |
| `Controllers/ProfilesController.cs` | Game profile CRUD, visibility toggle |
| `Controllers/EquipmentController.cs` | Equipment CRUD within a profile |
| `Controllers/SlotsController.cs` | Equipment slot CRUD + reordering |
| `Controllers/StatTypesController.cs` | Stat type CRUD |
| `Controllers/PatchNotesController.cs` | Version bump + patch note creation |
| `Controllers/SolverController.cs` | Run solver, preset CRUD |
| `Controllers/UserStateController.cs` | Per-user equipment/slot enable/disable |
| `Controllers/SocialController.cs` | Browse, vote, copy, use public profiles |
| `Controllers/GamesController.cs` | IGDB game search proxy |

### DTOs

| Directory | Contents |
|-----------|----------|
| `DTOs/Auth/` | `LoginRequest`, `RegisterRequest`, `RefreshRequest`, `AuthResponse` |
| `DTOs/Profiles/` | `CreateProfileRequest`, `UpdateProfileRequest`, `ProfileResponse`, `ProfileDetailResponse`, `EquipmentRequest`, `SlotRequest`, `StatTypeRequest`, `PatchNoteRequest`, `UserStateRequest` |
| `DTOs/Social/` | `BrowseProfilesResponse`, `PublicProfileDetailResponse`, `SocialRequests` |
| `DTOs/Solver/` | `SolveRequest`, `SolveResponse`, `PresetDtos` |
| `DTOs/ErrorResponse.cs` | Standardized error response format |

### Middleware

| File | Purpose |
|------|---------|
| `Middleware/GlobalExceptionHandler.cs` | Catches unhandled exceptions, returns consistent error responses |

---

## Backend — `src/EquipmentSolver.Core/`

Domain models, business interfaces, and solver logic. No dependencies on infrastructure.

### Entities

| File | Purpose |
|------|---------|
| `Entities/ApplicationUser.cs` | ASP.NET Identity user |
| `Entities/GameProfile.cs` | Game profile (IGDB data, version, visibility) |
| `Entities/Equipment.cs` | Equipment piece |
| `Entities/EquipmentSlot.cs` | Named slot within a profile |
| `Entities/EquipmentSlotCompatibility.cs` | Many-to-many: equipment ↔ slot |
| `Entities/EquipmentStat.cs` | Stat value on an equipment piece |
| `Entities/StatType.cs` | Stat type definition per profile |
| `Entities/ProfilePatchNote.cs` | Version changelog entry |
| `Entities/ProfileVote.cs` | User vote on a public profile |
| `Entities/ProfileUsage.cs` | User "using" a public profile |
| `Entities/UserEquipmentState.cs` | Per-user equipment enable/disable |
| `Entities/UserSlotState.cs` | Per-user slot enable/disable |
| `Entities/SolverPreset.cs` | Saved solver configuration |
| `Entities/SolverConstraint.cs` | Constraint within a preset |
| `Entities/SolverPriority.cs` | Priority/weight within a preset |

### Interfaces

| File | Purpose |
|------|---------|
| `Interfaces/IAuthService.cs` | Auth operations contract |
| `Interfaces/IGameProfileService.cs` | Profile CRUD contract |
| `Interfaces/IIgdbService.cs` | IGDB search contract |
| `Interfaces/ISocialService.cs` | Social features contract |
| `Interfaces/ISolverService.cs` | Solver execution contract |

### Solver

| File | Purpose |
|------|---------|
| `Solver/SolverEngine.cs` | Branch-and-bound optimization algorithm |
| `Solver/SolverModels.cs` | Input/output models for the solver |

### Models

| File | Purpose |
|------|---------|
| `Models/AuthResult.cs` | Auth operation result |
| `Models/GameSearchResult.cs` | IGDB search result shape |
| `Models/JwtSettings.cs` | JWT configuration POCO |
| `Models/IgdbSettings.cs` | IGDB API configuration POCO |

---

## Backend — `src/EquipmentSolver.Infrastructure/`

EF Core, service implementations, external integrations.

### Data

| File | Purpose |
|------|---------|
| `Data/AppDbContext.cs` | EF Core DbContext with all DbSets |
| `Data/Configurations/*.cs` | Fluent API entity configurations (14 files, one per entity) |
| `Data/Migrations/` | EF Core migration files |

### Services

| File | Purpose |
|------|---------|
| `Services/AuthService.cs` | Identity + JWT auth implementation |
| `Services/GameProfileService.cs` | Profile CRUD, slots, stats, equipment, presets, user state |
| `Services/IgdbService.cs` | IGDB API client with Twitch OAuth2 + stale-while-revalidate cache |
| `Services/SocialService.cs` | Browse, vote, copy, use public profiles |
| `Services/SolverService.cs` | Loads profile data, filters by user state, runs solver engine |

### DI

| File | Purpose |
|------|---------|
| `DependencyInjection.cs` | Registers all infrastructure services |

---

## Frontend — `src/EquipmentSolver.Web/src/app/`

Angular application with Angular Material.

### App Shell

| File | Purpose |
|------|---------|
| `app.ts` + `app.html` + `app.scss` | Root component with toolbar, theme toggle, user menu |
| `app.config.ts` | Angular providers (HTTP, router, animations) |
| `app.routes.ts` | Route definitions with lazy loading |

### Pages

| Directory | Purpose |
|-----------|---------|
| `pages/login/` | Login form |
| `pages/register/` | Registration form |
| `pages/dashboard/` | Profile cards list (owned + used profiles) |
| `pages/profile-editor/` | Tabbed profile editor (general, slots, stats, equipment, solver, etc.) |
| `pages/browse/` | Search/filter public profiles |
| `pages/profile-detail/` | Public profile view (overview, equipment, solver presets, patch notes) |

### Components

| Directory | Purpose |
|-----------|---------|
| `components/create-profile-dialog/` | Dialog: create profile with IGDB game search |
| `components/equipment-dialog/` | Dialog: create/edit equipment (slot checkboxes, stat picker) |
| `components/profile-general-tab/` | General settings tab (name, game, description, visibility) |
| `components/profile-slots-tab/` | Slots tab with drag-and-drop reordering |
| `components/profile-stat-types-tab/` | Stat types tab with inline editing |
| `components/profile-equipment-tab/` | Equipment list tab (accordion, slot names, stats) |
| `components/profile-solver-tab/` | Solver tab (constraints, priorities, presets, results) |
| `components/profile-patch-notes-tab/` | Patch notes tab (version bump, history) |
| `components/profile-user-selection-tab/` | Per-user equipment/slot enable/disable toggles |

### Services

| File | Purpose |
|------|---------|
| `services/auth.service.ts` | Login, logout, JWT token management |
| `services/profile.service.ts` | Profile CRUD, equipment, slots, stats, solver, user state API calls |
| `services/browse.service.ts` | Public profile search, vote, copy, use |
| `services/game.service.ts` | IGDB game search API calls |
| `services/theme.service.ts` | Dark/light theme toggle with localStorage |
| `services/notification.service.ts` | MatSnackBar notification wrapper |

### Models

| File | Purpose |
|------|---------|
| `models/auth.models.ts` | Auth request/response interfaces |
| `models/profile.models.ts` | Profile, equipment, slot, stat, solver DTOs |

### Guards & Interceptors

| File | Purpose |
|------|---------|
| `guards/auth.guard.ts` | Route guards (authenticated + guest) |
| `interceptors/auth.interceptor.ts` | Adds JWT to requests, handles token refresh |
| `interceptors/error.interceptor.ts` | Global HTTP error handler (5xx, 429, network errors) |

### Frontend Config (project root)

| File | Purpose |
|------|---------|
| `angular.json` | Angular CLI workspace config |
| `proxy.conf.json` | Dev proxy → backend API |
| `package.json` | NPM dependencies and scripts |
| `tsconfig.json` | Base TypeScript config |

---

## Tests — `tests/EquipmentSolver.Tests/`

| File | Purpose |
|------|---------|
| `Solver/SolverEngineTests.cs` | 18 xUnit tests for the solver engine |
