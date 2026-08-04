# VacaFlow

Internal vacation and absence request workflow — limited MVP for **BIG Solutions**.

Employees register, log in, create and submit absence requests. Managers log in and approve or reject the requests assigned to them. The system records the final state and the authenticated manager responsible for the decision.

> **Status: the MVP is complete.** All fourteen acceptance criteria (`AC-01`–`AC-14` in [`docs/Intent.md`](docs/Intent.md) §13) are implemented and demonstrable end to end — register, log in, draft, edit, submit, cancel, approve, reject, and see the recorded decision. The API, the web application, the architecture tests and the unit / integration / functional suites all run locally from source.
>
> **Product language: English** throughout — interface, code, data values and documentation. Note that the prototype in `docs/prototype/` was built with Spanish copy; it is authoritative for layout and interaction, never for strings. See [`docs/Backlog.md`](docs/Backlog.md) §2 and §3.5.
>
> **Open decision — read this before demonstrating the approval flow.** How an employee's manager is assigned is still undecided (`OQ-01`). Registration does not assign one, so a **freshly registered employee has no manager**: their submitted requests reach nobody's approval queue, and a direct approve or reject fails with `VF-DEC-003`. Manager assignment exists only in the seeder. Demonstrate `AC-09`–`AC-13` with the seeded accounts below, not with an account you just created. See `OQ-01` in [`docs/Backlog.md`](docs/Backlog.md) §6.

## Scope in one line

Prove the complete request lifecycle — register → login → draft → edit → submit → decide → view result — with real local accounts, and nothing else.

The authoritative scope boundary is [`docs/Intent.md`](docs/Intent.md). Anything not listed there as in scope is out of scope.

---

## Running it

### Prerequisites

| Tool | Version used |
|---|---|
| .NET SDK | 10.0.103 |
| Node.js | 22.16 |

No database server, no container runtime, no cloud service. The database is a single SQLite file created on first run (`NFR-POR-003`).

### Build

```bash
dotnet build VacaFlow.slnx
```

### Starting the API

The API must be running before the web application is useful. Port `5217` is not optional — it is the port the web application proxies to (see below), and it matches the `http` profile in `src/BigSolutions.VacaFlow.Api/Properties/launchSettings.json`.

```bash
dotnet run --project src/BigSolutions.VacaFlow.Api --urls http://localhost:5217
```

`GET http://localhost:5217/health` then returns `{"status":"ok"}`. On first run the API creates the SQLite file, applies migrations and seeds the accounts and absence types listed below.

### Starting the web application

In a second terminal, with the API already running:

```bash
npm install --prefix src/web
```

```bash
npm --prefix src/web run dev
```

The application is then at `http://localhost:3000`. That redirects to `/requests`, which — with no session yet — gets `VF-AUT-004` from the API and sends you on to the sign-in screen.

`src/web/next.config.mjs` rewrites `/api/*` to `http://localhost:5217/api/*`, so the browser sees a single origin and the session cookie is first-party — there is no CORS configuration and no separate API base URL to set (`ADR-009`). If the API is not running on `5217`, every call from the web application fails.

### Running the tests

Every test, including the architecture suite:

```bash
dotnet test VacaFlow.slnx
```

Static checks for the web application:

```bash
npm --prefix src/web run lint && npm --prefix src/web run typecheck && npm --prefix src/web run depcruise
```

There is no frontend test runner — the web application is verified through the backend suites and manual end-to-end passes.

### Inspecting the database

The whole application state is one SQLite file, `src/BigSolutions.VacaFlow.Api/vacaflow.db` — from `"Data Source=vacaflow.db"` in `src/BigSolutions.VacaFlow.Api/appsettings.json`, resolved relative to the API's working directory. It is created on first run and is never committed.

Open it with any SQLite client — the `sqlite3` CLI, [DB Browser for SQLite](https://sqlitebrowser.org/), or a SQLite extension in your editor:

```bash
sqlite3 src/BigSolutions.VacaFlow.Api/vacaflow.db ".tables"
```

Reading while the API is running is fine; do not write to it while the API holds the connection. The tables are `Employees`, `UserAccounts` (email and password hash), `AbsenceTypes`, `Requests` and `Approvals`.

### Resetting the database

**This is irreversible and there are no backups** — it destroys every registered account, request and approval.

If the state matters, back it up **while the API is stopped**, so the copy cannot catch a half-written transaction, and take the `-shm` / `-wal` siblings with it if they exist:

```bash
cp src/BigSolutions.VacaFlow.Api/vacaflow.db* ~/vacaflow-backup/
```

Copy it outside the repository — this tree lives under OneDrive, and the file holds password hashes and personal data that a synced backup would replicate to cloud storage.

Then, still with the API stopped, delete `src/BigSolutions.VacaFlow.Api/vacaflow.db` along with any `vacaflow.db-shm` / `vacaflow.db-wal` beside it, and start again. Migrations and the seeder rebuild a clean state (`FR-DAT-006`, `NFR-OPS-001`).

If you have ever started the API from a different working directory, check for a stray `vacaflow.db` at the repository root too — that copy would be picked up on the next run from that directory.

### Seeded accounts

> ⚠️ **Non-production credentials.** These accounts exist only to make the MVP demonstrable without registering first. The emails use the `.test` domain reserved for exactly this purpose and must never be reused anywhere real.

| Name | Email | Password | Role | Manager |
|---|---|---|---|---|
| Laura Méndez | `manager@vacaflow.test` | `Manager123!` | Manager | — |
| Carlos Ruiz | `employee@vacaflow.test` | `Employee123!` | Employee | Laura Méndez |
| Ana Torres | `ana@vacaflow.test` | `Employee123!` | Employee | Laura Méndez |

Seeded on every startup against a database that does not already have them (`TE-003`) — restarting against an existing database creates no duplicates. See [`docs/Backlog.md`](docs/Backlog.md) §3.6.

The two seeded employees are the only accounts in the system with a manager assigned, because the seeder is the only caller of `Employee.AssignManager` (`OQ-01`, above). Sign in as **Carlos Ruiz** to raise a request and as **Laura Méndez** to decide it — that pair demonstrates the whole workflow.

---

## Endpoint summary

Every endpoint requires an authenticated session except the three marked anonymous. Authentication is a first-party session cookie, issued by `POST /api/auth/login` and also by a successful `POST /api/auth/register`; there is no bearer token and no API key.

**Health**

| Method | Path | Purpose |
|---|---|---|
| `GET` | `/health` | Liveness — returns `{"status":"ok"}`. Anonymous |

**Authentication**

| Method | Path | Purpose |
|---|---|---|
| `POST` | `/api/auth/register` | Create an account and sign in. Anonymous |
| `POST` | `/api/auth/login` | Sign in. Anonymous |
| `POST` | `/api/auth/logout` | Invalidate the session cookie |
| `GET` | `/api/auth/me` | The caller's own id, name, email and role |

**Catalog**

| Method | Path | Purpose |
|---|---|---|
| `GET` | `/api/absence-types` | The three active absence types |

**Requests**

| Method | Path | Purpose |
|---|---|---|
| `POST` | `/api/requests` | Create a Draft. Returns `201` with the new id |
| `GET` | `/api/requests` | What the caller may see, decided by role — no query parameters |
| `GET` | `/api/requests/{id}` | Request detail, including the decision once made |
| `PUT` | `/api/requests/{id}` | Edit a Draft |
| `POST` | `/api/requests/{id}/submit` | Draft → Submitted. Empty body |
| `POST` | `/api/requests/{id}/cancel` | Cancel. Empty body |
| `POST` | `/api/requests/{id}/approve` | Approve. Body `{ comment? }` |
| `POST` | `/api/requests/{id}/reject` | Reject. Body `{ comment? }` |

**The identity rule.** The API derives request ownership and approval responsibility from `ICurrentUser`; no handler reads an actor id from a payload. See [`docs/Intent.md`](docs/Intent.md) §7.5. `SourceRuleTests.No_Contract_Or_Command_Should_Carry_An_Identity_Field` blocks the three known identity field names in `*Contract.cs` / `*Command.cs` / `*Query.cs` / `*Request.cs` — but it is a source-scanning naming lint, not a semantic proof: an identity bound inline in an endpoint lambda, or named differently, would not trip it. Review new endpoints by hand.

**Errors.** Business and validation failures come back as `{ code, message, field? }` with a stable code. The catalog — `VF-AUT-*`, `VF-REQ-*`, `VF-DEC-*`, `VF-CAT-*`, `VF-VAL-001` — is in [`docs/FRD.md`](docs/FRD.md). Two further families exist but are not business errors: `VF-INT-001`–`003` for invariants that should be unreachable, and `VF-SRV-001`, written by the global exception handler on anything unhandled. Both map to `500`.

The exception handler returns that fixed body and logs the exception server-side, so no stack trace or provider detail reaches a client — note this rests on the middleware registration in `Program.cs` alone and no test pins it. Two framework-level cases fall outside the envelope. A `403` from the cookie handler carries no body — not currently reachable, since no endpoint declares a role or policy requirement, but it is there. And in Development, where minimal APIs throw on a bad request, a malformed request body surfaces as `500 VF-SRV-001`; in other environments it is a bodiless `400`.

---

## Scope limitations

This is a deliberately small MVP. The scope principle, taken from the project presentation, is: *add no feature that is not required to demonstrate the end-to-end request decision flow.*

What that means in practice:

- **Local accounts only.** No corporate SSO, no MFA, no password reset, no email verification, and no login rate limiting or account lockout — online guessing is unbounded. Passwords are validated on length alone, 8–128 characters, with no complexity, breach-list or reuse checks.
- **User enumeration is mitigated on login only.** `POST /api/auth/login` runs a matched-cost decoy hash for unknown emails, so timing does not distinguish them. `POST /api/auth/register` does not: it returns `VF-AUT-001` for an address that already exists, which is an unauthenticated existence oracle over the whole directory.
- **Role is self-elected.** The registration payload carries the role, and it is only checked for being a defined enum value — any anonymous caller can register as `Manager`. Harmless today only because no employee is ever assigned to a self-registered manager (`OQ-01`); it becomes privilege escalation the moment manager assignment is implemented.
- **Development transport posture.** The session cookie is `HttpOnly` and `SameSite=Lax` with an 8-hour sliding expiry, but `SecurePolicy` is left at its default and there is no HTTPS redirection or HSTS. Correct for localhost; the first thing to change before any deployment.
- **CSRF protection is `SameSite=Lax` alone.** There are no antiforgery tokens — state-changing calls rely on the browser not attaching the cookie to cross-site requests. Do not relax `SameSite` (to `None` for a cross-origin deployment, say) without adding token-based protection first.
- **Two roles.** Employee and Manager, chosen at registration. There is no account or role administration screen — see the self-election note below.
- **Absence types are seed data.** There is no maintenance screen for them.
- **Managers see only pending work.** The approval queue shows `Submitted` requests assigned to the caller — there is no manager history view.
- **Single-level approval.** One manager decides; no delegation and no multi-step chain.
- **No calendar intelligence.** No vacation balances, no holiday calendars, no working-day maths, no overlap detection between requests.
- **Runs from source, locally.** No Docker, no cloud hosting, no CI/CD pipeline, no backups.

The authoritative boundary is [`docs/Intent.md`](docs/Intent.md) — §5 for what is in scope, §6 for what is out. Anything not listed in §5 is out of scope.

### Known controlled deviations

| ID | Deviation |
|---|---|
| `DV-01` / `OQ-03` | `CA-TST-001` wants the architecture tests in a merge-blocking pipeline. CI/CD is out of scope (`OS-08`), so they run locally — run them before every handover |
| `FUT-30` | **The seeded demo accounts are created on every startup, in every environment.** `Program.cs` runs the database initializer unconditionally and `DatabaseSeeder` hard-codes the passwords, so `manager@vacaflow.test` / `Manager123!` would be a working manager login in a deployed build. The sign-in hint block is suppressed outside development (`TestAccountsBlock.tsx`), but that hides the signpost, not the door — any deployment must gate the seeder on `IHostEnvironment.IsDevelopment()` or remove it first |

## Deferred backlog

Everything below was **deliberately deferred**, not overlooked. Adding any of it is a separate scope decision, not a judgement call during implementation. Identifiers are the `OS-*` items in [`docs/Intent.md`](docs/Intent.md) §6; the post-MVP product backlog that elaborates them is Part B of [`docs/Backlog.md`](docs/Backlog.md).

| Area | Deferred |
|---|---|
| Identity and access | Entra ID / corporate SSO, MFA, password reset, email verification, account and role administration (`OS-01`–`OS-05`) |
| Hosting and delivery | Azure or any cloud hosting, Docker, CI/CD, automated backups, HA and production support, data migration (`OS-06`–`OS-11`) |
| Notifications | Email and Microsoft Teams notifications (`OS-12`) |
| Calendar and balances | Vacation balance calculation, holiday calendars and working-day maths, overlapping-request validation (`OS-13`–`OS-15`) |
| Workflow depth | Multi-level approvals, approval delegation, extra states such as "returned for correction" (`OS-19`–`OS-21`) |
| Views and reporting | HR views and administration, reports, dashboards, exports, approval letters, manager full-history screen (`OS-17`, `OS-18`, `OS-22`) |
| Other | Attachments, absence type maintenance screen, payroll / HR / calendar / directory integrations, audit trail beyond the Approval record (`OS-16`, `OS-23`–`OS-25`) |

---

## Layout

```
vacaflow/
├── VacaFlow.slnx
├── Directory.Build.props          # shared settings, warnings as errors
├── docs/                          # the six deliverable documents + prototype
├── src/
│   ├── BigSolutions.VacaFlow.Domain/           # ring 1 — no project references at all
│   ├── BigSolutions.VacaFlow.Application/      # ring 2 — use cases and ports
│   ├── BigSolutions.VacaFlow.Infrastructure/   # ring 3 — EF Core, SQLite, hashing
│   ├── BigSolutions.VacaFlow.Api/              # ring 4 — endpoints, composition root
│   └── web/                                    # Next.js — the browser application
└── tests/
    ├── BigSolutions.VacaFlow.Domain.UnitTests/
    ├── BigSolutions.VacaFlow.Application.UnitTests/
    ├── BigSolutions.VacaFlow.Infrastructure.IntegrationTests/
    ├── BigSolutions.VacaFlow.Api.FunctionalTests/
    └── BigSolutions.VacaFlow.ArchitectureTests/
```

Dependencies point inward only. `Domain` references no other project and no framework package — that is checked, not assumed.

## The architecture tests are the point

`BigSolutions.VacaFlow.ArchitectureTests` asserts the boundaries this project claims to have. A failure there is a rejected change, not a style opinion.

| Test file | Asserts |
|---|---|
| `DependencyRuleTests` | Dependency direction, framework isolation, `internal` infrastructure, no feature cycles |
| `NamingRuleTests` | No transport types in the domain, and the `Request` exception recorded as `DV-03` |
| `SourceRuleTests` | No non-deterministic statics (clock, `Guid.NewGuid`, `Random`) and no service locator; every endpoint declares its authorization; no contract or command carries an identity field; every domain error code has a status mapping, and thirteen of them are pinned to their exact documented status — read from source, since IL inspection cannot see which member is called |

These tests were written before the code they guard, and several returned early while their target types did not yet exist. Now that the MVP is complete, every guard finds its types and the suite is fully armed — none is a placeholder that always passes.

`CA-TST-001` requires these to run in a merge-blocking pipeline. CI/CD is out of scope, so they run locally — carried as deviation `DV-01` and open question `OQ-03`. Run them before every handover.

## Conventions

- **Warnings are errors.** `Directory.Build.props` sets `TreatWarningsAsErrors`, which includes the NuGet vulnerability audit. A dependency with a known advisory fails the build, satisfying `NFR-SEC-010`.
- **Nullable reference types are enabled** across every project.
- Folders group by business concept (`Employees/`, `Requests/`, `AbsenceTypes/`), never by technical type (`CA-STR-003`).
- Namespaces mirror the folder path, so a namespace always reveals its ring (`CA-STR-004`).

---

## Documentation

| Document | Purpose | Status |
|---|---|---|
| [`docs/Intent.md`](docs/Intent.md) | Project summary, scope, out of scope, business / technical / legal constraints, rules, acceptance criteria | ✅ Baseline v1.0 |
| [`docs/Backlog.md`](docs/Backlog.md) | User stories covering the whole solution and the MVP, with per-screen visual criteria | ✅ **v2.0** |
| [`docs/prototype/`](docs/prototype/) | Functional prototype — 11 screens and the markup that is the design source for the backlog | ✅ Reference |
| [`docs/FRD.md`](docs/FRD.md) | Functional Requirement Document | ✅ Baseline v1.0 |
| [`docs/NFR.md`](docs/NFR.md) | Non-Functional Requirement Document | ✅ Baseline v1.0 |
| [`docs/SAD.md`](docs/SAD.md) | Software Architecture Document, audited rule by rule against the `CA-*` rules | ✅ **v2.0** |
| [`docs/WBS.md`](docs/WBS.md) | Work Breakdown Structure | ✅ Baseline v1.0 |

Normative rule sets applied to this project (`Docs/` in the project workspace, outside this repository):

- `reglas-clean-architecture-onion.md` — `CA-*` architecture rules
- `reglas-diseno-ui-ux-web.md` — `UX-*` interface rules

## Stack

| Layer | Technology |
|---|---|
| Web | Next.js + React |
| API | ASP.NET Core Minimal API |
| Application / Domain | C#, reduced Onion Architecture |
| Persistence | Entity Framework Core + SQLite |

Explicitly **not** used: MediatR, CQRS, event sourcing, generic repositories, messaging, microservices, Docker, CI/CD, Azure.

## Notes for whoever picks this up

- `SQLitePCLRaw.bundle_e_sqlite3` is pinned to `2.1.12` in the Infrastructure project. The version EF Core 10 pulls transitively carries a high-severity advisory. Do not remove the pin without checking the audit still passes.
- The solution uses the `.slnx` format, which needs .NET SDK 9.0.200+ or Visual Studio 17.13+. `dotnet build` works regardless of editor. `SAD.md` §4.1 still says `VacaFlow.sln`; the document is the one that needs correcting.
- This tree lives under OneDrive. `bin/`, `obj/` and `node_modules/` generate heavy sync traffic; excluding the folder from OneDrive sync is worth the two minutes.

## Security note

The SQLite database file is never committed (see `.gitignore`). It stores emails, password hashes, names, request reasons and approval comments. Seed and demo accounts must use clearly non-production credentials.

Passwords are hashed with PBKDF2-HMAC-SHA256, 210,000 iterations, a per-password salt and a fixed-time comparison — no plain-text password is stored or logged.

**This is a localhost development posture, not a deployable one.** Before anyone deploys this, read `FUT-30` under [Known controlled deviations](#known-controlled-deviations) — the seeder creates working demo logins in every environment — together with the Scope limitations bullets on transport posture, CSRF, rate limiting and role self-election. Each names something a deployment has to close.
