# Equipment Solver — Project Specification

## Overview

**Equipment Solver** is a web application that helps gamers find optimal equipment loadouts. Users input available equipment with their stats, define constraints (e.g., weight limit), and set weighted priorities (e.g., maximize armor, some fire resistance). The solver then computes the best equipment combination.

## Core Concepts

### Equipment

An equipment piece belongs to a **game profile** and has:
- A **name**
- One or more compatible **slots** (e.g., a ring might fit ring1 or ring2 — many-to-many relationship)
- A set of **stats** (key-value pairs, e.g., `armor: 50`, `fire_resistance: 12`, `weight: 3.5`)

Stats are **flat numeric values** (no formulas or percentage scaling). Stats can be positive or negative. The set of stat types is defined per game profile.

An equipment piece that is compatible with multiple slots can only be equipped in **one slot at a time** — the solver enforces this constraint.

### Solver

The solver takes:
1. A pool of **enabled** equipment (grouped by compatible **active** slots)
2. **Constraints** — hard limits that must be satisfied (e.g., `weight <= 70`)
3. **Priorities** — weighted objectives to maximize (or minimize if negative)

**Priority weighting** works as follows:
- Each priority has a **stat** and a **weight** (float).
- The solver scores each combination as: `score = Σ (stat_total × weight)`
- A weight of `1.0` on armor and `0.5` on fire resistance means armor is twice as important.
- A weight of `0` means the stat is ignored.
- A **negative weight** means the solver will try to minimize that stat (useful for avoiding penalties).

The solver finds the equipment combinations with the highest total score that satisfy all constraints.

**Solver behavior:**
- **Algorithm**: Branch-and-bound (brute force with constraint pruning). Revisit if real-world profiles prove too large.
- **Output**: Returns **top N results** (default 5, user-configurable) so the user can compare alternatives.
- **Timeout**: 10-second limit via `CancellationToken`. If the solver times out, it returns the best results found so far with a warning message.
- **Empty slots**: The solver may leave a slot **empty** if that produces a better score (e.g., when all available items for that slot would hurt the score via negative stats). An empty slot contributes zero to all stats.
- **Disabled slots**: Users can **disable individual slots**, causing the solver to skip them entirely. Disabled slots do not appear in results.

### Per-User Equipment Selection

When a user **uses** or **copies** a public profile (or works with their own profile), they can **enable or disable individual equipment items** to indicate which items they own or can equip:

- Each equipment item has an **enabled/disabled toggle** per user.
- **Enable all / Disable all** buttons for quick bulk toggling.
- Only **enabled** items are fed to the solver.
- The enabled/disabled state is **stored per user, per equipment item** and persists across sessions.
- If the profile owner **updates an existing item** (e.g., changes its stats), the user's enabled/disabled state for that item is **preserved** — it's keyed by the equipment record, not its contents.
- If the profile owner **adds new items**, they default to **enabled** for all users.
- If the profile owner **deletes an item**, the user's state for that item is cleaned up (cascade delete).

### Game Profiles

A game profile defines:
- **Game** — selected from IGDB (see [Game Selection](#game-selection) below)
- Optional **description** of the profile (e.g., "PvP build", "Max magic resist")
- **Version number** — incremented by the creator when publishing updates (see [Profile Versioning](#profile-versioning) below)
- **Equipment slots** available in that game (e.g., head, chest, legs, boots, ring1, ring2) — each slot can be **enabled or disabled** by the user
- **Stat types** used in that game (e.g., armor, weight, fire_res, magic_res)
- **Equipment catalog** — all equipment pieces available
- **Solver presets** — saved constraint + priority configurations

### Profile Versioning & Patch Notes

Profiles have a **version number** and a **patch notes** history so creators can communicate changes to users:

- The profile has a version in **major.minor.patch** format (e.g., `0.6.1`, `1.0.0`). Stored as a **single string** in the database (e.g., `"0.6.1"`), but the UI presents it as **three separate integer inputs** (each constrained to **0–999**). New profiles start at `0.1.0`. Validate the range on both frontend and backend.
- When the creator updates a profile, they can **bump the version** and write a **patch note** describing what changed.
- Patch notes are stored as a list of entries, each with:
  - **Version** — major.minor.patch at time of the note
  - **Date** — when the note was published
  - **Content** — free-text description of changes (supports basic text, no rich formatting needed)
- Users viewing or using a profile can see the **full patch notes history** (newest first).
- Version bumps and patch notes are **optional** — the creator can make small edits without publishing a note. But the version is always visible on the profile.
- **No rollback support** — patch notes are informational only. The profile always reflects the latest state.

> **Future plan**: Profiles will eventually support **export/import as a file** (JSON or similar). This enables creators and users to keep their own backups of specific versions. Design the data model with this in mind — the profile, its slots, stats, equipment, and solver presets should be serializable as a self-contained unit.

### Game Selection

Games are sourced from the **IGDB API** (Internet Game Database):
- When creating or editing a game profile, the user selects a game from a **searchable dropdown** that queries IGDB.
- The selected game's IGDB ID, name, and cover image URL are stored with the profile.
- This ensures consistent game naming across all profiles and enables filtering in search.

### Visibility & Sharing

- Profiles are **private by default**.
- Owner can set a profile to **public**, making it searchable by other users.
- Other users can **use** a public profile (read-only). If the owner edits it, users see the updated version automatically.
- Users can **copy** a public profile to their own account, creating an independent (unlinked) clone.
- Public profiles can be **upvoted or downvoted** by other users.
- Public profiles display their **vote score** and **usage count** (number of people actively using them).

### Browsing & Searching Public Profiles

Users can search and discover public profiles with:
- **Game filter** — searchable dropdown (same IGDB-powered component) to filter profiles by game
- **Text search** — search by profile name or creator's username
- **Sort options**:
  - Most votes
  - Most used (active users)
  - Newest
  - Alphabetical (profile name)
  - Alphabetical (creator name)

## User Accounts

- **Username + password** authentication (ASP.NET Identity + JWT).
- No email required. No password reset flow (admin can manually reset if needed).
- Each user can have **multiple game profiles**.
- Users can **delete their account**, which cascades to all their data.

## Tech Stack

See [AGENTS.md](../AGENTS.md) for the full tech stack and coding conventions.

| Component | Technology |
|-----------|-----------|
| Backend API | C# / ASP.NET Core (.NET 10) |
| Frontend | Angular + TypeScript + Angular Material |
| Database | PostgreSQL via EF Core (Npgsql) |
| Game data | IGDB API |
| Auth | ASP.NET Identity + JWT |
| Logging | Serilog (console + file) |
| Testing | xUnit (solver + critical logic) |
| Hosting | Single Docker container on Unraid (HTTP, behind reverse proxy) |

## Architecture

### Backend (ASP.NET Core Web API)

```
EquipmentSolver.Api/            — Controllers, middleware, startup, serves Angular static files
EquipmentSolver.Core/           — Domain models, service interfaces, solver logic
EquipmentSolver.Infrastructure/ — EF Core DbContext, repositories, IGDB client, migrations
```

**Key API areas:**
- `Auth` — Register, login, token refresh, account deletion
- `Games` — Proxy/cache for IGDB game search
- `Profiles` — CRUD for game profiles, visibility toggle, versioning, patch notes
- `Equipment` — CRUD for equipment within a profile (including slot compatibility)
- `UserState` — Per-user slot enable/disable + equipment enable/disable (with bulk toggle)
- `Solver` — Run solver with given constraints/priorities, manage presets (respects user's enabled slots/items)
- `Social` — Browse/search public profiles, vote, copy, usage tracking

### Frontend (Angular + Angular Material)

- **Auth pages** — Login, register
- **Dashboard** — List of user's game profiles
- **Profile editor** — Select game (IGDB search), manage slots, stats, equipment, version + patch notes
- **Equipment selection UI** — Enable/disable items per user, enable all/disable all buttons, slot toggles
- **Solver UI** — Configure constraints and priorities, run solver, view top N results
- **Browse** — Search/filter public profiles by game, name, creator; sort by votes/usage/date/name
- **Profile detail (public)** — View a public profile, vote, copy, use, view patch notes history

### Database (EF Core)

Key entities (preliminary):
- `User` — id, username, password hash (managed by ASP.NET Identity)
- `GameProfile` — id, owner_id, igdb_game_id, game_name, game_cover_url, description, version (string, e.g. "0.6.1"), is_public, vote_score, usage_count
- `ProfilePatchNote` — id, profile_id, version, date, content
- `EquipmentSlot` — id, profile_id, name, sort_order
- `StatType` — id, profile_id, display_name
- `Equipment` — id, profile_id, name
- `EquipmentSlotCompatibility` — equipment_id, slot_id (many-to-many: which slots an item can go in)
- `EquipmentStat` — equipment_id, stat_type_id, value
- `UserSlotState` — user_id, slot_id, is_enabled (per-user slot enable/disable; defaults to enabled)
- `UserEquipmentState` — user_id, equipment_id, is_enabled (per-user item enable/disable; defaults to enabled)
- `SolverPreset` — id, profile_id, name
- `SolverConstraint` — preset_id, stat_type_id, operator, value
- `SolverPriority` — preset_id, stat_type_id, weight
- `ProfileVote` — user_id, profile_id, vote (+1/-1)
- `ProfileUsage` — user_id, profile_id (tracks active users of a public profile)

## Spec Documents

| File | Purpose |
|------|---------|
| `spec/README.md` | This file — project overview and plan |
| `spec/PROGRESS.md` | Progress tracking — what's done, what's next |

## Decisions Made

- [x] **Database**: PostgreSQL (2026-02-07)
- [x] **Auth**: ASP.NET Identity + JWT, username/password only, no email (2026-02-07)
- [x] **Solver algorithm**: Branch-and-bound with pruning, 10s timeout, top N results (2026-02-07)
- [x] **Stat values**: Flat numbers only, no formulas/scaling (2026-02-07)
- [x] **Multi-slot equipment**: Supported via many-to-many relationship (2026-02-07)
- [x] **UI framework**: Angular Material (2026-02-07)
- [x] **Game selection**: IGDB API for game search/selection (2026-02-07)
- [x] **Search/browse**: Filter by game (IGDB), search by profile/creator name, sort by votes/usage/date/name (2026-02-07)
- [x] **Frontend serving**: Single container — Angular built into wwwroot, served by ASP.NET (2026-02-07)
- [x] **Hosting**: HTTP-only container behind reverse proxy on Unraid (2026-02-07)
- [x] **Logging**: Serilog to console + file (2026-02-07)
- [x] **Testing**: xUnit for solver and critical business logic (2026-02-07)
- [x] **Rate limiting**: Deferred — add ASP.NET Core rate limiting middleware at deployment time (2026-02-07)
- [x] **CI/CD**: Manual build/deploy for now (2026-02-07)
- [x] **Account management**: Simple account deletion with cascade, no data export (2026-02-07)
- [x] **Profile categorization/tags**: Deferred — game name is sufficient for now (2026-02-07)
- [x] **Backups**: Scheduled pg_dump via Unraid User Scripts (2026-02-07)
- [x] **Slot disabling**: Users can disable slots so the solver skips them (2026-02-07)
- [x] **Empty slots**: Solver can leave a slot empty if optimal (2026-02-07)
- [x] **Per-user equipment selection**: Users toggle items on/off (own/use), persisted per user, survives owner edits (2026-02-07)
- [x] **Profile versioning**: Major.minor.patch (3 int fields) + patch notes history, no rollback (2026-02-07)
- [x] **Profile import/export**: Deferred — design data model to be serializable for future file export/import (2026-02-07)

## Future Enhancements (Deferred)

- Formula/percentage-based stat values
- Profile tags/categories beyond game name
- Email support + password reset flow
- CI/CD pipeline (GitHub Actions)
- Rate limiting on public API
- Bulk import/export of equipment (CSV/JSON)
- **Profile export/import as file** (JSON) — enables users to back up specific versions and share offline
- Profile version rollback
- Data export for account deletion (GDPR-style)
