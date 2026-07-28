# VacaFlow — Backlog

| Field | Value |
|---|---|
| Project | VacaFlow |
| Company | BIG Solutions |
| Document | `Backlog.md` (Deliverable 1 of 6 — Project Documentation) |
| Version | **2.0** — rebuilt against the functional prototype |
| Date | 2026-07-28 |
| Baseline | [`Intent.md`](Intent.md) v1.0 · [`FRD.md`](FRD.md) v1.0 |
| Design source | [`docs/prototype/`](prototype/) — 11 screens and the prototype markup |

> **Purpose.** The user stories covering **the whole solution and the MVP**, with each MVP story now specifying *how the screen must look and behave*, taken from the functional prototype rather than described in the abstract.
>
> **What changed from v1.0.** Every story gained a **Screen** reference and visual acceptance criteria. Seven stories were added (`US-030`–`US-036`) for interface behavior the prototype revealed and the earlier backlog never named: the application shell, the notification banner, list loading and empty states, the two modals, the pending-count badge and the accessibility baseline. Story identifiers from v1.0 are unchanged, so the traceability in `FRD.md` and `WBS.md` still holds — see §9 for the impact.

---

## 1. Conventions

### 1.1 Identifiers

| Prefix | Meaning |
|---|---|
| `EP-*` | Epic |
| `US-*` | MVP user story (Part A) |
| `TE-*` | Technical enabler — no direct user value, required before dependent stories |
| `S-*` | Screen or component defined in §3.2 |
| `FUT-*` | Post-MVP story (Part B) |

### 1.2 Priority

**Must** — the acceptance demo fails without it. **Should** — expected quality; its absence is a defect, not a demo blocker. **Could** — included only if it costs nothing.

### 1.3 Sizing

`S` ≈ half a day · `M` ≈ one day · `L` ≈ two or more days.

### 1.4 Definition of Ready

Traces to `Intent.md`; acceptance criteria written as verifiable Given/When/Then; a screen reference where the story has a visible surface; dependencies done or in the same increment; no open question blocking it.

### 1.5 Definition of Done

Code compiles · the business rule is enforced in the domain or application layer, **not only in the UI** · the endpoint derives identity server-side (`TC-08`) · invalid actions return a clear specific error · **the rendered screen matches the referenced prototype screen in structure and states, with the English copy of §3.5** · architecture tests pass · the acceptance criteria are demonstrable in the running application.

---

## 2. Product language

**The application interface is in English**, as are code identifiers, database values, API contracts, error codes and this documentation set. The product is monolingual; no localization mechanism is built (`FUT-32`).

> **The prototype is in Spanish.** It was produced with Spanish labels throughout — `Mis solicitudes`, `Bandeja de aprobación`, `Iniciar sesión`, and the state badges `Borrador` / `Enviada` / `Aprobada` / `Rechazada` / `Cancelada`. The decision to ship in English came after it was built.
>
> **Consequence.** Take the prototype as authoritative for *layout, structure, spacing, color and interaction*, and take §3.5 of this document as authoritative for *every string*. The screenshots in `docs/prototype/` will not match the implementation word for word, and that is expected. `US-028` covers re-cutting the prototype in English before it ships as deliverable 2 — see §7.

One convenient side effect: the state labels shown in the interface now coincide exactly with the values persisted in `Request.State`. The two remain separate concerns — §3.4 is still a presentation table, carrying the badge palette — but no translation sits between them.

---

## 3. Design reference

Everything in this section is read from the prototype, with the copy rendered in English. Where an implementation detail is not specified here, follow the prototype markup.

### 3.1 Design tokens

| Token | Value | Use |
|---|---|---|
| Font — interface | `IBM Plex Sans`, weights 400/500/600/700 | All text |
| Font — brand and data | `IBM Plex Mono`, weights 400/500/600 | The VacaFlow wordmark, credentials block |
| Page background | `oklch(98% 0.004 250)` | Application background |
| Surface | `white` | Cards, header, modals |
| Text primary | `oklch(22% 0.02 260)` | Body |
| Text secondary | `oklch(50% 0.02 260)` | Subtitles, metadata |
| Border | `oklch(90% 0.006 250)` | Cards, header rule |
| Border — input | `oklch(85% 0.008 260)` | Fields, secondary buttons |
| Accent | `oklch(52% 0.15 260)` | Primary buttons, links, focus ring |
| Success | `oklch(55% 0.14 150)` | Approve button |
| Danger | `oklch(55% 0.18 25)` | Destructive confirmation |
| Radius | 7–8px controls · 10px list rows · 12px cards and modals | — |
| Shadow | `0 1px 3px oklch(0% 0 0 / 0.06)` | Auth cards only |
| Content width | 1100px main · 560px form card · 400–420px auth card | — |
| Focus ring | `2px solid oklch(52% 0.15 260)`, offset 2px | Every interactive element |

### 3.2 Screen inventory

| ID | Screen | Prototype file | Stories |
|---|---|---|---|
| `S-01` | Sign in | [`01-login.png`](prototype/screenshots/01-login.png) | `US-013` |
| `S-02` | Create account | [`03-register.png`](prototype/screenshots/03-register.png) | `US-012` |
| `S-03` | Application shell — header, nav, banner | visible in `05`–`11` | `US-030`, `US-031`, `US-035` |
| `S-04` | My Requests — list | [`05-my-requests.png`](prototype/screenshots/05-my-requests.png) | `US-024`, `US-032` |
| `S-04b` | My Requests — empty | [`09-my-requests-manager.png`](prototype/screenshots/09-my-requests-manager.png) | `US-032` |
| `S-05` | Request form — create and edit | [`06-new-request-form.png`](prototype/screenshots/06-new-request-form.png) | `US-017` |
| `S-06` | Request detail — read-only with decision | [`07-request-detail.png`](prototype/screenshots/07-request-detail.png) | `US-025` |
| `S-07` | Approval Queue | [`10-manager-queue.png`](prototype/screenshots/10-manager-queue.png) | `US-023` |
| `S-08` | Cancel confirmation modal | [`08-cancel-confirm-modal.png`](prototype/screenshots/08-cancel-confirm-modal.png) | `US-033` |
| `S-09` | Decision modal | [`11-approve-decision-modal.png`](prototype/screenshots/11-approve-decision-modal.png) | `US-034` |

The exact markup, inline styles and client logic are in [`prototype/VacaFlow.dc.html`](prototype/VacaFlow.dc.html). Where this document and the prototype markup disagree: the markup wins for visual detail, this document wins for business behavior, and §3.5 always wins for copy.

### 3.3 Component patterns

**Auth card** (`S-01`, `S-02`) — centered, full viewport height, white card 400–420px wide, 40px padding, 12px radius, subtle shadow. Wordmark `VacaFlow` in mono 22px/600, then a 14px secondary subtitle, then a 28px gap before the form. Fields stack with a 16px gap. Primary button full width. A secondary link line below, then a divider before the auxiliary block.

**Application header** (`S-03`) — white bar, 14px/32px padding, bottom border. Left: mono wordmark 18px, then nav buttons with 36px separation. Right: current user's name (14px/600) over role (12px secondary), right-aligned, then a bordered `Sign out` button.

**Nav tab** — pill button, 8px/16px padding, 8px radius. Active: background `oklch(93% 0.03 260)`, text `oklch(35% 0.1 260)`. Inactive: transparent background, text `oklch(45% 0.02 260)`.

**List row** (`S-04`) — white card, 1px border, 10px radius, 16px/20px padding, rows separated by 10px. Layout: type name (15px/600) over date range (13px secondary) on the left and flexible; state badge; action buttons. Wraps on narrow viewports.

**Queue card** (`S-07`) — white card, 20px padding. Top line: employee name (15px/600) over `type · start → end` (13px secondary); actions on the right. Then a top-bordered block, 12px above and below, with the reason at 14px.

**Empty state** — white card with a **dashed** border, 12px radius, centered, 64px/24px padding: a 16px/600 title, a 14px secondary line, and a primary call-to-action button where an action is available.

**Loading skeleton** — grey blocks `oklch(94% 0.004 250)`, 10px radius, stacked with a 10px gap. Three blocks of 64px for `S-04`; two blocks of 96px for `S-07`.

**Modal** — fixed overlay `oklch(0% 0 0 / 0.4)`, centered white panel, 12px radius, 28px padding, max-width 400px (`S-08`) or 420px (`S-09`). Actions right-aligned. Closes on overlay click and on `Escape`; a click inside the panel does not close it.

**Banner** — inside the content column, 12px/16px padding, 8px radius, `role="status"`, message on the left and a `×` dismiss button on the right, 150ms fade-in. Success: background `oklch(93% 0.06 150)`, text `oklch(30% 0.12 150)`. Error: background `oklch(95% 0.03 25)`, text `oklch(35% 0.15 25)`.

**Inline field error** — `role="alert"`, 13px, `oklch(50% 0.18 25)`, 6px below its field.

### 3.4 State badges

Pill, `999px` radius, 4px/12px padding, 12px/600. The label matches the persisted value.

| State | Label | Background | Foreground |
|---|---|---|---|
| `Draft` | **Draft** | `oklch(93% 0.01 260)` | `oklch(35% 0.02 260)` |
| `Submitted` | **Submitted** | `oklch(90% 0.09 80)` | `oklch(38% 0.12 70)` |
| `Approved` | **Approved** | `oklch(90% 0.09 150)` | `oklch(33% 0.12 150)` |
| `Rejected` | **Rejected** | `oklch(91% 0.09 25)` | `oklch(40% 0.15 25)` |
| `Cancelled` | **Cancelled** | `oklch(93% 0.01 260)` | `oklch(45% 0.01 260)` |

`Draft` and `Cancelled` share a background. They are distinguished by label and by the actions offered, never by color alone — which is also what `NFR-USA-007` requires.

### 3.5 Microcopy catalog

Implement these strings verbatim. This table, not the prototype, is authoritative for copy.

**Navigation and shell**

| Context | String |
|---|---|
| Wordmark | `VacaFlow` |
| Sign-in subtitle | `Absence request management` |
| Create-account subtitle | `Create an account` |
| Nav — employee view | `My Requests` |
| Nav — manager queue | `Approval Queue (N)` — the count is omitted when zero |
| Role display | `Employee` · `Manager` |
| Sign out | `Sign out` |
| Skip link | `Skip to main content` |
| Banner dismiss | `aria-label="Dismiss notification"` |
| Form back button | `aria-label="Back to my requests"` |

**Forms**

| Field | Label | Helper |
|---|---|---|
| Email | `Email` | — |
| Password | `Password` | `Minimum 8 characters.` (create account only) |
| Name | `Full name` | — |
| Role | `Role (for demo purposes)` | options `Employee` · `Manager` |
| Absence type | `Absence type` | placeholder option `Select…` |
| Start date | `Start date` | — |
| End date | `End date` | — |
| Reason | `Reason` | live counter `N/500` |
| Decision comment | `Comment (optional)` | — |

**Buttons**

`Sign in` · `Create account` · `New request` · `Create request` · `Save draft` · `Save changes` · `Cancel` · `Back` · `Edit` · `Submit` · `View` · `Cancel request` · `Approve` · `Reject` · `Yes, cancel`

**Link lines**

`Don't have an account? Sign up` (on `S-01`) · `Already have an account? Sign in` (on `S-02`)

**Success banners**

| Trigger | Message |
|---|---|
| Account created | `Account created. Welcome to VacaFlow!` |
| Signed in | `Signed in as {name}.` |
| Draft created | `Draft created.` |
| Draft edited | `Changes saved.` |
| Submitted | `Request submitted for approval.` |
| Cancelled | `Request cancelled.` |
| Approved | `Request approved.` |
| Rejected | `Request rejected.` |

**Error messages** — these are exactly the `FRD.md` §7 catalog

| Code | Message |
|---|---|
| `VF-AUT-001` | `An account with this email already exists.` |
| `VF-AUT-002` | `The email or password is incorrect.` |
| `VF-AUT-003` | `This account is not active.` |
| `VF-AUT-004` | `You must be signed in to perform this action.` |
| `VF-CAT-001` | `The selected absence type does not exist or is not available.` |
| `VF-REQ-001` | `The end date cannot be earlier than the start date.` |
| `VF-REQ-002` | `The start date cannot be in the past.` |
| `VF-REQ-003` | `Only Draft requests can be edited.` |
| `VF-REQ-004` | `You can only act on your own requests.` |
| `VF-REQ-005` | `This request cannot move from {current} to {target}.` |
| `VF-REQ-006` | `The request was not found.` |
| `VF-DEC-001` | `Only Submitted requests can be approved or rejected.` |
| `VF-DEC-002` | `Only a manager can approve or reject a request.` |
| `VF-DEC-003` | `You are not the manager assigned to this employee.` |
| `VF-DEC-004` | `You cannot decide on your own request.` |
| `VF-DEC-005` | `This request already has a final decision.` |

Validation messages: `Full name is required (max 120 characters).` · `Enter a valid email address, for example name@company.com` · `The password must be at least 8 characters.` · `The start date is required.` · `The end date is required.` · `The reason is required (1 to 500 characters).`

**Empty states**

| Screen | Title | Body | Action |
|---|---|---|---|
| `S-04b` | `You haven't created any requests yet` | `Create your first absence request to get started.` | `Create request` |
| `S-07` empty | `No pending requests` | `When an employee assigned to you submits a request, it will appear here.` | — |

**Modals**

| Modal | Title | Body | Actions |
|---|---|---|---|
| `S-08` | `Cancel this request?` | `This action cannot be undone. The request will move to the Cancelled state.` | `Back` · `Yes, cancel` |
| `S-09` approve | `Approve this request?` | comment field | `Cancel` · `Approve` |
| `S-09` reject | `Reject this request?` | comment field | `Cancel` · `Reject` |

**Detail decision block** (`S-06`)

Heading `DECISION` (uppercase, letter-spaced) · decision as `Approved` or `Rejected` · attribution line `By {manager name} · {date and time}`.

**Sign-in test credentials block** (`S-01`)

Heading `TEST ACCOUNTS (NON-PRODUCTION)`, then the two credential pairs in monospace.

### 3.6 Seed data fixed by the prototype

| Employee | Email | Role | Manager |
|---|---|---|---|
| Laura Méndez | `manager@vacaflow.test` | Manager | — |
| Carlos Ruiz | `employee@vacaflow.test` | Employee | Laura Méndez |
| Ana Torres | `ana@vacaflow.test` | Employee | Laura Méndez |

Passwords: `Manager123!` and `Employee123!`. Absence types: **Vacation** (`VACATION`), **Personal Leave** (`PERSONAL_LEAVE`), **Sick Leave** (`SICK_LEAVE`) — the prototype's Spanish display names are replaced by the English names already used in `Intent.md` §7.1.

`S-01` displays a `TEST ACCOUNTS (NON-PRODUCTION)` block listing the two credentials — acceptable and useful for the MVP review, and consistent with `LC-04`. It must not survive into any deployed build (`FUT-30`).

---

## 4. Epic map

| Epic | Title | Stories |
|---|---|---|
| `EP-01` | Foundations | `TE-001`–`TE-006` |
| `EP-02` | Authentication and identity | `US-007`–`US-013`, `TE-011` |
| `EP-03` | Application shell and feedback | `US-030`–`US-032`, `US-035`, `US-036` |
| `EP-04` | Absence catalog | `US-014` |
| `EP-05` | Request authoring | `US-015`–`US-017` |
| `EP-06` | Request lifecycle | `US-018`, `US-019`, `US-033` |
| `EP-07` | Manager decision | `US-020`–`US-023`, `US-034` |
| `EP-08` | Visibility and results | `US-024`, `US-025` |
| `EP-09` | Delivery artifacts | `US-026`–`US-029` |

---

# Part A — MVP backlog

## EP-01 · Foundations

### `TE-001` — Solution skeleton with Onion rings
**Must** · `L` · Depends on: — · **Traces:** `SC-13`, `TC-05`, `TC-06`

As the development team, I need the solution physically separated into `Domain`, `Application`, `Infrastructure`, `Api` and `Web`, so that the dependency rule can be enforced instead of merely intended.

- Given the solution, when inspecting project references, then dependencies point only inward: `Domain` has zero internal references; `Application` references only `Domain`; `Infrastructure` references `Application` and `Domain`; `Api` references `Application` and `Domain`.
- Given `Domain`, when searched, then there is no reference to EF Core, ASP.NET Core or any serialization library.
- Given the codebase, when reviewed, then no MediatR, CQRS dispatcher, event sourcing, generic repository, messaging or microservice pattern is present.

### `TE-002` — Persistence with EF Core and SQLite
**Must** · `M` · Depends on: `TE-001` · **Traces:** `SC-11`, `SC-12`, `TC-03`, `TC-10`, `TC-11`

- Given a clean checkout with no database file, when the API starts, then the SQLite file is created with the full schema.
- Given the domain entities, when inspected, then they carry no persistence attributes; mapping is Fluent API in `Infrastructure`.
- Given the README reset procedure, when followed, then a clean seeded database is produced.
- Given a full application run, when `git status` is checked, then the database file is untracked.

### `TE-003` — Seed data
**Must** · `S` · Depends on: `TE-002` · **Traces:** `SC-14`, `BC-03`, `LC-04`

- Given a new database, when the API starts, then the three absence types of §3.6 exist with their English display names and matching codes.
- Given a new database, when the API starts, then the three employees of §3.6 exist, with Carlos Ruiz and Ana Torres assigned to Laura Méndez.
- Given a restart on an existing database, when seeding runs, then no duplicates are created.
- Given the seeded credentials, when inspected, then they are clearly non-production and documented in the README.

### `TE-004` — Injected time provider
**Must** · `S` · Depends on: `TE-001` · **Traces:** `TC-13`, `RULE-02`

- Given `Domain` and `Application`, when searched, then there is no `DateTime.Now` or `DateTime.UtcNow`.
- Given a unit test with a fixed date, when the date rules run, then they evaluate deterministically.

### `TE-005` — Centralized error handling
**Must** · `M` · Depends on: `TE-001` · **Traces:** §7.5 of the FRD

- Given a rule violation, when the endpoint returns, then the response carries `{ code, message, field? }` with the code and message from the `FRD.md` §7 catalog.
- Given the endpoints, when reviewed, then error translation happens in one place, not per endpoint.
- Given a `VF-REQ-005`, when returned, then `{current}` and `{target}` interpolate the state names of §3.4.
- Given an unhandled exception, when it occurs, then a generic `500` is returned that leaks no internals.

### `TE-006` — Architecture tests
**Should** · `M` · Depends on: `TE-001` · **Traces:** `TC-17`, `OQ-03`

- Given the test project, when run with `dotnet test`, then it validates `CA-DEP-001`, `CA-DEP-002`, `CA-DEP-003`, `CA-DEP-008`, `CA-APP-004` and `CA-APP-005`.
- Given a deliberate violation, when the tests run, then they fail naming the offending type.

---

## EP-02 · Authentication and identity

### `US-007` — Create an account
**Must** · `M` · Depends on: `TE-002`, `TE-003` · **Screen:** `S-02` · **Traces:** `SC-02`, `SC-03`, `LC-02`, `AC-01`

As a new user, I want to register with my name, email, password and role, so that I can access VacaFlow with my own account.

**Behavior**
- Given valid data, when I `POST /auth/register`, then an `Employee` and a `UserAccount` are created and I am signed in directly, landing on `S-04` with the banner `Account created. Welcome to VacaFlow!`
- Given an already-registered email, when I register, then `VF-AUT-001` is returned beneath the email field and no second account is created. Comparison is case-insensitive.
- Given any registration, when the database is inspected, then the password is stored hashed.
- Given a name over 120 characters, a malformed email or a password under 8 characters, when I submit, then the corresponding validation message from §3.5 appears beneath that field.

**Visual — `S-02`**
- Auth card 420px per §3.3, subtitle `Create an account`.
- Four groups in order: `Full name` (maxlength 120), `Email` (`type=email`), `Password` (`type=password`, helper `Minimum 8 characters.`), and a `fieldset` with legend `Role (for demo purposes)`.
- The role control is two radio options side by side, each in a bordered 8px-radius box, equal width, labels `Employee` and `Manager`. `Employee` is preselected.
- Primary full-width button `Create account`, disabled while saving.
- Below the form: `Already have an account? Sign in`.

### `US-008` — Sign in
**Must** · `M` · Depends on: `US-007` · **Screen:** `S-01` · **Traces:** `SC-02`, `AC-02`

- Given correct credentials, when I `POST /auth/login`, then a session is established and I land on `S-04` with the banner `Signed in as {name}.`
- Given a wrong password or unknown email, when I sign in, then `VF-AUT-002` appears in an alert block above the form, the email is preserved and the password field is cleared.
- Given an inactive employee, when I sign in, then `VF-AUT-003` is returned.

**Visual — `S-01`**
- Auth card 400px, subtitle `Absence request management`.
- Two fields with `autocomplete` set to `email` and `current-password`; primary full-width `Sign in`.
- Below: `Don't have an account? Sign up`.
- Bottom block separated by a top border: an 11px uppercase letter-spaced heading `TEST ACCOUNTS (NON-PRODUCTION)` and the two credential pairs in mono 12px.
- The error block, when present, sits between the subtitle and the form with `role="alert"`.

### `US-009` — Sign out
**Should** · `S` · Depends on: `US-008` · **Screen:** `S-03` · **Traces:** `SC-02`

- Given a signed-in user, when I press `Sign out`, then the session is invalidated and I return to `S-01` with no banner carried over.
- Given a signed-out user, when a workflow endpoint is called, then it returns `VF-AUT-004`.

### `US-010` — Retrieve the current user
**Must** · `S` · Depends on: `US-008` · **Traces:** `SC-02`, `SC-09`

- Given a signed-in user, when `GET /auth/me` is called, then it returns identifier, name, email and role — never the password hash.
- Given no session, when it is called, then it returns `VF-AUT-004`.

### `TE-011` — Server-side identity derivation
**Must** · `M` · Depends on: `US-008` · **Traces:** `SC-09`, `TC-08`, `OBJ-02`, `RK-02`, `AC-14`

As the sponsor, I need every business decision to use the identity from the authenticated context, so that nobody can act on behalf of another person by editing a request payload.

- Given the API contracts, when reviewed, then no endpoint accepts `employeeId` or `responsibleManagerId`.
- Given a payload containing such a field, when processed, then the value is ignored entirely.
- Given a use case needing the acting user, when it runs, then it obtains it through the `ICurrentUser` port.

> The single most important technical story in the MVP. `RK-02` and `AC-14` both hang on it.

### `US-012` — Create-account screen
**Must** · `M` · Depends on: `US-007` · **Screen:** `S-02`

Covered by the visual criteria of `US-007`, plus: every field has a visible `<label>` bound by `for`/`id`; errors render with `role="alert"`; the entered values survive a rejected submission.

### `US-013` — Sign-in screen
**Must** · `M` · Depends on: `US-008`, `US-010` · **Screen:** `S-01`

Covered by the visual criteria of `US-008`, plus: after signing in, the header of `S-03` shows the current user's name and role on every screen.

---

## EP-03 · Application shell and feedback

### `US-030` — Application shell
**Must** · `M` · Depends on: `US-010` · **Screen:** `S-03` · **Traces:** `SC-01`, `FR-UIX-001`

As a signed-in user, I want a consistent header showing where I am and who I am, so that I never have to wonder about either.

- Given any signed-in screen, when it renders, then the header shows the wordmark, the navigation, my name, my role and `Sign out`.
- Given an Employee, when the header renders, then the navigation contains only `My Requests`.
- Given a Manager, when the header renders, then the navigation contains `My Requests` and `Approval Queue`.
- Given the active tab, when it renders, then it uses the active pill style of §3.3 and the other uses the inactive style.
- Given a Manager, when they open `My Requests`, then they see their own requests as any employee would.

**Visual** — header per §3.3; main content constrained to 1100px, centered, 32px padding.

### `US-031` — Notification banner
**Must** · `S` · Depends on: `US-030` · **Screen:** `S-03` · **Traces:** `FR-UIX-003`, `NFR-USA-002`

As a user, I want every action to tell me what happened, so that no operation completes silently.

- Given a successful action, when it completes, then the success banner of §3.5 appears at the top of the content column with `role="status"`.
- Given a rejected action, when the error returns, then the error banner shows the message from §3.5 in the error palette.
- Given a banner, when I press `×`, then it disappears.
- Given a banner, when I navigate to another screen, then it is cleared rather than carried over.
- Given a banner, when it appears, then it fades in over 150ms.

### `US-032` — List loading and empty states
**Must** · `S` · Depends on: `US-024`, `US-020` · **Screens:** `S-04`, `S-04b`, `S-07` · **Traces:** `FR-UIX-004`, `NFR-USA-008`

As a user, I want the list to tell me when it is loading and when there is nothing to show, so that I never face an ambiguous blank area.

- Given a list being fetched, when it renders, then the skeleton of §3.3 is shown — three 64px blocks on `S-04`, two 96px blocks on `S-07`.
- Given an employee with no requests, when `S-04` renders, then the `S-04b` empty state appears with its title, body and the `Create request` button.
- Given a manager with an empty queue, when `S-07` renders, then the empty state appears with its own copy and **no** action button.
- Given the empty state card, when it renders, then its border is dashed, distinguishing it from a populated row.

### `US-035` — Pending count on the manager tab
**Should** · `S` · Depends on: `US-030`, `US-020` · **Screen:** `S-03`

- Given a manager with pending requests, when the header renders, then the tab reads `Approval Queue (N)` with N the number of requests awaiting their decision.
- Given a manager with none pending, when the header renders, then the tab reads `Approval Queue` with no parenthetical.
- Given a decision that empties the queue, when it completes, then the count disappears without a page reload.

### `US-036` — Accessibility baseline
**Should** · `S` · Depends on: `US-030` · **Traces:** `TC-15`, `NFR-USA-004`–`007`

- Given any page, when I press Tab from the top, then the first stop is a `Skip to main content` link that becomes visible on focus and jumps to `#main-content`.
- Given any interactive element, when focused, then a 2px accent outline with 2px offset is visible.
- Given an open modal, when I press `Escape`, then it closes.
- Given any form control, when inspected, then it has a visible label associated by `for`/`id`; no placeholder acts as a label.
- Given the request list rendered in greyscale, when read, then every state remains identifiable by its text label.

---

## EP-04 · Absence catalog

### `US-014` — List absence types
**Must** · `S` · Depends on: `TE-003` · **Screen:** `S-05` · **Traces:** `SC-14`

- Given a signed-in user, when `GET /absence-types` is called, then the active types are returned with identifier, code and display name.
- Given the request form, when it loads, then the `Absence type` select is populated from this endpoint and never hardcoded.
- Given the select, when it renders, then the first option is the disabled-value placeholder `Select…`.
- Given no session, when the endpoint is called, then it returns `VF-AUT-004`.

---

## EP-05 · Request authoring

### `US-015` — Create a Draft request
**Must** · `M` · Depends on: `TE-011`, `US-014`, `TE-004` · **Screen:** `S-05` · **Traces:** `RULE-01`, `RULE-02`, `AC-03`–`AC-05`

- Given valid data, when I `POST /requests`, then a request is created in `Draft` owned by the authenticated user, I return to `S-04` and the banner reads `Draft created.`
- Given an end date before the start date, when I save, then `VF-REQ-001` appears beneath `End date`.
- Given a start date before today, when I save, then `VF-REQ-002` appears beneath `Start date`.
- Given a payload carrying an `employeeId`, when processed, then the owner is still the authenticated user.
- Given a missing type, date or reason, when I save, then the corresponding validation message appears beneath that field.

### `US-016` — Edit a Draft request
**Must** · `M` · Depends on: `US-015` · **Screen:** `S-05` · **Traces:** `RULE-03`, `RULE-04`, `AC-06`, `AC-08`

- Given my own `Draft`, when I press `Edit` and save, then the type, dates and reason are updated and the banner reads `Changes saved.`
- Given a request in any other state, when an edit is attempted, then `VF-REQ-003` is returned.
- Given another employee's request, when an edit is attempted, then `VF-REQ-004` is returned.
- Given an edit violating `RULE-01` or `RULE-02`, when saved, then the same field messages as on creation appear.

**Visual** — the form title reads `Edit draft`; the primary button reads `Save changes`.

### `US-017` — Request form screen
**Must** · `M` · Depends on: `US-015`, `US-016` · **Screen:** `S-05`

As an employee, I want one form for creating and editing, so that the experience is consistent.

**Visual — `S-05`**
- Header row: a `←` back button with `aria-label="Back to my requests"`, then the title — `New request`, `Edit draft` or `Request detail`.
- White card, max-width 560px, 32px padding, fields with an 18px gap.
- Order: `Absence type` select · a row with `Start date` and `End date` side by side, each min-width 180px, wrapping on narrow viewports · `Reason` textarea, 4 rows, `maxlength=500`, vertically resizable.
- `Start date` carries `min` set to today; `End date` carries `min` set to the chosen start date. This is an affordance — the API validates regardless.
- The `Reason` label row shows a live `N/500` counter, right-aligned, 12px secondary.
- Action row 28px below the card content: primary `Save draft` or `Save changes`, then a secondary `Cancel`.
- A general error, when present, renders in an alert block at the top of the card.
- Given a request that is not a `Draft`, when the form opens, then every control is disabled, the primary save button is absent and the secondary button reads `Back`.

---

## EP-06 · Request lifecycle

### `US-018` — Submit a request
**Must** · `M` · Depends on: `US-015`, `TE-011` · **Screen:** `S-04` · **Traces:** `RULE-04`, `AC-07`, `AC-08`

- Given my own `Draft`, when I press `Submit`, then it becomes `Submitted`, the list reloads and the banner reads `Request submitted for approval.`
- Given a `Draft` whose start date has since passed, when I submit, then `VF-REQ-002` is returned in an error banner. *(`OQ-04`, confirmed by the prototype.)*
- Given a request that is not a `Draft`, when a submit is attempted, then `VF-REQ-005` is returned.
- Given another employee's request, when a submit is attempted, then `VF-REQ-004` is returned.
- Given a submitted request, when an edit is attempted, then it is rejected.

### `US-019` — Cancel a request
**Must** · `S` · Depends on: `US-018`, `US-033` · **Screens:** `S-04`, `S-06`, `S-08` · **Traces:** `SC-06`, `RULE-04`

- Given my own request in `Draft` or `Submitted`, when I confirm cancellation, then it becomes `Cancelled` and the banner reads `Request cancelled.`
- Given a request in a final state, when cancellation is attempted, then `VF-REQ-005` is returned.
- Given another employee's request, when cancellation is attempted, then `VF-REQ-004` is returned.
- Given a `Submitted` request opened as detail, when `S-06` renders, then a `Cancel request` button appears pushed to the right of the action row.

### `US-033` — Cancel confirmation modal
**Must** · `S` · Depends on: `US-030` · **Screen:** `S-08` · **Traces:** `NFR-USA-009`

As an employee, I want to confirm before cancelling, so that I do not lose a request by a stray click.

- Given I press `Cancel` on a row or `Cancel request` on the detail, when it activates, then the `S-08` modal opens with the title and body of §3.5.
- Given the modal, when I press `Back`, click the overlay or press `Escape`, then it closes with no change.
- Given the modal, when I press `Yes, cancel`, then the cancellation executes and the modal closes.
- Given a click inside the modal panel, when it happens, then the modal does not close.

**Visual** — 400px panel; `Back` secondary and `Yes, cancel` in the danger palette, right-aligned.

---

## EP-07 · Manager decision

### `US-020` — Manager queue
**Must** · `M` · Depends on: `TE-011` · **Screen:** `S-07` · **Traces:** `SC-09`, `RULE-06`, `AC-10`, `OS-22`

- Given a signed-in manager, when `GET /requests` is called, then I receive the `Submitted` requests of the employees assigned to me.
- Given a submitted request belonging to another manager's employee, when I list, then it is absent.
- Given a request in a final state, when I list as a manager, then it is absent from my queue.
- Given my own request, when my queue renders, then it is absent — a manager never sees their own request in the queue.
- Given a signed-in employee, when the same endpoint is called, then only their own requests are returned; the filter is decided server-side by role.

### `US-021` — Approve a request
**Must** · `L` · Depends on: `US-020`, `US-018`, `US-034` · **Screens:** `S-07`, `S-09` · **Traces:** `RULE-05`–`RULE-09`, `AC-11`, `AC-12`, `AC-14`

- Given a `Submitted` request from an employee assigned to me, when I approve it, then it becomes `Approved`, exactly one `Approval` record is created, and the banner reads `Request approved.`
- Given the created `Approval`, when inspected, then the responsible manager is the authenticated user, never a payload value.
- Given a request in any state other than `Submitted`, when I approve, then `VF-DEC-001`.
- Given a user without the Manager role, when they approve, then `VF-DEC-002`.
- Given a manager acting on a request they own, when they approve, then `VF-DEC-004`.
- Given a request from an employee not assigned to me, when I approve, then `VF-DEC-003`.
- Given an already-decided request, when I decide again, then `VF-DEC-005`.

### `US-022` — Reject a request with a comment
**Must** · `M` · Depends on: `US-021` · **Screens:** `S-07`, `S-09` · **Traces:** `RULE-08`, `AC-11`

- Given a `Submitted` request assigned to me, when I reject it with a comment, then it becomes `Rejected`, one `Approval` record carries the comment, and the banner reads `Request rejected.`
- Given a rejection with no comment, when submitted, then it succeeds — the comment is optional.
- Given a rejection, when the record is inspected, then it is structurally identical to an approval except for decision and comment.
- All authorization criteria of `US-021` apply identically.

### `US-023` — Approval Queue screen
**Must** · `M` · Depends on: `US-020`, `US-021`, `US-022` · **Screen:** `S-07`

**Visual — `S-07`**
- Page title `Approval Queue`, 24px/600, 24px below.
- One card per request per §3.3: employee name, then `{type} · {start} → {end}` in 13px secondary; `Reject` (outlined, danger) and `Approve` (solid, success) on the right, in that order.
- A top-bordered block below shows the full reason at 14px.
- Cards ordered most recent first; the action group does not shrink on narrow viewports.
- Given a completed decision, when it returns, then the list reloads from the API and the request leaves the queue.
- Given a failed decision, when the error returns, then it appears in an error banner and the request stays in the queue.

### `US-034` — Decision modal
**Must** · `S` · Depends on: `US-030` · **Screen:** `S-09` · **Traces:** `AC-11`, `FR-DEC-008`

As a manager, I want to add an optional comment before deciding, so that the employee understands the outcome.

- Given I press `Approve` or `Reject`, when it activates, then the `S-09` modal opens with the matching title from §3.5.
- Given the modal, when it renders, then it contains a labelled `Comment (optional)` textarea, 3 rows, `maxlength=500`.
- Given the approve modal, when it renders, then the confirm button reads `Approve` in the success palette; for reject it reads `Reject` in the danger palette.
- Given `Cancel`, an overlay click or `Escape`, when triggered, then the modal closes with no decision recorded.
- Given the modal is reopened, when it renders, then the comment field is empty rather than retaining the previous text.

**Visual** — 420px panel, actions right-aligned.

---

## EP-08 · Visibility and results

### `US-024` — My Requests screen
**Must** · `M` · Depends on: `US-020`, `US-018`, `US-019` · **Screen:** `S-04` · **Traces:** `SC-01`, `RULE-04`

**Behavior**
- Given my request list, when it renders, then it shows only my own requests, most recent first.
- Given another employee's request, when my list renders, then it never appears.

**Visual — `S-04`**
- Title row: `My Requests` at 24px/600 on the left, primary `New request` on the right, 24px below.
- One row card per request per §3.3: absence type name over `{start} → {end}`, then the state badge of §3.4, then the action buttons.
- Actions strictly by state:

| State | Buttons, in order |
|---|---|
| `Draft` | `Edit` · `Submit` · `Cancel` |
| `Submitted` | `View` · `Cancel` |
| `Approved` · `Rejected` · `Cancelled` | `View` |

- `Submit` is the primary style; `Edit` and `View` are outlined; `Cancel` is outlined in the danger palette.
- No action that would be rejected for the current state is rendered — an affordance only; the API rejects it regardless.

### `US-025` — See the final decision
**Must** · `S` · Depends on: `US-021`, `US-022`, `US-024` · **Screen:** `S-06` · **Traces:** `AC-13`

As an employee, I want to see the outcome of my request and who decided it, so that the decision is unambiguous and attributable.

- Given a decided request, when I press `View`, then `S-06` opens read-only with the request data.
- Given an `Approved` or `Rejected` request, when `S-06` renders, then a decision block appears below the fields.
- Given the decision block, when it renders, then it shows a 12px uppercase heading `DECISION`, then the decision as `Approved` or `Rejected` at 15px/600, then `By {manager name} · {date and time}` at 14px.
- Given a decision comment, when present, then it renders in a tinted block with 8px radius below the attribution line.
- Given a decided request, when `S-06` renders, then no state-changing action is offered.
- Given a `Submitted` request, when `S-06` renders, then no decision block appears and `Cancel request` is available.

---

## EP-09 · Delivery artifacts

### `US-026` — README
**Must** · `M` · Depends on: `TE-002`, `TE-003` · **Traces:** `TC-09`, `TC-11`

Covers prerequisites, starting the API, starting the web application, the SQLite file location, the reset procedure, the seeded accounts of §3.6, the endpoint summary, scope limitations and the deferred backlog. A reviewer following it reaches the full workflow unaided.

### `US-027` — Unit tests for rules and transitions
**Should** · `M` · Depends on: `US-021` · **Traces:** `SC-16`, `TC-16`, `RK-05`

- `RULE-01` and `RULE-02` covered including boundaries: start equal to today (valid); start equal to end (valid); end one day before start (invalid).
- Every valid transition passes and every invalid transition is rejected.
- Domain tests require no database, no network and no IO mocks.

### `US-028` — Functional HTML prototype in English
**Must** · `M` · Depends on: `US-017`, `US-023`, `US-024`, `US-025` · **Traces:** §12 deliverable 2

The prototype exists and is the design source for this backlog, but its copy is Spanish and the product ships in English (§2).

- Given the delivered prototype, when opened, then every string matches the §3.5 catalog.
- Given the delivered prototype, when opened, then layout, spacing, palette and interactions are unchanged from the current version.
- Given the ZIP, when extracted, then it opens in a browser with no server and no build step.
- Given the screenshots in `docs/prototype/`, when the English version is cut, then they are regenerated so the documentation matches what ships.

### `US-029` — Source package and demo video
**Must** · `M` · Depends on: all acceptance stories · **Traces:** §12 deliverables 3 and 4, `TC-18`

- The source ZIP contains no `node_modules`, `.next`, `bin` or `obj`, no database file and no real credentials.
- The video demonstrates `AC-01`–`AC-14` in sequence.

---

## 5. Suggested delivery sequence

| Increment | Stories | Outcome |
|---|---|---|
| **1 — Skeleton** | `TE-001`–`TE-005` | The API starts, migrates and seeds |
| **2 — Identity** | `US-007`–`US-010`, `TE-011`, `US-012`, `US-013` | A real user registers, signs in and is recognized server-side |
| **3 — Shell** | `US-030`, `US-031`, `US-036` | The signed-in frame, feedback and accessibility baseline exist |
| **4 — Employee flow** | `US-014`–`US-019`, `US-024`, `US-032`, `US-033` | `AC-01`–`AC-08` demonstrable |
| **5 — Decision flow** | `US-020`–`US-023`, `US-025`, `US-034`, `US-035` | `AC-09`–`AC-14` demonstrable |
| **6 — Hardening and handover** | `TE-006`, `US-026`–`US-029` | Tests, documentation, prototype, package and video |

Increment 3 is new relative to v1.0. Building the shell before the feature screens means `US-024`, `US-017` and `US-023` each drop into an existing frame instead of inventing their own — and the banner, modal and skeleton patterns get written once.

---

# Part B — Product backlog beyond the MVP

Not estimated and not scheduled. Recorded so the deferred boundary stays explicit. Each traces to a deferral in `Intent.md` §6.

## Identity and access

| ID | Story | Traces |
|---|---|---|
| `FUT-01` | Sign in with a corporate account | `OS-01` |
| `FUT-02` | Multifactor authentication | `OS-02` |
| `FUT-03` | Reset a forgotten password | `OS-03` |
| `FUT-04` | Confirm registration by email | `OS-04` |
| `FUT-05` | Manage accounts and roles | `OS-05` |
| `FUT-06` | Assign and reassign each employee's manager | `OS-05`, `OQ-01` |
| `FUT-30` | Remove the test-credentials block from `S-01` for any non-review build | `LC-04` |

## Hosting, delivery and operations

| ID | Story | Traces |
|---|---|---|
| `FUT-07` | Host VacaFlow in Azure | `OS-06` |
| `FUT-08` | Containerized build | `OS-07` |
| `FUT-09` | CI/CD with merge-blocking architecture tests | `OS-08`, `OQ-03` |
| `FUT-10` | Automated backups and a restore procedure | `OS-09` |
| `FUT-11` | Availability and monitoring | `OS-10` |
| `FUT-12` | Migrate existing request history | `OS-11` |
| `FUT-13` | Move off SQLite to a server database | `OS-06`, `LC-08` |

## Functional

| ID | Story | Traces |
|---|---|---|
| `FUT-14` | Notify a manager when a request needs a decision | `OS-12` |
| `FUT-15` | Notify an employee of the decision | `OS-12` |
| `FUT-16` | Show the remaining vacation balance | `OS-13` |
| `FUT-17` | Account for holidays and working days | `OS-14` |
| `FUT-18` | Flag overlapping requests | `OS-15` |
| `FUT-19` | Attach supporting documents | `OS-16` |
| `FUT-20` | HR view of all absences | `OS-17` |
| `FUT-21` | Reports and exports | `OS-18` |
| `FUT-22` | Multi-level approvals | `OS-19` |
| `FUT-23` | Delegate approvals while away | `OS-20` |
| `FUT-24` | Return a request for correction | `OS-21` |
| `FUT-25` | Manager history of decided requests | `OS-22` |
| `FUT-26` | Maintain absence types | `OS-23` |
| `FUT-27` | Integrate with payroll, HR, calendar and directory | `OS-24` |
| `FUT-28` | Full audit trail of every state change | `OS-25` |

## Interface and compliance

| ID | Story | Traces |
|---|---|---|
| `FUT-29` | Privacy notice and retention policy | `LC-05`, `LC-06`, `LC-08` |
| `FUT-31` | Mobile-optimized layout below 768px | `NFR-USA-010` |
| `FUT-32` | Interface localization — Spanish alongside English | `AS-05`, §2 |
| `FUT-33` | Full WCAG 2.2 AA conformance and audit | `NFR-USA` non-requirement |

---

## 6. Open questions — resolved by the prototype

The prototype implements a specific answer to four of the five open questions. A prototype is a design proposal, not a sponsor decision, so each is marked **resolved pending confirmation** rather than closed.

| ID | Question | What the prototype does | Status |
|---|---|---|---|
| `OQ-01` | How is `ManagerId` set for a self-registered employee? | A new **Employee** is assigned to the *first manager found in the table*. A new **Manager** is created with `ManagerId = null` | ⚠️ **Needs a real decision** — see below |
| `OQ-02` | Is role selection allowed at registration? | Yes, via radio buttons explicitly labelled as being for demo purposes | ✅ Resolved pending confirmation |
| `OQ-04` | Is `RULE-02` re-evaluated at submit? | Yes — the submit path re-checks the start date and refuses | ✅ Resolved pending confirmation — matches the assumption in `FR-LFC-003` |
| `OQ-05` | Is `RULE-06` the stricter reading? | Yes — the queue filters on the owner's assigned manager and excludes the manager's own requests | ✅ Resolved pending confirmation — matches `FR-DEC-003` |

> **`OQ-01` is only half-answered.** "The first manager found" is a demo convenience, not a business rule: with two managers seeded it silently assigns everyone to whichever row the database returns first. It works for the acceptance demo and it is wrong as a rule. The options remain what they were — a designated default manager, selection at registration, or an assignment screen (`FUT-06`) — and `US-021` should not be considered Ready until the sponsor picks one. The prototype's behavior is acceptable as the *documented MVP fallback* if the sponsor says so explicitly.
>
> `OQ-03` (architecture tests without a pipeline) is unaffected by the prototype and remains open.

---

## 7. What the prototype adds, and where it diverges

Recorded so the delta is auditable rather than absorbed silently.

**Added by the prototype**

| # | Element | Story |
|---|---|---|
| 1 | Application shell with a two-tab navigation | `US-030` |
| 2 | Dismissible success and error banner | `US-031` |
| 3 | Loading skeletons and dashed-border empty states | `US-032` |
| 4 | Cancel confirmation modal | `US-033` |
| 5 | Decision modal with an optional comment | `US-034` |
| 6 | Pending count on the manager tab | `US-035` |
| 7 | Skip link, focus ring, `Escape` to close | `US-036` |
| 8 | Live `N/500` counter on the reason field | `US-017` |
| 9 | `min` constraints on the date inputs | `US-017` |
| 10 | Test-credentials block on the sign-in screen | `US-008`, `FUT-30` |
| 11 | Three seeded employees rather than one manager | `TE-003` |

**Where the implementation must diverge from the prototype**

| # | Prototype | Implementation | Reason |
|---|---|---|---|
| 1 | Spanish interface copy throughout | English copy per §3.5 | Product language decision (§2) |
| 2 | State badges read `Borrador`, `Enviada`, `Aprobada`, `Rechazada`, `Cancelada` | `Draft`, `Submitted`, `Approved`, `Rejected`, `Cancelled` | Same |
| 3 | Absence types read `Vacaciones`, `Permiso personal`, `Incapacidad médica` | `Vacation`, `Personal Leave`, `Sick Leave` | Same, and already the names in `Intent.md` §7.1 |
| 4 | Passwords hashed with a reversible `btoa` placeholder | PBKDF2 per `SAD.md` `ADR-010` | The prototype is a mock; `LC-02` is binding |
| 5 | State persisted in browser `localStorage` | SQLite through EF Core | The prototype has no backend |
| 6 | Rules evaluated in client JavaScript | Rules in the domain and application layers | `RK-05` — the prototype is not the enforcement model |

Rows 4 to 6 are inherent to a prototype and are listed only so nobody mistakes the mock for a reference implementation.

---

## 8. Traceability

| Epic | Stories | Acceptance criteria |
|---|---|---|
| `EP-01` | `TE-001`–`TE-006` | — *(enabling)* |
| `EP-02` | `US-007`–`US-013`, `TE-011` | `AC-01`, `AC-02`, `AC-09`, `AC-14` |
| `EP-03` | `US-030`–`US-032`, `US-035`, `US-036` | supports all |
| `EP-04` | `US-014` | `AC-03` |
| `EP-05` | `US-015`–`US-017` | `AC-03`–`AC-06` |
| `EP-06` | `US-018`, `US-019`, `US-033` | `AC-07`, `AC-08` |
| `EP-07` | `US-020`–`US-023`, `US-034` | `AC-10`–`AC-12`, `AC-14` |
| `EP-08` | `US-024`, `US-025` | `AC-13` |
| `EP-09` | `US-026`–`US-029` | demonstration of all |

---

## 9. Impact on the other documents

This version introduces facts the rest of the set does not yet carry. None is contradictory; all are additive.

| Document | Required change |
|---|---|
| `FRD.md` §7 | **No change.** The English error catalog is correct as written and is reproduced in §3.5 |
| `FRD.md` §8 | Add the shell, banner, skeleton, empty-state and modal requirements now specified in `US-030`–`US-036` |
| `FRD.md` §12 | Mark `OQ-02`, `OQ-04` and `OQ-05` as resolved pending confirmation |
| `Intent.md` §15 | Same update to the open-question table |
| `Intent.md` §7.6 | Seed data grows from one manager to the three employees of §3.6 |
| `NFR.md` | Add the product language as a constraint; `NFR-USA-004`–`007` now have concrete screens to verify against |
| `SAD.md` §7 | The web structure gains a shell layout, a banner provider and two modal components |
| `WBS.md` §3 | Packages 4.5, 5.7 and 6.5 absorb most of `US-030`–`US-036`; package 8.2 grows to cover re-cutting the prototype in English. Net addition roughly **+2 person-days**, moving the remaining total from 34.25 to about 36.25 |

None of these blocks implementation. They are a consistency pass to run before the next document is treated as authoritative.
