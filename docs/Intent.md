# VacaFlow — Intent

| Field | Value |
|---|---|
| Project | VacaFlow |
| Company | BIG Solutions |
| Sponsor | James Parker — Operations Manager |
| Functional Analyst | Emily Harrison |
| Document | `Intent.md` (Deliverable 1 of 6 — Project Documentation) |
| Version | 1.0 |
| Date | 2026-07-28 |
| Status | Baseline for FRD, NFR, Backlog, SAD and WBS |

> **Purpose.** This document fixes *what VacaFlow is, what it is not, and under which constraints it must be built*. Every downstream document (`FRD.md`, `NFR.md`, `Backlog.md`, `SAD.md`, `WBS.md`) traces back to the identifiers defined here. Any change to scope requires a new version of this document, not an inline decision during implementation.

---

## 1. Project summary

VacaFlow is a **limited internal MVP** for managing vacation and absence requests at BIG Solutions.

Employees register and log in with an account managed by the application, create an absence request as a Draft, edit it while it is still a Draft, and submit it for a decision. Managers log in, see the submitted requests assigned to them, and approve or reject them with an optional comment. The system stores the final state and the authenticated manager who is responsible for the decision.

The MVP is delivered as **source code executed locally**. It is not deployed, not hosted, and not integrated with any corporate system.

The measure of success is not feature count. It is a single demonstrable statement: *the complete request lifecycle works end to end, with real accounts, and the business rules cannot be bypassed from the frontend.*

---

## 2. Business context and problem

Today, vacation and absence requests at BIG Solutions travel through email, chat messages and spreadsheets. The consequences are:

| Problem | Effect |
|---|---|
| No single request record | Nobody can state with certainty whether a request was actually sent |
| No recorded decision | An informal chat reply was once taken as an approval that was never registered |
| No traceable ownership | It is unclear which manager was responsible for a decision |
| Manual status chasing | Managers spend time confirming status instead of deciding |

VacaFlow replaces this with **one controlled request record**, a small explicit state machine, and a decision that is attributed to an authenticated manager.

---

## 3. Objectives and success criteria

### 3.1 Objectives

| ID | Objective |
|---|---|
| `OBJ-01` | Prove the full request lifecycle in a working application: register → login → draft → edit → submit → decide → view result |
| `OBJ-02` | Guarantee that user identity for business decisions is derived server-side and cannot be supplied by the frontend |
| `OBJ-03` | Enforce the mandatory business rules in the API/application layer, not only in the UI |
| `OBJ-04` | Keep the architecture small and readable enough to be handed over and extended later |
| `OBJ-05` | Produce a validated baseline for a future scope decision on broader company use |

### 3.2 Definition of success

The MVP is successful when the sponsor — plus one manager and one employee acting as reviewers — can execute the acceptance demo (§13) from a locally started application, and when no reviewer can perform an action they are not entitled to perform.

### 3.3 Explicit non-goals

- VacaFlow is **not** an HR platform.
- VacaFlow is **not** a production system in this delivery.
- VacaFlow does **not** calculate entitlements, balances, or working days.

---

## 4. Users and roles

The MVP has exactly **two application roles**. No HR role, no administrator role, no external user, no executive dashboard.

### 4.1 Employee

Registers and logs in · views **only their own** requests · creates a request · edits a Draft · submits a Draft · cancels a Draft or Submitted request · views the final decision.

### 4.2 Manager

Logs in · views **Submitted requests assigned to them** · approves a request · rejects a request · adds an optional decision comment.

A Manager who also owns requests acts as an Employee for those requests, and is explicitly forbidden from deciding on them (`RULE-05`).

---

## 5. In scope

| ID | Item |
|---|---|
| `SC-01` | Compact Next.js / React web application (Register, Login, My Requests, Request form, Manager Queue) |
| `SC-02` | Local account registration and login managed inside VacaFlow (name, email, password, role) |
| `SC-03` | Password hashing; no plain-text password is ever stored |
| `SC-04` | Two roles: Employee and Manager |
| `SC-05` | Four business entities: Employee, Absence Type, Request, Approval |
| `SC-06` | Request state machine: Draft, Submitted, Approved, Rejected, Cancelled |
| `SC-07` | Named actions: create, edit Draft, submit, cancel, approve, reject |
| `SC-08` | ASP.NET Core Minimal API with the endpoint surface defined in §7.5 |
| `SC-09` | Server-side derivation of request owner and responsible approver from the authenticated session/token |
| `SC-10` | Mandatory business rules (§7.4) enforced in the API/application layer |
| `SC-11` | SQLite database file for application data and local authentication tables |
| `SC-12` | Entity Framework Core for data access |
| `SC-13` | Reduced Onion Architecture: Domain, Application, Infrastructure, API, Web |
| `SC-14` | Seed data: three absence types and at least one Manager account |
| `SC-15` | Local execution from source code, with reset instructions for the database |
| `SC-16` | A small set of unit tests covering date rules and state transitions |
| `SC-17` | Project documentation set, functional prototype, source code package and demo video (§12) |

**Scope principle (from the MVP presentation):** *add no feature that is not required to demonstrate the end-to-end request decision flow.*

---

## 6. Out of scope

Everything below is **deliberately deferred**. Adding any of it requires a separate, explicit scope decision — not a judgement call during implementation.

### 6.1 Identity and access

| ID | Deferred item |
|---|---|
| `OS-01` | Microsoft Entra ID / corporate single sign-on / any external identity provider |
| `OS-02` | Multifactor authentication |
| `OS-03` | Password reset |
| `OS-04` | Email verification / registration confirmation email |
| `OS-05` | Account administration and role administration screens |

### 6.2 Hosting and delivery

| ID | Deferred item |
|---|---|
| `OS-06` | Azure deployment or any cloud hosting |
| `OS-07` | Docker and containerization |
| `OS-08` | CI/CD pipelines |
| `OS-09` | Automated backups |
| `OS-10` | High availability, uptime commitments, production operational support |
| `OS-11` | Data migration from existing systems |

### 6.3 Functional

| ID | Deferred item |
|---|---|
| `OS-12` | Email or Microsoft Teams notifications |
| `OS-13` | Vacation balance calculation |
| `OS-14` | Holiday calendars and working-day calculations |
| `OS-15` | Overlapping request validation |
| `OS-16` | Attachments and supporting documents |
| `OS-17` | HR views and HR administration screens |
| `OS-18` | Reports, dashboards, exports, formal approval letters |
| `OS-19` | Multi-level approvals |
| `OS-20` | Approval delegation |
| `OS-21` | Additional states such as "returned for correction" or "pending HR review" |
| `OS-22` | Manager full-history screen (managers see only pending Submitted requests) |
| `OS-23` | Absence type maintenance screen (types are seed data) |
| `OS-24` | Integrations with payroll, HR, calendar or directory systems |
| `OS-25` | Advanced audit trail beyond the core Approval record |

---

## 7. What the MVP includes

### 7.1 Business entities

| Entity | Content |
|---|---|
| **Employee** | Name, email, role, active status, and a simple manager assignment (a manager reference — not an organization chart) |
| **Absence Type** | Catalog classifying the request. Seeded values: Vacation, Personal Leave, Sick Leave |
| **Request** | Owner, absence type, start date, end date, reason, current state, relevant dates |
| **Approval** | Request, responsible manager, decision, optional comment, decision date |

Authentication account records are **technical infrastructure**, not a fifth business entity.

### 7.2 Request lifecycle

States: `Draft` · `Submitted` · `Approved` · `Rejected` · `Cancelled`

Valid transitions — and **only** these:

| From | To | Actor |
|---|---|---|
| Draft | Submitted | Owner (Employee) |
| Draft | Cancelled | Owner (Employee) |
| Submitted | Approved | Assigned Manager |
| Submitted | Rejected | Assigned Manager |
| Submitted | Cancelled | Owner (Employee) |

`Approved`, `Rejected` and `Cancelled` are **final** for the MVP. There is no generic state-change action.

### 7.3 Screens

| Screen | Purpose |
|---|---|
| Register | Name, email, password, role |
| Login | Email and password; the current user is displayed after login |
| My Requests | Employee sees only their own requests, with only the actions valid for each state |
| Request form | Create and edit a Draft: absence type, start date, end date, reason |
| Manager Queue | Manager sees Submitted requests assigned to them, with approve/reject and optional comment |

Buttons change by role and state. Actions that are invalid for the current state are not shown. After each action the list reloads from the API.

### 7.4 Mandatory business rules

| ID | Rule |
|---|---|
| `RULE-01` | The end date cannot be earlier than the start date |
| `RULE-02` | The start date cannot be in the past |
| `RULE-03` | Only Draft requests can be edited |
| `RULE-04` | Only the authenticated owner can edit, submit or cancel their request |
| `RULE-05` | Only Submitted requests can be approved or rejected |
| `RULE-06` | Only a user with the Manager role, assigned as the manager of the request owner, can approve or reject |
| `RULE-07` | A manager cannot approve or reject their own request |
| `RULE-08` | Approving or rejecting creates exactly one Approval record, with the authenticated manager as responsible |
| `RULE-09` | A request can have only one final decision; final states are immutable |

These rules live in the **domain/application layers**. UI-side hiding of invalid actions is a usability aid, never the enforcement point.

### 7.5 API surface

**Authentication** — `POST /auth/register` · `POST /auth/login` · `POST /auth/logout` · `GET /auth/me`

**Catalog and workflow** — `GET /absence-types` · `GET /requests` · `POST /requests` · `PUT /requests/{id}` · `POST /requests/{id}/submit` · `POST /requests/{id}/cancel` · `POST /requests/{id}/approve` · `POST /requests/{id}/reject`

**Identity rule (non-negotiable).** The API derives request ownership and approval responsibility from the authenticated user. `employeeId` and `responsibleManagerId` are never accepted as trusted values from the frontend. `GET /requests` returns what the caller is entitled to see, based on role.

**Error behavior.** Invalid actions return a clear, specific error — e.g. editing a Submitted request states that only Draft requests can be edited; a non-manager attempting to approve is forbidden.

### 7.6 Seed data

- Absence types: Vacation, Personal Leave, Sick Leave.
- At least one Manager account, so approvals can be tested without an administration screen.
- A sample Employee account is useful but not required, since registration is available.

---

## 8. Business constraints

| ID | Constraint | Source |
|---|---|---|
| `BC-01` | The MVP must not grow into an HR platform. Features are not added because they seem easy | Sponsor, Transcript 03 |
| `BC-02` | Only two roles exist. No HR role, no administration module | Transcript 01 |
| `BC-03` | No role administration screen. Manager accounts are seeded or controlled manually during the pilot | Transcript 01 / 02 |
| `BC-04` | The manager assignment lives on the Employee record and is a simple reference, not an organization chart | Transcript 02 |
| `BC-05` | The MVP is delivered as source code run locally, not as a hosted service | Transcript 03 |
| `BC-06` | Support means fixing defects found during the review window; blocking defects first, cosmetic issues later. No post-deployment operational support | Transcript 03 |
| `BC-07` | James Parker signs off functionally. One manager and one employee may run the workflow before final acceptance | Transcript 03 |
| `BC-08` | Acceptance depends on workflow completeness, authorization and rule enforcement — not on performance or visual polish | Transcript 02 / 03 |
| `BC-09` | "VacaFlow" is the official project name in all documents and in the application interface | Transcript 01 |
| `BC-10` | The user population is small; throughput and scalability are not acceptance concerns | Transcript 02 |
| `BC-11` | Scope creep is the primary identified project risk and is managed by treating this document as the boundary | Transcript 02 |

---

## 9. Technical constraints

| ID | Constraint | Source |
|---|---|---|
| `TC-01` | Web: Next.js and React | Transcript 02 / Slide 8 |
| `TC-02` | API: ASP.NET Core **Minimal API** | Transcript 02 / Slide 10 |
| `TC-03` | Persistence: **SQLite**, single local database file | Transcript 02 / 03 |
| `TC-04` | Data access: Entity Framework Core | Executive Summary §9 |
| `TC-05` | Architecture: reduced Onion — Domain, Application, Infrastructure, API, Web — following `Docs/reglas-clean-architecture-onion.md` (`CA-*` rules, normative) | Transcript 02 / Docs |
| `TC-06` | **Forbidden patterns:** MediatR, CQRS, event sourcing, generic repositories, messaging, microservices, complex deployment pipelines | Slide 8 |
| `TC-07` | Passwords must be stored hashed. The mechanism for the authenticated session (cookie or token) is an implementation choice, provided it is consistent and safe | Transcript 02 |
| `TC-08` | The frontend must never supply a trusted employee or approver identifier for business decisions | Transcript 02, Slide 4 |
| `TC-09` | The application must run locally from source with a documented start sequence for API and Web | Transcript 03 |
| `TC-10` | Database creation may use EF migrations or automatic creation — whichever is simpler and clearer — but a reviewer must obtain the initial data by running the API | Transcript 02 |
| `TC-11` | A documented procedure to reset the database must exist | Transcript 03 |
| `TC-12` | Business rules must not be implemented in the database (no triggers/stored procedures with domain logic) — `CA-INF-003` | Docs |
| `TC-13` | Time must be injected (`TimeProvider` / `IClock`), never read statically inside domain logic — `CA-DOM-009`, `CA-CRS-002`. Required by `RULE-02` | Docs |
| `TC-14` | No connection strings, keys or secrets hardcoded in source — `CA-INF-007` | Docs |
| `TC-15` | Frontend follows `Docs/reglas-diseno-ui-ux-web.md` (`UX-*` rules). Blocking (🔴) rules apply; formal WCAG 2.2 AA certification is not required, but forms must have readable labels and the interface must not be difficult to use | Docs / Transcript 02 |
| `TC-16` | Testing: unit tests for date rules and state transitions are expected. Full test automation is not required | Transcript 02 |
| `TC-17` | Architecture tests are run locally with `dotnet test`. `CA-TST-001` requires them in a merge-blocking pipeline; since CI/CD is out of scope (`OS-08`), this is a **documented controlled deviation** (§15, `OQ-03`) | Docs / Transcript 02 |
| `TC-18` | Source code package must exclude `node_modules`, `.next`, `bin` and `obj` | Slide 12 |

---

## 10. Legal, privacy and security constraints

| ID | Constraint | Source |
|---|---|---|
| `LC-01` | Protected data in this MVP: user emails, password hashes, names, request reasons, dates and approval comments | Transcript 03 |
| `LC-02` | Plain-text passwords are never stored | Transcript 01 / 02 / Slide 4 |
| `LC-03` | The SQLite database file must not be publicly exposed, and must not be committed to the repository containing real passwords or real personal data | Transcript 03 |
| `LC-04` | Seed and demo accounts must use clearly non-production credentials | Derived from `LC-03` |
| `LC-05` | No privacy notice or consent flow is included in the MVP. It is documented that the application stores basic employee identity and absence request data | Transcript 03 |
| `LC-06` | No data retention rule applies. Data remains in the SQLite file until manually deleted or reset | Transcript 03 |
| `LC-07` | A user must not be able to read or act on requests they do not own, and must not be able to decide without the Manager role and the corresponding assignment. A breach of this is an **acceptance failure**, not a defect to triage | Transcript 03 |
| `LC-08` | If VacaFlow moves toward production use, privacy, retention, authentication hardening and audit records must be revisited through a formal, separate decision | Transcript 03 |

---

## 11. Assumptions

| ID | Assumption |
|---|---|
| `AS-01` | Reviewers can run .NET and Node.js locally on their own machines |
| `AS-02` | Registration is open during the MVP; role selection at registration is used for testing convenience and is not a production-grade control |
| `AS-03` | The manager assignment for a registered employee is established through seed data or controlled setup, since no administration screen exists (see `OQ-01`) |
| `AS-04` | Dates are handled as calendar dates, without time zones, working-day logic or partial days |
| `AS-05` | The MVP operates in a single language and a single organization |
| `AS-06` | The number of concurrent reviewers is small enough that SQLite write concurrency is not a limiting factor |

---

## 12. Deliverables

| # | Deliverable | Content |
|---|---|---|
| 1 | **Project documentation** | `Intent.md` (this document) · `FRD.md` · `NFR.md` · `Backlog.md` · `SAD.md` · `WBS.md` |
| 2 | **Functional prototype** | HTML, delivered as a ZIP |
| 3 | **Source code** | ZIP, excluding `node_modules`, `.next`, `bin`, `obj` |
| 4 | **Demo video** | Full acceptance demo recorded end to end |
| 5 | **README** | Setup, how to run API and Web, how to access SQLite, seeded accounts, endpoint summary, scope limitations, deferred backlog |

---

## 13. Acceptance criteria

The MVP is accepted when the following can be demonstrated live, with real local accounts:

| # | Scenario |
|---|---|
| `AC-01` | Register an employee account |
| `AC-02` | Log in |
| `AC-03` | Create a Draft request |
| `AC-04` | Reject an invalid date range (end before start) |
| `AC-05` | Reject a start date in the past |
| `AC-06` | Edit a Draft request |
| `AC-07` | Submit a request |
| `AC-08` | Prevent editing after submission |
| `AC-09` | Log in as a manager |
| `AC-10` | View Submitted requests assigned to that manager |
| `AC-11` | Approve **or** reject with a comment — both decisions create an Approval record |
| `AC-12` | Record the authenticated manager as responsible |
| `AC-13` | Show the final decision to the employee |
| `AC-14` | Block unauthorized operations (non-owner acting on a request; non-manager approving; manager approving their own request) |

---

## 14. Risks

| ID | Risk | Severity | Mitigation |
|---|---|---|---|
| `RK-01` | **Scope creep** — the MVP drifts toward an HR platform | High | This document is the boundary; §6 items require a new scope decision |
| `RK-02` | **Identity spoofing** — authentication is built such that the frontend can still supply the acting user | High | `TC-08` is non-negotiable; `AC-14` tests it explicitly |
| `RK-03` | **Over-engineering** — patterns from the `CA-*` rules applied beyond what a 4-entity MVP needs | Medium | `TC-06` forbidden-pattern list; physical layer separation without a mediator |
| `RK-04` | **UI expansion** — the interface grows into dashboards and navigation | Medium | §7.3 fixes the five screens |
| `RK-05` | **Rules only in the UI** — validation implemented in React and not in the API | High | `RULE-*` enforced in domain/application; unit tests per `SC-16` |
| `RK-06` | **Unassigned manager** — a registered employee has no manager, making `RULE-06` unsatisfiable | Medium | Resolve `OQ-01` before implementing the approval use case |

---

## 15. Open questions

These must be resolved before the affected work item is implemented. None of them blocks the start of the project.

| ID | Question | Blocks | Owner |
|---|---|---|---|
| `OQ-01` | How is the manager assignment set for an employee who self-registers? Registration collects name, email, password and role — but not a manager. Options: seed a default manager, assign at registration, or a controlled setup step | `RULE-06`, `AC-10`, `AC-14` | James Parker |
| `OQ-02` | Are the initial Manager account credentials provided by the sponsor, and is role selection allowed during registration for the MVP review? (Open action item from Transcript 02) | `SC-14`, `AS-02` | James Parker |
| `OQ-03` | `CA-TST-001` requires architecture tests in a merge-blocking pipeline, but CI/CD is out of scope (`OS-08`). Confirm the controlled deviation: tests exist and run locally, no pipeline | `TC-17` | Technical lead |
| `OQ-04` | Is `RULE-02` (no past start date) evaluated only at create/edit, or re-evaluated at submit? A Draft created today for tomorrow becomes invalid if submitted in two days | `RULE-02`, `AC-05` | Emily Harrison |
| `OQ-05` | The Executive Summary states "only a Manager can approve or reject"; the presentation and both transcripts state "only a Manager **assigned to the employee**". This document takes the stricter reading (`RULE-06`) — confirm | `RULE-06` | James Parker |

---

## 16. Traceability

This document consolidates and supersedes the scope statements scattered across the following sources. Where sources conflict, the resolution is recorded in §15.

| Source | Contribution |
|---|---|
| `Mettings/VacaFlow-MVP-01-Meeting-Transcript-Product-Scope-and-Workflow.md` | Business problem, users, entities, lifecycle, core rules |
| `Mettings/VacaFlow-MVP-02-Meeting-Transcript-Delivery-Architecture-and-Acceptance.md` | Architecture, authentication model, API surface, acceptance scenarios, risks |
| `Mettings/VacaFlow-MVP-03-Meeting-Transcript-Launch-Operations-and-Handover.md` | Delivery model, support, data protection, retention, documentation expectations |
| `Mettings/VacaFlow-MVP-04-Executive-Summary.md` | Consolidated scope, rules, acceptance criteria, deferred backlog |
| `Mettings/VacaFlow-MVP-Scope-Validation.md` | Validated included/deferred boundary, authentication refinement |
| `Presentation/VacaFlow_MVP_Presentation.pptx` | Scope principle, identity boundary, forbidden patterns, deliverables list |
| `Docs/reglas-clean-architecture-onion.md` | Normative `CA-*` architecture rules |
| `Docs/reglas-diseno-ui-ux-web.md` | Normative `UX-*` interface rules |

---

## 17. Glossary

| Term | Meaning |
|---|---|
| **Draft** | A request created but not yet submitted. The only editable state |
| **Submitted** | A request awaiting a manager decision. Read-only for the employee, cancellable by the owner |
| **Approval** | The record of a manager's decision — responsible manager, decision, optional comment, date |
| **Responsible manager** | The authenticated Manager who approved or rejected. Always derived server-side |
| **Manager assignment** | The simple manager reference stored on the Employee record |
| **Final state** | Approved, Rejected or Cancelled. Not modifiable in the MVP |
| **Seed data** | Absence types and the initial Manager account created automatically at startup |
