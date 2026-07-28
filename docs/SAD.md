# VacaFlow — Software Architecture Document

| Field | Value |
|---|---|
| Project | VacaFlow |
| Company | BIG Solutions |
| Document | `SAD.md` (Deliverable 1 of 6 — Project Documentation) |
| Version | 1.0 |
| Date | 2026-07-28 |
| Baseline | [`Intent.md`](Intent.md) · [`FRD.md`](FRD.md) · [`NFR.md`](NFR.md) · [`Backlog.md`](Backlog.md) |

> **Purpose.** Decide *how* the system is built. This document resolves the choices the FRD deliberately left open — session mechanism, project structure, aggregate boundaries, where each port lives — and records the reasoning as ADRs so a later reader can tell a decision from an accident.
>
> **Governing tension.** Two constraints pull against each other and shape every decision here: the architecture rules (`reglas-clean-architecture-onion.md`) are normative, while the sponsor explicitly forbade MediatR, CQRS, event sourcing, generic repositories, messaging and microservices. The resolution applied throughout is **strict boundaries, plain mechanics**: physical layer separation and inward-only dependencies, implemented with ordinary classes and constructor injection.

---

## 1. Architectural drivers

| # | Driver | Source | Architectural consequence |
|---|---|---|---|
| D1 | Identity must never come from the client | `TC-08`, `NFR-SEC-003` | A single `ICurrentUser` port; no identity parameter in any command |
| D2 | Business rules must not live only in the UI | `RK-05`, `NFR-SEC-004` | Invariants inside aggregates; authorization in the application layer |
| D3 | Decision and state change must be atomic | `FR-DEC-009`, `NFR-REL-001` | `Approval` is part of the `Request` aggregate, one transaction |
| D4 | No unnecessary patterns | `TC-06`, `RK-03` | Handlers injected directly; no mediator, no generic repository |
| D5 | Must run locally from source | `TC-09`, `NFR-POR-001` | SQLite file, two processes, no container |
| D6 | The rules must be testable without infrastructure | `CA-TST-002`, `NFR-MNT-006` | Pure domain; `TimeProvider` injected |
| D7 | Foundation for a later scope decision | `OBJ-05` | Ports isolate every replaceable adapter |

---

## 2. Solution structure

### 2.1 Physical layout

Layer separation is by **project**, not by folder — folders do not prevent a forbidden reference, project boundaries do (`CA-STR-001`).

```
vacaflow/
├── VacaFlow.sln
├── src/
│   ├── BigSolutions.VacaFlow.Domain/
│   ├── BigSolutions.VacaFlow.Application/
│   ├── BigSolutions.VacaFlow.Infrastructure/
│   ├── BigSolutions.VacaFlow.Api/
│   └── web/                                  # Next.js application
├── tests/
│   ├── BigSolutions.VacaFlow.Domain.UnitTests/
│   ├── BigSolutions.VacaFlow.Application.UnitTests/
│   └── BigSolutions.VacaFlow.ArchitectureTests/
└── docs/
```

### 2.2 Permitted references

| Project | May reference |
|---|---|
| `Domain` | *nothing internal* — BCL only |
| `Application` | `Domain` |
| `Infrastructure` | `Application`, `Domain` |
| `Api` | `Application`, `Domain`, `Infrastructure` *(composition root only)* |
| `web` | the HTTP API only |

`Api` references `Infrastructure` **solely** to invoke `AddInfrastructure()` in `Program.cs`. No endpoint may reference an infrastructure type; this is asserted by an architecture test (`CA-DEP-008`).

### 2.3 Ring contents

| Ring | Contents |
|---|---|
| **Domain** | Aggregates, entities, value objects, enums, strongly-typed ids, domain errors, `Result`, the approval policy domain service |
| **Application** | Use case handlers, command and query records, DTOs, ports |
| **Infrastructure** | `DbContext`, entity configurations, repositories, unit of work, password hasher, seeder, migrations |
| **Api** | Minimal API endpoints, authentication configuration, error mapping, `ICurrentUser` implementation, composition root |
| **web** | Pages, components, the API client |

---

## 3. Domain model

### 3.1 Aggregates

Two aggregate roots and one seeded catalog entity.

| Aggregate root | Members | Rationale |
|---|---|---|
| `Employee` | `Employee` | Identity and manager assignment change independently of requests |
| `Request` | `Request`, `Approval` | The decision and the state change are one transactional fact (D3) |
| `AbsenceType` | `AbsenceType` | Seeded catalog, read-only at runtime |

References across aggregates are **by identity**, never by navigation (`CA-DOM-007`). `Request` holds an `EmployeeId`, not an `Employee`.

```
┌──────────────────────┐         ┌─────────────────────────────┐
│  Employee  «root»    │         │      Request  «root»        │
│  ─────────────────   │         │  ────────────────────────   │
│  EmployeeId Id       │◄────────│  EmployeeId OwnerId         │
│  string FullName     │ by id   │  AbsenceTypeId TypeId       │
│  Email Email  «VO»   │         │  DateRange Period    «VO»   │
│  EmployeeRole Role   │         │  string Reason              │
│  bool IsActive       │         │  RequestState State         │
│  EmployeeId? ManagerId├───┐    │  Approval? Approval  ────┐  │
└──────────────────────┘   │    └──────────────────────────┼──┘
            ▲              │                               │
            └──────────────┘                    ┌──────────▼──────────┐
             self-reference                     │  Approval  «child»  │
             (manager assignment)               │  ─────────────────  │
                                                │  EmployeeId Manager │
┌──────────────────────┐                        │  Decision Decision  │
│ AbsenceType  «root»  │                        │  string? Comment    │
│  Code · Name · Active│                        │  DateTime DecidedAt │
└──────────────────────┘                        └─────────────────────┘
```

### 3.2 Value objects

| Value object | Invariant enforced at construction |
|---|---|
| `Email` | Non-empty, valid format, ≤ 200 characters, normalized to lower case |
| `DateRange` | `End` ≥ `Start` (`RULE-01`). Both are `DateOnly` — no time, no time zone (`AS-04`) |
| `EmployeeId`, `RequestId`, `ApprovalId`, `AbsenceTypeId` | Non-empty `Guid` |

`DateRange` is where `RULE-01` becomes unbreakable: a request cannot hold an invalid range because the range itself cannot be constructed (`CA-DOM-005`).

`RULE-02` is **not** a `DateRange` invariant — it depends on the current date, and a value object must not read a clock (`CA-DOM-009`). It is checked by `Request.Create`, `Request.UpdateDetails` and `Request.Submit`, each receiving today's date as a parameter.

### 3.3 Request behavior

The aggregate owns its transitions. There is no public state setter (`CA-DOM-002`).

```csharp
public sealed class Request : AggregateRoot<RequestId>
{
    public EmployeeId OwnerId { get; private set; }
    public AbsenceTypeId AbsenceTypeId { get; private set; }
    public DateRange Period { get; private set; }
    public string Reason { get; private set; }
    public RequestState State { get; private set; }
    public Approval? Approval { get; private set; }

    private Request() { }                       // ORM only

    public static Result<Request> Create(
        EmployeeId owner, AbsenceTypeId type,
        DateRange period, string reason, DateOnly today)
    {
        if (period.Start < today)
            return Result.Failure<Request>(RequestErrors.StartDateInPast);
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 500)
            return Result.Failure<Request>(RequestErrors.ReasonRequired);

        return Result.Success(new Request(owner, type, period, reason));
    }

    public Result UpdateDetails(AbsenceTypeId type, DateRange period,
                                string reason, DateOnly today)
    {
        if (State is not RequestState.Draft)
            return Result.Failure(RequestErrors.OnlyDraftEditable);
        // … same date and reason checks …
    }

    public Result Submit(DateOnly today, DateTime nowUtc)
    {
        if (State is not RequestState.Draft)
            return Result.Failure(RequestErrors.InvalidTransition(State, RequestState.Submitted));
        if (Period.Start < today)
            return Result.Failure(RequestErrors.StartDateInPast);   // FR-LFC-003

        State = RequestState.Submitted;
        SubmittedAtUtc = nowUtc;
        return Result.Success();
    }

    public Result Cancel(DateTime nowUtc) { /* Draft or Submitted only */ }

    public Result Decide(EmployeeId responsibleManager, DecisionType decision,
                         string? comment, DateTime nowUtc)
    {
        if (State is not RequestState.Submitted)
            return Result.Failure(RequestErrors.OnlySubmittedDecidable);   // RULE-05
        if (Approval is not null)
            return Result.Failure(RequestErrors.AlreadyDecided);           // RULE-09

        Approval = Approval.Create(responsibleManager, decision, comment, nowUtc);
        State = decision is DecisionType.Approved
            ? RequestState.Approved : RequestState.Rejected;
        ClosedAtUtc = nowUtc;
        return Result.Success();
    }
}
```

`Decide` cannot produce a state change without an approval record, or the reverse — they are two statements in one method on one object, committed in one transaction. That is how `NFR-REL-001` is satisfied structurally rather than by discipline.

### 3.4 Approval policy — domain service

`RULE-06` and `RULE-07` span two aggregates: they compare the acting manager against the request owner. That logic has no single natural owner, so it belongs in a stateless domain service (`CA-SRV-001`, `CA-SRV-002`).

```csharp
public static class ApprovalPolicy          // Domain/Requests/Services
{
    public static Result CanDecide(Request request, Employee owner, Employee actingManager)
    {
        if (actingManager.Role is not EmployeeRole.Manager)
            return Result.Failure(ApprovalErrors.NotAManager);              // RULE-06

        if (owner.Id == actingManager.Id)
            return Result.Failure(ApprovalErrors.SelfDecisionForbidden);    // RULE-07

        if (owner.ManagerId is null)
            return Result.Failure(ApprovalErrors.NoManagerAssigned);        // OQ-01

        if (owner.ManagerId != actingManager.Id)
            return Result.Failure(ApprovalErrors.NotAssignedManager);       // RULE-06

        return Result.Success();
    }
}
```

It receives loaded entities and returns a `Result`. It never touches a repository (`CA-SRV-003`), so it is unit-testable with no infrastructure.

> **`OQ-01`.** The `owner.ManagerId is null` branch returns a distinct error rather than defaulting to permission. It fails closed. When the sponsor decides how assignment happens, only this branch and the registration use case change — no other code moves. That containment is the reason the check is here rather than scattered through the handler.

### 3.5 Errors

Expected business failures are `Result` values, not exceptions (`CA-APP-009`, `CA-DOM-010`). Each `Error` carries the code from the FRD catalog, so mapping to HTTP is a lookup rather than a translation.

```csharp
public sealed record Error(string Code, string Message);

public static class RequestErrors
{
    public static readonly Error OnlyDraftEditable =
        new("VF-REQ-003", "Only Draft requests can be edited.");
}
```

Exceptions are reserved for the genuinely exceptional — a failed database connection, a corrupt configuration.

---

## 4. Application layer

### 4.1 Use case handlers

One class per use case, named in business language, registered scoped, injected straight into the endpoint. No mediator (`ADR-002`).

```
Application/
├── Abstractions/                     # ports
│   ├── IEmployeeRepository.cs
│   ├── IRequestRepository.cs
│   ├── IAbsenceTypeRepository.cs
│   ├── IUnitOfWork.cs
│   ├── ICurrentUser.cs
│   └── IPasswordHasher.cs
├── Authentication/
│   ├── RegisterEmployeeHandler.cs
│   ├── LoginHandler.cs
│   └── GetCurrentUserHandler.cs
├── AbsenceTypes/
│   └── ListAbsenceTypesHandler.cs
└── Requests/
    ├── CreateRequestHandler.cs
    ├── UpdateRequestHandler.cs
    ├── SubmitRequestHandler.cs
    ├── CancelRequestHandler.cs
    ├── DecideRequestHandler.cs        # approve and reject
    └── ListVisibleRequestsHandler.cs
```

Approve and reject share `DecideRequestHandler` because they differ only in the `DecisionType` — the authorization path, the record created and the transaction are identical (`FR-DEC-005`).

### 4.2 Handler shape

```csharp
public sealed class SubmitRequestHandler(
    IRequestRepository requests,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
{
    public async Task<Result> Handle(RequestId id, CancellationToken ct)
    {
        var request = await requests.GetByIdAsync(id, ct);
        if (request is null) return Result.Failure(RequestErrors.NotFound);

        if (request.OwnerId != currentUser.EmployeeId)          // RULE-04
            return Result.Failure(RequestErrors.NotOwner);

        var now = timeProvider.GetUtcNow();
        var result = request.Submit(DateOnly.FromDateTime(now.Date), now.UtcDateTime);
        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
```

Three properties hold in every handler:

1. The acting user comes from `ICurrentUser`, never from a parameter (D1).
2. Ownership and role checks — *authorization* — sit here; invariants sit in the aggregate.
3. The transaction boundary is the handler, expressed through `IUnitOfWork` without knowing the mechanism (`CA-APP-008`).

### 4.3 Ports

Declared in `Application`, implemented in `Infrastructure` or `Api` (`CA-DEP-004`).

| Port | Implemented in | Why |
|---|---|---|
| `IEmployeeRepository`, `IRequestRepository`, `IAbsenceTypeRepository` | Infrastructure | EF Core access, one per aggregate root (`CA-INF-004`) |
| `IUnitOfWork` | Infrastructure | Wraps `DbContext.SaveChangesAsync` |
| `IPasswordHasher` | Infrastructure | Hashing is a technical concern |
| `ICurrentUser` | **Api** | It reads claims from `HttpContext`; keeping it there stops the web framework leaking into Infrastructure |
| `TimeProvider` | BCL | No custom port needed — `TimeProvider` is in the BCL, so the domain may depend on it (`CA-DEP-003`) |

There is no `IRepository<T>`. Each repository exposes only the operations its aggregate needs (`CA-INF-004`).

---

## 5. Infrastructure

### 5.1 Persistence

```
Infrastructure/
├── Persistence/
│   ├── VacaFlowDbContext.cs
│   ├── Configurations/            # one IEntityTypeConfiguration per aggregate
│   ├── Repositories/              # internal sealed
│   ├── Migrations/
│   ├── UnitOfWork.cs
│   └── DatabaseSeeder.cs
├── Security/
│   └── Pbkdf2PasswordHasher.cs
└── DependencyInjection.cs         # AddInfrastructure()
```

Every implementation type is `internal sealed`; only `AddInfrastructure()` is public (`CA-DEP-007`, `CA-CFG-002`). The API physically cannot construct a repository.

Mapping is Fluent API only. No domain type carries a persistence attribute (`CA-DOM-001`). Value objects map as owned types or through value converters; strongly-typed ids map through converters.

### 5.2 Schema

| Table | Columns | Indexes and constraints |
|---|---|---|
| `Employees` | `Id`, `FullName`, `Email`, `Role`, `IsActive`, `ManagerId` | `UNIQUE(Email)`; `FK ManagerId → Employees(Id)` |
| `UserAccounts` | `Id`, `EmployeeId`, `PasswordHash`, `CreatedAtUtc` | `UNIQUE(EmployeeId)`; `FK → Employees(Id)` |
| `AbsenceTypes` | `Id`, `Code`, `Name`, `IsActive` | `UNIQUE(Code)` |
| `Requests` | `Id`, `EmployeeId`, `AbsenceTypeId`, `StartDate`, `EndDate`, `Reason`, `State`, `CreatedAtUtc`, `UpdatedAtUtc`, `SubmittedAtUtc`, `ClosedAtUtc` | `FK EmployeeId`, `FK AbsenceTypeId`; index on `(EmployeeId, State)` |
| `Approvals` | `Id`, `RequestId`, `ResponsibleManagerId`, `Decision`, `Comment`, `DecidedAtUtc` | **`UNIQUE(RequestId)`** — `RULE-09` safety net; `FK ResponsibleManagerId → Employees(Id)` |

`UserAccounts` is a technical table with no domain entity behind it (`Intent.md` §7.1). It is mapped and accessed inside `Infrastructure` only.

The `UNIQUE(RequestId)` constraint is a **safety net**, not the enforcement point. The rule lives in `Request.Decide` (`CA-INF-003`).

The index on `(EmployeeId, State)` serves both list queries in `FR-VIS-001`.

### 5.3 Password hashing

PBKDF2-HMAC-SHA256 · 210,000 iterations · 128-bit random salt per password · 256-bit derived key · constant-time comparison. Stored as a single encoded string carrying the algorithm, iteration count, salt and hash, so the parameters can be raised later without invalidating existing accounts.
*Satisfies:* `NFR-SEC-001`

### 5.4 Database creation and seeding

EF Core migrations, applied at startup (`ADR-008`). The seeder is idempotent: it inserts the three absence types and the seeded manager only when absent (`FR-DAT-004`), matching on `AbsenceType.Code` and `Employee.Email`.

---

## 6. API

### 6.1 Structure

```
Api/
├── Endpoints/
│   ├── AuthEndpoints.cs
│   ├── AbsenceTypeEndpoints.cs
│   └── RequestEndpoints.cs
├── Contracts/                # request and response records — Api-owned (CA-PRE-003)
├── Security/
│   └── CurrentUserAccessor.cs
├── ErrorHandling/
│   └── ResultExtensions.cs   # Result → IResult, centralized (CA-PRE-004)
├── appsettings.json
└── Program.cs                # composition root (CA-CFG-001)
```

### 6.2 Endpoint shape

Receive, delegate, map. No business conditional, no data access (`CA-PRE-001`).

```csharp
group.MapPost("/{id:guid}/approve",
    async (Guid id, ApproveRequestContract body,
           DecideRequestHandler handler, CancellationToken ct) =>
    {
        var result = await handler.Handle(
            new RequestId(id), DecisionType.Approved, body.Comment, ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization();
```

The contract carries **only** the comment. There is no `responsibleManagerId` to send, so `FR-DEC-006` is satisfied by the shape of the contract, not by a runtime check.

### 6.3 Error mapping

One `ToHttpResult()` extension maps every `Error.Code` to its status from the FRD §7 catalog and emits `{ code, message, field? }`. Unhandled exceptions are caught by a single exception handler returning a generic `500` that leaks no internals (`FR-ERR-002`, `FR-ERR-003`, `NFR-USA-003`).

### 6.4 Authentication

Cookie authentication (`ADR-003`). On login the API issues an `HttpOnly`, `SameSite=Lax` cookie carrying the employee id and role as claims. `CurrentUserAccessor` reads those claims and exposes them through `ICurrentUser`.

Role checks use `.RequireAuthorization()` at the endpoint for coarse gating; the *business* permission rules stay in the application and domain layers (`CA-PRE-005`).

---

## 7. Web application

### 7.1 Structure

```
web/
├── app/
│   ├── login/page.tsx
│   ├── register/page.tsx
│   ├── requests/page.tsx           # My Requests
│   ├── requests/new/page.tsx
│   ├── requests/[id]/page.tsx      # form or read-only detail
│   └── queue/page.tsx              # Manager queue
├── components/                     # RequestList, RequestForm, DecisionDialog, …
├── lib/
│   ├── api.ts                      # fetch wrapper, credentials: 'include'
│   └── session.ts
└── next.config.mjs
```

### 7.2 Session and origins

The browser talks to **one origin**. `next.config.mjs` rewrites `/api/*` to the .NET API, so the authentication cookie is first-party and CORS never enters the picture (`ADR-009`).

If the proxy is not used, the API must enable CORS with an explicit origin and `AllowCredentials`; a wildcard origin is invalid with credentials. Note that `localhost:3000` and `localhost:5001` are *same-site* — port is not part of the site — so `SameSite=Lax` still works. The proxy is preferred because it removes the question entirely.

### 7.3 Client rules

- No business rule is implemented in the frontend. The UI hides actions that are invalid for the current role and state as an affordance, and the API rejects them regardless (`FR-UIX-002`, `RK-05`).
- After every mutation the list is refetched (`FR-UIX-005`, `NFR-REL-007`).
- Every API error surfaces to the user with its message (`FR-UIX-003`).
- A `401` returns the user to login with an explanation (`FR-UIX-007`).

---

## 8. Composition and configuration

`Program.cs` is the only place that knows every layer (`CA-CFG-001`):

```csharp
builder.Services.AddApplication();                       // handlers
builder.Services.AddInfrastructure(builder.Configuration); // DbContext, repos, hasher
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ICurrentUser, CurrentUserAccessor>();
builder.Services.AddAuthentication(/* cookie */).AddCookie(/* … */);
```

| Service | Lifetime |
|---|---|
| `DbContext`, `IUnitOfWork`, repositories, handlers, `ICurrentUser` | Scoped |
| `IPasswordHasher`, `TimeProvider` | Singleton |

No `IServiceProvider.GetService()` inside any layer (`CA-CFG-003`). No mutable static state (`CA-CFG-004`). Startup fails fast when required configuration is missing (`NFR-OPS-004`).

The connection string and the cookie signing configuration come from configuration, never from source (`NFR-SEC-006`).

---

## 9. Architecture decision records

### `ADR-001` — Reduced Onion in five physical projects
**Decision.** Domain, Application, Infrastructure, Api as separate .NET projects, plus the Next.js application.
**Alternatives.** Folders in one project — rejected: folders do not stop a forbidden reference, and `CA-STR-001` requires physical separation. More projects (Contracts, Shared) — rejected as unnecessary for four entities.
**Consequence.** The dependency rule is enforceable by the compiler and by architecture tests.

### `ADR-002` — No mediator; handlers injected directly
**Decision.** Endpoints depend on a handler class directly through constructor injection.
**Alternatives.** MediatR — explicitly forbidden (`TC-06`), and it would add indirection with no benefit at this size.
**Consequence.** Cross-cutting behavior (validation, logging) is not available as a pipeline. At this scale it is applied inside handlers or at the endpoint. If the system grows, `CA-CRS-003` will argue for decorators — a later decision.

### `ADR-003` — Cookie authentication rather than bearer tokens
**Decision.** ASP.NET Core cookie authentication with `HttpOnly` and `SameSite=Lax`.
**Alternatives.** A JWT held in `localStorage` — rejected: readable by any injected script, and logout cannot truly invalidate it. A JWT in memory — rejected: lost on refresh, needing a refresh-token mechanism that is more machinery than this MVP warrants.
**Consequence.** `FR-AUT-008` (logout invalidates the session) works as specified. The frontend stores no credential material.
*Satisfies:* `TC-07`, `NFR-SEC-005`

### `ADR-004` — `Approval` is part of the `Request` aggregate
**Decision.** `Approval` is a child entity of `Request`, not a separate aggregate root.
**Alternatives.** A separate aggregate with its own repository — rejected: it would place `RULE-08` and `RULE-09` across a boundary and make atomicity a coordination problem instead of an invariant.
**Consequence.** `FR-DEC-009` and `NFR-REL-001` hold structurally. There is no `IApprovalRepository`.

### `ADR-005` — Manager-assignment rule as a domain service
**Decision.** `ApprovalPolicy` — stateless, in Domain, receiving loaded entities.
**Alternatives.** In the handler — rejected: it is business logic, and `CA-APP-010` forbids business conditionals in orchestration. In `Request` — rejected: `Request` would need to know about `Employee`, coupling two aggregates.
**Consequence.** `RULE-06` and `RULE-07` are unit-testable with three plain objects and no mocks.

### `ADR-006` — `Result<T>` for expected failures
**Decision.** Business failures return `Result`; exceptions are reserved for the exceptional.
**Alternatives.** Exceptions for rule violations — rejected: control flow by exception, and every rule violation here is an expected outcome.
**Consequence.** Error codes flow from domain to HTTP without translation. Handlers have an explicit failure path.

### `ADR-007` — Strongly-typed identifiers
**Decision.** `EmployeeId`, `RequestId`, `AbsenceTypeId`, `ApprovalId` as readonly record structs with EF value converters.
**Alternatives.** Bare `Guid` — rejected despite `CA-DOM-006` being only a 🟡 recommendation, because this domain passes an owner id, a manager id and a type id through the same methods, and they are mutually substitutable when bare.
**Cost.** One value converter per id, and `Guid` conversion at the API boundary. Accepted.

### `ADR-008` — EF Core migrations rather than `EnsureCreated`
**Decision.** One initial migration, applied at startup.
**Alternatives.** `EnsureCreated()` — simpler, and `TC-10` permits it, but it produces no schema history and cannot evolve a database without deleting it.
**Consequence.** `CA-INF-008` is satisfied and the reviewer still gets a working database from a single start command. Cost: one extra tooling package.

### `ADR-009` — Next.js proxies `/api` to the .NET API
**Decision.** A rewrite in `next.config.mjs` so the browser sees a single origin.
**Alternatives.** Direct cross-origin calls with CORS and `AllowCredentials` — workable but adds configuration whose failure mode (credentialed requests silently dropped) is confusing to debug.
**Consequence.** No CORS configuration in the MVP. The API base URL is a single environment variable.

### `ADR-010` — PBKDF2 with encoded parameters
**Decision.** PBKDF2-HMAC-SHA256, 210,000 iterations, per-password salt, parameters encoded in the stored string.
**Alternatives.** Argon2id — stronger, but needs a third-party package; PBKDF2 is in the framework and adequate for an internal MVP. A bare hash — forbidden by `LC-02`.
**Consequence.** Parameters can be raised later without invalidating stored accounts.

---

## 10. Architecture tests

`BigSolutions.VacaFlow.ArchitectureTests` runs with `dotnet test` (`TE-006`, `NFR-MNT-001`–`003`).

| Test | Rule |
|---|---|
| `Domain` has no dependency on `Application`, `Infrastructure` or `Api` | `CA-DEP-001`, `CA-DEP-002` |
| `Domain` has no dependency on EF Core, ASP.NET Core or a serializer | `CA-DEP-003` |
| `Application` has no dependency on EF Core or ASP.NET Core | `CA-APP-004`, `CA-APP-005` |
| No endpoint type references `DbContext` or a repository implementation | `CA-DEP-008` |
| Repository and unit-of-work types are not public | `CA-DEP-007` |
| No type in `Domain` ends in `Dto`, `Request`, `Response` or `ViewModel` | `CA-DOM-011` |
| No cycles between projects | `CA-DEP-005` |
| `Domain` and `Application` contain no `DateTime.Now` or `DateTime.UtcNow` | `CA-DOM-009`, `CA-CRS-002` |

> `Request` as a domain type name would trip the `CA-DOM-011` naming assertion. The rule targets DTO-suffixed types, and `Request` here is the core business concept, not a transport object. The test excludes this exact type by name, with a comment stating why — a deliberate, recorded exclusion rather than a weakened rule.

---

## 11. Rule compliance summary

| Rule | How it is satisfied |
|---|---|
| `CA-DEP-001`–`005` | Physical projects, inward references only, asserted by tests |
| `CA-DEP-007` | Infrastructure types `internal sealed`; only `AddInfrastructure()` public |
| `CA-DEP-008` | Endpoints depend only on handlers |
| `CA-DOM-001` | Fluent API configuration; no attributes on domain types |
| `CA-DOM-002` | Private constructors, private setters, static factories returning `Result` |
| `CA-DOM-003` | `Request` owns its transitions; the model is not anemic |
| `CA-DOM-005` | `Email` and `DateRange` as value objects |
| `CA-DOM-006` | Strongly-typed ids (`ADR-007`) |
| `CA-DOM-007` | Two aggregate roots; cross-aggregate references by identity |
| `CA-DOM-009` | `TimeProvider` injected; today's date passed into domain methods |
| `CA-SRV-001`–`003` | `ApprovalPolicy` is stateless and free of persistence |
| `CA-APP-003` | All ports declared in `Application/Abstractions` |
| `CA-APP-004`, `CA-APP-005` | No web or ORM types in `Application`, asserted by tests |
| `CA-APP-008` | `IUnitOfWork` marks the transaction boundary in the handler |
| `CA-INF-001`, `CA-INF-004` | Every implementation backs a port; one repository per aggregate root |
| `CA-INF-003` | The only database-level rule is a uniqueness safety net |
| `CA-INF-007` | Connection string and cookie configuration from configuration |
| `CA-PRE-001` | Endpoints receive, delegate, map |
| `CA-PRE-003` | API contracts are `Api`-owned records |
| `CA-PRE-004` | Single `ToHttpResult()` mapping plus one exception handler |
| `CA-CFG-001`–`004` | `Program.cs` is the sole composition root; no service locator; no mutable statics |
| `CA-TST-001` | Architecture tests exist — **run locally, not in a pipeline** (see §12) |
| `CA-TST-002` | Domain tests need no infrastructure |

---

## 12. Recorded deviations

| Rule | Severity | Deviation | Reason | Exit condition |
|---|---|---|---|---|
| `CA-TST-001` | 🔴 | Architecture tests run locally with `dotnet test`, not in a merge-blocking pipeline | CI/CD is out of scope (`OS-08`); the tests themselves exist and pass | `FUT-09` introduces CI/CD |
| `CA-CRS-003` | 🟡 | Cross-cutting concerns are applied inside handlers, not as pipeline behaviors | A mediator is forbidden (`TC-06`); decorators for ten handlers would cost more than they return | The handler count grows beyond roughly twenty |
| `CA-DOM-011` | 🟡 | The domain type `Request` matches a naming assertion aimed at DTOs | `Request` is the central business concept, named as the business names it | — |

No 🔴 rule other than `CA-TST-001` is deviated from, and that one is waived by an explicit scope decision rather than by convenience. Per `Intent.md` §18, deviations at 🟠 or 🟡 require this record; 🔴 rules admit none, so `CA-TST-001` is carried as an open confirmation (`OQ-03`) rather than as a closed decision.

---

## 13. Local execution

| # | Step |
|---|---|
| 1 | `dotnet run --project src/BigSolutions.VacaFlow.Api` — applies migrations, seeds, listens |
| 2 | `npm install && npm run dev` in `src/web` — serves the interface and proxies `/api` |
| 3 | Open the web application, register or sign in with the seeded manager |

Resetting: stop the API, delete `vacaflow.db`, start again. The migration and the seeder rebuild a clean state (`FR-DAT-006`, `NFR-OPS-001`).

Two processes, one file, no container, no external service (`NFR-POR-001`, `NFR-POR-003`).

---

## 14. Architectural risks

| Risk | Impact | Mitigation |
|---|---|---|
| `ICurrentUser` bypassed by a handler taking an id parameter | `RK-02` materializes; delivery rejected | No command record carries an identity field; reviewed explicitly at code review; `NFR-SEC-003` test |
| A rule implemented in the endpoint or in React instead of the domain | `RK-05` | `CA-PRE-001` line guideline; rule-to-test mapping in `NFR-MNT-007` |
| `ManagerId` null handled by defaulting to permitted | `RULE-06` silently broken | `ApprovalPolicy` fails closed with a distinct error; blocked on `OQ-01` |
| Aggregate boundary eroded by adding an `IApprovalRepository` | `NFR-REL-001` lost | `ADR-004`; no such port exists |
| Architecture drifts because tests are not in CI | Slow erosion | `TE-006` run before each handover; `FUT-09` closes it |

---

## 15. Open questions with architectural impact

| ID | Question | Architectural impact |
|---|---|---|
| `OQ-01` | How is `Employee.ManagerId` set? | Contained to `ApprovalPolicy` and `RegisterEmployeeHandler`. Assignment at registration adds a field to the register contract; a seeded default adds seeder logic; an assignment screen is `FUT-06` and a scope change |
| `OQ-02` | Is role selection allowed at registration? | If not, `RegisterEmployeeHandler` stops accepting a role and always creates an Employee |
| `OQ-03` | Confirm the `CA-TST-001` deviation | Recorded in §12; no code impact |
| `OQ-04` | Is `RULE-02` re-evaluated at submit? | One guard in `Request.Submit` |
| `OQ-05` | Confirm the stricter `RULE-06` | One branch in `ApprovalPolicy` |

Every one of them is contained to a named location. That containment is deliberate: an open question that would ripple through the design is a question that has to be answered before building, and none of these do.
