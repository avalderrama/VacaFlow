# VacaFlow — Backlog

| Field | Value |
|---|---|
| Project | VacaFlow |
| Company | BIG Solutions |
| Document | `Backlog.md` (Deliverable 1 of 6 — Project Documentation) |
| Version | 1.0 |
| Date | 2026-07-28 |
| Baseline | [`Intent.md`](Intent.md) v1.0 |

> **Purpose.** The user stories covering **the whole solution and the MVP**. Part A is the MVP — everything required to pass the acceptance demo. Part B is the product backlog beyond the MVP, derived from the deferred items in `Intent.md` §6, kept here so the boundary stays visible rather than forgotten.
>
> Every MVP story traces to at least one scope item (`SC-*`), business rule (`RULE-*`) or acceptance criterion (`AC-*`) from `Intent.md`. A story with no trace is out of scope by definition.

---

## 1. Conventions

### 1.1 Identifiers

| Prefix | Meaning |
|---|---|
| `EP-*` | Epic |
| `US-*` | MVP user story (Part A) |
| `FUT-*` | Post-MVP story (Part B) |
| `TE-*` | Technical enabler — no direct user value, but required before dependent stories |

### 1.2 Priority (MoSCoW, scoped to the MVP)

| Level | Meaning |
|---|---|
| **Must** | The acceptance demo fails without it |
| **Should** | Expected quality; its absence is a defect, not a demo blocker |
| **Could** | Included only if it costs nothing |

There is no *Won't* column: everything deliberately excluded lives in Part B.

### 1.3 Sizing

`S` ≈ half a day · `M` ≈ one day · `L` ≈ two or more days. Relative, not contractual.

### 1.4 Definition of Ready

A story is ready when: it traces to `Intent.md`; its acceptance criteria are written as verifiable Given/When/Then; its dependencies are done or in the same increment; and no open question (`OQ-*`) blocks it.

### 1.5 Definition of Done

Code compiles · the business rule is enforced in the domain/application layer, **not only in the UI** · the endpoint derives identity server-side (`TC-08`) · invalid actions return a clear specific error · architecture tests still pass · the story's acceptance criteria are demonstrable in the running application.

---

## 2. Epic map

| Epic | Title | Stories | Value |
|---|---|---|---|
| `EP-01` | Foundations | `TE-001`–`TE-006` | The skeleton every other story stands on |
| `EP-02` | Authentication and identity | `US-007`–`US-013` | Real accounts replace the old user selector |
| `EP-03` | Absence catalog | `US-014` | Requests can be classified |
| `EP-04` | Request authoring | `US-015`–`US-017` | An employee can express a request |
| `EP-05` | Request lifecycle | `US-018`–`US-019` | The request moves through the state machine |
| `EP-06` | Manager decision | `US-020`–`US-023` | A decision is made and attributed |
| `EP-07` | Visibility and results | `US-024`–`US-025` | Everyone sees what they are entitled to see |
| `EP-08` | Delivery artifacts | `US-026`–`US-029` | The MVP can be handed over and demonstrated |

---

# Part A — MVP backlog

## EP-01 · Foundations

### `TE-001` — Solution skeleton with Onion rings
**Must** · `L` · Depends on: —
**Traces:** `SC-13`, `TC-05`, `TC-06`

As the development team, I need the solution physically separated into `Domain`, `Application`, `Infrastructure`, `Api` and `Web`, so that the dependency rule can be enforced instead of merely intended.

**Acceptance criteria**
- Given the solution, when inspecting project references, then dependencies point only inward: `Domain` has zero internal references; `Application` references only `Domain`; `Infrastructure` references `Application` and `Domain`; `Api` references `Application` and `Domain`.
- Given the solution, when searching `Domain`, then there is no reference to EF Core, ASP.NET Core, or any serialization library (`CA-DEP-003`).
- Given the codebase, when reviewing it, then no MediatR, CQRS dispatcher, event sourcing, generic repository, messaging or microservice pattern is present (`TC-06`).
- Given the `Web` project, when started, then it runs independently of the API process.

---

### `TE-002` — Persistence with EF Core and SQLite
**Must** · `M` · Depends on: `TE-001`
**Traces:** `SC-11`, `SC-12`, `TC-03`, `TC-04`, `TC-10`, `TC-11`

As a reviewer, I need the database to be created automatically when the API starts, so that I can run the application without manual setup steps.

**Acceptance criteria**
- Given a clean checkout with no database file, when the API starts, then the SQLite file is created with the full schema.
- Given the running API, when inspecting the domain entities, then they carry no persistence attributes; mapping is configured in `Infrastructure` via Fluent API (`CA-DOM-001`, `CA-INF-002`).
- Given the README, when a reviewer follows the reset procedure, then deleting the database file and restarting recreates a clean database with seed data.
- Given the repository, when running `git status` after starting the API, then the database file is untracked.

---

### `TE-003` — Seed data
**Must** · `S` · Depends on: `TE-002`
**Traces:** `SC-14`, `BC-03`, `LC-04`

As a reviewer, I need absence types and at least one Manager account to exist on first run, so that approvals can be tested without an administration screen.

**Acceptance criteria**
- Given a newly created database, when the API starts, then the absence types Vacation, Personal Leave and Sick Leave exist.
- Given a newly created database, when the API starts, then at least one account with the Manager role exists.
- Given the seeded manager, when inspecting its credentials, then they are clearly non-production and documented in the README.
- Given a restart on an existing database, when seeding runs, then no duplicate absence types or accounts are created.

---

### `TE-004` — Injected time provider
**Must** · `S` · Depends on: `TE-001`
**Traces:** `TC-13`, `RULE-02`

As the development team, I need the current date supplied through an injected abstraction, so that the "start date cannot be in the past" rule is testable and the domain stays free of static clock reads.

**Acceptance criteria**
- Given the domain and application code, when searching for `DateTime.Now` or `DateTime.UtcNow`, then there are no occurrences (`CA-DOM-009`).
- Given a unit test, when a fixed date is supplied, then the date rules evaluate deterministically against that date.

---

### `TE-005` — Centralized error handling
**Must** · `M` · Depends on: `TE-001`
**Traces:** `TC-05`, §7.5 error behavior

As an API consumer, I need business rule violations to return a consistent, specific and readable error, so that the UI can show the user what actually went wrong.

**Acceptance criteria**
- Given a rule violation, when the endpoint returns, then the response has an appropriate status code (`400` validation, `403` authorization, `404` not found, `409` invalid state transition) and a message naming the specific rule.
- Given the API code, when reviewing endpoints, then error translation happens in one place, not in a `try/catch` per endpoint (`CA-PRE-004`).
- Given an attempt to edit a Submitted request, when it fails, then the message states that only Draft requests can be edited.

---

### `TE-006` — Architecture tests
**Should** · `M` · Depends on: `TE-001`
**Traces:** `TC-17`, `OQ-03`

As the technical lead, I need automated tests asserting the dependency rules, so that the architecture cannot silently erode.

**Acceptance criteria**
- Given the test project, when run with `dotnet test`, then it validates at minimum `CA-DEP-001`, `CA-DEP-002`, `CA-DEP-003`, `CA-DEP-008`, `CA-APP-004` and `CA-APP-005`.
- Given a deliberate violation introduced locally, when the tests run, then they fail and name the offending type.

> Runs locally only. `CA-TST-001` requires a merge-blocking pipeline, waived as a documented deviation because CI/CD is out of scope (`OS-08`).

---

## EP-02 · Authentication and identity

### `US-007` — Register an account
**Must** · `M` · Depends on: `TE-002`, `TE-003`
**Traces:** `SC-02`, `SC-03`, `LC-02`, `AC-01`

As a new user, I want to register with my name, email, password and role, so that I can access VacaFlow with my own account.

**Acceptance criteria**
- Given valid data, when I `POST /auth/register`, then an account and its Employee record are created and the response indicates success.
- Given an email already registered, when I register, then the request is rejected with a clear message and no second account is created.
- Given any registration, when inspecting the database, then the password is stored hashed — never in plain text (`LC-02`).
- Given a missing required field or a malformed email, when I register, then the request is rejected with a field-level validation message.

> **Assumption pending `OQ-02`:** role selection at registration is permitted for MVP testing convenience. It is not a production-grade control.

---

### `US-008` — Log in
**Must** · `M` · Depends on: `US-007`
**Traces:** `SC-02`, `AC-02`

As a registered user, I want to log in with my email and password, so that the application knows who I am.

**Acceptance criteria**
- Given correct credentials, when I `POST /auth/login`, then an authenticated session or token is established.
- Given an incorrect password or unknown email, when I log in, then it is rejected with a generic message that does not reveal whether the email exists.
- Given an inactive employee, when I log in, then access is denied.

---

### `US-009` — Log out
**Should** · `S` · Depends on: `US-008`
**Traces:** `SC-02`

As a logged-in user, I want to log out, so that my session is closed on a shared machine.

**Acceptance criteria**
- Given a logged-in user, when I `POST /auth/logout`, then the session or token is invalidated.
- Given a logged-out user, when I call a workflow endpoint, then it returns unauthorized.

---

### `US-010` — Retrieve the current user
**Must** · `S` · Depends on: `US-008`
**Traces:** `SC-02`, `SC-09`

As the web application, I need to retrieve the authenticated user, so that I can display who is logged in and render role-appropriate actions.

**Acceptance criteria**
- Given a logged-in user, when I call `GET /auth/me`, then the response contains name, email and role — never the password hash.
- Given no session, when I call `GET /auth/me`, then it returns unauthorized.

---

### `TE-011` — Server-side identity derivation
**Must** · `M` · Depends on: `US-008`
**Traces:** `SC-09`, `TC-08`, `OBJ-02`, `RK-02`, `AC-14`

As the sponsor, I need every business decision to use the identity from the authenticated context, so that nobody can act on behalf of another person by editing a request payload.

**Acceptance criteria**
- Given the API contracts, when reviewing them, then no endpoint accepts `employeeId` or `responsibleManagerId` in its request body or query string.
- Given a request payload containing an extra identifier field, when the endpoint processes it, then the value is ignored entirely.
- Given the application layer, when a use case needs the acting user, then it obtains it through a port (`ICurrentUser`) implemented in `Infrastructure` (`CA-APP-003`).

> This is the single most important technical story in the MVP. `RK-02` and `AC-14` both hang on it.

---

### `US-012` — Register screen
**Must** · `M` · Depends on: `US-007`
**Traces:** `SC-01`, §7.3, `TC-15`

As a new user, I want a registration form, so that I can create my account without using the API directly.

**Acceptance criteria**
- Given the register page, when it renders, then it has labelled fields for name, email, password and role.
- Given a validation error from the API, when it returns, then the message is displayed next to the relevant field or in a visible summary — never silently swallowed (`UX-EST`, `UX-FBK`).
- Given a successful registration, when it completes, then I am taken to login or logged in directly, with visible confirmation.

---

### `US-013` — Login screen and current-user display
**Must** · `M` · Depends on: `US-008`, `US-010`
**Traces:** `SC-01`, §7.3, `AC-02`

As a registered user, I want a login form and to see who I am logged in as, so that I always know whose actions I am performing.

**Acceptance criteria**
- Given the login page, when I submit valid credentials, then I land on the view appropriate to my role.
- Given a logged-in session, when any page renders, then the current user's name is visible (`UX-PRN-002`).
- Given invalid credentials, when I submit, then an error is displayed and the password field is cleared.

---

## EP-03 · Absence catalog

### `US-014` — List absence types
**Must** · `S` · Depends on: `TE-003`
**Traces:** `SC-14`, §7.5

As an employee, I want to choose from the available absence types, so that my request is properly classified.

**Acceptance criteria**
- Given a logged-in user, when I call `GET /absence-types`, then Vacation, Personal Leave and Sick Leave are returned.
- Given the request form, when it loads, then the type selector is populated from this endpoint, not hardcoded in the frontend.
- Given no session, when I call the endpoint, then it returns unauthorized.

---

## EP-04 · Request authoring

### `US-015` — Create a Draft request
**Must** · `M` · Depends on: `TE-011`, `US-014`, `TE-004`
**Traces:** `SC-07`, `RULE-01`, `RULE-02`, `AC-03`, `AC-04`, `AC-05`

As an employee, I want to create an absence request as a Draft, so that I can prepare it before committing to it.

**Acceptance criteria**
- Given valid data, when I `POST /requests`, then a request is created in state `Draft`, owned by the authenticated user.
- Given an end date earlier than the start date, when I submit the form, then creation is rejected with a message naming that rule (`RULE-01`).
- Given a start date in the past, when I submit the form, then creation is rejected with a message naming that rule (`RULE-02`).
- Given a payload carrying an `employeeId`, when it is processed, then the owner is still the authenticated user (`TC-08`).
- Given a missing absence type or reason, when I submit, then it is rejected with a field-level validation message.

---

### `US-016` — Edit a Draft request
**Must** · `M` · Depends on: `US-015`
**Traces:** `RULE-03`, `RULE-04`, `AC-06`, `AC-08`

As an employee, I want to edit my request while it is still a Draft, so that I can correct it before submitting.

**Acceptance criteria**
- Given my own Draft request, when I `PUT /requests/{id}`, then the type, dates and reason are updated.
- Given a request in state Submitted, Approved, Rejected or Cancelled, when I try to edit it, then it is rejected stating that only Draft requests can be edited (`RULE-03`).
- Given a Draft request owned by another employee, when I try to edit it, then it is forbidden (`RULE-04`).
- Given an edit that would violate `RULE-01` or `RULE-02`, when I save, then it is rejected with the same messages as on creation.

---

### `US-017` — Request form screen
**Must** · `M` · Depends on: `US-015`, `US-016`
**Traces:** `SC-01`, §7.3, `TC-15`

As an employee, I want a single form to create and edit a request, so that the experience is consistent.

**Acceptance criteria**
- Given the form, when it renders, then it has labelled fields for absence type, start date, end date and reason.
- Given a rule violation returned by the API, when it arrives, then the message is displayed clearly and the entered data is preserved.
- Given a request that is not a Draft, when I open it, then the form is read-only and no save action is offered.

---

## EP-05 · Request lifecycle

### `US-018` — Submit a request
**Must** · `M` · Depends on: `US-015`, `TE-011`
**Traces:** `SC-07`, `RULE-04`, `AC-07`, `AC-08`

As an employee, I want to submit my Draft request, so that a manager can decide on it.

**Acceptance criteria**
- Given my own Draft request, when I `POST /requests/{id}/submit`, then it transitions to `Submitted`.
- Given a request that is not a Draft, when I try to submit it, then it is rejected as an invalid transition.
- Given a request owned by another employee, when I try to submit it, then it is forbidden (`RULE-04`).
- Given a submitted request, when I try to edit it, then it is rejected (`AC-08`).

> **Open — `OQ-04`:** whether `RULE-02` is re-evaluated at submit time is undecided. Current assumption: **it is re-evaluated**, since submitting a request whose start date has already passed is meaningless. Confirm before implementing.

---

### `US-019` — Cancel a request
**Must** · `S` · Depends on: `US-018`
**Traces:** `SC-06`, `SC-07`, `RULE-04`

As an employee, I want to cancel my request before a decision is made, so that I can withdraw it when my plans change.

**Acceptance criteria**
- Given my own request in state Draft or Submitted, when I `POST /requests/{id}/cancel`, then it transitions to `Cancelled`.
- Given a request in state Approved, Rejected or Cancelled, when I try to cancel it, then it is rejected as an invalid transition (`RULE-09`).
- Given a request owned by another employee, when I try to cancel it, then it is forbidden.

---

## EP-06 · Manager decision

### `US-020` — Manager queue
**Must** · `M` · Depends on: `TE-011`
**Traces:** `SC-09`, `RULE-06`, `AC-10`, `OS-22`

As a manager, I want to see the submitted requests assigned to me, so that I know what is waiting for my decision.

**Acceptance criteria**
- Given a logged-in manager, when I call `GET /requests`, then I receive the `Submitted` requests of the employees assigned to me.
- Given a submitted request belonging to another manager's employee, when I list requests, then it is not returned.
- Given a request in a final state, when I list requests as a manager, then it is not in my queue (no history screen in the MVP — `OS-22`).
- Given a logged-in employee, when I call the same endpoint, then I receive only my own requests — the filter is decided server-side by role.

---

### `US-021` — Approve a request
**Must** · `L` · Depends on: `US-020`, `US-018`
**Traces:** `RULE-05`–`RULE-09`, `AC-11`, `AC-12`, `AC-14`

As a manager, I want to approve a submitted request, so that the employee has a recorded, authoritative decision.

**Acceptance criteria**
- Given a Submitted request from an employee assigned to me, when I `POST /requests/{id}/approve`, then it transitions to `Approved` and exactly one Approval record is created (`RULE-08`).
- Given the created Approval, when inspecting it, then the responsible manager is the authenticated user, never a value from the payload (`AC-12`).
- Given a request in any state other than Submitted, when I try to approve it, then it is rejected (`RULE-05`).
- Given a user without the Manager role, when they try to approve, then it is forbidden (`RULE-06`).
- Given a manager acting on a request they own, when they try to approve it, then it is forbidden (`RULE-07`).
- Given a request from an employee not assigned to me, when I try to approve it, then it is forbidden (`RULE-06`).
- Given an already-decided request, when I try to decide again, then it is rejected (`RULE-09`).

> **Blocked by `OQ-01`.** A self-registered employee has no manager assignment, which makes `RULE-06` unsatisfiable for them. This story cannot be marked Ready until the sponsor decides how the assignment is set.

---

### `US-022` — Reject a request with a comment
**Must** · `M` · Depends on: `US-021`
**Traces:** `RULE-08`, `AC-11`

As a manager, I want to reject a request and explain why, so that the employee understands the decision.

**Acceptance criteria**
- Given a Submitted request assigned to me, when I `POST /requests/{id}/reject` with a comment, then it transitions to `Rejected` and one Approval record is created carrying the comment.
- Given a rejection without a comment, when I submit it, then it succeeds — the comment is optional.
- Given a rejection, when inspecting the Approval record, then it is structurally identical to an approval except for the decision and comment.
- All authorization criteria from `US-021` apply identically.

---

### `US-023` — Manager queue screen
**Must** · `M` · Depends on: `US-020`, `US-021`, `US-022`
**Traces:** `SC-01`, §7.3, `AC-11`

As a manager, I want a screen listing my pending requests with approve and reject actions, so that I can decide without using the API directly.

**Acceptance criteria**
- Given the queue, when it renders, then each row shows employee, absence type, dates, reason and the available actions.
- Given a decision action, when I trigger it, then I can add an optional comment before confirming.
- Given a completed decision, when it returns, then the list reloads from the API and the request disappears from the queue.
- Given a failed decision, when the error returns, then the reason is displayed and the request stays in the queue.

---

## EP-07 · Visibility and results

### `US-024` — My Requests screen
**Must** · `M` · Depends on: `US-020`, `US-018`, `US-019`
**Traces:** `SC-01`, §7.3, `RULE-04`

As an employee, I want to see my own requests with only the actions valid for each one, so that I am never offered something that will fail.

**Acceptance criteria**
- Given my request list, when it renders, then it shows type, start date, end date and state.
- Given a Draft request, when the row renders, then Edit, Submit and Cancel are offered.
- Given a Submitted request, when the row renders, then only Cancel and View are offered.
- Given a request in a final state, when the row renders, then only View is offered.
- Given another employee's request, when my list renders, then it never appears.

---

### `US-025` — See the final decision
**Must** · `S` · Depends on: `US-021`, `US-022`, `US-024`
**Traces:** `AC-13`, `OBJ-01`

As an employee, I want to see the outcome of my request and who decided it, so that the decision is unambiguous and attributable.

**Acceptance criteria**
- Given a decided request, when I open it, then I see the final state, the responsible manager, the decision date and the comment if present.
- Given a decided request, when it renders, then no action that would change its state is offered.

---

## EP-08 · Delivery artifacts

### `US-026` — README with setup and operating instructions
**Must** · `M` · Depends on: `TE-002`, `TE-003`
**Traces:** §12 deliverable 5, `TC-09`, `TC-11`

As a reviewer, I want clear instructions, so that I can run VacaFlow without help.

**Acceptance criteria**
- The README covers: prerequisites, how to start the API, how to start the web app, where the SQLite file lives, how to reset the database, seeded accounts, endpoint summary, scope limitations and the deferred backlog.
- Given a clean machine meeting the prerequisites, when a reviewer follows the README, then the full workflow is reachable without further guidance.

---

### `US-027` — Unit tests for date rules and state transitions
**Should** · `M` · Depends on: `US-015`, `US-018`, `US-021`
**Traces:** `SC-16`, `TC-16`, `RK-05`

As the technical lead, I want the rules covered by tests, so that a regression is caught before the demo.

**Acceptance criteria**
- Given the test suite, when run, then `RULE-01` and `RULE-02` are covered including boundary cases (same start and end date; start date equal to today).
- Given the test suite, when run, then every valid transition passes and every invalid transition is rejected.
- Given the domain tests, when run, then they require no database, no network and no IO mocks (`CA-TST-002`).

---

### `US-028` — Functional HTML prototype
**Must** · `M` · Depends on: `US-012`, `US-013`, `US-017`, `US-023`, `US-024`
**Traces:** §12 deliverable 2

As the sponsor, I want a navigable HTML prototype, so that the interface can be reviewed independently of the running system.

**Acceptance criteria**
- The prototype covers the five MVP screens and is delivered as a ZIP.
- It opens in a browser without a server or a build step.

---

### `US-029` — Source package and demo video
**Must** · `M` · Depends on: all `AC-*` stories
**Traces:** §12 deliverables 3 and 4, `TC-18`

As the sponsor, I want the source code and a recorded demo, so that acceptance is verifiable and archivable.

**Acceptance criteria**
- Given the source ZIP, when inspected, then it contains no `node_modules`, `.next`, `bin` or `obj` directory (`TC-18`).
- Given the source ZIP, when inspected, then it contains no SQLite database file and no real credentials (`LC-03`).
- Given the video, when watched, then all fourteen acceptance criteria `AC-01`–`AC-14` are demonstrated in sequence.

---

## 3. Suggested delivery sequence

| Increment | Stories | Outcome |
|---|---|---|
| **1 — Skeleton** | `TE-001`, `TE-002`, `TE-003`, `TE-004`, `TE-005` | The API starts, creates its database and seeds data |
| **2 — Identity** | `US-007`–`US-010`, `TE-011`, `US-012`, `US-013` | A real user registers, logs in and is recognized server-side |
| **3 — Employee flow** | `US-014`–`US-019`, `US-024` | `AC-01`–`AC-08` demonstrable |
| **4 — Decision flow** | `US-020`–`US-023`, `US-025` | `AC-09`–`AC-14` demonstrable |
| **5 — Hardening and handover** | `TE-006`, `US-026`–`US-029` | Tests, documentation, prototype, package and video |

Increment 2 is the risk-carrying one: `TE-011` determines whether `RK-02` materializes. It should be reviewed with particular care.

---

# Part B — Product backlog beyond the MVP

Not estimated and not scheduled. Recorded so that the deferred boundary stays explicit, and so that a future scope decision starts from a list rather than from memory. Each item traces to its deferral in `Intent.md` §6.

## Identity and access

| ID | Story | Traces |
|---|---|---|
| `FUT-01` | As an employee, I want to sign in with my corporate account, so that I do not manage another password | `OS-01` |
| `FUT-02` | As a security owner, I want multifactor authentication, so that account takeover is harder | `OS-02` |
| `FUT-03` | As a user, I want to reset my forgotten password, so that I am not locked out | `OS-03` |
| `FUT-04` | As a security owner, I want registration confirmed by email, so that accounts map to real mailboxes | `OS-04` |
| `FUT-05` | As an administrator, I want to manage accounts and roles, so that access does not depend on seed data | `OS-05` |
| `FUT-06` | As an administrator, I want to assign and reassign each employee's manager, so that the approval chain reflects the organization | `OS-05`, `OQ-01` |

## Hosting, delivery and operations

| ID | Story | Traces |
|---|---|---|
| `FUT-07` | As the company, we want VacaFlow hosted in Azure, so that it is reachable without running it locally | `OS-06` |
| `FUT-08` | As the team, we want a containerized build, so that environments are reproducible | `OS-07` |
| `FUT-09` | As the team, we want CI/CD with merge-blocking architecture tests, so that quality gates are automatic — closes the `TC-17` deviation | `OS-08`, `OQ-03` |
| `FUT-10` | As an operations owner, I want automated backups and a restore procedure, so that data loss is recoverable | `OS-09` |
| `FUT-11` | As an operations owner, I want availability and monitoring, so that the service is dependable | `OS-10` |
| `FUT-12` | As the company, we want existing request history migrated, so that we do not start empty | `OS-11` |
| `FUT-13` | As a security owner, I want the database migrated off SQLite to a server database, so that concurrency and durability are production-grade | `OS-06`, `LC-08` |

## Functional

| ID | Story | Traces |
|---|---|---|
| `FUT-14` | As a manager, I want to be notified when a request needs my decision, so that I do not have to check the app | `OS-12` |
| `FUT-15` | As an employee, I want to be notified of the decision on my request, so that I learn it without checking | `OS-12` |
| `FUT-16` | As an employee, I want to see my remaining vacation balance, so that I know what I can request | `OS-13` |
| `FUT-17` | As the company, we want holidays and working days accounted for, so that duration is calculated correctly | `OS-14` |
| `FUT-18` | As a manager, I want overlapping requests flagged, so that a team is not left uncovered | `OS-15` |
| `FUT-19` | As an employee, I want to attach supporting documents, so that sick leave can be justified | `OS-16` |
| `FUT-20` | As HR, I want a view of all absences, so that I can support the process | `OS-17` |
| `FUT-21` | As a manager, I want reports and exports, so that I can analyze absence patterns | `OS-18` |
| `FUT-22` | As the company, we want multi-level approvals, so that longer absences get a second review | `OS-19` |
| `FUT-23` | As a manager, I want to delegate approvals while away, so that requests are not blocked | `OS-20` |
| `FUT-24` | As a manager, I want to return a request for correction, so that a small error does not force a rejection | `OS-21` |
| `FUT-25` | As a manager, I want a history of the requests I have decided, so that I can review past decisions | `OS-22` |
| `FUT-26` | As an administrator, I want to maintain absence types, so that the catalog can change without a deployment | `OS-23` |
| `FUT-27` | As the company, we want integration with payroll, HR, calendar and directory systems, so that absence data flows automatically | `OS-24` |
| `FUT-28` | As an auditor, I want a full audit trail of every state change, so that any dispute can be reconstructed | `OS-25` |

## Compliance

| ID | Story | Traces |
|---|---|---|
| `FUT-29` | As a data protection owner, I want a privacy notice and a retention policy, so that personal data handling is compliant | `LC-05`, `LC-06`, `LC-08` |

---

## 4. Open questions affecting this backlog

| ID | Question | Blocks |
|---|---|---|
| `OQ-01` | How is the manager assignment established for a self-registered employee? | `US-021`, `US-022` — **not Ready** |
| `OQ-02` | Are the initial manager credentials provided, and is role selection allowed at registration? | `TE-003`, `US-007` |
| `OQ-03` | Confirm the `CA-TST-001` deviation: architecture tests run locally, no pipeline | `TE-006` |
| `OQ-04` | Is `RULE-02` re-evaluated at submit time? | `US-018` |
| `OQ-05` | Confirm the stricter reading of `RULE-06` (manager **assigned to** the employee) | `US-020`, `US-021` |

`OQ-01` is the only one that leaves a story unable to enter development. The rest can proceed under the assumptions stated in each story.
