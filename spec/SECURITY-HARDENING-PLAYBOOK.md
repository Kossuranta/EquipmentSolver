## Security Hardening Playbook (Agent)

This document is a **step-by-step, multi-phase** workflow for running a repo-wide security improvement pass on EquipmentSolver using the **`vibesec-skill`** Agent Skill.

### Preconditions

- **Invoke the skill**: In Agent chat, run `/vibesec-skill` (or explicitly say “use `vibesec-skill` for this task”) before starting each phase.
- **Work in small batches**: Prefer small, reviewable changesets (one theme at a time).
- **Don’t commit build artifacts**: Never include `**/obj/`, `**/bin/`, `node_modules/`, etc.
- **Store working notes**: Keep intermediate findings and checklists under `.temp/` (recommended: `.temp/security/`).

---

## Phase 0 — Baseline + Safety Rails (no behavior changes yet)

### Goals

- Establish a baseline security posture and a repeatable audit loop.
- Identify obvious footguns (secrets, unsafe config, overly permissive CORS).

### Steps

- **Inventory runtime surfaces**
  - Backend: ASP.NET Core API (controllers, middleware, auth setup)
  - Frontend: Angular (auth storage, template safety)
  - Infrastructure: Docker, environment variables, reverse proxy assumptions
- **Run dependency vulnerability scans**
  - .NET: `dotnet list package --vulnerable` (solution root)
  - Angular: `npm audit` (web project)
- **Secret scan**
  - Check for committed secrets in `appsettings.*.json`, Angular env files, CI configs, docs.
  - Ensure JWT signing secret and IGDB credentials are only via env vars/config, not hardcoded.

### Deliverables

- `.temp/security/phase-0-baseline.md`
  - Dependency scan results (summarize only; don’t paste huge logs)
  - “Top risks” shortlist (5–10 bullets)
  - Proposed prioritization order

---

## Phase 1 — Threat Model + Attack Surface Map

### Goals

- Build an attacker’s view of the system.
- Identify where authorization boundaries must be enforced.

### Steps

- **Map trust boundaries**
  - Browser ↔ API
  - API ↔ PostgreSQL
  - API ↔ IGDB
- **Enumerate entry points**
  - API endpoints (especially: auth, profile CRUD, equipment CRUD, solver execution, IGDB proxy)
  - File upload/download (if any)
  - Redirect/callback endpoints (if any)
- **Identify sensitive assets**
  - User data, profile ownership, “public profile” surface, voting/usage tracking, tokens, logs

### Deliverables

- `.temp/security/phase-1-attack-surface.md`
  - Table: endpoint/feature → auth required? → authorization rule → notes

---

## Phase 2 — Authentication & Session/JWT Hardening

### Goals

- Ensure auth is robust against common bypasses and token mishandling.

### Checklist (apply `vibesec-skill`)

- **JWT validation**
  - Algorithm explicitly restricted
  - Validate issuer/audience (if used)
  - Reasonable expiration + clock skew
  - Key length/entropy appropriate (HMAC) or correct key type (RSA/ECDSA)
- **Token storage approach**
  - Prefer httpOnly cookie storage when feasible; if using bearer tokens in JS, document risks and mitigations.
- **Refresh tokens**
  - Rotation, revocation strategy, theft detection (if refresh is implemented)
- **Account lifecycle**
  - Account deletion invalidates sessions/tokens

### Deliverables

- `.temp/security/phase-2-auth.md`
  - Findings + exact config/code locations
  - Proposed changes (minimal diff first)
  - Test plan (happy path + bypass attempts)

---

## Phase 3 — Authorization (IDOR / Ownership / Role Checks)

### Goals

- Prevent horizontal/vertical privilege escalation and IDOR across all CRUD endpoints.

### Checklist (apply `vibesec-skill`)

- Verify **ownership checks at the data layer** (not only controller routing).
- Ensure “public profile” read-only paths do not allow edits via alternate endpoints.
- Ensure bulk toggle operations cannot affect other users’ state.
- Consider returning **404 vs 403** where appropriate to reduce enumeration.

### Deliverables

- `.temp/security/phase-3-authorization.md`
  - List of endpoints audited
  - For each: authz rule, current enforcement point, fix (if needed)

---

## Phase 4 — Input Validation, Mass Assignment, and Safe Error Handling

### Goals

- Ensure all request DTOs are validated server-side.
- Avoid over-posting/mass assignment.
- Avoid leaking internals in production.

### Checklist (apply `vibesec-skill`)

- Validate IDs, pagination, sorting fields (whitelist `OrderBy` fields).
- Validate constraints/priorities payloads (ranges, stat types, operators).
- Ensure consistent problem responses (no stack traces in prod).
- Confirm logging does not include secrets (tokens, connection strings).

### Deliverables

- `.temp/security/phase-4-validation-errors.md`

---

## Phase 5 — Web Security: CORS, Security Headers, CSP, CSRF

### Goals

- Reduce cross-origin and browser attack surface.

### Checklist (apply `vibesec-skill`)

- **CORS**: no wildcard + credentials combination; restrict origins/methods/headers.
- **Headers**: `X-Content-Type-Options`, `Referrer-Policy`, `X-Frame-Options`/`frame-ancestors`, appropriate caching headers for sensitive responses.
- **CSP**: feasible policy for SPA + API, document constraints.
- **CSRF**: if using cookies for auth, ensure CSRF protections are correct.

### Deliverables

- `.temp/security/phase-5-web.md`

---

## Phase 6 — SSRF / Open Redirect / File Handling (only if applicable)

### Goals

- Close off “server makes requests on user input” risks.

### Checklist (apply `vibesec-skill`)

- Any URL fetching/proxying (IGDB proxy, image fetching, metadata) is allowlisted and has timeouts/size limits.
- Redirect parameters are allowlisted or relative-only.
- Uploads (if any) validate type/signature, size limits, safe storage, and safe serving headers.

### Deliverables

- `.temp/security/phase-6-ssrf-redirects-files.md`

---

## Phase 7 — Frontend (Angular) Hardening

### Goals

- Prevent XSS and token exfiltration risks.

### Checklist (apply `vibesec-skill`)

- Locate any `innerHTML`, sanitizer bypasses, unsafe markdown/SVG rendering.
- Confirm token storage strategy and XSS implications are understood/mitigated.
- Ensure error rendering doesn’t reflect raw server messages in unsafe contexts.

### Deliverables

- `.temp/security/phase-7-frontend.md`

---

## Phase 8 — Security Regression Tests + Documentation

### Goals

- Lock fixes in with tests and clear docs so they don’t regress.

### Steps

- Add/extend xUnit tests for critical authz paths and high-risk business rules.
- Add a “Security Considerations” section to relevant spec docs if new rules are introduced.
- Summarize changes, remaining risks, and follow-ups.

### Deliverables

- `.temp/security/phase-8-regression.md`
- Update `spec/PROGRESS.md` with completed security items (if you’re tracking progress there).

---

## Recommended “Agent prompt” per phase

Paste and adjust:

> Use `/vibesec-skill`. Execute **Phase X** from `spec/SECURITY-HARDENING-PLAYBOOK.md`.  
> Produce: findings (severity-labeled), exploit scenario, exact file locations, and a minimal fix plan.  
> Then implement fixes as small commits with a short test plan per commit. Store working notes under `.temp/security/`.

