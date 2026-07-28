# VacaFlow — Work Breakdown Structure

| Field | Value |
|---|---|
| Project | VacaFlow |
| Company | BIG Solutions |
| Document | `WBS.md` (Deliverable 1 of 6 — Project Documentation) |
| Version | 1.0 |
| Date | 2026-07-28 |
| Baseline | [`Intent.md`](Intent.md) · [`FRD.md`](FRD.md) · [`NFR.md`](NFR.md) · [`SAD.md`](SAD.md) · [`Backlog.md`](Backlog.md) |

> **Purpose.** Decompose the MVP into work packages small enough to estimate, assign and track, each producing a verifiable output and tracing to the backlog.
>
> **Scope boundary.** This WBS covers **the MVP only**. The post-MVP stories in `Backlog.md` Part B are not decomposed here — decomposing deferred work would imply it is scheduled, and it is not.

---

## 1. Conventions

### 1.1 Structure

Three levels. Level 1 is a major deliverable, level 2 a work package, level 3 an activity where the package warrants it. Every level-2 package states its output, its effort, its dependencies and the stories it satisfies.

### 1.2 Estimating basis

- The unit is the **person-day** of effort, not elapsed duration.
- One developer working roughly six productive hours per day.
- Estimates assume the developer knows .NET, EF Core and Next.js, and does not need to learn them here.
- Effort includes writing the code, making it work, and the developer's own testing. It does not include review latency or the sponsor's availability.
- The 100 % rule applies: the sum of the children equals the parent. No work exists outside this structure.

### 1.3 Status

`✅ Done` · `🔵 Ready` (dependencies met, no open question) · `🟡 Blocked` (waiting on a decision)

---

## 2. WBS overview

```
1.0  VacaFlow MVP
├── 1.1  Project management                                   2.00 d
├── 2.0  Project documentation                                4.75 d   ✅
├── 3.0  Foundations                                          6.25 d
├── 4.0  Authentication and identity                          5.50 d
├── 5.0  Request lifecycle                                    6.50 d
├── 6.0  Manager decision                                     4.75 d   🟡
├── 7.0  Quality and verification                             5.50 d
└── 8.0  Delivery and handover                                3.75 d
                                                       ─────────────
                                              Total          39.00 d
                                              Remaining      34.25 d
```

---

## 3. WBS dictionary

### 1.1 — Project management · 2.00 d

| ID | Package | Output | Effort | Status |
|---|---|---|---|---|
| 1.1.1 | Scope control | Change requests assessed against `Intent.md` §5/§6 | 0.75 | 🔵 |
| 1.1.2 | Open question resolution | `OQ-01`–`OQ-05` answered and documents updated | 0.50 | 🔵 |
| 1.1.3 | Progress reporting | Status against the milestones in §5 | 0.50 | 🔵 |
| 1.1.4 | Acceptance coordination | Demo scheduled; reviewers briefed | 0.25 | 🔵 |

*Mitigates `RK-01` (scope creep), the sponsor's stated primary risk.*

---

### 2.0 — Project documentation · 4.75 d · ✅ Complete

| ID | Package | Output | Effort | Status |
|---|---|---|---|---|
| 2.1 | Intent | `docs/Intent.md` | 1.00 | ✅ |
| 2.2 | Backlog | `docs/Backlog.md` | 1.00 | ✅ |
| 2.3 | Functional requirements | `docs/FRD.md` | 1.00 | ✅ |
| 2.4 | Non-functional requirements | `docs/NFR.md` | 0.75 | ✅ |
| 2.5 | Software architecture | `docs/SAD.md` | 0.75 | ✅ |
| 2.6 | Work breakdown structure | `docs/WBS.md` | 0.25 | ✅ |

*Satisfies deliverable 1 of `Intent.md` §12.*

---

### 3.0 — Foundations · 6.25 d

| ID | Package | Output | Effort | Depends on | Stories |
|---|---|---|---|---|---|
| 3.1 | Solution skeleton | Five projects, three test projects, references wired inward | 1.00 | — | `TE-001` |
| 3.2 | Domain model | Aggregates, value objects, typed ids, `Result`, error catalog | 1.50 | 3.1 | `TE-001` |
| 3.3 | Persistence | `DbContext`, configurations, repositories, `IUnitOfWork`, initial migration | 1.50 | 3.2 | `TE-002` |
| 3.4 | Seed data | Idempotent seeder: three absence types, one manager account | 0.50 | 3.3 | `TE-003` |
| 3.5 | Error handling | `ToHttpResult()`, exception handler, `{ code, message, field? }` shape | 0.50 | 3.1 | `TE-005` |
| 3.6 | Time provider | `TimeProvider` registered and threaded into domain calls | 0.25 | 3.2 | `TE-004` |
| 3.7 | Architecture tests | Eight assertions from `SAD.md` §10 | 1.00 | 3.1 | `TE-006` |

**3.2 detail** — `Employee`, `Request`, `Approval`, `AbsenceType`; `Email` and `DateRange` value objects; `EmployeeId`, `RequestId`, `AbsenceTypeId`, `ApprovalId`; `Result`/`Error`; `RequestErrors` and `ApprovalErrors` populated from the FRD §7 catalog.

**Exit criterion.** `dotnet run` creates the database, seeds it and serves a health response; architecture tests pass.

---

### 4.0 — Authentication and identity · 5.50 d

| ID | Package | Output | Effort | Depends on | Stories |
|---|---|---|---|---|---|
| 4.1 | Password hashing | PBKDF2 hasher per `ADR-010`, parameters encoded in the stored value | 0.50 | 3.1 | `US-007` |
| 4.2 | Registration | `RegisterEmployeeHandler`, `POST /auth/register`, uniqueness and validation | 1.00 | 3.3, 4.1 | `US-007` |
| 4.3 | Login, logout, current user | Cookie authentication, three endpoints | 1.00 | 4.2 | `US-008`–`US-010` |
| 4.4 | Current user accessor | `ICurrentUser` reading claims; no identity accepted from any payload | 0.50 | 4.3 | `TE-011` |
| 4.5 | Web foundation | Next.js app, `/api` proxy, fetch client, session handling, redirect on `401` | 1.00 | 4.3 | `US-012`, `US-013` |
| 4.6 | Register screen | Labelled form, field-level errors | 0.75 | 4.5 | `US-012` |
| 4.7 | Login screen | Form, error handling, current user displayed on every screen | 0.75 | 4.5 | `US-013` |

**Exit criterion.** `AC-01`, `AC-02` and `AC-09` demonstrable. A payload carrying a foreign `employeeId` has no effect.

> **4.4 is the risk-carrying package of the whole project.** `RK-02` and `NFR-CMP-002` both resolve here: a demonstrated identity bypass is rejection of the delivery, not a defect. It gets a dedicated review before 5.0 begins.

---

### 5.0 — Request lifecycle · 6.50 d

| ID | Package | Output | Effort | Depends on | Stories |
|---|---|---|---|---|---|
| 5.1 | Absence type catalog | Repository, handler, `GET /absence-types` | 0.50 | 3.4 | `US-014` |
| 5.2 | Create draft | Handler, `POST /requests`, `RULE-01`, `RULE-02`, required fields | 1.00 | 4.4, 5.1 | `US-015` |
| 5.3 | Edit draft | Handler, `PUT /requests/{id}`, `RULE-03`, `RULE-04`, re-validation | 0.75 | 5.2 | `US-016` |
| 5.4 | Submit | Handler, endpoint, transition `T1`, date re-check | 0.50 | 5.3 | `US-018` |
| 5.5 | Cancel | Handler, endpoint, transitions `T2` and `T5` | 0.50 | 5.4 | `US-019` |
| 5.6 | Visible request listing | `ListVisibleRequestsHandler`, role-driven server-side filtering | 0.75 | 4.4 | `US-020`, `US-024` |
| 5.7 | My Requests screen | List with per-state actions, four interface states | 1.25 | 4.5, 5.6 | `US-024` |
| 5.8 | Request form screen | Create and edit, read-only when not Draft, values preserved on error | 1.25 | 5.7 | `US-017` |

**Exit criterion.** `AC-03` through `AC-08` demonstrable end to end.

---

### 6.0 — Manager decision · 4.75 d · 🟡 Blocked on `OQ-01`

| ID | Package | Output | Effort | Depends on | Stories |
|---|---|---|---|---|---|
| 6.1 | Approval policy | `ApprovalPolicy` domain service: `RULE-06`, `RULE-07`, null-assignment branch | 0.50 | 3.2 | `US-021` |
| 6.2 | Decision handler | `DecideRequestHandler`, `Request.Decide`, one transaction | 1.00 | 6.1, 5.4 | `US-021`, `US-022` |
| 6.3 | Decision endpoints | `POST /{id}/approve`, `POST /{id}/reject`, comment-only contract | 0.50 | 6.2 | `US-021`, `US-022` |
| 6.4 | Manager queue query | Assigned employees' Submitted requests, no history | 0.50 | 5.6, 6.1 | `US-020` |
| 6.5 | Manager queue screen | List, decision dialog with optional comment, reload after decision | 1.50 | 5.7, 6.3 | `US-023` |
| 6.6 | Decision detail | Employee's view of final state, responsible manager, date, comment | 0.75 | 6.5 | `US-025` |

**Exit criterion.** `AC-10` through `AC-14` demonstrable, including all six unauthorized-operation cases.

> **6.1 and 6.2 cannot start until `OQ-01` is answered.** They sit 12.75 days into the remaining plan, so the decision is needed well before then — see §6. The rest of 6.0 is unaffected.

---

### 7.0 — Quality and verification · 5.50 d

| ID | Package | Output | Effort | Depends on | Stories |
|---|---|---|---|---|---|
| 7.1 | Domain unit tests | `RULE-01`–`RULE-09` plus the full transition matrix, valid and invalid | 1.50 | 6.2 | `US-027` |
| 7.2 | Application tests | Authorization paths and the identity-derivation assertions of `NFR-SEC-003` | 1.00 | 6.2 | `US-027` |
| 7.3 | NFR verification pass | The inspection and measurement rows of `NFR.md` §13, evidenced | 1.00 | 6.6 | — |
| 7.4 | Defect resolution | Fixes from review, blocking defects before cosmetic ones | 2.00 | 7.3 | `BC-06` |

**7.1 boundary cases that must be covered:** start date equal to today (valid); start equal to end (valid); end one day before start (invalid); a Draft whose start date has passed being submitted (invalid, `FR-LFC-003`).

**Exit criterion.** Every rule maps to at least one test; architecture tests pass; `NFR.md` §13 has evidence for each row.

---

### 8.0 — Delivery and handover · 3.75 d

| ID | Package | Output | Effort | Depends on | Stories |
|---|---|---|---|---|---|
| 8.1 | README | Prerequisites, run, SQLite location, reset, seeded accounts, endpoints, limitations, deferred backlog | 0.75 | 7.3 | `US-026` |
| 8.2 | HTML prototype | Five screens, static, opens without a server, delivered as a ZIP | 1.50 | 6.6 | `US-028` |
| 8.3 | Source package | ZIP without `node_modules`, `.next`, `bin`, `obj`, database file or real credentials | 0.25 | 8.1 | `US-029` |
| 8.4 | Demo video | `AC-01`–`AC-14` demonstrated in sequence | 0.75 | 8.3 | `US-029` |
| 8.5 | Acceptance session | Sponsor plus one manager and one employee run the workflow | 0.50 | 8.4 | `BC-07` |

**Exit criterion.** All five artifacts of `Intent.md` §12 delivered; sponsor sign-off.

---

## 4. Effort roll-up

| Level 1 | Effort (d) | Remaining (d) | Share of remaining |
|---|---:|---:|---:|
| 1.1 Project management | 2.00 | 2.00 | 6 % |
| 2.0 Documentation | 4.75 | 0.00 | — |
| 3.0 Foundations | 6.25 | 6.25 | 18 % |
| 4.0 Authentication and identity | 5.50 | 5.50 | 16 % |
| 5.0 Request lifecycle | 6.50 | 6.50 | 19 % |
| 6.0 Manager decision | 4.75 | 4.75 | 14 % |
| 7.0 Quality and verification | 5.50 | 5.50 | 16 % |
| 8.0 Delivery and handover | 3.75 | 3.75 | 11 % |
| **Total** | **39.00** | **34.25** | **100 %** |

**Contingency.** 15 % on remaining work ≈ **5.15 d**, giving a planning figure of **≈ 39.5 d** remaining. The contingency covers estimate error, not scope growth — new scope is a change request, not a draw on contingency.

**Elapsed duration.** At one developer and 34.25 days of remaining effort, roughly **7–8 calendar weeks** including the contingency and normal interruptions. A second developer could take 5.0 and 6.0 in parallel after 4.0 completes, bringing it to roughly 5 weeks — but the two would contend on the shared web foundation, so the saving is less than proportional.

---

## 5. Milestones

| # | Milestone | Reached when | Cumulative effort |
|---|---|---|---:|
| `M0` | Documentation baseline | All six documents published | 4.75 d ✅ |
| `M1` | Foundations ready | API starts, migrates, seeds; architecture tests pass | 11.00 d |
| `M2` | Identity working | `AC-01`, `AC-02`, `AC-09`; identity cannot be supplied by the client | 16.50 d |
| `M3` | Employee flow complete | `AC-03`–`AC-08` demonstrable | 23.00 d |
| `M4` | Decision flow complete | `AC-10`–`AC-14` demonstrable | 27.75 d |
| `M5` | Quality gate | Rules tested, NFR evidence collected, blocking defects closed | 33.25 d |
| `M6` | Handover | Five artifacts delivered, sponsor sign-off | 39.00 d |

`M2` is the gate that matters most. If identity derivation is not airtight there, everything built on top of it inherits the defect, and `NFR-CMP-002` makes it fatal rather than fixable-later.

---

## 6. Decisions needed, and by when

| Question | Blocks | Needed before | Cumulative day |
|---|---|---|---:|
| `OQ-02` role selection at registration; seeded manager credentials | 3.4, 4.2 | Foundations seeding | ~day 9 |
| `OQ-04` is `RULE-02` re-checked at submit | 5.4 | Submit implementation | ~day 21 |
| **`OQ-01` how `ManagerId` is set** | **6.1, 6.2** | **Approval policy** | **~day 24** |
| `OQ-05` confirm the stricter `RULE-06` | 6.1 | Approval policy | ~day 24 |
| `OQ-03` confirm the `CA-TST-001` deviation | 3.7 | Architecture tests | ~day 11 |

`OQ-01` has the latest deadline but the largest consequence: it is the only one that can change a contract (registration) and a data flow, rather than a single branch. Raising it early costs nothing; raising it on day 24 costs rework in 4.2 as well as 6.1.

---

## 7. Dependency network

```
3.1 ──┬── 3.2 ── 3.3 ── 3.4 ─────────────── 5.1
      │    │      │
      ├── 3.5     └── 3.6                    │
      └── 3.7                                │
                                             │
4.1 ── 4.2 ── 4.3 ──┬── 4.4 ──┬── 5.2 ── 5.3 ── 5.4 ── 5.5
                    │         │
                    └── 4.5 ──┼── 4.6
                              ├── 4.7
                              └── 5.6 ── 5.7 ── 5.8
                                          │
                    3.2 ── 6.1 ── 6.2 ── 6.3 ── 6.5 ── 6.6
                                   │              │
                                   └── 6.4 ───────┘
                                                  │
                                   7.1 ── 7.2 ── 7.3 ── 7.4
                                                        │
                                          8.1 ── 8.3 ── 8.4 ── 8.5
                                          8.2 ──────────┘
```

**Critical path:** `3.1 → 3.2 → 3.3 → 4.2 → 4.3 → 4.4 → 5.2 → 5.3 → 5.4 → 6.2 → 6.3 → 6.5 → 6.6 → 7.3 → 7.4 → 8.1 → 8.3 → 8.4 → 8.5`

Everything on that path delays the delivery day for day. The packages with float are 3.7 (architecture tests), 8.2 (HTML prototype, which needs only the screens to exist) and parts of 5.0 that the UI does not gate.

---

## 8. Resource and estimating assumptions

| # | Assumption | If it proves false |
|---|---|---|
| A1 | One full-time developer, familiar with the stack | Add learning time; the estimate assumes none |
| A2 | The sponsor answers open questions within two working days | `OQ-01` becomes a schedule risk at 6.0 |
| A3 | The five screens use plain, unstyled-to-lightly-styled components | A design system or custom branding is new scope (`Intent.md` §10 excludes it) |
| A4 | Reviewers supply their own environment per `NFR-POR-002` | Add environment support time |
| A5 | The HTML prototype reuses the built screens' markup | 8.2 grows from 1.5 d to roughly 3 d if authored separately |
| A6 | No production deployment activity of any kind | Hosting is `FUT-07`, an entirely separate scope |

---

## 9. Explicitly out of this WBS

No work package exists for, and no effort is reserved for: Azure deployment, Docker, CI/CD pipelines, notifications, password reset, account or role administration, vacation balances, holiday calendars, overlap validation, attachments, HR views, reports, exports, multi-level approval, delegation, or integrations. Each traces to a deferral in `Intent.md` §6 and, where it becomes real work later, to a story in `Backlog.md` Part B.

If any of these appears during execution, it is a change request assessed under 1.1.1 — not a task absorbed into an existing package.

---

## 10. Traceability

| WBS | Stories | Acceptance criteria |
|---|---|---|
| 3.0 | `TE-001`–`TE-006` | — *(enabling)* |
| 4.0 | `US-007`–`US-013`, `TE-011` | `AC-01`, `AC-02`, `AC-09`, `AC-14` |
| 5.0 | `US-014`–`US-019`, `US-024` | `AC-03`–`AC-08` |
| 6.0 | `US-020`–`US-023`, `US-025` | `AC-10`–`AC-14` |
| 7.0 | `US-027` | verification of all |
| 8.0 | `US-026`, `US-028`, `US-029` | demonstration of all |

Every story in `Backlog.md` Part A appears in exactly one work package. Every work package traces to at least one story. Neither list contains an orphan.
