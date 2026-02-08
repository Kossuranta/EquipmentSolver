# Equipment Solver — Progress Tracker

## Status Legend

| Symbol | Meaning |
|--------|---------|
| :white_large_square: | Not started |
| :construction: | In progress |
| :white_check_mark: | Complete |
| :no_entry: | Blocked |

---

## Phase 0: Project Setup

| Task | Status | Notes |
|------|--------|-------|
| Initialize git repo | :white_check_mark: | Done |
| Write AGENTS.md | :white_check_mark: | Done |
| Write project spec (spec/README.md) | :white_check_mark: | Done |
| Write progress tracker (spec/PROGRESS.md) | :white_check_mark: | This file |
| Finalize all architectural decisions | :white_check_mark: | See decisions log below |
| Create ASP.NET Core solution structure | :white_check_mark: | Api, Core, Infrastructure projects with references + NuGet packages |
| Create Angular frontend project | :white_check_mark: | Angular 21 + Angular Material, standalone components |
| Set up Docker + docker-compose | :white_check_mark: | Multi-stage Dockerfile (Angular + .NET), docker-compose with PostgreSQL |
| Choose and configure database | :white_check_mark: | PostgreSQL via Npgsql |

## Phase 1: Foundation

| Task | Status | Notes |
|------|--------|-------|
| Define domain models (Core) | :white_check_mark: | All 15 entities: User, GameProfile, Equipment, Slots, Stats, Solver, Votes, Usage |
| Set up EF Core + DbContext | :white_check_mark: | AppDbContext with Fluent API configurations, cascade/NoAction for cycles |
| Create initial migration | :white_check_mark: | InitialCreate migration generated |
| Implement ASP.NET Identity + JWT auth | :white_check_mark: | AuthService with register/login/refresh/delete, JWT token generation |
| Basic auth middleware | :white_check_mark: | JWT Bearer auth configured in Program.cs |
| Set up Serilog logging | :white_check_mark: | Console + file sinks, rolling daily, 30-day retention |
| Angular project scaffold | :white_check_mark: | Routing, AuthService, JWT interceptor, auth/guest guards, proxy config |
| Login / register UI | :white_check_mark: | Angular Material forms with validation, error handling, loading states |

## Phase 2: Core Features — Game Profiles & Equipment

| Task | Status | Notes |
|------|--------|-------|
| IGDB API integration (backend) | :white_check_mark: | IgdbService with Twitch OAuth2, stale-while-revalidate cache (fresh 24h, stale-serve 72h), GamesController |
| Game profile CRUD (API) | :white_check_mark: | GameProfileService + ProfilesController, full CRUD with owner checks |
| Game profile UI (list, create, edit) | :white_check_mark: | Dashboard with profile cards, create dialog with IGDB search, profile editor with tabs |
| Profile patch notes (API) | :white_check_mark: | PatchNotesController, version bump + patch note creation |
| Profile patch notes (UI) | :white_check_mark: | Patch notes tab with version bump form (3 int inputs), history list |
| Equipment slots management (API + UI) | :white_check_mark: | SlotsController with CRUD + reorder, drag-and-drop UI via CDK DragDrop |
| Stat types management (API + UI) | :white_check_mark: | StatTypesController with CRUD, table UI with inline editing; DisplayName is sole name field with unique constraint per profile |
| Equipment CRUD (API) | :white_check_mark: | EquipmentController with slot/stat validation, replace-on-update pattern |
| Equipment editor UI | :white_check_mark: | Equipment dialog with slot checkboxes + stat picker; duplicate button, header actions always visible, comma-to-dot decimal input |
| Per-user equipment selection (API + UI) | :white_check_mark: | UserStateController, toggle items/slots, enable/disable all, optimistic UI updates |
| Bulk import/export equipment | :white_large_square: | Deferred — nice-to-have |

## Phase 3: Solver

| Task | Status | Notes |
|------|--------|-------|
| Solver algorithm (branch-and-bound) | :white_check_mark: | SolverEngine with score + constraint pruning, 10s timeout, empty slots, shared-item dedup |
| Solver service implementation | :white_check_mark: | ISolverService + SolverService — loads profile data, filters by user state, runs solver |
| Solver API endpoint | :white_check_mark: | POST /solver/solve + preset CRUD under /solver/presets, input validation |
| Solver UI — constraint editor | :white_check_mark: | Dynamic constraint rows with stat select, operator, and value inputs |
| Solver UI — priority/weight editor | :white_check_mark: | Dynamic priority rows with stat select and weight input |
| Solver UI — results display | :white_check_mark: | Expandable accordion with rank, score, stat totals, and per-slot assignments |
| Solver presets (save/load) | :white_check_mark: | Create/update/delete presets, load preset populates constraint + priority config |
| Solver unit tests (xUnit) | :white_check_mark: | 18 tests covering: basic solving, constraints, multi-slot, shared items, TopN, cancellation, edge cases |

## Phase 4: Social Features

| Task | Status | Notes |
|------|--------|-------|
| Profile visibility toggle (public/private) | :white_check_mark: | Slide toggle on General tab, PUT /profiles/{id}/visibility endpoint |
| Browse/search public profiles (API + UI) | :white_check_mark: | GET /browse with search, gameId filter, sort, pagination; Browse page with cards |
| Sort public profiles | :white_check_mark: | By votes, usage, newest, name (A-Z), creator (A-Z) |
| Upvote / downvote system | :white_check_mark: | POST /browse/{id}/vote (+1/-1/0), toggle behavior, can't vote on own profile |
| Copy public profile to own account | :white_check_mark: | POST /browse/{id}/copy, deep clones slots, stats, equipment, solver presets |
| "Use" a public profile (linked read-only) | :white_check_mark: | POST/DELETE /browse/{id}/use, appears on dashboard, stop cleans user state |
| Display vote score + usage count | :white_check_mark: | Shown on browse cards and profile detail page |
| Public profile detail page | :white_check_mark: | Overview, Equipment (accordion), Patch Notes tabs; vote/use/copy actions |

## Phase 5: Polish & Infrastructure

| Task | Status | Notes |
|------|--------|-------|
| Error handling & validation (backend) | :white_check_mark: | GlobalExceptionHandler middleware, consistent ErrorResponse for model validation, dev/prod error detail |
| Error handling & UX (frontend) | :white_check_mark: | Global error interceptor (5xx, 429, network errors), NotificationService with MatSnackBar |
| Account deletion (cascade) | :white_check_mark: | Properly cleans up NoAction FK records (votes, usages, user states) before cascade delete; UI in user menu |
| Dockerize (single container: API + Angular static) | :white_check_mark: | Multi-stage Dockerfile (Node + .NET SDK + runtime), Angular into wwwroot, fixed .slnx reference |
| docker-compose (app + PostgreSQL) | :white_check_mark: | IGDB env vars added, logs volume, health check on db |
| Rate limiting (ASP.NET Core middleware) | :white_check_mark: | Fixed-window: auth (10/min/IP), API (120/min/user), solver (10/min/user) |
| Set up reverse proxy on Unraid | :white_large_square: | Nginx Proxy Manager or similar |
| Deploy to Unraid | :white_large_square: | |
| Backup strategy (pg_dump schedule) | :white_large_square: | Unraid User Scripts |

---

## Decisions Log

| Date | Decision | Rationale |
|------|----------|-----------|
| 2026-02-07 | Project initialized | — |
| 2026-02-07 | C# backend + Angular frontend | Developer preference |
| 2026-02-07 | EF Core code-first | Standard .NET ORM, supports migrations |
| 2026-02-07 | Docker deployment on Unraid | Developer's existing infrastructure |
| 2026-02-07 | PostgreSQL as database | Excellent EF Core support (Npgsql), lightweight Docker image, JSONB flexibility |
| 2026-02-07 | ASP.NET Identity + JWT | Battle-tested auth with minimal custom code; no email/password reset needed |
| 2026-02-07 | Branch-and-bound solver | Brute force with pruning, 10s timeout, top N results; revisit if too slow |
| 2026-02-07 | Flat stat values only | Formulas/scaling deferred — users can pre-compute effective values |
| 2026-02-07 | Multi-slot equipment | Many-to-many via EquipmentSlotCompatibility; easier to build in from the start |
| 2026-02-07 | Angular Material for UI | Maintained by Angular team, good defaults, all needed components |
| 2026-02-07 | IGDB for game selection | Consistent game naming, searchable dropdown, cover images |
| 2026-02-07 | Single Docker container | Angular static files served from ASP.NET wwwroot; simpler deployment |
| 2026-02-07 | HTTP-only container | TLS handled by Unraid reverse proxy (to be set up) |
| 2026-02-07 | Serilog for logging | Console + file sinks, minimal config |
| 2026-02-07 | xUnit for testing | Solver + critical business logic only; no UI or integration tests initially |
| 2026-02-07 | No email, no password reset | Solo hobby project; admin can manually reset |
| 2026-02-07 | Rate limiting deferred | Add ASP.NET Core middleware at deployment time |
| 2026-02-07 | Manual CI/CD | Solo dev; script-based build/deploy for now |
| 2026-02-07 | Profile search via IGDB game filter | Filter by game, search by profile/creator name, sort by votes/usage/date/name |
| 2026-02-07 | No tags/categories | Game name is sufficient grouping; tags deferred |
| 2026-02-07 | Account deletion with cascade | Simple delete, no data export |
| 2026-02-07 | Backups via pg_dump | Scheduled via Unraid User Scripts |
| 2026-02-07 | Slot disabling | Users can disable slots so solver skips them |
| 2026-02-07 | Empty slots in solver | Solver can leave a slot empty if that's optimal |
| 2026-02-07 | Per-user equipment selection | Toggle items on/off per user; persisted; survives owner edits; enable/disable all buttons |
| 2026-02-07 | Profile versioning + patch notes | Free-form version string, patch notes history, no rollback; design for future export/import |
| 2026-02-08 | IGDB stale-while-revalidate cache | Fresh for 24h, background-refresh 24–72h, expire after 72h; 3-day resilience if IGDB is down |
| 2026-02-08 | Remove StatType internal Name | DisplayName is the sole naming field; unique constraint per profile enforced in DB + API |
| 2026-02-08 | Equipment duplicate + header actions | Duplicate button with auto-incremented name, Edit/Duplicate/Delete visible in panel header on wide screens |
| 2026-02-08 | Comma-to-dot decimal input | Comma keypress converted to dot in all numeric inputs for European locale support |
| 2026-02-08 | Social features via SocialController | Separate controller/service for browse, vote, copy, use; visibility toggle on ProfilesController |
| 2026-02-08 | Deep copy for profile cloning | Copies slots, stat types, equipment (with compatibilities + stats), solver presets (with constraints + priorities); excludes patch notes and votes |
| 2026-02-08 | Stop using cleanup | Stopping use of a profile also cleans up user equipment/slot states for that profile |
| 2026-02-08 | Solver presets shared on public profiles | Public profile detail includes solver presets; non-owners can see/load presets in solver tab |
| 2026-02-08 | Unsubscribe from used profiles | "Stop Using" button on dashboard cards (non-owned) and profile editor header |
| 2026-02-08 | Dark mode default + theme toggle | ThemeService with localStorage persistence, dark default, sun/moon toggle in toolbar; theme-aware color-mix backgrounds |
| 2026-02-08 | Global exception handler | Middleware catches unhandled exceptions, returns generic message in production, detailed in development |
| 2026-02-08 | Model validation returns ErrorResponse | ConfigureApiBehaviorOptions maps ModelState errors to consistent ErrorResponse format |
| 2026-02-08 | Rate limiting policies | auth (10/min/IP), api (120/min/user), solver (10/min/user); fixed-window via built-in middleware |
| 2026-02-08 | Account deletion cleanup | Manually removes other users' votes, usages, equipment/slot states for owned profiles before cascade delete |
| 2026-02-08 | Frontend error interceptor | Global HTTP error interceptor for 5xx, 429, and network errors via MatSnackBar notifications |
| 2026-02-08 | User menu with account deletion | Replaced inline logout with dropdown menu containing Logout + Delete Account (with confirmation) |
| 2026-02-08 | Dockerfile .NET 10 GA images | Changed from preview SDK/runtime to stable mcr.microsoft.com/dotnet/sdk:10.0 and aspnet:10.0 |

## Notes

- **Phases 0, 1, 2, 3, and 4** are now complete.
- Phase 2 added: IGDB integration (Twitch OAuth2 + caching), game profile CRUD (API + UI), equipment slots with drag-and-drop reordering, stat types with auto-naming, equipment CRUD with slot/stat picker dialogs, profile patch notes with version bumping, and per-user equipment/slot selection with optimistic UI.
- Phase 3 added: Branch-and-bound solver engine with constraint + score pruning, solver service with user state filtering, REST API for solving and preset CRUD, Angular solver tab with dynamic constraint/priority editors, results display with expandable loadout details, preset save/load, and 18 xUnit tests.
- Post-Phase 3 refinements: Equipment duplicate button with auto-naming, always-visible header actions, comma-to-dot decimal conversion, StatType simplified to single DisplayName with per-profile uniqueness.
- Phase 4 added: ISocialService + SocialController for visibility toggle, browse/search public profiles (filter by game, sort by votes/usage/newest/name/creator, pagination), upvote/downvote system, deep-copy profiles to own account, "use" public profiles (linked read-only), public profile detail page with overview/equipment/solver-presets/patch-notes tabs, Browse page in toolbar, visibility slide toggle in profile editor general tab.
- Post-Phase 4 fixes: Solver presets visible to non-owners in solver tab (read-only load), "Stop Using" button on dashboard and profile editor for unsubscribing from used profiles. Dark mode as default with light/dark toggle in toolbar, theme-aware backgrounds across all components.
- Phase 5 (partial): GlobalExceptionHandler middleware for consistent error responses (dev/prod aware), model validation returns ErrorResponse format, global error interceptor + NotificationService (MatSnackBar) on frontend, account deletion with proper NoAction FK cleanup + user menu UI, Dockerfile fixed (.slnx, non-preview images), docker-compose updated with IGDB env vars + logs volume, rate limiting added to all controllers (auth 10/min, API 120/min, solver 10/min).
- Remaining for **Phase 5**: Unraid-specific tasks (reverse proxy setup, deployment, backup strategy).
- See `spec/README.md` for the full list of deferred future enhancements.
