# VacaFlow — Non-Functional Requirement Document

| Field | Value |
|---|---|
| Project | VacaFlow |
| Company | BIG Solutions |
| Document | `NFR.md` (Deliverable 1 of 6 — Project Documentation) |
| Version | 1.0 |
| Date | 2026-07-28 |
| Baseline | [`Intent.md`](Intent.md) v1.0 · [`FRD.md`](FRD.md) v1.0 · [`Backlog.md`](Backlog.md) v1.0 |

> **Purpose.** Specify *how well* the system must behave, and — equally important — *which quality attributes are deliberately not pursued*. The sponsor was explicit that performance, high availability and formal accessibility certification are not acceptance criteria for this MVP. Writing invented targets for them would misrepresent the agreement, so §12 records them as non-requirements with the reason and the condition for revisiting each.
>
> **Verification principle.** Every requirement in this document states how it is checked. A non-functional requirement with no verification method is an aspiration, not a requirement, and does not belong here.

---

## 1. Conventions

### 1.1 Identifiers

| Prefix | Quality attribute |
|---|---|
| `NFR-SEC-*` | Security |
| `NFR-PRV-*` | Privacy and data protection |
| `NFR-MNT-*` | Maintainability and architecture |
| `NFR-USA-*` | Usability and accessibility |
| `NFR-REL-*` | Reliability and data integrity |
| `NFR-POR-*` | Portability and local execution |
| `NFR-PER-*` | Performance efficiency |
| `NFR-OPS-*` | Operability and supportability |
| `NFR-CMP-*` | Compliance |

### 1.2 Obligation

**MUST** — mandatory for acceptance. **SHOULD** — expected; its absence is a defect to be recorded. **MAY** — optional.

### 1.3 Verification methods

| Method | Meaning |
|---|---|
| **Demo** | Observed during the acceptance demonstration |
| **Inspection** | Verified by reading code, configuration or data |
| **Automated** | Verified by a test that can be re-run |
| **Measurement** | Verified by taking a measurement against a stated threshold |

---

## 2. Quality priorities

The sponsor ranked the qualities that matter for this MVP: *correctness, clarity and reliability of the workflow*, with security sufficient that passwords are protected and users cannot operate on requests they do not own.

| Priority | Attribute | Why it matters here |
|:---:|---|---|
| 1 | **Security — authorization** | `LC-07` makes a bypass an acceptance failure, not a defect |
| 2 | **Reliability of the workflow** | The MVP exists to prove the lifecycle is trustworthy |
| 3 | **Maintainability** | The delivery is a foundation for a later scope decision, so it must be extensible |
| 4 | **Portability (local)** | A reviewer must run it from source without assistance |
| 5 | **Usability** | Compact and unambiguous; visual polish is not an objective |
| 6 | **Privacy** | Small data set, but real personal data |
| — | Performance, availability, scalability | **Explicitly not acceptance criteria** (§12) |

---

## 3. Security

### `NFR-SEC-001` — Password storage · **MUST** · Inspection + Automated
Passwords are stored using a salted, iterated, industry-standard password hashing function (for example PBKDF2 with a high iteration count, bcrypt or Argon2id). Plain passwords and reversible encryption are forbidden.
**Verification:** inspect the database — no field contains a readable password; inspect the hashing configuration.
*Traces:* `SC-03`, `LC-02`, `FR-AUT-003`

### `NFR-SEC-002` — Passwords never leave the server · **MUST** · Inspection
No API response, log entry, error message or diagnostic output contains a password or a password hash.
**Verification:** inspect `GET /auth/me` and every response contract; inspect log output after a full workflow run.
*Traces:* `LC-02`, `FR-AUT-009`

### `NFR-SEC-003` — Server-derived identity · **MUST** · Automated + Demo
The acting user for every business operation is derived from the authenticated context. No endpoint accepts an identity value as input for a business decision.
**Verification:** an automated test posts a request payload containing a foreign `employeeId` and asserts the created request is owned by the authenticated caller; repeated for `responsibleManagerId` on approve.
*Traces:* `TC-08`, `OBJ-02`, `AC-12`, `AC-14`, `FR-AUT-010`

> This is the highest-priority requirement in the document. `RK-02` is the risk it mitigates, and the sponsor stated that a system allowing identity to be supplied by the frontend is not acceptable regardless of any other quality.

### `NFR-SEC-004` — Authorization enforced server-side · **MUST** · Automated + Demo
Ownership and manager-assignment checks execute in the application layer on every protected operation. Hiding an action in the UI is never the enforcement point.
**Verification:** automated tests call every protected endpoint as a non-owner and as a non-manager, asserting rejection; demonstrated live for `AC-14`.
*Traces:* `LC-07`, `RULE-04`, `RULE-06`, `RULE-07`, `FR-AUT-010`, `RK-05`

### `NFR-SEC-005` — Session handling · **MUST** · Inspection
The session or token has a bounded lifetime and is invalidated on logout. If cookie-based, the cookie is `HttpOnly` and `SameSite`; if token-based, the token is signed and its signing key comes from configuration, never from source.
**Verification:** inspect the authentication configuration and the cookie or token attributes.
*Traces:* `TC-07`, `TC-14`, `FR-AUT-008`

### `NFR-SEC-006` — No secrets in source · **MUST** · Inspection
No connection string, signing key, token or password is hardcoded. Configurable values are read from configuration and injected as typed options.
**Verification:** search the repository for credential-shaped literals; inspect configuration files tracked in git.
*Traces:* `TC-14`, `CA-INF-007`

### `NFR-SEC-007` — Input validation at the boundary · **MUST** · Automated
Every endpoint validates the structure, type, presence and length of its input before the use case executes. Over-long strings, wrong types and missing fields are rejected with `VF-VAL-001` rather than reaching the domain.
**Verification:** automated tests submit malformed payloads to each endpoint.
*Traces:* `CA-APP-007`, `FR-REQ-004`

### `NFR-SEC-008` — Injection resistance · **MUST** · Inspection
All data access goes through Entity Framework Core with parameterized queries. No SQL is assembled by string concatenation.
**Verification:** inspect the infrastructure layer for raw SQL construction.
*Traces:* `TC-04`

### `NFR-SEC-009` — Generic authentication failures · **SHOULD** · Automated
Failed login does not disclose whether the email exists.
**Verification:** automated test asserts identical responses for unknown email and wrong password.
*Traces:* `FR-AUT-006`

### `NFR-SEC-010` — Dependency hygiene · **SHOULD** · Inspection
No dependency with a known critical vulnerability is shipped. Package versions are pinned rather than floating.
**Verification:** inspect the lock files and the restore output for advisories.
*Traces:* `LC-08`

---

## 4. Privacy and data protection

### `NFR-PRV-001` — Personal data inventory · **MUST** · Inspection
The system stores exactly this personal data: full name, email, password hash, request reasons, absence dates and approval comments. No additional personal data is collected.
**Verification:** compare the schema against this list.
*Traces:* `LC-01`

### `NFR-PRV-002` — Database file never versioned · **MUST** · Automated
The SQLite database file, its journal and WAL companions are excluded from version control.
**Verification:** `.gitignore` covers `*.db`, `*.db-shm`, `*.db-wal`; `git status` is clean after a full application run.
*Traces:* `LC-03`

### `NFR-PRV-003` — No real personal data in the repository · **MUST** · Inspection
Seed data, fixtures, screenshots and documentation contain no real employee names, emails or credentials.
**Verification:** inspect the seed data and the delivered artifacts.
*Traces:* `LC-03`, `LC-04`

### `NFR-PRV-004` — Least disclosure between users · **MUST** · Automated
No response exposes another employee's personal data beyond what the workflow requires. A manager sees the name of an employee whose request they must decide; an employee never sees another employee's data.
**Verification:** automated tests inspect list and detail responses for both roles.
*Traces:* `LC-07`, `FR-VIS-002`

### `NFR-PRV-005` — Personal data absent from logs · **MUST** · Inspection
Application logs contain no passwords, no request reasons and no approval comments. Identifiers may be logged; free-text personal content may not.
**Verification:** inspect log output after executing the full workflow.
*Traces:* `LC-01`

### `NFR-PRV-006` — Retention documented, not implemented · **MUST** · Inspection
No retention or deletion mechanism exists. The README states that data persists in the SQLite file until manually deleted or reset.
**Verification:** confirm the statement is present in the README.
*Traces:* `LC-06`

---

## 5. Maintainability and architecture

### `NFR-MNT-001` — Dependency rule · **MUST** · Automated
Dependencies point only inward. `Domain` has zero internal project references and no dependency on EF Core, ASP.NET Core or serialization libraries.
**Verification:** architecture tests covering `CA-DEP-001`, `CA-DEP-002`, `CA-DEP-003`, `CA-DEP-005`.
*Traces:* `TC-05`, `TE-001`, `TE-006`

### `NFR-MNT-002` — Presentation isolated from persistence · **MUST** · Automated
API endpoints do not reference `DbContext`, concrete repositories or SQL.
**Verification:** architecture test covering `CA-DEP-008`.
*Traces:* `TC-05`, `TE-006`

### `NFR-MNT-003` — Application layer free of framework types · **MUST** · Automated
The application layer contains no ASP.NET Core or EF Core types.
**Verification:** architecture tests covering `CA-APP-004` and `CA-APP-005`.
*Traces:* `TC-05`, `TE-006`

### `NFR-MNT-004` — Thin endpoints · **SHOULD** · Inspection
An endpoint receives, delegates to a use case and maps the result. Guideline threshold: 15 lines per endpoint. No business conditional appears in an endpoint.
**Verification:** code inspection at review.
*Traces:* `CA-PRE-001`

### `NFR-MNT-005` — Forbidden patterns absent · **MUST** · Inspection
MediatR, CQRS dispatchers, event sourcing, generic repositories, messaging infrastructure and microservice decomposition are absent.
**Verification:** inspect the dependency manifests and the project structure.
*Traces:* `TC-06`, `RK-03`

### `NFR-MNT-006` — Domain testable in isolation · **MUST** · Automated
Domain unit tests require no database, no network, no filesystem and no IO mocks.
**Verification:** run the domain test project with no infrastructure available.
*Traces:* `CA-TST-002`, `US-027`

### `NFR-MNT-007` — Rule coverage · **SHOULD** · Automated
Every rule `RULE-01` through `RULE-09` has at least one test asserting it is enforced, and each state transition has a test for its valid and its invalid paths.
**Verification:** map tests to rules; the mapping is complete.
*Traces:* `SC-16`, `TC-16`, `US-027`, `RK-05`

### `NFR-MNT-008` — Architecture compliance score · **SHOULD** · Inspection
The project scores at least **90/100** on the rubric in `reglas-clean-architecture-onion.md` §16, with zero unremediated blocking (🔴) findings.
**Verification:** run the audit checklist in §15.1 of that document.
*Traces:* `TC-05`

### `NFR-MNT-009` — Naming reflects the business · **SHOULD** · Inspection
Types are named in business language. Generic suffixes — `Manager`, `Helper`, `Util`, `Processor` — are avoided, with the exception of the domain concept *Manager* as a role, which is business vocabulary.
**Verification:** code inspection.
*Traces:* `CA-STR-005`

### `NFR-MNT-010` — Documented deviations · **MUST** · Inspection
Any deviation from a 🟠 or 🟡 architecture rule is recorded with its rule identifier, reason and exit condition. Blocking rules admit no deviation.
**Verification:** the deviation list exists and matches the code.
*Traces:* `TC-17`, §18 of the architecture rules

---

## 6. Usability and accessibility

The sponsor asked for *basic readable forms and labels*, with no formal accessibility certification. The requirements below are the blocking (🔴) subset of the `UX-*` rules plus what the workflow needs to be unambiguous. They are deliberately modest.

### `NFR-USA-001` — Location and state always communicated · **MUST** · Demo
Every screen communicates who is signed in, what the user is looking at, and which actions are available.
**Verification:** demonstrated on each of the five screens.
*Traces:* `UX-PRN-002`, `FR-UIX-001`

### `NFR-USA-002` — Every action produces feedback · **MUST** · Demo
Create, edit, submit, cancel, approve and reject each produce a perceptible result: an updated list, a confirmation, or an error message. No action completes silently.
**Verification:** demonstrated for each action.
*Traces:* `UX-PRN-002`, `FR-UIX-003`, `FR-UIX-005`

### `NFR-USA-003` — Errors are specific and actionable · **MUST** · Demo
An error tells the user what failed and what to do. Raw exception text, stack traces and bare status codes are never shown.
**Verification:** trigger each error in the §7 catalog of the FRD and observe the message.
*Traces:* `UX-FBK`, `FR-UIX-003`, `TE-005`

### `NFR-USA-004` — Labelled controls · **MUST** · Inspection
Every form control has a visible, programmatically associated label. Placeholder text is not used as a label.
**Verification:** inspect the markup of the register, login and request forms.
*Traces:* `TC-15`, `UX-FRM`, `FR-UIX-006`

### `NFR-USA-005` — Keyboard operability · **SHOULD** · Demo
The complete workflow is operable by keyboard, with a visible focus indicator on every interactive element.
**Verification:** execute the full workflow without a pointing device.
*Traces:* `UX-ACC`

### `NFR-USA-006` — Text contrast · **SHOULD** · Measurement
Body text meets a contrast ratio of at least 4.5:1 against its background; large text at least 3:1.
**Verification:** measure with a contrast checking tool on each screen.
*Traces:* `UX-ACC`, WCAG 2.2 AA

### `NFR-USA-007` — Meaning not conveyed by color alone · **SHOULD** · Inspection
Request state is communicated by text, not only by a color.
**Verification:** inspect the request list rendered in greyscale.
*Traces:* `UX-ACC`

### `NFR-USA-008` — Interface states handled · **SHOULD** · Demo
Lists and forms handle loading, empty, error and populated states explicitly. An empty list explains why it is empty.
**Verification:** demonstrate a first login with no requests.
*Traces:* `UX-EST`, `FR-UIX-004`

### `NFR-USA-009` — Destructive action confirmed · **SHOULD** · Demo
Cancelling a request asks for confirmation, since cancellation is final and cannot be undone.
**Verification:** demonstrated on a Submitted request.
*Traces:* `UX-CMP`, `RULE-09`

### `NFR-USA-010` — Usable viewport range · **MAY** · Demo
The interface remains usable from 1280px down to 768px width. Mobile-first optimization is not an objective.
**Verification:** resize the browser during the demo.
*Traces:* `SC-01`

---

## 7. Reliability and data integrity

### `NFR-REL-001` — Atomic decision · **MUST** · Automated
The state transition and the creation of the `Approval` record commit together. A partial outcome is never observable.
**Verification:** automated test forces a failure after the transition and asserts that neither change persisted.
*Traces:* `FR-DEC-009`, `RULE-08`, `CA-APP-008`

### `NFR-REL-002` — One decision per request enforced twice · **MUST** · Automated
The single-decision rule is enforced in the domain and additionally protected by a unique constraint on `Approval.RequestId`.
**Verification:** automated test attempts a second decision and asserts rejection; inspect the schema for the constraint.
*Traces:* `RULE-09`, `FR-DEC-007`

### `NFR-REL-003` — No invalid state reachable · **MUST** · Automated
Every transition outside the table in `FRD.md` §4.2 is rejected. A request cannot exist in a state inconsistent with its data.
**Verification:** automated tests exercise the full transition matrix, valid and invalid.
*Traces:* `FRD` §4.2, `RULE-09`, `US-027`

### `NFR-REL-004` — Idempotent startup · **MUST** · Demo
Starting the API repeatedly against an existing database creates no duplicate seed data and loses no existing data.
**Verification:** start the API three times and inspect the data.
*Traces:* `FR-DAT-004`

### `NFR-REL-005` — Deterministic date evaluation · **MUST** · Automated
Date rules evaluate against an injected time source, producing identical results for a fixed date regardless of when the test runs.
**Verification:** automated tests with a fixed clock, including the boundary where the start date equals today.
*Traces:* `TC-13`, `FR-REQ-003`, `TE-004`

### `NFR-REL-006` — Failures do not corrupt state · **MUST** · Inspection
A rejected operation leaves the request exactly as it was. No partial mutation is persisted before validation completes.
**Verification:** inspect the use cases for mutation-before-validation ordering.
*Traces:* `CA-DOM-002`

### `NFR-REL-007` — Frontend reflects server state · **MUST** · Demo
After every action the affected list is reloaded from the API. The UI never displays an optimistic state that the server did not confirm.
**Verification:** demonstrated after a rejected action — the list shows the unchanged server state.
*Traces:* `FR-UIX-005`

---

## 8. Portability and local execution

### `NFR-POR-001` — Runs from source · **MUST** · Demo
With the documented prerequisites installed, a reviewer starts the API and the web application from a clean checkout and completes the workflow, without Docker, container runtime or cloud service.
**Verification:** a reviewer who did not build the system follows the README end to end.
*Traces:* `TC-09`, `BC-05`, `US-026`

### `NFR-POR-002` — Pinned, documented runtime versions · **MUST** · Inspection
The required .NET SDK and Node.js versions are pinned in the project files and stated in the README. Long-term-support versions are preferred.
**Verification:** inspect the project files and the README.
*Traces:* `US-026`

### `NFR-POR-003` — Self-contained storage · **MUST** · Inspection
The database is a single SQLite file at a path relative to the application, requiring no database server and no external configuration.
**Verification:** inspect the connection configuration.
*Traces:* `TC-03`, `SC-11`

### `NFR-POR-004` — Operating system neutrality · **SHOULD** · Inspection
No hardcoded absolute path, drive letter or platform-specific path separator. The application runs on Windows, macOS and Linux.
**Verification:** inspect path handling; run on a second operating system if one is available.
*Traces:* `TC-09`

### `NFR-POR-005` — Clean source package · **MUST** · Inspection
The delivered source archive excludes `node_modules`, `.next`, `bin`, `obj`, and contains no database file and no real credentials.
**Verification:** extract the archive and inspect its contents.
*Traces:* `TC-18`, `LC-03`, `US-029`

---

## 9. Performance efficiency

The sponsor stated that speed is not a concern because the user count is small. The requirements below are **guardrails against pathological behavior**, not performance targets, and they are not acceptance gates.

### `NFR-PER-001` — Interactive response · **SHOULD** · Measurement
On a typical development machine with a seeded database of up to 100 requests, list and detail endpoints respond in under 500 ms, and write operations in under 1 s.
**Verification:** measure during the demo run.
*Traces:* `BC-10`

### `NFR-PER-002` — No unbounded query growth · **SHOULD** · Inspection
Listing requests does not issue one query per row. Related data required by a list is retrieved in the same query.
**Verification:** inspect the generated SQL for the list endpoint.
*Traces:* `BC-10`

### `NFR-PER-003` — Bounded result sets · **SHOULD** · Inspection
List endpoints filter server-side by role rather than retrieving all rows and filtering in memory.
**Verification:** inspect the query for `GET /requests`.
*Traces:* `FR-VIS-001`, `NFR-SEC-004`

### `NFR-PER-004` — Reasonable startup · **MAY** · Measurement
The API becomes ready within 10 seconds on a cold start, including database creation and seeding.
**Verification:** measure a first run.
*Traces:* `NFR-POR-001`

---

## 10. Operability and supportability

### `NFR-OPS-001` — Documented reset · **MUST** · Demo
A reviewer returns to a clean seeded state by following a documented procedure.
**Verification:** perform the reset and confirm the seeded state.
*Traces:* `TC-11`, `FR-DAT-006`

### `NFR-OPS-002` — Complete README · **MUST** · Inspection
The README covers prerequisites, starting the API, starting the web application, the SQLite file location, the reset procedure, seeded accounts, the endpoint summary, scope limitations and the deferred backlog.
**Verification:** compare the README against this list.
*Traces:* `US-026`, `Intent.md` §12

### `NFR-OPS-003` — Diagnosable failures · **SHOULD** · Inspection
Server-side failures are logged with enough context — operation, request identifier, error code — to diagnose them, through the `ILogger` abstraction rather than a concrete logging framework.
**Verification:** inspect the logging calls and a sample log.
*Traces:* `CA-CRS-001`

### `NFR-OPS-004` — Fail fast on misconfiguration · **SHOULD** · Demo
Missing required configuration causes startup to fail with a clear message, rather than a runtime failure later.
**Verification:** start with a required setting removed.
*Traces:* `CA-CFG-006`

### `NFR-OPS-005` — Defect response during review · **MUST** · Process
During the validation window, defects blocking registration, login, request creation, submission, approval, rejection or final status visibility are addressed before cosmetic issues.
**Verification:** the defect log shows this ordering.
*Traces:* `BC-06`

---

## 11. Compliance

### `NFR-CMP-001` — No privacy notice or consent flow · **MUST** · Inspection
The MVP implements neither. The documentation states that the application stores basic employee identity and absence request data.
**Verification:** confirm the statement exists and no consent UI is present.
*Traces:* `LC-05`

### `NFR-CMP-002` — Acceptance-level authorization guarantee · **MUST** · Demo
A demonstrated ability to bypass the logged-in identity, or to approve without being the assigned manager, constitutes rejection of the delivery rather than a defect to be triaged.
**Verification:** `AC-14` executed during the acceptance demo.
*Traces:* `LC-07`, `BC-08`

### `NFR-CMP-003` — Production hardening recorded · **MUST** · Inspection
The conditions requiring a formal review before any broader deployment — privacy, retention, authentication hardening, audit records, hosting and backups — are documented in the deferred backlog.
**Verification:** confirm `Backlog.md` Part B covers them.
*Traces:* `LC-08`

---

## 12. Explicit non-requirements

Recorded so that their absence is understood as a decision rather than an omission. Each states the condition under which it must be revisited.

| Attribute | Status | Reason | Revisit when |
|---|---|---|---|
| **High availability / uptime target** | Not required | Local MVP; no production availability is promised | The application is hosted for real users |
| **Scalability / load capacity** | Not required | Small, known user population | Usage extends beyond the pilot group |
| **Performance SLA** | Not required | The sponsor stated speed is not a concern | A hosted deployment introduces network latency and shared load |
| **Formal WCAG 2.2 AA certification** | Not required | Basic readable forms and labels are sufficient for the MVP | The application is rolled out company-wide |
| **Automated backup and recovery** | Not required | Local SQLite file; the README explains how to copy it | The database holds data that cannot be recreated |
| **Disaster recovery / RPO / RTO** | Not required | No production hosting | Hosting is introduced |
| **Localization / multi-language** | Not required | Single language, single organization (`AS-05`) | The company operates the tool across languages |
| **Browser support matrix** | Not required | Reviewers use current desktop browsers | The user base becomes heterogeneous |
| **Penetration testing** | Not required | Local execution, internal reviewers, no exposed surface | Before any internet-facing deployment |
| **Audit trail beyond the Approval record** | Not required | `OS-25` defers it | Disputes must be reconstructable |
| **Rate limiting / brute-force protection** | Not required | No exposed surface in the MVP | The login endpoint becomes network-reachable |
| **HTTPS in transport** | Not required locally | Traffic does not leave the machine | Any deployment beyond localhost — then **mandatory** |
| **Monitoring, alerting, telemetry** | Not required | No operational responsibility in this delivery | The application is operated as a service |

> The last two deserve emphasis. Running without HTTPS and without brute-force protection is acceptable **only** because the application never leaves the reviewer's machine. Both become mandatory the moment `FUT-07` (Azure hosting) is considered, and the scope decision that authorizes hosting must authorize them together.

---

## 13. Verification summary

| Method | Requirements | When |
|---|---|---|
| **Automated** | `NFR-SEC-003`, `NFR-SEC-004`, `NFR-SEC-007`, `NFR-SEC-009`, `NFR-PRV-002`, `NFR-PRV-004`, `NFR-MNT-001`–`003`, `NFR-MNT-006`, `NFR-MNT-007`, `NFR-REL-001`–`003`, `NFR-REL-005` | Every `dotnet test` run |
| **Inspection** | `NFR-SEC-001`, `NFR-SEC-002`, `NFR-SEC-005`, `NFR-SEC-006`, `NFR-SEC-008`, `NFR-SEC-010`, `NFR-PRV-001`, `NFR-PRV-003`, `NFR-PRV-005`, `NFR-PRV-006`, `NFR-MNT-004`, `NFR-MNT-005`, `NFR-MNT-008`–`010`, `NFR-USA-004`, `NFR-USA-007`, `NFR-REL-006`, `NFR-POR-002`–`005`, `NFR-PER-002`, `NFR-PER-003`, `NFR-OPS-002`, `NFR-OPS-003`, `NFR-CMP-001`, `NFR-CMP-003` | Code review, before handover |
| **Demo** | `NFR-SEC-003`, `NFR-SEC-004`, `NFR-USA-001`–`003`, `NFR-USA-005`, `NFR-USA-008`–`010`, `NFR-REL-004`, `NFR-REL-007`, `NFR-POR-001`, `NFR-OPS-001`, `NFR-OPS-004`, `NFR-CMP-002` | Acceptance demonstration |
| **Measurement** | `NFR-USA-006`, `NFR-PER-001`, `NFR-PER-004` | During the demo run |
| **Process** | `NFR-OPS-005` | Throughout the validation window |

---

## 14. Traceability

| Intent constraint | Requirements |
|---|---|
| `SC-03` Hashed passwords | `NFR-SEC-001`, `NFR-SEC-002` |
| `SC-09` Server-derived identity | `NFR-SEC-003`, `NFR-SEC-004` |
| `SC-16` Rule tests | `NFR-MNT-006`, `NFR-MNT-007` |
| `TC-05` Onion architecture | `NFR-MNT-001`–`003`, `NFR-MNT-008` |
| `TC-06` Forbidden patterns | `NFR-MNT-005` |
| `TC-08` No trusted identifiers | `NFR-SEC-003` |
| `TC-09` Local execution | `NFR-POR-001`, `NFR-POR-004` |
| `TC-11` Database reset | `NFR-OPS-001` |
| `TC-13` Injected time | `NFR-REL-005` |
| `TC-14` No hardcoded secrets | `NFR-SEC-005`, `NFR-SEC-006` |
| `TC-15` UI rules | `NFR-USA-001`–`010` |
| `TC-17` Local architecture tests | `NFR-MNT-001`–`003`, `NFR-MNT-010` |
| `TC-18` Clean package | `NFR-POR-005` |
| `LC-01` Personal data inventory | `NFR-PRV-001`, `NFR-PRV-005` |
| `LC-02` No plain passwords | `NFR-SEC-001`, `NFR-SEC-002` |
| `LC-03` Database not exposed | `NFR-PRV-002`, `NFR-PRV-003`, `NFR-POR-005` |
| `LC-05` No consent flow | `NFR-CMP-001` |
| `LC-06` No retention rule | `NFR-PRV-006` |
| `LC-07` No unauthorized access | `NFR-SEC-004`, `NFR-PRV-004`, `NFR-CMP-002` |
| `LC-08` Production hardening | `NFR-SEC-010`, `NFR-CMP-003`, §12 |
| `BC-06` Review-window support | `NFR-OPS-005` |
| `BC-10` Small user population | `NFR-PER-001`–`003` |
