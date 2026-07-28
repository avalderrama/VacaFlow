# VacaFlow — Software Architecture Document

| Field | Value |
|---|---|
| Project | VacaFlow |
| Company | BIG Solutions |
| Document | `SAD.md` (Deliverable 1 of 6 — Project Documentation) |
| Version | **2.0** — audited rule by rule against the normative architecture rules |
| Date | 2026-07-28 |
| Baseline | [`Intent.md`](Intent.md) · [`FRD.md`](FRD.md) · [`NFR.md`](NFR.md) · [`Backlog.md`](Backlog.md) v2.0 |
| Normative source | `Docs/reglas-clean-architecture-onion.md` v1.0 — Clean Architecture, Onion model |

> **Purpose.** Decide *how* the system is built, and demonstrate — rule by rule — that the design satisfies the normative architecture rules. §12 is the compliance matrix, §15 the deviation register, §16 the rubric self-assessment.
>
> **Status honesty.** No code exists yet. This document describes a design that satisfies the rules; §16 states a **target** score, not an achieved one. Compliance is only verifiable once §13 runs green against real assemblies.

---

## 1. Governing tension

Two constraints pull against each other and shape every decision here.

| Force | Source |
|---|---|
| The `CA-*` rules are **normative**: 🔴 rules admit no exception | `Docs/reglas-clean-architecture-onion.md` §18 |
| The sponsor **forbade** MediatR, CQRS, event sourcing, generic repositories, messaging and microservices, and asked for "no unnecessary patterns" | `Intent.md` `TC-06`, Transcript 02 |

**Resolution applied throughout: strict boundaries, plain mechanics.** Physical layer separation and inward-only dependencies, built from ordinary classes and constructor injection. Every rule that demands a *boundary* is honored. Every rule whose usual implementation demands *machinery* is honored with the simplest mechanism that satisfies it, and where the machinery genuinely is not warranted at four entities, the deviation is registered in §15 with an exit condition — never silently skipped.

---

## 2. Architectural drivers

| # | Driver | Source | Consequence |
|---|---|---|---|
| D1 | Identity must never come from the client | `TC-08`, `NFR-SEC-003` | A single `ICurrentUser` port; no identity parameter in any command |
| D2 | Business rules must not live only in the UI | `RK-05`, `NFR-SEC-004` | Invariants inside aggregates; authorization in the application layer |
| D3 | Decision and state change must be atomic | `FR-DEC-009`, `NFR-REL-001` | `Approval` is part of the `Request` aggregate, one transaction |
| D4 | No unnecessary patterns | `TC-06`, `RK-03` | Handlers injected directly; no mediator, no generic repository |
| D5 | Must run locally from source | `TC-09`, `NFR-POR-001` | SQLite file, two processes, no container |
| D6 | Rules testable without infrastructure | `CA-TST-002`, `NFR-MNT-006` | Pure domain; `TimeProvider` injected |
| D7 | Foundation for a later scope decision | `OBJ-05` | Ports isolate every replaceable adapter |
| D8 | The architecture must be auditable, not asserted | `CA-TST-001`, `NFR-MNT-008` | §12 matrix, §13 tests, §15 register |

---

## 3. The Onion model applied to VacaFlow

```
        ┌────────────────────────────────────────────────────────┐
        │  4. Presentation                                       │
        │     BigSolutions.VacaFlow.Api    ·    web (Next.js)    │
        │     Endpoints · Contracts · Auth · Error mapping       │
        │     Program.cs = Composition Root                      │
        │  ┌──────────────────────────────────────────────────┐  │
        │  │  3. Infrastructure                               │  │
        │  │     BigSolutions.VacaFlow.Infrastructure         │  │
        │  │     DbContext · Configurations · Repositories    │  │
        │  │     UnitOfWork · PasswordHasher · Seeder         │  │
        │  │  ┌────────────────────────────────────────────┐  │  │
        │  │  │  2. Application                            │  │  │
        │  │  │     BigSolutions.VacaFlow.Application      │  │  │
        │  │  │     Use case handlers · Commands · DTOs    │  │  │
        │  │  │     Abstractions/ = every port             │  │  │
        │  │  │  ┌──────────────────────────────────────┐  │  │  │
        │  │  │  │  1.b Domain Services                 │  │  │  │
        │  │  │  │      ApprovalPolicy                  │  │  │  │
        │  │  │  │  ┌────────────────────────────────┐  │  │  │  │
        │  │  │  │  │  1.a Domain Model              │  │  │  │  │
        │  │  │  │  │      BigSolutions.VacaFlow     │  │  │  │  │
        │  │  │  │  │        .Domain                 │  │  │  │  │
        │  │  │  │  │      Employee · Request        │  │  │  │  │
        │  │  │  │  │      Approval · AbsenceType    │  │  │  │  │
        │  │  │  │  │      Email · DateRange · Ids   │  │  │  │  │
        │  │  │  │  │      Result · Error            │  │  │  │  │
        │  │  │  │  └────────────────────────────────┘  │  │  │  │
        │  │  │  └──────────────────────────────────────┘  │  │  │
        │  │  └────────────────────────────────────────────┘  │  │
        │  └──────────────────────────────────────────────────┘  │
        └────────────────────────────────────────────────────────┘

                 Dependencies point ─────► inward, without exception
```

---

## 4. Solution structure

### 4.1 Physical layout

Separation is by **project**, not by folder — folders do not prevent a forbidden reference, project boundaries do (`CA-STR-001`).

```
vacaflow/
├── VacaFlow.sln
├── src/
│   ├── BigSolutions.VacaFlow.Domain/
│   │   ├── Primitives/               # Entity, AggregateRoot, ValueObject, Result, Error
│   │   ├── Employees/
│   │   │   ├── Employee.cs · EmployeeId.cs · EmployeeRole.cs
│   │   │   ├── Email.cs
│   │   │   └── Errors/EmployeeErrors.cs
│   │   ├── AbsenceTypes/
│   │   │   ├── AbsenceType.cs · AbsenceTypeId.cs · AbsenceTypeCode.cs
│   │   └── Requests/
│   │       ├── Request.cs · RequestId.cs · RequestState.cs
│   │       ├── Approval.cs · ApprovalId.cs · DecisionType.cs
│   │       ├── DateRange.cs
│   │       ├── Services/ApprovalPolicy.cs
│   │       └── Errors/RequestErrors.cs · ApprovalErrors.cs
│   ├── BigSolutions.VacaFlow.Application/
│   │   ├── Abstractions/             # every port
│   │   ├── Authentication/
│   │   ├── AbsenceTypes/
│   │   └── Requests/
│   ├── BigSolutions.VacaFlow.Infrastructure/
│   │   ├── Persistence/
│   │   ├── Security/
│   │   └── DependencyInjection.cs
│   ├── BigSolutions.VacaFlow.Api/
│   │   ├── Endpoints/ · Contracts/ · Security/ · ErrorHandling/
│   │   └── Program.cs                # composition root
│   └── web/                          # Next.js
├── tests/
│   ├── BigSolutions.VacaFlow.Domain.UnitTests/
│   ├── BigSolutions.VacaFlow.Application.UnitTests/
│   ├── BigSolutions.VacaFlow.Infrastructure.IntegrationTests/
│   └── BigSolutions.VacaFlow.ArchitectureTests/
└── docs/
```

Folders inside each ring are grouped by **business concept** — `Employees/`, `Requests/`, `AbsenceTypes/` — never by technical type (`CA-STR-003`). Namespaces mirror the physical path, so the namespace always reveals the ring (`CA-STR-004`).

> **Naming note on `Primitives/`.** The reference structure in the rules uses `Common/` for base types, while `CA-STR-005` lists `Common` among the forbidden generic names. `Primitives/` satisfies both: it names what the folder holds — `Result`, `Error`, `Entity`, `ValueObject` — and it is not a catch-all. Nothing with a business meaning goes there.

### 4.2 Permitted references

| Project | May reference |
|---|---|
| `Domain` | **nothing internal** — BCL only (`CA-DEP-002`, `CA-DEP-003`) |
| `Application` | `Domain` |
| `Infrastructure` | `Application`, `Domain` |
| `Api` | `Application`, `Domain`, `Infrastructure` *(composition root only)* |
| `web` | the HTTP API only |

`Api` references `Infrastructure` **solely** to invoke `AddInfrastructure()` in `Program.cs` — the exception `CA-DEP-001` note ² permits, and `CA-CFG-001` requires. No endpoint may reference an infrastructure type; §13 asserts this.

There is no `Shared`, `Common` or `Kernel` project. The primitives that would live there sit in `Domain/Primitives/`, which is already the innermost ring — so `CA-DEP-009` has nothing to violate and `CA-STR-006` has no ambiguous `Core` to resolve.

---

## 5. Domain model

### 5.1 Aggregates

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
┌──────────────────────┐                        │  DecisionType       │
│ AbsenceType  «root»  │                        │  string? Comment    │
│  Code · Name · Active│                        │  DateTime DecidedAt │
└──────────────────────┘                        └─────────────────────┘
```

### 5.2 Value objects

| Value object | Invariant enforced at construction |
|---|---|
| `Email` | Non-empty, valid format, ≤ 200 characters, normalized to lower case |
| `DateRange` | `End` ≥ `Start` (`RULE-01`). Both `DateOnly` — no time, no time zone (`AS-04`) |
| `EmployeeId`, `RequestId`, `ApprovalId`, `AbsenceTypeId` | Non-empty `Guid` |
| `AbsenceTypeCode` | One of `VACATION`, `PERSONAL_LEAVE`, `SICK_LEAVE` |

Immutable, structural equality, validated at construction (`CA-DOM-005`).

`DateRange` is where `RULE-01` becomes unbreakable: a request cannot hold an invalid range because the range itself cannot be constructed.

`RULE-02` is **not** a `DateRange` invariant — it depends on the current date, and a value object must not read a clock (`CA-DOM-009`). It is checked by `Request.Create`, `Request.UpdateDetails` and `Request.Submit`, each receiving today's date as a parameter.

### 5.3 Request behavior

The aggregate owns its transitions. There is no public state setter (`CA-DOM-002`), and the type has real behavior rather than being a property bag (`CA-DOM-003`).

```csharp
public sealed class Request : AggregateRoot<RequestId>
{
    public EmployeeId OwnerId { get; private set; }
    public AbsenceTypeId AbsenceTypeId { get; private set; }
    public DateRange Period { get; private set; }
    public string Reason { get; private set; }
    public RequestState State { get; private set; }
    public Approval? Approval { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public DateTime? SubmittedAtUtc { get; private set; }
    public DateTime? ClosedAtUtc { get; private set; }

    private Request() { }                        // ORM only

    public static Result<Request> Create(
        EmployeeId owner, AbsenceTypeId type, DateRange period,
        string reason, DateOnly today, DateTime nowUtc)
    {
        if (period.Start < today)
            return Result.Failure<Request>(RequestErrors.StartDateInPast);
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 500)
            return Result.Failure<Request>(RequestErrors.ReasonRequired);

        return Result.Success(new Request(owner, type, period, reason.Trim(), nowUtc));
    }

    public Result UpdateDetails(AbsenceTypeId type, DateRange period,
                                string reason, DateOnly today, DateTime nowUtc)
    {
        if (State is not RequestState.Draft)
            return Result.Failure(RequestErrors.OnlyDraftEditable);      // RULE-03
        if (period.Start < today)
            return Result.Failure(RequestErrors.StartDateInPast);        // RULE-02
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 500)
            return Result.Failure(RequestErrors.ReasonRequired);

        AbsenceTypeId = type; Period = period;
        Reason = reason.Trim(); UpdatedAtUtc = nowUtc;
        return Result.Success();
    }

    public Result Submit(DateOnly today, DateTime nowUtc)
    {
        if (State is not RequestState.Draft)
            return Result.Failure(
                RequestErrors.InvalidTransition(State, RequestState.Submitted));
        if (Period.Start < today)
            return Result.Failure(RequestErrors.StartDateInPast);        // FR-LFC-003

        State = RequestState.Submitted;
        SubmittedAtUtc = nowUtc; UpdatedAtUtc = nowUtc;
        return Result.Success();
    }

    public Result Cancel(DateTime nowUtc)
    {
        if (State is not (RequestState.Draft or RequestState.Submitted))
            return Result.Failure(
                RequestErrors.InvalidTransition(State, RequestState.Cancelled));

        State = RequestState.Cancelled;
        ClosedAtUtc = nowUtc; UpdatedAtUtc = nowUtc;
        return Result.Success();
    }

    public Result Decide(EmployeeId responsibleManager, DecisionType decision,
                         string? comment, DateTime nowUtc)
    {
        if (State is not RequestState.Submitted)
            return Result.Failure(RequestErrors.OnlySubmittedDecidable);  // RULE-05
        if (Approval is not null)
            return Result.Failure(RequestErrors.AlreadyDecided);          // RULE-09

        Approval = Approval.Create(responsibleManager, decision, comment, nowUtc);
        State = decision is DecisionType.Approved
            ? RequestState.Approved : RequestState.Rejected;
        ClosedAtUtc = nowUtc; UpdatedAtUtc = nowUtc;
        return Result.Success();
    }
}
```

`Decide` cannot produce a state change without an approval record, or the reverse — two statements, one method, one object, one transaction. `NFR-REL-001` is satisfied structurally rather than by discipline.

### 5.4 Approval policy — domain service

`RULE-06` and `RULE-07` span two aggregates: they compare the acting manager against the request owner. That logic has no single natural owner, so it is a stateless domain service (`CA-SRV-001`, `CA-SRV-002`), named for its business intent rather than as a `Manager` or `Helper` (`CA-SRV-004`).

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

It receives loaded entities and returns a `Result`. It never touches a repository, opens a transaction or publishes anything (`CA-SRV-003`), so it is unit-testable with three plain objects and no mocks — which is exactly what `CA-TST-002` demands.

> **`OQ-01`.** The `owner.ManagerId is null` branch returns a distinct error rather than defaulting to permission. It **fails closed**. When the sponsor decides how assignment happens, only this branch and the registration use case change. The prototype's fallback — assign the first manager found — is a demo convenience, not a rule, and is not encoded here.

### 5.5 Errors

Expected business failures are `Result` values, not exceptions (`CA-APP-009`, `CA-DOM-010`). Each `Error` carries the code from the `FRD.md` §7 catalog, so mapping to HTTP is a lookup rather than a translation. No HTTP status ever appears inside the domain.

```csharp
public sealed record Error(string Code, string Message);

public static class RequestErrors
{
    public static readonly Error OnlyDraftEditable =
        new("VF-REQ-003", "Only Draft requests can be edited.");

    public static Error InvalidTransition(RequestState from, RequestState to) =>
        new("VF-REQ-005", $"This request cannot move from {from} to {to}.");
}
```

Exceptions are reserved for the genuinely exceptional — a failed database connection, a corrupt configuration.

### 5.6 Domain events — deliberately absent

`CA-DOM-008` governs how domain events are defined *if they exist*. VacaFlow raises none: there is no notification, no projection, no downstream consumer, and the sponsor deferred all of them (`OS-12`, `OS-25`). Introducing an event bus for zero subscribers would be exactly the machinery `TC-06` forbids. Marked **not applicable** in §12, not deviated.

---

## 6. Application layer

### 6.1 Use case handlers

One class per use case, named in business language (`CA-APP-001`), registered scoped, injected straight into the endpoint. No mediator (`ADR-002`).

```
Application/
├── Abstractions/                     # every port (CA-APP-003)
│   ├── IEmployeeRepository.cs
│   ├── IRequestRepository.cs
│   ├── IAbsenceTypeRepository.cs
│   ├── IUnitOfWork.cs
│   ├── ICurrentUser.cs
│   └── IPasswordHasher.cs
├── Authentication/
│   ├── RegisterEmployeeHandler.cs
│   ├── SignInHandler.cs
│   └── GetCurrentUserHandler.cs
├── AbsenceTypes/
│   └── ListAbsenceTypesHandler.cs
└── Requests/
    ├── CreateRequestHandler.cs
    ├── UpdateRequestHandler.cs
    ├── SubmitRequestHandler.cs
    ├── CancelRequestHandler.cs
    ├── DecideRequestHandler.cs       # approve and reject
    └── ListVisibleRequestsHandler.cs
```

Approve and reject share `DecideRequestHandler` because they differ only in the `DecisionType` — the authorization path, the record created and the transaction are identical (`FR-DEC-005`).

### 6.2 Handler shape

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

Four properties hold in every handler:

1. The acting user comes from `ICurrentUser`, never from a parameter (D1, `TC-08`).
2. **Authorization** — ownership and role — sits here; **invariants** sit in the aggregate. The handler contains no business conditional of its own (`CA-APP-010`); `request.OwnerId != currentUser.EmployeeId` is an authorization check, not a rule about what a request may be.
3. The transaction boundary is the handler, expressed through `IUnitOfWork` without knowing the mechanism (`CA-APP-008`).
4. No ASP.NET Core or EF Core type appears anywhere (`CA-APP-004`, `CA-APP-005`).

### 6.3 Ports

Declared in `Application`, implemented in the outer ring (`CA-DEP-004`, `CA-APP-003`).

| Port | Implemented in | Why |
|---|---|---|
| `IEmployeeRepository`, `IRequestRepository`, `IAbsenceTypeRepository` | Infrastructure | One per aggregate root (`CA-INF-004`) |
| `IUnitOfWork` | Infrastructure | Wraps `DbContext.SaveChangesAsync` |
| `IPasswordHasher` | Infrastructure | Hashing is a technical concern |
| `ICurrentUser` | **Api** | It reads claims from `HttpContext`; keeping it there stops the web framework leaking into Infrastructure |
| `TimeProvider` | BCL | No custom port needed — `TimeProvider` is in the BCL, so the domain may depend on it (`CA-DEP-003`) |

There is no `IRepository<T>` and no port returns `IQueryable<T>` (`CA-APP-005`, anti-patterns 3 and 6). Each repository exposes only the operations its aggregate needs:

```csharp
public interface IRequestRepository
{
    Task<Request?> GetByIdAsync(RequestId id, CancellationToken ct);
    Task<IReadOnlyList<Request>> ListOwnedByAsync(EmployeeId owner, CancellationToken ct);
    Task<IReadOnlyList<Request>> ListPendingForManagerAsync(EmployeeId manager, CancellationToken ct);
    void Add(Request request);
}
```

### 6.4 Input validation

Structural validation — presence, type, length, format — happens at the application boundary before the use case runs (`CA-APP-007`). Business rules stay in the domain and are not duplicated here.

No validation framework is introduced. Each command record exposes a `Validate()` returning `Result`, invoked as the handler's first statement. For eleven use cases with at most four fields each, a FluentValidation pipeline would be more machinery than the rule requires — see `ADR-011`.

### 6.5 DTOs

Each use case defines its own output DTO. A domain entity is never returned across the boundary (`CA-APP-006`, anti-pattern 4). Mapping is explicit, hand-written, and lives in `Application` (`CA-APP-011`) — no convention-based mapper, so no mapper configuration test is needed.

---

## 7. Infrastructure

### 7.1 Structure

```
Infrastructure/
├── Persistence/
│   ├── VacaFlowDbContext.cs
│   ├── Configurations/            # one IEntityTypeConfiguration per aggregate
│   │   ├── EmployeeConfiguration.cs
│   │   ├── UserAccountConfiguration.cs
│   │   ├── AbsenceTypeConfiguration.cs
│   │   └── RequestConfiguration.cs      # owns Approval
│   ├── Repositories/              # internal sealed
│   ├── Migrations/
│   ├── UnitOfWork.cs
│   └── DatabaseSeeder.cs
├── Security/
│   └── Pbkdf2PasswordHasher.cs
└── DependencyInjection.cs         # AddInfrastructure() — the only public surface
```

Every implementation type is `internal sealed`; only `AddInfrastructure()` is public (`CA-DEP-007`, `CA-CFG-002`). The API physically cannot construct a repository, which is what makes `CA-DEP-008` a compile-time guarantee rather than a convention.

Mapping is Fluent API only, one configuration per aggregate (`CA-INF-002`). No domain type carries a persistence attribute (`CA-DOM-001`, anti-pattern 9). Value objects map as owned types; strongly-typed ids map through value converters.

### 7.2 Schema

| Table | Columns | Indexes and constraints |
|---|---|---|
| `Employees` | `Id`, `FullName`, `Email`, `Role`, `IsActive`, `ManagerId` | `UNIQUE(Email)`; `FK ManagerId → Employees(Id)` |
| `UserAccounts` | `Id`, `EmployeeId`, `PasswordHash`, `CreatedAtUtc` | `UNIQUE(EmployeeId)`; `FK → Employees(Id)` |
| `AbsenceTypes` | `Id`, `Code`, `Name`, `IsActive` | `UNIQUE(Code)` |
| `Requests` | `Id`, `EmployeeId`, `AbsenceTypeId`, `StartDate`, `EndDate`, `Reason`, `State`, `CreatedAtUtc`, `UpdatedAtUtc`, `SubmittedAtUtc`, `ClosedAtUtc` | `FK EmployeeId`, `FK AbsenceTypeId`; index on `(EmployeeId, State)` |
| `Approvals` | `Id`, `RequestId`, `ResponsibleManagerId`, `Decision`, `Comment`, `DecidedAtUtc` | **`UNIQUE(RequestId)`**; `FK ResponsibleManagerId → Employees(Id)` |

`UserAccounts` is a technical table with no domain entity behind it (`Intent.md` §7.1). It is mapped and accessed inside `Infrastructure` only, which is why the domain `Employee` never carries a password hash.

The `UNIQUE(RequestId)` constraint is a **data-integrity safety net**, not the enforcement point — the rule lives in `Request.Decide`. `CA-INF-003` forbids business rules in the database, and a uniqueness constraint mirroring a domain invariant is not a rule the database decides; it is a guard against a bug in code that already decided.

The index on `(EmployeeId, State)` serves both list queries of `FR-VIS-001`.

### 7.3 Password hashing

PBKDF2-HMAC-SHA256 · 210,000 iterations · 128-bit random salt per password · 256-bit derived key · constant-time comparison. Stored as a single encoded string carrying the algorithm, iteration count, salt and hash, so parameters can be raised later without invalidating existing accounts (`NFR-SEC-001`, `LC-02`).

### 7.4 Error translation

`SqliteException` and any other provider exception is caught at the repository boundary and translated into an application-level error before crossing the ring (`CA-INF-005`). No provider type appears in a `Result` returned to a handler.

### 7.5 Database creation and seeding

EF Core migrations, applied at startup (`ADR-008`), versioned in the repository (`CA-INF-008`). The seeder is idempotent: it inserts the three absence types and the three seeded employees of `Backlog.md` §3.6 only when absent, matching on `AbsenceType.Code` and `Employee.Email` (`FR-DAT-004`).

### 7.6 Resilience — not applicable

`CA-INF-006` places retries, timeouts and circuit breakers in the adapter. VacaFlow makes no network call to any external system: SQLite is a local file and there is no integration (`OS-24`). Marked **not applicable** in §12.

---

## 8. API

### 8.1 Structure

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
│   ├── ResultExtensions.cs   # Result → IResult, single mapping point (CA-PRE-004)
│   └── ExceptionHandler.cs
├── appsettings.json
└── Program.cs                # composition root (CA-CFG-001)
```

### 8.2 Endpoint shape

Receive, delegate, map. No business conditional, no data access, well under the 15-line guideline (`CA-PRE-001`).

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

The contract carries **only** the comment. There is no `responsibleManagerId` to send, so `FR-DEC-006` is satisfied by the *shape of the contract* rather than by a runtime check — a guarantee nobody can delete by accident.

### 8.3 Command responses

Command endpoints return **`204 No Content`**. They do not return the mutated request.

`CA-APP-002` asks that commands not return read data. The interface already refetches after every mutation (`FR-UIX-005`, `NFR-REL-007`), so a response body would be discarded — returning it would violate the rule *and* be dead weight. Queries return DTOs; commands return a status. See `ADR-012` and the `FRD.md` delta in §18.

### 8.4 Error mapping

One `ToHttpResult()` extension maps every `Error.Code` to its status from the `FRD.md` §7 catalog and emits `{ code, message, field? }`. Unhandled exceptions are caught by a single exception handler returning a generic `500` that leaks no internals. There is no `try/catch` in any endpoint (`CA-PRE-004`).

### 8.5 Authentication and authorization

Cookie authentication (`ADR-003`). On sign-in the API issues an `HttpOnly`, `SameSite=Lax` cookie carrying the employee id and role as claims. `CurrentUserAccessor` reads those claims and exposes them through `ICurrentUser`.

`.RequireAuthorization()` at the endpoint provides coarse gating; the **business** permission rules — ownership, manager assignment, self-decision — are evaluated in the application and domain layers through `ICurrentUser` (`CA-PRE-005`).

`HttpContext`, cookies and headers never cross into `Application`. `CurrentUserAccessor` extracts the claims and passes plain values (`CA-PRE-006`).

---

## 9. Web application

### 9.1 Scope of layering

`CA-STR-001` and the ring rules govern the backend; the rules document scopes itself to "backend / application services (adaptable to frontend)". For a five-screen client with no business logic of its own, imposing a four-ring onion would be the over-engineering `TC-06` forbids. Instead the frontend adopts **one enforced boundary**: all server communication is confined to `lib/api`, and no rule is evaluated outside it. See `ADR-013`.

### 9.2 Structure

```
web/
├── app/
│   ├── (auth)/sign-in/page.tsx        # S-01
│   ├── (auth)/register/page.tsx       # S-02
│   ├── (app)/layout.tsx               # S-03 shell: header, nav, banner slot
│   ├── (app)/requests/page.tsx        # S-04
│   ├── (app)/requests/new/page.tsx    # S-05
│   ├── (app)/requests/[id]/page.tsx   # S-05 edit / S-06 detail
│   └── (app)/queue/page.tsx           # S-07
├── components/
│   ├── shell/          AppHeader · NavTabs · SkipLink
│   ├── feedback/       Banner · Skeleton · EmptyState
│   ├── requests/       RequestRow · RequestForm · StateBadge
│   ├── queue/          QueueCard
│   └── modals/         ConfirmCancelModal · DecisionModal
├── lib/
│   ├── api.ts          # the only module that calls fetch
│   ├── types.ts        # response shapes mirroring the API contracts
│   └── session.ts
└── next.config.mjs
```

The component tree maps one-to-one onto the screens of `Backlog.md` §3.2, and the shell components exist because `US-030`, `US-031` and `US-033`–`US-035` named them.

### 9.3 Enforced frontend rules

Validated with `dependency-cruiser`, mirroring the pattern in the rules document §13.4:

```javascript
// .dependency-cruiser.js
module.exports = {
  forbidden: [
    { name: 'only-lib-api-may-fetch', severity: 'error',
      comment: 'CA-DEP-008 in spirit: no component talks to the server directly',
      from: { pathNot: '^lib/api\\.ts$' },
      to:   { path: 'node_modules/(axios|node-fetch)' } },

    { name: 'components-do-not-import-pages', severity: 'error',
      from: { path: '^components' }, to: { path: '^app' } },

    { name: 'no-circular', severity: 'error',
      from: {}, to: { circular: true } },
  ],
};
```

A lint rule additionally forbids the bare `fetch` identifier outside `lib/api.ts`.

### 9.4 Session and origins

The browser talks to **one origin**. `next.config.mjs` rewrites `/api/*` to the .NET API, so the authentication cookie is first-party and CORS never enters the picture (`ADR-009`).

If the proxy is dropped, the API must enable CORS with an explicit origin and `AllowCredentials`; a wildcard origin is invalid with credentials. Note that `localhost:3000` and `localhost:5001` are *same-site* — port is not part of the site — so `SameSite=Lax` still works. The proxy is preferred because it removes the question entirely.

### 9.5 Client rules

- No business rule is implemented in the frontend. The UI hides actions invalid for the current role and state as an affordance; the API rejects them regardless (`FR-UIX-002`, `RK-05`).
- After every mutation the list is refetched (`FR-UIX-005`, `NFR-REL-007`).
- Every API error surfaces to the user through the banner with the message from `Backlog.md` §3.5 (`FR-UIX-003`).
- A `401` returns the user to sign-in with an explanation (`FR-UIX-007`).
- Copy comes from `Backlog.md` §3.5, in English; the Spanish prototype is authoritative only for layout and interaction (`Backlog.md` §2).

---

## 10. Composition and configuration

`Program.cs` is the only place that knows every layer (`CA-CFG-001`):

```csharp
builder.Services.AddApplication();                          // handlers
builder.Services.AddInfrastructure(builder.Configuration);  // DbContext, repos, hasher
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ICurrentUser, CurrentUserAccessor>();
builder.Services.AddAuthentication(/* cookie */).AddCookie(/* … */);
```

| Service | Lifetime |
|---|---|
| `DbContext`, `IUnitOfWork`, repositories, handlers, `ICurrentUser` | Scoped (`CA-CFG-005`) |
| `IPasswordHasher`, `TimeProvider` | Singleton |

No `IServiceProvider.GetService()` inside any layer — constructor injection only (`CA-CFG-003`, anti-pattern 7). No mutable static state (`CA-CFG-004`). Startup fails fast when required configuration is missing or a registered port cannot be resolved (`CA-CFG-006`).

Configuration is bound to typed options; no inner layer reads a configuration key by literal (`CA-CRS-004`). The connection string and cookie signing configuration come from configuration, never from source (`CA-INF-007`, `NFR-SEC-006`).

Logging uses `ILogger<T>` from `Microsoft.Extensions.Logging.Abstractions` in Application and Infrastructure; the domain logs nothing (`CA-CRS-001`).

---

## 11. Architecture decision records

### `ADR-001` — Reduced Onion in five physical projects
**Decision.** Domain, Application, Infrastructure, Api as separate .NET projects, plus the Next.js application.
**Alternatives.** Folders in one project — rejected: folders do not stop a forbidden reference (`CA-STR-001`, anti-pattern 2). More projects (`Contracts`, `Shared`) — rejected as unnecessary at four entities, and `Shared` invites `CA-DEP-009` violations.
**Consequence.** The dependency rule is enforceable by the compiler and by §13.

### `ADR-002` — No mediator; handlers injected directly
**Decision.** Endpoints depend on a handler class directly through constructor injection.
**Alternatives.** MediatR — explicitly forbidden (`TC-06`), and it adds indirection with no benefit at this size.
**Consequence.** Cross-cutting behavior is not available as a pipeline; see deviation `DV-02`.

### `ADR-003` — Cookie authentication rather than bearer tokens
**Decision.** ASP.NET Core cookie authentication, `HttpOnly`, `SameSite=Lax`.
**Alternatives.** A JWT in `localStorage` — rejected: readable by any injected script, and logout cannot truly invalidate it. A JWT in memory — rejected: lost on refresh, requiring refresh-token machinery.
**Consequence.** `FR-AUT-008` works as specified; the frontend stores no credential material.

### `ADR-004` — `Approval` is part of the `Request` aggregate
**Decision.** `Approval` is a child entity of `Request`, not a separate aggregate root.
**Alternatives.** A separate aggregate with its own repository — rejected: it would place `RULE-08` and `RULE-09` across a boundary and turn atomicity into a coordination problem, violating `CA-DOM-007`.
**Consequence.** `FR-DEC-009` and `NFR-REL-001` hold structurally. There is no `IApprovalRepository`.

### `ADR-005` — Manager-assignment rule as a domain service
**Decision.** `ApprovalPolicy` — stateless, in Domain, receiving loaded entities.
**Alternatives.** In the handler — rejected by `CA-APP-010`. In `Request` — rejected: `Request` would need to know `Employee`, coupling two aggregates.
**Consequence.** `RULE-06` and `RULE-07` are unit-testable with three plain objects and no mocks.

### `ADR-006` — `Result<T>` for expected failures
**Decision.** Business failures return `Result`; exceptions are reserved for the exceptional.
**Alternatives.** Exceptions for rule violations — rejected: control flow by exception, and every rule violation here is an expected outcome (`CA-APP-009`).
**Consequence.** Error codes flow from domain to HTTP without translation.

### `ADR-007` — Strongly-typed identifiers
**Decision.** `EmployeeId`, `RequestId`, `AbsenceTypeId`, `ApprovalId` as readonly record structs with EF value converters.
**Alternatives.** Bare `Guid` — rejected despite `CA-DOM-006` being only 🟡, because this domain passes an owner id, a manager id and a type id through the same methods, and bare they are mutually substitutable.
**Cost.** One value converter per id, and `Guid` conversion at the API boundary. Accepted.

### `ADR-008` — EF Core migrations rather than `EnsureCreated`
**Decision.** One initial migration, applied at startup.
**Alternatives.** `EnsureCreated()` — simpler and permitted by `TC-10`, but produces no schema history and cannot evolve a database without deleting it (`CA-INF-008`).
**Consequence.** The reviewer still gets a working database from a single start command.

### `ADR-009` — Next.js proxies `/api` to the .NET API
**Decision.** A rewrite in `next.config.mjs` so the browser sees a single origin.
**Alternatives.** Cross-origin calls with CORS and `AllowCredentials` — workable, but its failure mode (credentialed requests silently dropped) is confusing to debug.
**Consequence.** No CORS configuration in the MVP.

### `ADR-010` — PBKDF2 with encoded parameters
**Decision.** PBKDF2-HMAC-SHA256, 210,000 iterations, per-password salt, parameters encoded in the stored string.
**Alternatives.** Argon2id — stronger, but needs a third-party package. A bare hash — forbidden by `LC-02`.
**Consequence.** Parameters can be raised later without invalidating stored accounts.

### `ADR-011` — Hand-written command validation, no validation framework
**Decision.** Each command record exposes `Validate()` returning `Result`, called first in the handler.
**Alternatives.** FluentValidation with a pipeline behavior — the canonical way to satisfy `CA-APP-007`, but it requires both a package and the mediator pipeline `ADR-002` rejected.
**Consequence.** `CA-APP-007` is satisfied — structural validation does happen at the application boundary — with no framework. Cost: the call is explicit in each handler and can be forgotten; §13 does not catch that, so it is a code-review item.

### `ADR-012` — Command endpoints return `204 No Content`
**Decision.** Create, update, submit, cancel, approve and reject return a status with no body. Only `GET` endpoints return DTOs.
**Alternatives.** Returning the mutated request, as `FRD.md` §6.3 currently specifies — rejected: it violates `CA-APP-002`, and the client refetches after every mutation anyway (`FR-UIX-005`), so the body would be discarded.
**Consequence.** `FRD.md` §6.3 needs updating; see §18. Create returns `201` with a `Location` header — an identifier, which `CA-APP-002` explicitly permits.

### `ADR-013` — The frontend is not layered as an onion
**Decision.** One enforced boundary — all server access confined to `lib/api` — instead of four rings.
**Alternatives.** A full onion in the client — rejected: the rules document scopes itself to the backend and marks frontend application "adaptable"; a client with zero business logic has no domain to protect, and the structure would be ceremony.
**Consequence.** `CA-STR-001` is satisfied where it applies. The client-side risk that remains — a rule creeping into React — is covered by `RK-05` and reviewed rather than compiled away.

---

## 12. Rule-by-rule compliance

Every rule in the normative document, with how this design satisfies it. **N/A** means the rule governs a construct VacaFlow does not have; the reason is stated. **DV-n** points to §15.

### 12.1 Dependency rules

| Rule | Sev | Status | How |
|---|:---:|:---:|---|
| `CA-DEP-001` inward dependencies | 🔴 | ✅ | §4.2; asserted by §13 |
| `CA-DEP-002` domain references nothing internal | 🔴 | ✅ | `Domain` has zero project references |
| `CA-DEP-003` domain free of frameworks | 🔴 | ✅ | BCL only; `TimeProvider` is BCL |
| `CA-DEP-004` ports declared inward | 🔴 | ✅ | §6.3 — all in `Application/Abstractions` |
| `CA-DEP-005` no cycles | 🔴 | ✅ | Asserted by §13 |
| `CA-DEP-006` no hidden transitive use | 🟠 | ✅ | Each project declares what it uses |
| `CA-DEP-007` infrastructure types `internal` | 🟠 | ✅ | §7.1 — only `AddInfrastructure()` public |
| `CA-DEP-008` presentation ≠ persistence | 🔴 | ✅ | Compile-time: repositories are `internal` |
| `CA-DEP-009` shared contracts isolated | 🟡 | ✅ | No `Shared` project exists |

### 12.2 Domain model

| Rule | Sev | Status | How |
|---|:---:|:---:|---|
| `CA-DOM-001` no persistence annotations | 🔴 | ✅ | Fluent API only, §7.1 |
| `CA-DOM-002` protected invariants | 🔴 | ✅ | §5.3 — private ctor, private setters, factories |
| `CA-DOM-003` no anemic model | 🟠 | ✅ | `Request` owns every transition |
| `CA-DOM-004` read-only collections | 🟠 | **N/A** | No aggregate holds a collection; `Approval` is a single optional child |
| `CA-DOM-005` value objects | 🟠 | ✅ | `Email`, `DateRange`, `AbsenceTypeCode` |
| `CA-DOM-006` typed identifiers | 🟡 | ✅ | `ADR-007` |
| `CA-DOM-007` aggregate boundaries | 🟠 | ✅ | §5.1 — references by identity, repository per root |
| `CA-DOM-008` domain events in the domain | 🟠 | **N/A** | No events raised; §5.6 |
| `CA-DOM-009` no IO or side effects | 🔴 | ✅ | Dates passed in; asserted by §13 |
| `CA-DOM-010` typed business errors | 🟡 | ✅ | §5.5 — `Error`/`Result`, no HTTP in domain |
| `CA-DOM-011` no DTOs in the domain | 🟠 | ⚠️ **DV-03** | The core entity is named `Request` |

### 12.3 Domain services

| Rule | Sev | Status | How |
|---|:---:|:---:|---|
| `CA-SRV-001` only for cross-aggregate logic | 🟠 | ✅ | `ApprovalPolicy` is the only one |
| `CA-SRV-002` stateless | 🟠 | ✅ | Static, pure, deterministic |
| `CA-SRV-003` no persistence or transactions | 🔴 | ✅ | Receives loaded entities |
| `CA-SRV-004` business-intent naming | 🟡 | ✅ | `ApprovalPolicy`, not `ApprovalManager` |

### 12.4 Application layer

| Rule | Sev | Status | How |
|---|:---:|:---:|---|
| `CA-APP-001` one use case per handler | 🟠 | ✅ | §6.1 — eleven handlers, business names |
| `CA-APP-002` command/query separation | 🟡 | ✅ | `ADR-012` — commands return `204` |
| `CA-APP-003` ports declared here | 🔴 | ✅ | §6.3 |
| `CA-APP-004` no web framework types | 🔴 | ✅ | Asserted by §13 |
| `CA-APP-005` no data-access libraries | 🔴 | ✅ | Asserted by §13; no `IQueryable` port |
| `CA-APP-006` explicit boundary DTOs | 🟠 | ✅ | §6.5 |
| `CA-APP-007` structural validation here | 🟠 | ✅ | `ADR-011` |
| `CA-APP-008` transaction boundary here | 🟠 | ✅ | `IUnitOfWork` in the handler |
| `CA-APP-009` no exceptions as control flow | 🟡 | ✅ | `ADR-006` |
| `CA-APP-010` no business logic in orchestration | 🟠 | ✅ | §6.2 — authorization only |
| `CA-APP-011` explicit verifiable mapping | 🟡 | ✅ | Hand-written; no mapper to verify |

### 12.5 Infrastructure

| Rule | Sev | Status | How |
|---|:---:|:---:|---|
| `CA-INF-001` implements ports, never defines them | 🔴 | ✅ | Every type backs a port declared inward |
| `CA-INF-002` O/RM mapping isolated | 🟠 | ✅ | One configuration per aggregate |
| `CA-INF-003` no business rules | 🔴 | ✅ | §7.2 — the unique constraint is a safety net |
| `CA-INF-004` repository per aggregate root | 🟠 | ✅ | Three repositories, no generic base |
| `CA-INF-005` external errors translated | 🟠 | ✅ | §7.4 |
| `CA-INF-006` resilience in the adapter | 🟡 | **N/A** | No external call exists; §7.6 |
| `CA-INF-007` no hardcoded secrets | 🔴 | ✅ | §10 |
| `CA-INF-008` versioned migrations | 🟡 | ✅ | `ADR-008` |

### 12.6 Presentation

| Rule | Sev | Status | How |
|---|:---:|:---:|---|
| `CA-PRE-001` thin controllers | 🟠 | ✅ | §8.2 — well under 15 lines |
| `CA-PRE-002` no direct database access | 🔴 | ✅ | Same guarantee as `CA-DEP-008` |
| `CA-PRE-003` decoupled versioned contracts | 🟠 | ✅ | `Api/Contracts` records; no versioning needed for a single-consumer MVP |
| `CA-PRE-004` centralized error mapping | 🟠 | ✅ | §8.4 |
| `CA-PRE-005` declarative authorization at the edge | 🟠 | ✅ | §8.5 |
| `CA-PRE-006` no session state in the model | 🟡 | ✅ | `CurrentUserAccessor` passes plain values |

### 12.7 Cross-cutting

| Rule | Sev | Status | How |
|---|:---:|:---:|---|
| `CA-CRS-001` logging by abstraction | 🟠 | ✅ | `ILogger<T>`; domain logs nothing |
| `CA-CRS-002` time and randomness injected | 🟠 | ✅ | `TimeProvider`; asserted by §13 |
| `CA-CRS-003` cross-cutting via pipeline | 🟡 | ⚠️ **DV-02** | No mediator, so no behaviors |
| `CA-CRS-004` typed configuration | 🟡 | ✅ | §10 |
| `CA-CRS-005` end-to-end traceability | 🟡 | ⚠️ **DV-04** | No correlation id in a local MVP |

### 12.8 Structure and naming

| Rule | Sev | Status | How |
|---|:---:|:---:|---|
| `CA-STR-001` one project per ring | 🟠 | ✅ | §4.1 |
| `CA-STR-002` reference solution structure | 🟡 | ✅ | §4.1 follows it, `Common/`→`Primitives/` |
| `CA-STR-003` feature folders per ring | 🟡 | ✅ | `Employees/`, `Requests/`, `AbsenceTypes/` |
| `CA-STR-004` namespaces mirror structure | 🟡 | ✅ | `BigSolutions.VacaFlow.<Ring>.<Feature>` |
| `CA-STR-005` forbidden names | 🟡 | ⚠️ **DV-05** | `Manager` is a business role here |
| `CA-STR-006` no ambiguous `Core` | 🟡 | ✅ | No `Core` project |

### 12.9 Testing

| Rule | Sev | Status | How |
|---|:---:|:---:|---|
| `CA-TST-001` architecture tests in CI | 🔴 | ⚠️ **DV-01** | Tests exist; no pipeline — CI/CD is out of scope |
| `CA-TST-002` domain testable without infrastructure | 🔴 | ✅ | Pure domain, no repository mocks needed |
| `CA-TST-003` use cases against test doubles | 🟠 | ✅ | Handler tests use fake ports |
| `CA-TST-004` infrastructure integration-tested | 🟠 | ✅ | Against a temporary SQLite file, not ORM mocks; §14.2 |
| `CA-TST-005` coverage weighted inward | 🟡 | ✅ | Targets: Domain ≥ 90 %, Application ≥ 80 % |
| `CA-TST-006` behavior-named tests | 🟡 | ✅ | `Submit_Should_Fail_When_Request_Is_Not_Draft` |

### 12.10 Composition and DI

| Rule | Sev | Status | How |
|---|:---:|:---:|---|
| `CA-CFG-001` single composition root | 🔴 | ✅ | `Program.cs` only |
| `CA-CFG-002` registration encapsulated per layer | 🟡 | ✅ | `AddApplication()`, `AddInfrastructure()` |
| `CA-CFG-003` no service locator | 🔴 | ✅ | Constructor injection only; asserted by §13 |
| `CA-CFG-004` no mutable statics | 🟠 | ✅ | `ApprovalPolicy` is static but stateless and pure |
| `CA-CFG-005` correct lifetimes | 🟡 | ✅ | §10 |
| `CA-CFG-006` configuration validated at startup | 🟡 | ✅ | Fail fast |

**Summary: 60 rules — 54 satisfied, 3 not applicable with stated reason, 5 deviations of which one is 🔴.**

---

## 13. Architecture tests

`BigSolutions.VacaFlow.ArchitectureTests` runs with `dotnet test` (`TE-006`).

| # | Test | Rule |
|---|---|---|
| 1 | `Domain` has no dependency on `Application`, `Infrastructure` or `Api` | `CA-DEP-001`, `CA-DEP-002` |
| 2 | `Domain` has no dependency on EF Core, ASP.NET Core or a serializer | `CA-DEP-003` |
| 3 | `Application` has no dependency on EF Core or ASP.NET Core | `CA-APP-004`, `CA-APP-005` |
| 4 | No `Api` type references `DbContext` or a repository implementation | `CA-DEP-008`, `CA-PRE-002` |
| 5 | Repository and unit-of-work types are not public | `CA-DEP-007` |
| 6 | No cycles between projects | `CA-DEP-005` |
| 7 | No type in `Domain` ends in `Dto`, `Response` or `ViewModel` | `CA-DOM-011` |
| 8 | `Domain` and `Application` contain no `DateTime.Now` or `DateTime.UtcNow` | `CA-DOM-009`, `CA-CRS-002` |
| 9 | No type calls `IServiceProvider.GetService` outside `Program.cs` | `CA-CFG-003` |
| 10 | Handler types are `sealed` and end in `Handler` | `CA-APP-001` |
| 11 | Every public `Infrastructure` type implements an interface from an inner ring | `CA-INF-001` |

```csharp
[Fact] // CA-DEP-003
public void Domain_Should_Not_Depend_On_Frameworks() =>
    Types.InAssembly(DomainAssembly.Instance)
        .ShouldNot()
        .HaveDependencyOnAny(
            "Microsoft.EntityFrameworkCore",
            "Microsoft.AspNetCore",
            "Newtonsoft.Json",
            "System.Text.Json",
            "Dapper")
        .GetResult().IsSuccessful.Should().BeTrue();
```

> **Test 7 exclusion.** The assertion excludes the type `Request` by name, with a comment naming `DV-03`. `CA-DOM-011` targets transport objects; `Request` here is the core business concept. The exclusion is by exact type name, not by pattern, so a future `CreateRequestDto` in `Domain` would still fail.

The frontend rules of §9.3 run with `npx depcruise`.

---

## 14. Test strategy

### 14.1 Pyramid

| Level | Project | Contents |
|---|---|---|
| Domain unit | `Domain.UnitTests` | `RULE-01`–`RULE-09`, the full transition matrix, `ApprovalPolicy` branches, `DateRange` and `Email` invariants |
| Application unit | `Application.UnitTests` | Authorization paths, identity derivation, transaction invocation — against fake ports (`CA-TST-003`) |
| Infrastructure integration | `Infrastructure.IntegrationTests` | Repositories and the seeder against a **real temporary SQLite file** (`CA-TST-004`) |
| Architecture | `ArchitectureTests` | §13 |

Tests are named for behavior (`CA-TST-006`).

### 14.2 A note on the integration project

`CA-TST-004` (🟠) forbids testing repositories by mocking the ORM. Normally this implies containers, which `OS-07` defers — but SQLite is a file, so a temporary database per test class satisfies the rule at near-zero cost and with no Docker. It is the one place where the rules ask for more than `Intent.md` `SC-16` budgeted; §18 records the ≈ 0.5-day WBS delta.

---

## 15. Deviation register

Recorded per §18 of the normative document: rule, reason, alternatives, cost, exit condition.

### `DV-01` — `CA-TST-001` 🔴 · Architecture tests do not run in a merge-blocking pipeline
**Reason.** CI/CD is out of scope by explicit sponsor decision (`OS-08`). The tests themselves exist, run with `dotnet test` and pass.
**Alternatives rejected.** Building a pipeline solely to satisfy the rule — outside the agreed scope and budget.
**Cost.** Drift is detected only when a developer runs the suite, not at merge.
**Exit condition.** `FUT-09` introduces CI/CD.
**Status.** ⚠️ **Not a valid deviation under §18** — 🔴 rules admit no exception. Carried as an **open confirmation** (`OQ-03`) for the technical lead and the sponsor, not as a closed decision. Until confirmed, §16 must be read as capped.

### `DV-02` — `CA-CRS-003` 🟡 · Cross-cutting concerns applied inside handlers
**Reason.** Behaviors and decorators presuppose a mediator pipeline, forbidden by `TC-06`.
**Alternatives rejected.** MediatR pipeline behaviors; hand-rolled decorators for eleven handlers — more construction than the concern justifies.
**Cost.** Validation and transaction handling are repeated per handler and can be forgotten.
**Exit condition.** The handler count passes roughly twenty, or a concern needs to apply uniformly and provably.

### `DV-03` — `CA-DOM-011` 🟠 · A domain type is named `Request`
**Reason.** `Request` is the central business concept, named as the business names it. Renaming to `AbsenceRequest` would satisfy the linter and misname the ubiquitous language.
**Alternatives rejected.** Renaming the entity; weakening the assertion to a looser pattern.
**Cost.** One named exclusion in test 7, which could mask a genuine future violation of that exact name.
**Exit condition.** None. This is a permanent, deliberate exception.

### `DV-04` — `CA-CRS-005` 🟡 · No correlation identifier
**Reason.** A single local process with no distributed calls and no observability requirement (`NFR` §12 non-requirements).
**Alternatives rejected.** Threading a correlation id through a context port — cost with no consumer.
**Exit condition.** `FUT-07` (hosting) or `FUT-11` (monitoring).

### `DV-05` — `CA-STR-005` 🟡 · The token `Manager` appears in type and member names
**Reason.** `Manager` is a business role in this domain — `EmployeeRole.Manager`, `ResponsibleManagerId`, `ListPendingForManagerAsync`. The rule targets `OrderManager`-style names signalling undefined responsibility.
**Alternatives rejected.** Renaming the role to `Approver` — it would diverge from every requirement document and from the sponsor's own vocabulary.
**Cost.** A naming linter would need an allowlist.
**Exit condition.** None. Deliberate.

---

## 16. Rubric self-assessment

Against §16 of the normative document. **This is a target for the design, not a measurement of code** — no code exists.

| Block | Weight | Projected | Basis |
|---|---:|---:|---|
| Dependency rules (`CA-DEP`) | 30 | 30 | Zero 🔴 violations; §13 tests 1–6 assert them |
| Domain richness (`CA-DOM`, `CA-SRV`) | 25 | 24 | Invariants protected, VOs present, boundaries clean; `DV-03` costs a point |
| Application layer (`CA-APP`) | 15 | 15 | Pure use cases, ports correct, no framework leakage |
| Infrastructure (`CA-INF`, `CA-CRS`) | 10 | 9 | Adapters replaceable, errors translated, no secrets; `DV-02` and `DV-04` cost a point |
| Presentation (`CA-PRE`) | 5 | 5 | Thin endpoints, decoupled contracts |
| Testing (`CA-TST`) | 10 | 8 | Correct pyramid; automated validation exists but not in a merge-blocking pipeline (`DV-01`) |
| Structure and composition (`CA-STR`, `CA-CFG`) | 5 | 5 | Physical separation, single composition root |
| **Projected total** | **100** | **96** | |

> **The cut-off rule applies.** §16 states that any unremediated 🔴 violation caps the score at **59**, regardless of everything else. `DV-01` is a 🔴 deviation. Therefore:
>
> - If the sponsor and technical lead **confirm** `OQ-03` — accepting that CI/CD is out of scope and that local execution of the suite is the agreed control — the project scores **96**.
> - If they do not, the project scores **59** by rule, and the only remedy is `FUT-09`.
>
> This is the single most consequential open item in the architecture, and it costs roughly half a day to close by building the pipeline anyway. Worth raising before implementation starts rather than at the audit.

---

## 17. Local execution

| # | Step |
|---|---|
| 1 | `dotnet run --project src/BigSolutions.VacaFlow.Api` — applies migrations, seeds, listens |
| 2 | `npm install && npm run dev` in `src/web` — serves the interface and proxies `/api` |
| 3 | Open the web application; sign in with a seeded account from `Backlog.md` §3.6 |

Resetting: stop the API, delete `vacaflow.db`, start again. The migration and seeder rebuild a clean state (`FR-DAT-006`, `NFR-OPS-001`).

Two processes, one file, no container, no external service (`NFR-POR-001`, `NFR-POR-003`).

---

## 18. Impact on the other documents

| Document | Required change | Source |
|---|---|---|
| `FRD.md` §6.3 | Command endpoints return `204 No Content` (`201` + `Location` for create), not the mutated request | `ADR-012`, `CA-APP-002` |
| `FRD.md` §8 | Add the shell, banner, skeleton, empty-state and modal requirements from `Backlog.md` `US-030`–`US-036` | `Backlog.md` v2.0 |
| `WBS.md` §3 | New package for `Infrastructure.IntegrationTests`, ≈ **+0.5 d** (§14.2); plus the ≈ +2 d already identified by `Backlog.md` v2.0 | `CA-TST-004` |
| `NFR.md` §5 | `NFR-MNT-008` targets ≥ 90/100; §16 projects 96 **conditional on `OQ-03`** — the conditionality should be stated there too | `DV-01` |
| `Intent.md` §15 | `OQ-03` is no longer a formality; §16 shows it decides between a 96 and a 59 | `DV-01` |

---

## 19. Architectural risks

| Risk | Impact | Mitigation |
|---|---|---|
| `ICurrentUser` bypassed by a handler taking an id parameter | `RK-02` materializes; delivery rejected | No command record carries an identity field; `NFR-SEC-003` test; dedicated review of WBS 4.4 |
| A rule implemented in the endpoint or in React instead of the domain | `RK-05` | `CA-PRE-001` guideline; rule-to-test mapping in `NFR-MNT-007`; §9.3 frontend rules |
| `ManagerId` null handled by defaulting to permitted | `RULE-06` silently broken | `ApprovalPolicy` fails closed; blocked on `OQ-01` |
| Aggregate boundary eroded by adding an `IApprovalRepository` | `NFR-REL-001` lost | `ADR-004`; no such port exists; `CA-INF-004` |
| `Validate()` forgotten in a handler | Structural validation bypassed | `ADR-011` cost, accepted; code-review item — §13 does not catch it |
| Architecture drifts because tests are not in CI | Slow erosion, and the §16 cut-off | `DV-01`; run before each handover; `FUT-09` closes it |

---

## 20. Open questions with architectural impact

| ID | Question | Impact |
|---|---|---|
| `OQ-01` | How is `Employee.ManagerId` set? | Contained to `ApprovalPolicy` and `RegisterEmployeeHandler` |
| `OQ-02` | Is role selection allowed at registration? | If not, `RegisterEmployeeHandler` stops accepting a role |
| **`OQ-03`** | **Confirm the `CA-TST-001` deviation** | **Decides between a rubric score of 96 and 59 — see §16** |
| `OQ-04` | Is `RULE-02` re-evaluated at submit? | One guard in `Request.Submit`; the prototype confirms yes |
| `OQ-05` | Confirm the stricter `RULE-06` | One branch in `ApprovalPolicy`; the prototype confirms yes |

Every one is contained to a named location. That containment is deliberate: an open question that would ripple through the design is one that must be answered before building, and none of these do.
