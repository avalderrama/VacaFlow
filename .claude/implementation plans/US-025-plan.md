# Plan de implementación — `US-025` · See the final decision

| Campo | Valor |
|---|---|
| Historia | `US-025` — See the final decision |
| Épica | `EP-08` — Visibility and results |
| Prioridad · Talla | **Must** · `S` |
| Pantalla | `S-06` (Request detail — read-only with decision) — **esta historia es la dueña nominal** (`Backlog.md` §3.2, fila `S-06 → US-025`; screenshot `07-request-detail.png`) |
| Depende de | `US-021` (approve, PR #18) · `US-022` (reject, PR #19) · `US-024` (My Requests, PR #21) — **las tres en `main`** |
| Trazas | `AC-13` · `FR-VIS-004` (*"For a decided request, the owner sees the final state, the responsible manager's name, the decision date and the comment when present."*) · `FR-UIX-015` (*"Opening a decided request shows the final state, the responsible manager, the decision date and the comment when present, with no state-changing action available."*) · `US-019 AC4` (botón `Cancel request` en `S-06`, **diferido íntegramente a esta historia** por el plan `US-019` §4/§7) · `Backlog.md` §EP-08 `US-025`, §3.3 (*Detail decision block*), §3.5 · prototipo `VacaFlow.dc.html` líneas 262–279 (markup exacto del bloque y del botón `Cancelar solicitud`) |
| Fuentes | `Backlog.md` v2.0 · `FRD.md` · `SAD.md` v2.0 · código real verificado en **`main` (commit `48b4c1a` — `US-024` mergeada, PR #21)**, backend y `src/web/` archivo por archivo · planes `US-019` (`D2`, §4 — deuda de `AC4` reanotada aquí), `US-020` (`D8` — deuda del bloque `approval?` reanotada a "US-021/US-025"), `US-024` (`D2`, `OQ-A` resuelta en implementación: cancel directo sin confirmación) |
| Repositorio | `C:\Users\avald\OneDrive\Work\VacaFlow\repositories\vacaflow` |
| Rama sugerida | `feat/us-025-final-decision`, creada desde `main` (`48b4c1a`) — sin precondiciones pendientes |
| Estado | Borrador presentado para aprobación (sesión de planificación delegada; **dos preguntas abiertas en §7 — `OQ-A` alcance del bloque `approval?` en el listado, `OQ-B` propietario del bloque DECISION en el árbol de componentes**) |

> **Este documento no implementa nada.** La implementación la ejecuta `/user-story-implement`.

---

## 1. Entendimiento

### 1.1 Contexto — a diferencia de `US-023`/`US-024`, esta historia **sí toca backend**: el dato de la decisión aún no viaja

Verificado contra `main` (`48b4c1a`):

**Backend — lo que existe:**

- El agregado ya tiene todo el dato (`US-021`/`US-022`): `Request.Approval` (`Approval?`, propiedad en `Domain/Requests/Request.cs` línea 65) con `ResponsibleManagerId: EmployeeId`, `Decision: DecisionType`, `Comment: string?` (normalizado — blank → null), `DecidedAtUtc: DateTime`. Persistido como owned entity en tabla `Approvals` (`RequestConfiguration.OwnsOne`, FK `Restrict` a `Employees`) — **EF carga los owned automáticamente, así que `RequestRepository.GetByIdAsync` ya devuelve el `Approval` sin cambio alguno de Infrastructure**.
- `GET /api/requests/{id}` (`GetRequestByIdHandler` → `RequestDetailDto` → `RequestDetailResponse`) — **NO incluye el bloque de decisión**. Verificado: `RequestDetailResponse(Guid Id, Guid AbsenceTypeId, DateOnly StartDate, DateOnly EndDate, string Reason, string State)` y el DTO son espejo exacto, sin `Approval`. El handler solo inyecta `ICurrentUser` + `IRequestRepository`.
- `RequestSummaryDto`/`RequestSummaryResponse` (`GET /requests`, `US-020`) — **tampoco** llevan `approval?`. El diferimiento de `US-020 D8` es real y sigue vigente; el doc-comment de `RequestSummaryDto` lo dice verbatim: *"A row of the FRD.md §6.3 GET /requests response — never the Approval block, which the Approval aggregate does not exist to populate yet (US-021, US-020 plan D8)."* La deuda quedó reanotada a "**US-021/US-025**" (`US-020-plan.md` §4: *"añadir el bloque `approval?` al DTO/response cuando `Approval` exista (`D8`)"*). Ver **`OQ-A`**.
- `ResponsibleManagerId` es un `EmployeeId`, **no un nombre** — el criterio pide `By {manager name}`, así que el handler debe resolver el Employee. El puerto ya lo permite: `IEmployeeRepository.GetByIdAsync(EmployeeId, CancellationToken)` existe (usado por `US-021`); **el puerto no crece**. El precedente análogo es `ListVisibleRequestsHandler` (`US-020`), que inyecta `IEmployeeRepository` para enriquecer con `FullName` — misma forma de DI, aquí con lookup single en vez de batch.
- La forma del bloque la fija el FRD §6.3 (contrato de `GET /requests`, verbatim): `approval?: { responsibleManagerName, decision, comment, decidedAtUtc }` — **nombre plano del manager, no un bloque `employee` anidado**. `GET /requests/{id}` no aparece en la tabla §6.3 del FRD (es adición del repo, `US-016`/`US-017`), así que su bloque de decisión espeja esa misma forma.

**Web — lo que existe en `src/web/` (tras `US-024`):**

- `app/(app)/requests/[id]/page.tsx` (`US-017`) — para estados no-`Draft` renderiza "Request detail" con `RequestForm` en modo solo lectura; su propio comentario dice verbatim: *"the interim view until US-025 delivers S-06's DECISION block"*. **No hay bloque de decisión** (no puede: la API no envía el dato).
- `components/requests/RequestForm.tsx` — en modo no editable renderiza los campos deshabilitados y una fila de acciones con **solo el botón `Back`** (`btn-secondary`). **"No state-changing action is offered" ya es cierto hoy para requests decididos** — no hay ningún botón mutador que retirar. Lo que **falta** es el botón `Cancel request` para `Submitted` (criterio de `US-019 AC4`, diferido íntegro a esta historia — verificado: `grep cancel` en el componente no devuelve nada).
- `lib/types.ts` — `RequestDetail` sin campo de decisión (espejo del contrato actual). `lib/api.ts` — `getRequest` y `cancelRequest` ya existen; **nada que añadir en `api.ts`**.
- `lib/session.ts` — `setPendingNotification` (el formulario ya lo usa para aterrizar banners en `/requests` tras navegar).
- Prototipo (`VacaFlow.dc.html` líneas 262–279, autoritativo junto con §3.3): el bloque va **dentro de la card del formulario**, tras los campos y antes de la fila de acciones, con separador `border-top: 1px solid oklch(92% 0.006 260)`, `margin-top: 24px; padding-top: 20px`; heading 12px/600 uppercase `letter-spacing: .04em` color `oklch(55% 0.02 260)`; decisión 15px/600; atribución 14px color `oklch(40% 0.02 260)`; comentario `font-size: 14px; color: oklch(30% 0.02 260); background: oklch(97% 0.004 250); padding: 12px 14px; border-radius: 8px; margin-top: 10px`. El botón `Cancelar solicitud` va en la fila de acciones con `margin-left: auto` (pushed right), estilo outlined danger 11px/22px.

### 1.2 Narrativa (verbatim)

> "As an employee, I want to see the outcome of my request and who decided it, so that the decision is unambiguous and attributable."

### 1.3 Criterios de aceptación — verbatim (`Backlog.md` §EP-08 · `US-025`)

| # | Criterio |
|---|---|
| `AC1` | "Given a decided request, when I press `View`, then `S-06` opens read-only with the request data." |
| `AC2` | "Given an `Approved` or `Rejected` request, when `S-06` renders, then a decision block appears below the fields." |
| `AC3` | "Given the decision block, when it renders, then it shows a 12px uppercase heading `DECISION`, then the decision as `Approved` or `Rejected` at 15px/600, then `By {manager name} · {date and time}` at 14px." |
| `AC4` | "Given a decision comment, when present, then it renders in a tinted block with 8px radius below the attribution line." |
| `AC5` | "Given a decided request, when `S-06` renders, then no state-changing action is offered." |
| `AC6` | "Given a `Submitted` request, when `S-06` renders, then no decision block appears and `Cancel request` is available." |

Patrón §3.3, verbatim de `Backlog.md`:

> **Detail decision block** (`S-06`) — Heading `DECISION` (uppercase, letter-spaced) · decision as `Approved` or `Rejected` · attribution line `By {manager name} · {date and time}`.

Además esta historia **paga la deuda diferida** de `US-019 AC4` (verbatim, dueña nominal declarada en `US-019-plan.md` §4: *"Íntegramente diferido a US-025"*):

> "Given a `Submitted` request opened as detail, when `S-06` renders, then a `Cancel request` button appears pushed to the right of the action row."

### 1.4 Alcance

**Entra**: extensión aditiva de `RequestDetailDto` + `GetRequestByIdHandler` (lookup del manager vía `IEmployeeRepository` existente) + `RequestDetailResponse` + mapeo en `RequestEndpoints`; tests unitarios/funcionales del bloque; en web: campo `approval` en `RequestDetail` (`types.ts`), bloque DECISION y botón `Cancel request` en la vista de detalle (ver `OQ-B`), orquestación de cancel desde detalle (llamada directa + banner `Request cancelled.` vía `setPendingNotification` + navegación a `/requests`).

**No entra**

| Excluido | Por qué / destino |
|---|---|
| Bloque `approval?` en `GET /requests` (listado, `RequestSummaryDto`/`Response`) | Ningún criterio de `US-025` lo pide (todos hablan de `S-06`) y ninguna pantalla lo consume (`S-04` no muestra decisiones). Pero el contrato FRD §6.3 del listado sí lo nombra y `US-020 D8` reanotó la deuda a "US-021/US-025" — **ver `OQ-A`**: el plan asume detalle-solo salvo indicación contraria |
| Modal `S-08` de confirmación al pulsar `Cancel request` | **`US-033`** (dueña `S-08`; su criterio nombra verbatim *"`Cancel request` on the detail"*). Igual que la fila de `S-04` (`US-024`, implementado): llamada directa, el modal se insertará después — `D5` |
| Shell S-03, skeleton/empty states, matriz completa del banner | `US-030` / `US-032` / `US-031` — mismos diferimientos ratificados en `US-023`/`US-024` |
| Backend de cancel | Existe desde `US-019` (`POST /requests/{id}/cancel`, errores mapeados) — cero cambios |
| Cambios en `Domain` | `Request.Approval` ya expone todo lo que el bloque necesita — cero ítems de Domain |
| Vista del manager sobre la decisión / historial | Fuera de alcance del producto (`FRD` §8 exclusions: *manager decision history*) |

---

## 2. Cambios estructurales / de base

**No se requieren cambios de esquema, migraciones, configuración, variables de entorno, feature flags, dependencias nuevas (npm ni NuGet) ni cambios de seed.** La tabla `Approvals` existe desde `US-021` y EF ya la carga como owned entity.

**Cambio de contrato público — aditivo, no rompe**: `RequestDetailResponse` gana un campo opcional `approval` (`null` para requests sin decisión). Ningún consumidor existente (la página `[id]` es el único) se rompe por un campo extra; el espejo TypeScript se actualiza en la misma historia (ítem #6).

---

## 3. Plan ordenado por dependencia

**Sin ítems de Domain ni Infrastructure** (§1.1: el agregado ya expone `Approval` y EF ya lo carga). Application → API → Web, con sus tests.

| # | Capa | Acción | Artefacto | Notas |
|---|---|---|---|---|
| 1 | Application | Modificar | `src/BigSolutions.VacaFlow.Application/Requests/RequestDetailDto.cs` | Añadir `RequestApprovalDto? Approval` al record y, en el mismo archivo (precedente de cohesión: `RequestSummaryDto` agrupa sus records anidados), `public sealed record RequestApprovalDto(string ResponsibleManagerName, string Decision, string? Comment, DateTime DecidedAtUtc)` — forma plana fijada por el contrato FRD §6.3 del bloque `approval?` (`responsibleManagerName`, no un bloque anidado `{ id, fullName }` — ver `D2`). `Decision` viaja como nombre del enum (`"Approved"`/`"Rejected"`), mismo precedente que `State` |
| 2 | Application | Modificar | `src/BigSolutions.VacaFlow.Application/Requests/GetRequestByIdHandler.cs` | Inyectar `IEmployeeRepository` (tercer parámetro del primary constructor — **el puerto no crece**: `GetByIdAsync(EmployeeId, ct)` existe desde `US-021`; misma forma de DI que `ListVisibleRequestsHandler`). Tras el guard de owner: si `request.Approval is not null` → `await employees.GetByIdAsync(approval.ResponsibleManagerId, ct)` y construir `RequestApprovalDto(manager.FullName, approval.Decision.ToString(), approval.Comment, approval.DecidedAtUtc)`; si no, `Approval = null`. El manager siempre existe (FK `Restrict` en `Approvals.ResponsibleManagerId` — ver `D3`). Actualizar el doc-comment del handler (US-017 + US-025) |
| 3 | API | Modificar | `src/BigSolutions.VacaFlow.Api/Contracts/RequestDetailResponse.cs` | Añadir `RequestApprovalResponse? Approval` y `public sealed record RequestApprovalResponse(string ResponsibleManagerName, string Decision, string? Comment, DateTime DecidedAtUtc)` en el mismo archivo — espejo campo a campo del DTO, nunca del dominio (CA-APP-006, doc-comment existente del contrato) |
| 4 | API | Modificar | `src/BigSolutions.VacaFlow.Api/Endpoints/RequestEndpoints.cs` | `ToDetailResponse`: mapear `dto.Approval` → `RequestApprovalResponse` (null-propagando). Ningún endpoint nuevo, ninguna ruta tocada |
| 5 | Test | Modificar | `tests/BigSolutions.VacaFlow.Application.UnitTests/Requests/GetRequestByIdHandlerTests.cs` | Casos nuevos (los existentes se actualizan solo en el constructor del handler): decidida `Approved` con comentario → bloque completo (`FullName` del manager, `"Approved"`, comentario, `DecidedAtUtc`); decidida `Rejected` sin comentario → `Comment` null; `Submitted`/`Draft` → `Approval` null. Fakes existentes en `Requests/Fakes` (ya hay fake de `IEmployeeRepository` usado por `ListVisibleRequestsHandlerTests`/`DecideRequestHandlerTests` — reutilizar) |
| 6 | Test | Modificar | `tests/BigSolutions.VacaFlow.Api.FunctionalTests/Endpoints/RequestEndpointTests.cs` | `GET /requests/{id}`: sobre una request aprobada vía `POST /approve` real → `approval.responsibleManagerName` = nombre seed del manager, `decision` = `"Approved"`, `comment`, `decidedAtUtc` presentes; sobre rechazada sin comentario → `comment` null; sobre `Submitted` → `approval` null (regresión de la forma actual) |
| 7 | Web | Modificar | `src/web/lib/types.ts` | En `RequestDetail`: `approval: RequestDecision \| null`; nueva `export interface RequestDecision { responsibleManagerName: string; decision: 'Approved' \| 'Rejected'; comment: string \| null; decidedAtUtc: string; }` — espejo verificado del contrato del ítem #3 (regla del propio archivo) |
| 8 | Web | Modificar | `src/web/components/requests/RequestForm.tsx` | **(Sujeto a `OQ-B` — el plan asume la opción (a): extender `RequestForm`.)** Dos props opcionales en el modo `edit`: `decision?: RequestDecision \| null` y `onCancelRequest?: () => void`. (1) **Bloque DECISION** (`AC2`–`AC4`), renderizado tras los campos y antes de la fila de acciones **solo si `decision` viene** — valores del prototipo líneas 262–268: contenedor `marginTop: 24px; paddingTop: 20px; borderTop: '1px solid oklch(92% 0.006 260)'`; heading `DECISION` 12px/600 `textTransform: 'uppercase'; letterSpacing: '.04em'` color secundario; `decision.decision` 15px/600; `By {responsibleManagerName} · {fecha y hora formateada de decidedAtUtc}` 14px (`D4`); si `comment` → bloque tintado `background: oklch(97% 0.004 250); padding: 12px 14px; borderRadius: 8px; marginTop: 10px` 14px (`AC4` — 8px radius verbatim). (2) **Botón `Cancel request`** (`AC6` + `US-019 AC4`), renderizado en la fila de acciones **solo si `onCancelRequest` viene**, con `marginLeft: 'auto'` (pushed right, prototipo línea 277) y estilo outlined danger talla formulario (11px/22px — `background: white; border: '1px solid oklch(80% 0.1 25)'; color: 'oklch(45% 0.15 25)'`; no reutiliza `.btn-row-danger` de `US-024`, que es talla fila 8px/14px — ver `D6`), deshabilitado durante la mutación. En modo no editable la fila queda: `Back` (existente) + `Cancel request` condicional — **ningún otro botón: `AC5` queda garantizado por construcción** (hoy ya solo hay `Back` para decididas; esta historia no añade nada para ellas) |
| 9 | Web | Modificar | `src/web/app/(app)/requests/[id]/page.tsx` | Pasar `decision={detail.approval}` (solo llega poblado en `Approved`/`Rejected` — el server es la autoridad, `AC2`/`AC6` mitad datos) y `onCancelRequest` **solo cuando `detail.state === 'Submitted'`** (afordancia espejo de `Request.Cancel`; para `Draft` el flujo de cancelación vive en la fila de `S-04`, ver `D7`). `onCancelRequest`: flag en vuelo + `await cancelRequest(detail.id)` (directa, sin confirmación — `D5`, precedente implementado de `US-024`) → `setPendingNotification('Request cancelled.')` + `router.push('/requests')` (mismo aterrizaje de banner que usa el propio formulario al guardar); a `ApplicationError` → error en la página (patrón de error existente de la página) y re-fetch del detalle (el estado pudo cambiar en otra pestaña). Actualizar el comentario *"interim view until US-025"* — esta historia lo salda |
| 10 | Web | Verificar | `cd src/web && npm run lint && npm run typecheck && npm run depcruise && npm run build` | `depcruise` confirma que `fetch` sigue solo en `lib/api.ts`; sin test runner de frontend (`D8`) |
| 11 | Test | Verificar | `dotnet build VacaFlow.slnx` + `dotnet test VacaFlow.slnx` | Suites completas verdes: las cuatro de siempre + los casos nuevos de #5/#6; `RequestRepositoryTests` (integración) ya cubre la carga del owned `Approval` desde `US-021` — verificar que sigue verde, sin ítems nuevos |
| 12 | E2E | Verificar | Verificación manual §6 con la app corriendo (API + `npm run dev`) | Único punto donde `AC1`–`AC6` son demostrables juntos contra `07-request-detail.png` |

**Dependencias:** 1 → 2 → {3 → 4, 5} · 4 → 6 · 3 → 7 → 8 → 9 · todo → {10, 11, 12}. **Paralelizable:** {5, 6} con {7, 8, 9}. **Ruta crítica:** 1 → 2 → 3 → 4 → 7 → 8 → 9 → 12. `OQ-A` no bloquea ningún ítem (su "sí" añadiría ítems, no cambia estos); `OQ-B` decide la forma de #8/#9, no su contenido.

---

## 4. Casos de uso y tabla de trazabilidad

**Cero casos de uso nuevos en Application.** Esta historia **enriquece** un caso de uso existente (*obtener request por id*, `US-016`/`US-017`) con el bloque de decisión (`FR-VIS-004`), y añade la superficie `S-06` que consume ese caso de uso más *cancelar* (`US-019`).

| Historia | Criterio de aceptación (verbatim) | Ítems del plan que lo cubren | Cómo se verifica |
|---|---|---|---|
| `US-025` | "Given a decided request, when I press `View`, then `S-06` opens read-only with the request data." | #9 (la ruta y el modo solo-lectura existen desde `US-017`/`US-024` — regresión; esta historia solo la enriquece) | §6 paso 2; suites existentes de `US-017` |
| `US-025` | "Given an `Approved` or `Rejected` request, when `S-06` renders, then a decision block appears below the fields." | #1, #2, #3, #4 (el dato viaja), #7, #8, #9 (se renderiza) | Tests #5/#6 (mitad datos) + §6 pasos 2–3 (mitad visual) |
| `US-025` | "Given the decision block, when it renders, then it shows a 12px uppercase heading `DECISION`, then the decision as `Approved` or `Rejected` at 15px/600, then `By {manager name} · {date and time}` at 14px." | #2 (nombre del manager resuelto server-side), #8 (tipografía verbatim) | Test #5 (FullName correcto) + inspección visual §6 paso 2 contra `07-request-detail.png` |
| `US-025` | "Given a decision comment, when present, then it renders in a tinted block with 8px radius below the attribution line." | #1/#2 (comment viaja, null cuando no hay), #8 (bloque tintado 8px condicional) | Tests #5/#6 (comment presente/null) + §6 pasos 2 y 4 |
| `US-025` | "Given a decided request, when `S-06` renders, then no state-changing action is offered." | #8, #9 (`onCancelRequest` solo para `Submitted`; para decididas la fila queda solo con `Back` — ya cierto hoy, garantizado por construcción) | §6 paso 5; el backend además rechaza cualquier intento (`VF-REQ-005`, probado en `US-018`/`US-019`) |
| `US-025` | "Given a `Submitted` request, when `S-06` renders, then no decision block appears and `Cancel request` is available." | #2 (`approval` null para `Submitted` — test #5/#6), #8, #9 (botón condicional + cancel directo + banner + navegación) | Tests #5/#6 (approval null) + §6 pasos 6–7 |
| `US-019` (deuda) | "Given a `Submitted` request opened as detail, when `S-06` renders, then a `Cancel request` button appears pushed to the right of the action row." | #8 (`marginLeft: 'auto'` — pushed right), #9 (orquestación) | §6 paso 6 — cierra el diferimiento declarado en `US-019-plan.md` §4 |

**Conteo: 6 criterios de entrada de `US-025` · 6 cubiertos** (+ 1 criterio diferido de `US-019` que esta historia salda, trazado aparte).

---

## 5. Supuestos y decisiones

Sesión de planificación delegada (Fase 3 no interactiva): decisiones de arquitecto documentadas con su reversibilidad. **Las dos que merecen ratificación del usuario están elevadas a §7 (`OQ-A`, `OQ-B`).**

| # | Decisión | Justificación | Impacto si es incorrecta |
|---|---|---|---|
| `D1` | **Sin ítems de Domain ni Infrastructure; el lookup del manager reutiliza `IEmployeeRepository.GetByIdAsync` — el puerto no crece** | Verificado en `main`: `Request.Approval` expone los cuatro datos del bloque; EF carga el owned automáticamente (`RequestRepository.GetByIdAsync` sin cambios); el método del puerto existe desde `US-021`. Precedente de DI: `ListVisibleRequestsHandler` ya inyecta `IEmployeeRepository` para el mismo problema ("necesito el nombre, no el id") | N/A — hecho del código |
| `D2` | **El bloque viaja con `responsibleManagerName` plano, no con un `employee { id, fullName }` anidado** | Es la forma verbatim que el FRD §6.3 fija para `approval?` en el listado: `{ responsibleManagerName, decision, comment, decidedAtUtc }` — el detalle la espeja para que, si `OQ-A` se responde "sí", ambas superficies compartan forma. El id del manager no lo consume nadie y exponerlo sin necesidad roza `FR-ERR-001` en espíritu | Reversible antes de mergear; después sería breaking change del contrato |
| `D3` | **Si `Approval` existe, el Employee del manager se asume presente (sin rama defensiva de "manager no encontrado")** | La FK `Approvals.ResponsibleManagerId → Employees(Id)` con `DeleteBehavior.Restrict` (verificada en `RequestConfiguration`) hace la ausencia imposible por construcción; no hay borrado de empleados en el producto. Una rama de fallback inventaría un estado irrepresentable | Si la invariante se rompiera (corrupción manual de la BD), el handler lanzaría — un 500 honesto ante datos corruptos, no un error de negocio a catalogar |
| `D4` | **`{date and time}` de la atribución se formatea client-side desde `decidedAtUtc`** (timestamp ISO UTC del contrato) con `toLocaleString` del navegador (o formato fijo equivalente legible) | El backlog no fija formato ni zona; el prototipo muestra fecha y hora locales. La regla `S1` de `US-017` ("el cliente no parsea fechas") aplica a los `DateOnly` de los campos, no a un timestamp que debe mostrarse como fecha y hora — no hay forma de renderizar "date and time" sin formatear | Cosmético — cambiar el formato es una línea |
| `D5` | **`Cancel request` llama `cancelRequest(id)` directamente, sin confirmación** | Ratificación del precedente ya **implementado** en `US-024` (comentario verbatim en `requests/page.tsx`: *"Cancel calls cancelRequest(id) directly, no confirmation — the S-08 modal is US-033's job; it will insert itself"*). `US-033` interceptará ambos puntos (fila y detalle: su criterio nombra los dos) | Ninguno — `US-033` inserta el modal sin retocar la llamada |
| `D6` | **El botón `Cancel request` usa la talla de formulario (11px/22px, outlined danger inline), no `.btn-row-danger` (8px/14px)** | El prototipo (línea 277) lo dibuja con `padding: 11px 22px` — talla de la fila de acciones del formulario, igual que `Back`. `.btn-row-danger` es la talla compacta de fila de lista (`US-024`) — reutilizarla rompería la paridad visual con `07-request-detail.png` | Cosmético — cambiar la clase es una línea |
| `D7` | **`Cancel request` en `S-06` se ofrece solo para `Submitted`, no para `Draft`** | Es lo que piden verbatim `US-019 AC4` y `US-025 AC6` ("Given a `Submitted` request opened as detail…"); un `Draft` abre `S-05` (Edit draft), cuyo flujo de cancelación vive en la fila de `S-04` (`US-024`, matriz `Draft → Edit·Submit·Cancel`) — el prototipo confirma (`currentViewRequestCancellable` solo para la vista detalle de `Submitted`) | Si se quisiera también en `Draft`, es relajar la condición del ítem #9 — una línea |
| `D8` | **Sin tests automatizados de frontend; verificación web = lint + typecheck + depcruise + build + E2E manual** | Ratificación de `US-023 D7`/`US-024 D5`: sigue sin existir runner en `src/web/package.json`. La semántica de datos del bloque queda probada server-side (#5, #6) | Si el usuario quiere estrenar runner aquí, se añade como ítem previo — ampliación, no corrección |
| `S1` | **La rama se crea desde `main` (`48b4c1a`) directamente** | Verificado: `US-021`, `US-022` y `US-024` mergeadas; todo lo necesario está en `main` | Ninguno |

---

## 6. Verificación end-to-end

Con la API corriendo y `npm run dev` en `src/web/`, seed §3.6 (Laura manager; Carlos y Ana empleados asignados a ella).

| # | Paso | Resultado esperado |
|---|---|---|
| 1 | `dotnet build` + `dotnet test` · `cd src/web && npm run lint && npm run typecheck && npm run depcruise && npm run build` | Todo verde, 0 warnings, incluidos los casos nuevos de #5/#6 |
| 2 | Como Carlos: crear + submit; como Laura: `Approve` con comentario; como Carlos: `/requests` → `View` sobre la `Approved` — **`AC1`–`AC4`** | `S-06` solo lectura con los datos de la request; bajo los campos, separador y bloque: `DECISION` 12px uppercase, `Approved` 15px/600, `By Laura Méndez · {fecha y hora}` 14px, comentario en bloque tintado 8px — contra `07-request-detail.png` |
| 3 | Repetir con `Reject` con comentario → `View` — **`AC2`/`AC3`** | Mismo bloque con `Rejected`; el nombre y la fecha corresponden a la decisión real |
| 4 | Decidir una request **sin** comentario → `View` — **`AC4`** | Bloque con heading, decisión y atribución; **sin** bloque tintado |
| 5 | En las vistas de los pasos 2–4 — **`AC5`** | Fila de acciones con **solo `Back`** — ni `Save`, ni `Cancel request`, ni ningún mutador |
| 6 | `View` sobre una `Submitted` — **`AC6`** + `US-019 AC4` | Sin bloque DECISION; fila de acciones: `Back` a la izquierda y `Cancel request` outlined danger **pegado a la derecha** |
| 7 | Pulsar `Cancel request` — deuda `US-019` | Botón deshabilitado en vuelo; navega a `/requests` con banner `Request cancelled.`; la fila muestra badge `Cancelled` con solo `View` |
| 8 | Dos pestañas sobre la misma `Submitted`: cancelar en una, `Cancel request` en la otra — robustez | La segunda recibe `409` `VF-REQ-005` en el error de la página y el detalle se recarga mostrando `Cancelled` sin botón |
| 9 | `GET /api/requests/{id}` directo (curl/devtools) sobre decidida y sobre `Draft` | JSON con `approval: { responsibleManagerName, decision, comment, decidedAtUtc }` vs `approval: null` |
| 10 | Como Ana, `GET /api/requests/{id}` de la request de Carlos | `403` `VF-REQ-004` — el guard de owner intacto (regresión `US-017`; el bloque no abre visibilidad cruzada, `FR-VIS-002`) |

---

## 7. Riesgos y preguntas abiertas

> ⚠️ **`OQ-A` — Pregunta abierta para el usuario (no bloquea ítems; un "sí" añade ítems):**
> `US-020 D8` difirió el bloque `approval?` del **listado** (`GET /requests`, contrato FRD §6.3 verbatim) "a US-021/US-025". `US-021`/`US-022` no lo añadieron. Los criterios de `US-025` solo exigen el bloque en el **detalle** (`S-06`), y ninguna pantalla consume el bloque en el listado (`S-04` no muestra decisiones). ¿Qué hacemos con la deuda del listado?
>
> - **(a) — recomendada — Solo detalle (este plan tal cual).** El listado queda como deuda documentada del contrato FRD §6.3, sin dueño posterior en el backlog — quedaría anotada en el reporte de esta historia. Contra: el contrato FRD §6.3 del listado seguiría sin honrarse en su campo opcional.
> - **(b) También el listado.** Añadiría ~3 ítems: `RequestApprovalDto` compartido en `RequestSummaryDto`, lookup batch de managers en `ListVisibleRequestsHandler` (extender el `ListByIdsAsync` existente con los `ResponsibleManagerId`), espejo en `RequestSummaryResponse` + tests. Aditivo y barato, pero payload muerto que ninguna UI del backlog llegará a renderizar.
>
> **El plan asume (a) salvo indicación contraria.**

> ⚠️ **`OQ-B` — Pregunta abierta para el usuario (decide la forma de los ítems #8/#9, no su contenido):**
> ¿Quién es el dueño del bloque DECISION y del botón `Cancel request` en el árbol de componentes? `SAD.md` §9.2 no nombra ningún componente para el bloque (su lista de `components/requests/` es `RequestRow · RequestForm · StateBadge`), y el prototipo dibuja ambos **dentro de la card del formulario**.
>
> - **(a) — recomendada — Extender `RequestForm`** con dos props opcionales (`decision?`, `onCancelRequest?`), como asume el ítem #8. Fiel al prototipo (bloque y botón viven dentro de la misma card, el botón en la misma fila de acciones que `Back`) y al árbol cerrado del SAD. Contra: `RequestForm` crece (ya es el componente más grande de `src/web`).
> - **(b) Componente nuevo `components/requests/DecisionBlock.tsx`** renderizado por `RequestForm` (o por la página, fuera de la card — esto último rompería la paridad con el prototipo). Más limpio de leer, pero añade un componente fuera de la lista del SAD y el botón `Cancel request` seguiría necesitando la prop en `RequestForm` de todas formas.
>
> **El plan asume (a) salvo indicación contraria.** Elegir (b) reparte el ítem #8 en dos archivos sin cambiar nada más.

| Riesgo | Mitigación |
|---|---|
| Cambio de contrato en `GET /requests/{id}` | Aditivo (`approval` opcional/null); el único consumidor (`[id]/page.tsx`) se actualiza en la misma historia; tests funcionales #6 fijan la forma nueva y la regresión (`approval: null`) |
| El lookup del manager añade una query al detalle | Una sola `GetByIdAsync` por PK, solo cuando hay decisión — coste trivial; sin N+1 (es un detalle, no una lista) |
| `AC5` podría leerse como "retirar acciones existentes" | Verificado: hoy la vista decidida solo ofrece `Back` (no mutador) — `AC5` se cumple por construcción y §6 paso 5 lo demuestra; el backend además rechaza cualquier transición desde estado final (`VF-REQ-005`, probado) |
| Cancelación irreversible a un clic (sin modal hasta `US-033`) | `D5`: precedente ya implementado y ratificado en `US-024` para la fila; `US-033` intercepta ambos puntos después; app de demo |
| El comentario del código (*"interim view until US-025"*) queda obsoleto | Ítem #9 lo actualiza expresamente |
| Sin tests de frontend, `AC2`–`AC6` visuales solo se demuestran manualmente | `D8` + §6; la semántica de datos (bloque presente/ausente, nombre, comment null) queda probada server-side en #5/#6 |

---

Siguiente paso — implementación:

```
/user-story-implement ".claude/implementation plans/US-025-plan.md"
```
