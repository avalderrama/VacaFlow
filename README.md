# VacaFlow

Internal vacation and absence request workflow — limited MVP for **BIG Solutions**.

Employees register, log in, create and submit absence requests. Managers log in and approve or reject the requests assigned to them. The system records the final state and the authenticated manager responsible for the decision.

> **Status: solution skeleton in place, no business behaviour yet.** The six planning documents are published (milestone `M0`) and work package `3.1` is done — the solution compiles, the API starts and the architecture tests pass. Work packages `3.2` onwards fill in the domain, persistence, use cases and endpoints.
>
> **Product language: English** throughout — interface, code, data values and documentation. Note that the prototype in `docs/prototype/` was built with Spanish copy; it is authoritative for layout and interaction, never for strings. See [`docs/Backlog.md`](docs/Backlog.md) §2 and §3.5.
>
> **Open decision:** how an employee's manager assignment is established. The prototype falls back to "the first manager in the table", which works for the demo but is not a business rule. See `OQ-01` in [`docs/Backlog.md`](docs/Backlog.md) §6.

## Scope in one line

Prove the complete request lifecycle — register → login → draft → edit → submit → decide → view result — with real local accounts, and nothing else.

The authoritative scope boundary is [`docs/Intent.md`](docs/Intent.md). Anything not listed there as in scope is out of scope.

---

## Running it

### Prerequisites

| Tool | Version used |
|---|---|
| .NET SDK | 10.0.103 |
| Node.js | 22.16 — for the web application, not yet scaffolded |

No database server, no container runtime, no cloud service. The database is a single SQLite file created on first run (`NFR-POR-003`).

### Commands

Build:

```bash
dotnet build VacaFlow.slnx
```

Run the API on a fixed port:

```bash
dotnet run --project src/BigSolutions.VacaFlow.Api --urls http://localhost:5080
```

`GET http://localhost:5080/health` then returns `{"status":"ok"}`.

Run every test, including the architecture suite:

```bash
dotnet test VacaFlow.slnx
```

### Resetting the database

Stop the API, delete `vacaflow.db` from the API project folder, start again. Migrations and the seeder rebuild a clean state (`FR-DAT-006`, `NFR-OPS-001`). *Applies once work package `3.3` lands.*

### Seeded accounts

Documented in [`docs/Backlog.md`](docs/Backlog.md) §3.6. They are clearly non-production and must never be reused anywhere real. *Applies once work package `3.4` lands.*

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
│   └── web/                                    # Next.js (work package 4.5)
└── tests/
    ├── BigSolutions.VacaFlow.Domain.UnitTests/
    ├── BigSolutions.VacaFlow.Application.UnitTests/
    ├── BigSolutions.VacaFlow.Infrastructure.IntegrationTests/
    └── BigSolutions.VacaFlow.ArchitectureTests/
```

Dependencies point inward only. `Domain` references no other project and no framework package — that is checked, not assumed.

## The architecture tests are the point

`BigSolutions.VacaFlow.ArchitectureTests` asserts the boundaries this project claims to have. A failure there is a rejected change, not a style opinion.

| Test file | Asserts |
|---|---|
| `DependencyRuleTests` | Dependency direction, framework isolation, `internal` infrastructure, no feature cycles |
| `NamingRuleTests` | No transport types in the domain, and the `Request` exception recorded as `DV-03` |
| `SourceRuleTests` | No static clock and no service locator — read from source, since IL inspection cannot see which member is called |

Several tests return early while the type they guard does not exist yet. They arm themselves as the code is written; none is a placeholder that always passes.

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
