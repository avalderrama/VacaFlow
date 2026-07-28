# VacaFlow — Functional Requirement Document

| Field | Value |
|---|---|
| Project | VacaFlow |
| Company | BIG Solutions |
| Document | `FRD.md` (Deliverable 1 of 6 — Project Documentation) |
| Version | 1.0 |
| Date | 2026-07-28 |
| Baseline | [`Intent.md`](Intent.md) v1.0 · [`Backlog.md`](Backlog.md) v1.0 |

> **Purpose.** Specify *what the system must do*, precisely enough to be implemented and tested without further interpretation. Non-functional characteristics belong in `NFR.md`; structural decisions belong in `SAD.md`.
>
> **Reading rule.** Where this document and `Intent.md` disagree, `Intent.md` wins and this document is corrected. Where this document is silent, the behavior is undefined and must be raised, not invented.

---

## 1. Conventions

### 1.1 Identifiers

| Prefix | Area |
|---|---|
| `FR-AUT-*` | Authentication and identity |
| `FR-CAT-*` | Absence type catalog |
| `FR-REQ-*` | Request authoring |
| `FR-LFC-*` | Request lifecycle |
| `FR-DEC-*` | Manager decision |
| `FR-VIS-*` | Visibility and results |
| `FR-UIX-*` | User interface behavior |
| `FR-ERR-*` | Error handling |
| `FR-DAT-*` | Data and seed |

### 1.2 Obligation

**MUST** — mandatory for acceptance. **SHOULD** — expected; its absence is a defect. **MAY** — optional.

### 1.3 Enforcement layer

Every functional requirement states where it is enforced. `Intent.md` `RK-05` identifies "rules only in the UI" as a high risk, so this is specified rather than assumed.

| Layer | Meaning |
|---|---|
| **Domain** | An invariant of an entity. Cannot be bypassed by any caller |
| **Application** | A use-case level rule (authorization, orchestration) |
| **API** | Structural request validation and HTTP mapping |
| **Web** | Presentation affordance only — **never** the sole enforcement point |

---

## 2. Actors

| Actor | Description | Authenticated |
|---|---|---|
| **Anonymous** | A visitor who has not logged in. May only register or log in | No |
| **Employee** | An authenticated user with the Employee role. Owns requests | Yes |
| **Manager** | An authenticated user with the Manager role. Decides on requests of employees assigned to them. Acts as an Employee for their own requests | Yes |
| **System** | Scheduled or startup behavior — database creation and seeding | n/a |

### 2.1 Permission matrix

| Operation | Anonymous | Employee | Manager (own request) | Manager (assigned employee's request) | Manager (unassigned employee's request) |
|---|:---:|:---:|:---:|:---:|:---:|
| Register | ✅ | ❌ | ❌ | — | — |
| Log in | ✅ | ❌ | ❌ | — | — |
| Log out / current user | ❌ | ✅ | ✅ | — | — |
| List absence types | ❌ | ✅ | ✅ | — | — |
| Create request | ❌ | ✅ | ✅ | — | — |
| Edit own Draft | ❌ | ✅ | ✅ | ❌ | ❌ |
| Submit own request | ❌ | ✅ | ✅ | ❌ | ❌ |
| Cancel own request | ❌ | ✅ | ✅ | ❌ | ❌ |
| Approve / reject | ❌ | ❌ | ❌ **(`RULE-07`)** | ✅ | ❌ **(`RULE-06`)** |
| View request | ❌ | own only | own only | ✅ (Submitted only) | ❌ |

---

## 3. Data dictionary

### 3.1 Employee — business entity

| Field | Type | Constraints |
|---|---|---|
| `Id` | Guid | Primary key, system-generated |
| `FullName` | string | Required, 1–120 characters |
| `Email` | string | Required, unique, valid email format, ≤ 200 characters. Also the login identifier |
| `Role` | enum | Required. `Employee` \| `Manager` |
| `IsActive` | bool | Required, defaults to `true` |
| `ManagerId` | Guid? | Optional foreign key to `Employee.Id`. The simple manager assignment (`Intent.md` `BC-04`). Self-reference is forbidden |

### 3.2 UserAccount — technical, not a business entity

`Intent.md` §7.1 states that authentication records are technical infrastructure. They are therefore modelled separately, so that the domain entity never carries a password hash.

| Field | Type | Constraints |
|---|---|---|
| `Id` | Guid | Primary key |
| `EmployeeId` | Guid | Required, unique, foreign key to `Employee.Id` |
| `PasswordHash` | string | Required. Produced by a salted, iterated password hashing algorithm. Never the plain password (`LC-02`) |
| `CreatedAtUtc` | datetime | Required |

Login resolves `Employee` by `Email`, then loads the matching `UserAccount` to verify the password.

### 3.3 AbsenceType — business entity

| Field | Type | Constraints |
|---|---|---|
| `Id` | Guid | Primary key |
| `Code` | string | Required, unique. `VACATION` \| `PERSONAL_LEAVE` \| `SICK_LEAVE` |
| `Name` | string | Required, ≤ 60 characters. Display label |
| `IsActive` | bool | Required, defaults to `true` |

Seed-only. There is no maintenance screen (`OS-23`).

### 3.4 Request — business entity

| Field | Type | Constraints |
|---|---|---|
| `Id` | Guid | Primary key |
| `EmployeeId` | Guid | Required, foreign key. The owner. **Always** the authenticated user at creation (`TC-08`) |
| `AbsenceTypeId` | Guid | Required, foreign key to an active absence type |
| `StartDate` | date | Required. Calendar date, no time component, no time zone (`AS-04`) |
| `EndDate` | date | Required. Must be ≥ `StartDate` (`RULE-01`) |
| `Reason` | string | Required, 1–500 characters |
| `State` | enum | Required. `Draft` \| `Submitted` \| `Approved` \| `Rejected` \| `Cancelled`. Initial value `Draft` |
| `CreatedAtUtc` | datetime | Required, set once |
| `UpdatedAtUtc` | datetime | Required, refreshed on every accepted mutation |
| `SubmittedAtUtc` | datetime? | Set when the request transitions to `Submitted` |
| `ClosedAtUtc` | datetime? | Set when the request reaches a final state |

### 3.5 Approval — business entity

| Field | Type | Constraints |
|---|---|---|
| `Id` | Guid | Primary key |
| `RequestId` | Guid | Required, **unique**, foreign key. Uniqueness enforces one decision per request (`RULE-09`) |
| `ResponsibleManagerId` | Guid | Required, foreign key to `Employee.Id`. **Always** the authenticated manager (`TC-08`, `AC-12`) |
| `Decision` | enum | Required. `Approved` \| `Rejected` |
| `Comment` | string? | Optional, ≤ 500 characters |
| `DecidedAtUtc` | datetime | Required |

> The unique constraint on `RequestId` is a data-integrity safety net. The rule itself is enforced in the domain — the database is not the enforcement point (`CA-INF-003`).

---

## 4. Request state machine

### 4.1 States

| State | Meaning | Editable | Final |
|---|---|:---:|:---:|
| `Draft` | Created, not yet submitted | ✅ | ❌ |
| `Submitted` | Awaiting a manager decision | ❌ | ❌ |
| `Approved` | Approved by the responsible manager | ❌ | ✅ |
| `Rejected` | Rejected by the responsible manager | ❌ | ✅ |
| `Cancelled` | Withdrawn by the owner | ❌ | ✅ |

### 4.2 Transitions

| # | From | To | Trigger | Actor | Guards | Effects |
|---|---|---|---|---|---|---|
| T1 | `Draft` | `Submitted` | Submit | Owner | `RULE-04`; `RULE-02` re-checked (`OQ-04`) | `SubmittedAtUtc` set |
| T2 | `Draft` | `Cancelled` | Cancel | Owner | `RULE-04` | `ClosedAtUtc` set |
| T3 | `Submitted` | `Approved` | Approve | Assigned manager | `RULE-05`, `RULE-06`, `RULE-07`, `RULE-09` | Approval record created; `ClosedAtUtc` set |
| T4 | `Submitted` | `Rejected` | Reject | Assigned manager | `RULE-05`, `RULE-06`, `RULE-07`, `RULE-09` | Approval record created; `ClosedAtUtc` set |
| T5 | `Submitted` | `Cancelled` | Cancel | Owner | `RULE-04` | `ClosedAtUtc` set |

Any transition not in this table **MUST** be rejected with `VF-REQ-005`. There is no generic state-change operation.

```
        create
          │
          ▼
      ┌───────┐  submit   ┌───────────┐  approve  ┌──────────┐
      │ Draft │──────────►│ Submitted │──────────►│ Approved │ ◄─ final
      └───┬───┘    T1     └─────┬─────┘    T3     └──────────┘
          │                     │
          │ cancel              │ reject T4       ┌──────────┐
          │ T2                  ├────────────────►│ Rejected │ ◄─ final
          │                     │                 └──────────┘
          │                     │ cancel T5
          ▼                     ▼
      ┌───────────┐◄────────────┘
      │ Cancelled │ ◄─ final
      └───────────┘
```

---

## 5. Functional requirements

### 5.1 Authentication and identity

#### `FR-AUT-001` — Registration · **MUST** · Application
The system accepts a registration containing full name, email, password and role, and creates one `Employee` and one `UserAccount`.
*Traces:* `SC-02`, `AC-01`, `US-007`

#### `FR-AUT-002` — Email uniqueness · **MUST** · Domain
Registration with an already-registered email is rejected with `VF-AUT-001`. No second account is created. Comparison is case-insensitive.
*Traces:* `US-007`

#### `FR-AUT-003` — Password hashing · **MUST** · Infrastructure
The password is stored only as a salted, iterated hash. No code path writes, logs or returns the plain password.
*Traces:* `SC-03`, `LC-02`, `US-007`

#### `FR-AUT-004` — Password strength · **SHOULD** · Application
A password shorter than 8 characters is rejected with `VF-VAL-001`. No further composition rules apply in the MVP.
*Traces:* `LC-02`

#### `FR-AUT-005` — Login · **MUST** · Application
Correct credentials establish an authenticated session or token. The mechanism (cookie or bearer token) is an implementation choice, provided it is consistent (`TC-07`).
*Traces:* `SC-02`, `AC-02`, `US-008`

#### `FR-AUT-006` — Non-disclosure on failed login · **MUST** · Application
An unknown email and a wrong password produce the same response, `VF-AUT-002`, so account existence is not disclosed.
*Traces:* `US-008`

#### `FR-AUT-007` — Inactive employee · **MUST** · Application
An employee with `IsActive = false` cannot log in. The attempt is rejected with `VF-AUT-003`.
*Traces:* `US-008`

#### `FR-AUT-008` — Logout · **SHOULD** · Application
Logout invalidates the current session or token. Subsequent calls to protected endpoints return `VF-AUT-004`.
*Traces:* `SC-02`, `US-009`

#### `FR-AUT-009` — Current user · **MUST** · Application
The system returns the authenticated user's identifier, full name, email and role. It **MUST NOT** return the password hash.
*Traces:* `SC-02`, `US-010`

#### `FR-AUT-010` — Server-side identity derivation · **MUST** · Application
Every operation that creates, mutates or decides on a request derives the acting user from the authenticated context, through a port. No endpoint accepts an employee or manager identifier as input for a business decision; if such a field is present in a payload it is ignored.
*Traces:* `SC-09`, `TC-08`, `OBJ-02`, `AC-14`, `TE-011`

> This is the requirement the sponsor named as the acceptance-critical one. A system that satisfies every other requirement but not this one is rejected.

#### `FR-AUT-011` — Protected endpoints · **MUST** · API
Every endpoint except registration and login requires an authenticated caller. An unauthenticated call returns `VF-AUT-004`.
*Traces:* `AC-14`

---

### 5.2 Absence type catalog

#### `FR-CAT-001` — List absence types · **MUST** · Application
An authenticated user retrieves the active absence types with identifier, code and display name.
*Traces:* `SC-14`, `US-014`

#### `FR-CAT-002` — Catalog is server-owned · **MUST** · Web
The web application populates the type selector from `FR-CAT-001`. Absence types **MUST NOT** be hardcoded in the frontend.
*Traces:* `US-014`

#### `FR-CAT-003` — Valid type on a request · **MUST** · Application
A request referencing an unknown or inactive absence type is rejected with `VF-CAT-001`.
*Traces:* `US-015`

---

### 5.3 Request authoring

#### `FR-REQ-001` — Create a Draft · **MUST** · Application
An authenticated employee creates a request with absence type, start date, end date and reason. The request is created in state `Draft`, owned by the authenticated user.
*Traces:* `SC-07`, `AC-03`, `US-015`

#### `FR-REQ-002` — Date order · **MUST** · Domain
`EndDate` **MUST NOT** be earlier than `StartDate`. A single-day absence where both dates are equal is valid. Violation returns `VF-REQ-001`.
*Traces:* `RULE-01`, `AC-04`, `US-015`

#### `FR-REQ-003` — Start date not in the past · **MUST** · Domain
`StartDate` **MUST NOT** be earlier than the current date. A start date equal to today is valid. The current date is obtained from the injected time provider, never from a static clock. Violation returns `VF-REQ-002`.
*Traces:* `RULE-02`, `AC-05`, `TC-13`, `US-015`

#### `FR-REQ-004` — Required content · **MUST** · Domain
Absence type, start date, end date and reason are all required. Reason is 1–500 characters. Missing or malformed values return `VF-VAL-001` identifying the offending field.
*Traces:* `US-015`

#### `FR-REQ-005` — Edit restricted to Draft · **MUST** · Domain
Only a request in state `Draft` can be edited. Any other state returns `VF-REQ-003`, whose message states that only Draft requests can be edited.
*Traces:* `RULE-03`, `AC-06`, `AC-08`, `US-016`

#### `FR-REQ-006` — Ownership on edit · **MUST** · Application
Only the owner may edit their request. Another user's attempt returns `VF-REQ-004`.
*Traces:* `RULE-04`, `AC-14`, `US-016`

#### `FR-REQ-007` — Re-validation on edit · **MUST** · Domain
`FR-REQ-002`, `FR-REQ-003` and `FR-REQ-004` are re-evaluated on every edit, with the same errors as on creation.
*Traces:* `RULE-01`, `RULE-02`, `US-016`

---

### 5.4 Request lifecycle

#### `FR-LFC-001` — Submit · **MUST** · Domain
The owner submits a `Draft` request, transitioning it to `Submitted` and setting `SubmittedAtUtc`. Any other source state returns `VF-REQ-005`.
*Traces:* `SC-07`, `AC-07`, `US-018`

#### `FR-LFC-002` — Ownership on submit · **MUST** · Application
Only the owner may submit. Another user's attempt returns `VF-REQ-004`.
*Traces:* `RULE-04`, `AC-14`, `US-018`

#### `FR-LFC-003` — Date re-validation at submit · **MUST** · Domain
`FR-REQ-003` is re-evaluated at submit time. A Draft whose start date has since passed cannot be submitted and returns `VF-REQ-002`.
*Traces:* `RULE-02`, `OQ-04`, `US-018`

> **Assumption pending `OQ-04`.** Rationale: submitting a request for a date that has already passed produces a decision that cannot be acted on. If the sponsor decides otherwise, this requirement is removed and `T1` loses that guard.

#### `FR-LFC-004` — Immutability after submission · **MUST** · Domain
Once a request leaves `Draft`, its type, dates and reason are immutable. No endpoint exposes a way to modify them.
*Traces:* `RULE-03`, `AC-08`, `US-018`

#### `FR-LFC-005` — Cancel · **MUST** · Domain
The owner cancels a request in state `Draft` or `Submitted`, transitioning it to `Cancelled` and setting `ClosedAtUtc`. A request in a final state returns `VF-REQ-005`.
*Traces:* `SC-06`, `RULE-09`, `US-019`

#### `FR-LFC-006` — Ownership on cancel · **MUST** · Application
Only the owner may cancel. A manager **MUST NOT** cancel another person's request.
*Traces:* `RULE-04`, `US-019`

---

### 5.5 Manager decision

#### `FR-DEC-001` — Decidable state · **MUST** · Domain
Only a request in state `Submitted` can be approved or rejected. Any other state returns `VF-DEC-001`.
*Traces:* `RULE-05`, `US-021`

#### `FR-DEC-002` — Manager role required · **MUST** · Application
A user without the `Manager` role attempting a decision receives `VF-DEC-002`.
*Traces:* `RULE-06`, `AC-14`, `US-021`

#### `FR-DEC-003` — Manager assignment required · **MUST** · Application
The acting manager **MUST** be the manager assigned to the request owner — that is, `owner.ManagerId` equals the authenticated manager's identifier. Otherwise `VF-DEC-003`.
*Traces:* `RULE-06`, `OQ-05`, `US-021`

> **Blocked by `OQ-01`.** Registration never sets `ManagerId`, so a self-registered employee has `ManagerId = null` and no manager can satisfy this requirement. The behavior for `ManagerId = null` is **undefined** until the sponsor decides. It must not be silently defaulted.

#### `FR-DEC-004` — No self-decision · **MUST** · Application
A manager **MUST NOT** approve or reject a request they own, even if they are formally their own assigned manager. Returns `VF-DEC-004`.
*Traces:* `RULE-07`, `AC-14`, `US-021`

#### `FR-DEC-005` — Approval record created · **MUST** · Application
An approval or a rejection creates exactly one `Approval` record, carrying the request, the responsible manager, the decision, the optional comment and the decision date.
*Traces:* `RULE-08`, `AC-11`, `US-021`, `US-022`

#### `FR-DEC-006` — Responsible manager is the authenticated user · **MUST** · Application
`ResponsibleManagerId` is taken from the authenticated context. A value supplied in the payload is ignored.
*Traces:* `RULE-08`, `TC-08`, `AC-12`, `US-021`

#### `FR-DEC-007` — One decision only · **MUST** · Domain
A request that already has an `Approval` cannot be decided again. Returns `VF-DEC-005`.
*Traces:* `RULE-09`, `US-021`

#### `FR-DEC-008` — Optional comment · **MUST** · Application
The decision comment is optional for both approval and rejection, and is limited to 500 characters when present.
*Traces:* `AC-11`, `US-022`

#### `FR-DEC-009` — Atomic decision · **MUST** · Application
The state transition and the creation of the `Approval` record occur in a single transaction. A partial outcome — a state change without its approval record, or the reverse — **MUST NOT** be observable.
*Traces:* `RULE-08`, `CA-APP-008`

---

### 5.6 Visibility and results

#### `FR-VIS-001` — Role-driven listing · **MUST** · Application
`GET /requests` returns a result set determined server-side by the caller's role:
- **Employee** — all of their own requests, in every state.
- **Manager** — the `Submitted` requests of the employees assigned to them, plus their own requests as an employee.

The caller cannot influence this through parameters.
*Traces:* `SC-09`, `AC-10`, `US-020`, `US-024`

#### `FR-VIS-002` — No cross-employee visibility · **MUST** · Application
An employee **MUST NOT** receive, by any endpoint, a request they do not own. A manager **MUST NOT** receive requests of employees not assigned to them.
*Traces:* `LC-07`, `AC-14`, `US-020`

#### `FR-VIS-003` — No manager history · **MUST** · Application
A manager's queue contains only requests awaiting decision. Requests in a final state leave the queue.
*Traces:* `OS-22`, `US-020`

#### `FR-VIS-004` — Final decision visible to the owner · **MUST** · Application
For a decided request, the owner sees the final state, the responsible manager's name, the decision date and the comment when present.
*Traces:* `AC-13`, `US-025`

#### `FR-VIS-005` — Ordering · **SHOULD** · Application
Request lists are returned ordered by `CreatedAtUtc` descending, so the most recent appears first.
*Traces:* `US-024`

---

## 6. API specification

Base path: `/api`. All payloads are JSON. All dates are `YYYY-MM-DD`; all timestamps are UTC ISO-8601.

### 6.1 Authentication

| # | Method | Path | Auth | Purpose |
|---|---|---|---|---|
| 1 | `POST` | `/auth/register` | Anonymous | Create an account |
| 2 | `POST` | `/auth/login` | Anonymous | Establish a session |
| 3 | `POST` | `/auth/logout` | Authenticated | End the session |
| 4 | `GET` | `/auth/me` | Authenticated | Current user |

**`POST /auth/register`**
Request: `{ fullName, email, password, role }`
Success `201`: `{ id, fullName, email, role }`
Errors: `VF-VAL-001` `400` · `VF-AUT-001` `409`

**`POST /auth/login`**
Request: `{ email, password }`
Success `200`: `{ id, fullName, email, role }` plus the session cookie or token
Errors: `VF-AUT-002` `401` · `VF-AUT-003` `403`

**`POST /auth/logout`** — Success `204` · Errors: `VF-AUT-004` `401`

**`GET /auth/me`** — Success `200`: `{ id, fullName, email, role }` · Errors: `VF-AUT-004` `401`

### 6.2 Catalog

**`GET /absence-types`** — Success `200`: `[{ id, code, name }]` · Errors: `VF-AUT-004` `401`

### 6.3 Workflow

| # | Method | Path | Actor | Purpose |
|---|---|---|---|---|
| 6 | `GET` | `/requests` | Employee / Manager | List visible requests |
| 7 | `POST` | `/requests` | Employee | Create a Draft |
| 8 | `PUT` | `/requests/{id}` | Owner | Edit a Draft |
| 9 | `POST` | `/requests/{id}/submit` | Owner | Submit |
| 10 | `POST` | `/requests/{id}/cancel` | Owner | Cancel |
| 11 | `POST` | `/requests/{id}/approve` | Assigned manager | Approve |
| 12 | `POST` | `/requests/{id}/reject` | Assigned manager | Reject |

**`GET /requests`**
Success `200`: `[{ id, absenceType: { id, code, name }, startDate, endDate, reason, state, employee: { id, fullName }, createdAtUtc, approval?: { responsibleManagerName, decision, comment, decidedAtUtc } }]`
The `employee` block is present so a manager can see whose request it is; for an employee it is always themselves.

**`POST /requests`**
Request: `{ absenceTypeId, startDate, endDate, reason }` — **no `employeeId`** (`FR-AUT-010`)
Success `201`: the created request
Errors: `VF-VAL-001` `400` · `VF-REQ-001` `400` · `VF-REQ-002` `400` · `VF-CAT-001` `400` · `VF-AUT-004` `401`

**`PUT /requests/{id}`**
Request: `{ absenceTypeId, startDate, endDate, reason }`
Success `200`: the updated request
Errors: `VF-VAL-001` `400` · `VF-REQ-001` `400` · `VF-REQ-002` `400` · `VF-REQ-004` `403` · `VF-REQ-006` `404` · `VF-REQ-003` `409`

**`POST /requests/{id}/submit`**
Request: empty · Success `200`: the updated request
Errors: `VF-REQ-002` `400` · `VF-REQ-004` `403` · `VF-REQ-006` `404` · `VF-REQ-005` `409`

**`POST /requests/{id}/cancel`**
Request: empty · Success `200`: the updated request
Errors: `VF-REQ-004` `403` · `VF-REQ-006` `404` · `VF-REQ-005` `409`

**`POST /requests/{id}/approve`** and **`POST /requests/{id}/reject`**
Request: `{ comment? }` — **no `responsibleManagerId`** (`FR-DEC-006`)
Success `200`: the updated request including its approval block
Errors: `VF-VAL-001` `400` · `VF-DEC-002` `403` · `VF-DEC-003` `403` · `VF-DEC-004` `403` · `VF-REQ-006` `404` · `VF-DEC-001` `409` · `VF-DEC-005` `409`

### 6.4 Contract rules

#### `FR-ERR-001` — No trusted identifiers · **MUST** · API
No request contract contains `employeeId`, `ownerId`, `responsibleManagerId` or any equivalent. This is verifiable by reading the contracts, and is part of acceptance.
*Traces:* `TC-08`, `AC-14`

#### `FR-ERR-002` — Consistent error shape · **MUST** · API
Every error response carries the same structure: `{ code, message, field? }`, where `code` is from the catalog in §7 and `message` is a specific, human-readable statement of what failed.
*Traces:* `TE-005`

#### `FR-ERR-003` — Centralized mapping · **MUST** · API
Application and domain errors are mapped to HTTP in one place, not by a `try/catch` per endpoint.
*Traces:* `CA-PRE-004`, `TE-005`

---

## 7. Error catalog

| Code | HTTP | Message | Rule |
|---|:---:|---|---|
| `VF-VAL-001` | 400 | The submitted data is not valid. *(field-specific detail)* | — |
| `VF-AUT-001` | 409 | An account with this email already exists. | `FR-AUT-002` |
| `VF-AUT-002` | 401 | The email or password is incorrect. | `FR-AUT-006` |
| `VF-AUT-003` | 403 | This account is not active. | `FR-AUT-007` |
| `VF-AUT-004` | 401 | You must be signed in to perform this action. | `FR-AUT-011` |
| `VF-CAT-001` | 400 | The selected absence type does not exist or is not available. | `FR-CAT-003` |
| `VF-REQ-001` | 400 | The end date cannot be earlier than the start date. | `RULE-01` |
| `VF-REQ-002` | 400 | The start date cannot be in the past. | `RULE-02` |
| `VF-REQ-003` | 409 | Only Draft requests can be edited. | `RULE-03` |
| `VF-REQ-004` | 403 | You can only act on your own requests. | `RULE-04` |
| `VF-REQ-005` | 409 | This request cannot move from *{current}* to *{target}*. | §4.2 |
| `VF-REQ-006` | 404 | The request was not found. | — |
| `VF-DEC-001` | 409 | Only Submitted requests can be approved or rejected. | `RULE-05` |
| `VF-DEC-002` | 403 | Only a manager can approve or reject a request. | `RULE-06` |
| `VF-DEC-003` | 403 | You are not the manager assigned to this employee. | `RULE-06` |
| `VF-DEC-004` | 403 | You cannot decide on your own request. | `RULE-07` |
| `VF-DEC-005` | 409 | This request already has a final decision. | `RULE-09` |

**Design note on `403` versus `404`.** Acting on a request owned by someone else returns `403` with an explicit message rather than `404`, because `AC-14` requires unauthorized operations to be visibly *blocked* during the demo. This trades a small amount of existence disclosure for demonstrability, which is acceptable in an internal MVP where every user is a known employee. In a production hardening pass this **SHOULD** be revisited.

---

## 8. User interface requirements

Five screens, no dashboard, no reports, no complex navigation (`Intent.md` §7.3).

### 8.1 Cross-cutting

#### `FR-UIX-001` — Current user always visible · **MUST**
Once signed in, every screen displays the current user's name and role (`UX-PRN-002`).
*Traces:* `US-013`

#### `FR-UIX-002` — No invalid actions offered · **MUST**
An action that would be rejected by the API for the current role and state is not rendered. This is an affordance, not enforcement — the API rejects it regardless.
*Traces:* `Intent.md` §7.3, `RK-05`

#### `FR-UIX-003` — Errors are surfaced · **MUST**
Every API error is displayed to the user with its message. No error is silently swallowed, and no failure is presented as a success.
*Traces:* `UX-EST`, `UX-FBK`, `TE-005`

#### `FR-UIX-004` — Interface states · **SHOULD**
Every list and form handles four states explicitly: loading, empty, error, and populated. An empty list shows an explanatory message, not a blank area.
*Traces:* `UX-EST`

#### `FR-UIX-005` — Reload after action · **MUST**
After a successful create, edit, submit, cancel, approve or reject, the affected list is reloaded from the API rather than mutated locally, so the displayed state always matches the server.
*Traces:* `Intent.md` §7.3

#### `FR-UIX-006` — Labelled form controls · **MUST**
Every input has a visible, associated label. Placeholders are not used as labels.
*Traces:* `TC-15`, `UX-FRM`

#### `FR-UIX-007` — Unauthenticated redirection · **MUST**
An unauthenticated visit to an application screen redirects to login. A session that expires mid-use returns the user to login with an explanatory message.
*Traces:* `FR-AUT-011`

### 8.2 Screens

#### `FR-UIX-010` — Register · **MUST**
Fields: full name, email, password, role. On success the user proceeds to the application. Validation errors are shown per field.
*Traces:* `US-012`

#### `FR-UIX-011` — Login · **MUST**
Fields: email, password. On failure an error is shown and the password field is cleared. On success the user lands on the view for their role.
*Traces:* `US-013`

#### `FR-UIX-012` — My Requests · **MUST**
A list of the employee's own requests showing absence type, start date, end date and state. Actions per state:

| State | Actions offered |
|---|---|
| `Draft` | Edit · Submit · Cancel |
| `Submitted` | View · Cancel |
| `Approved` / `Rejected` / `Cancelled` | View |

*Traces:* `US-024`

#### `FR-UIX-013` — Request form · **MUST**
Fields: absence type (from `FR-CAT-001`), start date, end date, reason. Used for both creation and editing. When the request is not a `Draft`, the form is read-only and offers no save action. On a rejected save the entered values are preserved.
*Traces:* `US-017`

#### `FR-UIX-014` — Manager queue · **MUST**
A list of `Submitted` requests of the manager's assigned employees, showing employee name, absence type, dates and reason. Each row offers Approve and Reject, each allowing an optional comment before confirmation. After a decision the list reloads and the request leaves the queue.
*Traces:* `US-023`

#### `FR-UIX-015` — Decision detail for the employee · **MUST**
Opening a decided request shows the final state, the responsible manager, the decision date and the comment when present, with no state-changing action available.
*Traces:* `US-025`, `AC-13`

---

## 9. Data and seed requirements

#### `FR-DAT-001` — Automatic database creation · **MUST**
On startup, if the SQLite database does not exist, it is created with the complete schema.
*Traces:* `TC-10`, `TE-002`

#### `FR-DAT-002` — Absence type seed · **MUST**
The catalog is seeded with Vacation, Personal Leave and Sick Leave.
*Traces:* `SC-14`, `TE-003`

#### `FR-DAT-003` — Manager seed · **MUST**
At least one account with the `Manager` role is seeded so approvals are testable without an administration screen.
*Traces:* `SC-14`, `BC-03`, `TE-003`

#### `FR-DAT-004` — Idempotent seed · **MUST**
Restarting against an existing database creates no duplicates.
*Traces:* `TE-003`

#### `FR-DAT-005` — Non-production seed credentials · **MUST**
Seeded credentials are clearly non-production and are documented in the README.
*Traces:* `LC-04`

#### `FR-DAT-006` — Database reset · **MUST**
A documented procedure allows a reviewer to return to a clean seeded state.
*Traces:* `TC-11`, `US-026`

---

## 10. Traceability matrix

### 10.1 Business rules → requirements → stories

| Rule | Requirement | Story | Acceptance |
|---|---|---|---|
| `RULE-01` End ≥ start | `FR-REQ-002`, `FR-REQ-007` | `US-015`, `US-016` | `AC-04` |
| `RULE-02` No past start | `FR-REQ-003`, `FR-REQ-007`, `FR-LFC-003` | `US-015`, `US-018` | `AC-05` |
| `RULE-03` Draft-only edit | `FR-REQ-005`, `FR-LFC-004` | `US-016`, `US-018` | `AC-06`, `AC-08` |
| `RULE-04` Owner-only actions | `FR-REQ-006`, `FR-LFC-002`, `FR-LFC-006` | `US-016`, `US-018`, `US-019` | `AC-14` |
| `RULE-05` Submitted-only decisions | `FR-DEC-001` | `US-021` | `AC-11` |
| `RULE-06` Assigned manager only | `FR-DEC-002`, `FR-DEC-003` | `US-021` | `AC-14` |
| `RULE-07` No self-decision | `FR-DEC-004` | `US-021` | `AC-14` |
| `RULE-08` One approval record | `FR-DEC-005`, `FR-DEC-006`, `FR-DEC-009` | `US-021`, `US-022` | `AC-11`, `AC-12` |
| `RULE-09` One final decision | `FR-DEC-007`, `FR-LFC-005` | `US-019`, `US-021` | `AC-11` |

### 10.2 Acceptance criteria → requirements

| Acceptance | Requirements |
|---|---|
| `AC-01` Register | `FR-AUT-001`–`FR-AUT-004`, `FR-UIX-010` |
| `AC-02` Log in | `FR-AUT-005`–`FR-AUT-007`, `FR-UIX-011` |
| `AC-03` Create Draft | `FR-REQ-001`, `FR-REQ-004`, `FR-UIX-013` |
| `AC-04` Invalid range rejected | `FR-REQ-002` |
| `AC-05` Past start rejected | `FR-REQ-003` |
| `AC-06` Edit Draft | `FR-REQ-005`, `FR-REQ-007` |
| `AC-07` Submit | `FR-LFC-001` |
| `AC-08` No edit after submit | `FR-REQ-005`, `FR-LFC-004` |
| `AC-09` Manager login | `FR-AUT-005`, `FR-DAT-003` |
| `AC-10` Manager sees assigned submitted | `FR-VIS-001`, `FR-VIS-003` |
| `AC-11` Approve or reject with comment | `FR-DEC-005`, `FR-DEC-008` |
| `AC-12` Manager recorded | `FR-DEC-006`, `FR-AUT-010` |
| `AC-13` Employee sees result | `FR-VIS-004`, `FR-UIX-015` |
| `AC-14` Unauthorized blocked | `FR-AUT-010`, `FR-AUT-011`, `FR-REQ-006`, `FR-LFC-002`, `FR-DEC-002`–`FR-DEC-004`, `FR-VIS-002` |

---

## 11. Explicitly not specified

The following are **not** requirements of this system and **MUST NOT** be implemented: vacation balance, working-day or holiday calculation, overlap detection between requests, attachments, notifications of any kind, password reset, email verification, role or account administration, absence type maintenance, manager decision history, reports, exports, and multi-level or delegated approval. Each traces to a deferral in `Intent.md` §6.

---

## 12. Open questions carried into this document

| ID | Question | Affects | Effect if unanswered |
|---|---|---|---|
| `OQ-01` | How is `Employee.ManagerId` set for a self-registered employee? | `FR-DEC-003` | The approval path cannot be implemented. **Behavior for `ManagerId = null` is undefined and must not be defaulted** |
| `OQ-02` | Seeded manager credentials; is role selection allowed at registration? | `FR-AUT-001`, `FR-DAT-003`, `FR-DAT-005` | Implemented under the assumption that role selection is allowed for testing |
| `OQ-04` | Is `RULE-02` re-evaluated at submit? | `FR-LFC-003` | Implemented as re-evaluated; remove the requirement if the sponsor decides otherwise |
| `OQ-05` | Confirm the stricter reading of `RULE-06` | `FR-DEC-003` | Implemented as the stricter reading |
