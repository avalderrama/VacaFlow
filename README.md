# VacaFlow

Internal vacation and absence request workflow — limited MVP for **BIG Solutions**.

Employees register, log in, create and submit absence requests. Managers log in and approve or reject the requests assigned to them. The system records the final state and the authenticated manager responsible for the decision.

> **Status: documentation baseline complete, implementation not started.** The six planning documents below are published (milestone `M0`). No application code has been written yet — this README gains setup and run instructions once the solution is scaffolded (work package `3.1`).
>
> **Product language: English** throughout — interface, code, data values and documentation. Note that the prototype in `docs/prototype/` was built with Spanish copy; it is authoritative for layout and interaction, never for strings. See [`docs/Backlog.md`](docs/Backlog.md) §2 and §3.5.
>
> **Open decision:** how an employee's manager assignment is established. The prototype falls back to "the first manager in the table", which works for the demo but is not a business rule. See `OQ-01` in [`docs/Backlog.md`](docs/Backlog.md) §6.

## Scope in one line

Prove the complete request lifecycle — register → login → draft → edit → submit → decide → view result — with real local accounts, and nothing else.

The authoritative scope boundary is [`docs/Intent.md`](docs/Intent.md). Anything not listed there as in scope is out of scope.

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

## Planned stack

| Layer | Technology |
|---|---|
| Web | Next.js + React |
| API | ASP.NET Core Minimal API |
| Application / Domain | C#, reduced Onion Architecture |
| Persistence | Entity Framework Core + SQLite |

Explicitly **not** used: MediatR, CQRS, event sourcing, generic repositories, messaging, microservices, Docker, CI/CD, Azure.

## Security note

The SQLite database file is never committed (see `.gitignore`). It stores emails, password hashes, names, request reasons and approval comments. Seed and demo accounts must use clearly non-production credentials.
