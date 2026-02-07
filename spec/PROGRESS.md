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
| IGDB API integration (backend) | :white_large_square: | Game search proxy/cache |
| Game profile CRUD (API) | :white_large_square: | Includes IGDB game selection, version field |
| Game profile UI (list, create, edit) | :white_large_square: | IGDB searchable dropdown for game, version display |
| Profile patch notes (API) | :white_large_square: | CRUD for patch notes, tied to version bumps |
| Profile patch notes (UI) | :white_large_square: | Creator: write notes on version bump; viewers: patch notes history |
| Equipment slots management (API + UI) | :white_large_square: | Define slots per game, per-user enable/disable |
| Stat types management (API + UI) | :white_large_square: | Define stat types per game |
| Equipment CRUD (API) | :white_large_square: | Multi-slot compatibility (many-to-many) |
| Equipment editor UI | :white_large_square: | Add/edit equipment with stats + slot compatibility |
| Per-user equipment selection (API + UI) | :white_large_square: | Enable/disable items, enable all/disable all, persisted per user |
| Bulk import/export equipment | :white_large_square: | Deferred — nice-to-have |

## Phase 3: Solver

| Task | Status | Notes |
|------|--------|-------|
| Solver algorithm (branch-and-bound) | :white_large_square: | With constraint pruning, 10s timeout, empty slots allowed |
| Solver service implementation | :white_large_square: | Returns top N results, respects user's enabled slots/items |
| Solver API endpoint | :white_large_square: | POST constraints + priorities, return ranked results |
| Solver UI — constraint editor | :white_large_square: | |
| Solver UI — priority/weight editor | :white_large_square: | |
| Solver UI — results display | :white_large_square: | Show top N loadouts + score breakdown |
| Solver presets (save/load) | :white_large_square: | |
| Solver unit tests (xUnit) | :white_large_square: | Critical path — test early |

## Phase 4: Social Features

| Task | Status | Notes |
|------|--------|-------|
| Profile visibility toggle (public/private) | :white_large_square: | |
| Browse/search public profiles (API + UI) | :white_large_square: | Filter by game (IGDB), search by name/creator |
| Sort public profiles | :white_large_square: | By votes, usage, newest, alphabetical (name/creator) |
| Upvote / downvote system | :white_large_square: | |
| Copy public profile to own account | :white_large_square: | Creates unlinked clone |
| "Use" a public profile (linked read-only) | :white_large_square: | Auto-updates when owner edits |
| Display vote score + usage count | :white_large_square: | |
| Public profile detail page | :white_large_square: | |

## Phase 5: Polish & Infrastructure

| Task | Status | Notes |
|------|--------|-------|
| Error handling & validation (backend) | :white_large_square: | |
| Error handling & UX (frontend) | :white_large_square: | Loading states, error messages |
| Account deletion (cascade) | :white_large_square: | |
| Dockerize (single container: API + Angular static) | :white_large_square: | |
| docker-compose (app + PostgreSQL) | :white_large_square: | |
| Set up reverse proxy on Unraid | :white_large_square: | Nginx Proxy Manager or similar |
| Deploy to Unraid | :white_large_square: | |
| Rate limiting (ASP.NET Core middleware) | :white_large_square: | Add at deployment time |
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

## Notes

- **Phase 0** and **Phase 1** are now complete. The full foundation is in place: solution structure, domain models, EF Core with migrations, ASP.NET Identity + JWT auth, Serilog logging, Angular scaffold with Material UI, login/register pages, and Docker configuration.
- Ready to begin **Phase 2**: IGDB integration, game profile CRUD, equipment management.
- See `spec/README.md` for the full list of deferred future enhancements.
