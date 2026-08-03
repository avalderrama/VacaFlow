# Plan de implementación — `US-024` · My Requests screen

| Campo | Valor |
|---|---|
| Historia | `US-024` — My Requests screen |
| Épica | `EP-08` — Visibility and results |
| Prioridad · Talla | **Must** · `M` |
| Pantalla | `S-04` (My Requests — list) — **esta historia es la dueña nominal** (`Backlog.md` §3.2, fila `S-04 → US-024, US-032`; screenshot `05-my-requests.png`). `S-04b` (empty) y el skeleton **quedan fuera**: dueña `US-032` (ver `D4`). `S-08` (Cancel confirmation modal) **queda fuera**: dueña `US-033` (ver `OQ-A`) |
| Depende de | `US-020` (listado, PR #17) · `US-018` (submit, mergeada) · `US-019` (cancel, mergeada) — **las tres en `main`**; además `US-023` (PR #20) ya dejó `listRequests`/`getMe`/`RequestSummary` escritos |
| Trazas | `SC-01` · `RULE-04` · `FR-UIX-012` (My Requests — *Traces: US-024*, con la **misma tabla de acciones por estado verbatim**) · `FR-UIX-002` (no invalid actions) · `FR-UIX-005` (reload after action) · `FR-VIS-001` · `Backlog.md` §EP-08 `US-024`, §3.3 (*List row*), §3.4 (badges), §3.5 (banners `Request submitted for approval.` / `Request cancelled.`, botones `Edit`/`Submit`/`View`/`Cancel`) · `SAD.md` §9.2 (árbol web: `(app)/requests/page.tsx # S-04`, `components/requests/RequestRow · StateBadge`) · `ADR-013` · prototipo `VacaFlow.dc.html` líneas 139–168 (markup exacto del título y la fila) y 579–596 (matriz de acciones por estado) |
| Fuentes | `Backlog.md` v2.0 · `FRD.md` · `SAD.md` v2.0 · `NFR.md` §6 (las reglas `UX-*` viven ahí como trazas; **no existe** `docs/reglas-diseno-ui-ux-web.md` — verificado) · código real verificado en **`main` (commit `8ada501` — `US-023` mergeada)**, backend y `src/web/` archivo por archivo · planes `US-018` (`D2`, §4 deuda), `US-019` (`D2`, §4 deuda), `US-020` (`D2`/`D3`), `US-023` (`D2`/`D5`/`D6`/`D7` — precedentes ratificados en implementación) |
| Repositorio | `C:\Users\avald\OneDrive\Work\VacaFlow\repositories\vacaflow` |
| Rama sugerida | `feat/us-024-my-requests`, creada desde `main` (`8ada501`) — sin precondiciones pendientes |
| Estado | Borrador presentado para aprobación (sesión de planificación delegada; **una pregunta abierta en §7 — `OQ-A`, el comportamiento del botón `Cancel` antes del modal `S-08`**) |

> **Este documento no implementa nada.** La implementación la ejecuta `/user-story-implement`.

---

## 1. Entendimiento

### 1.1 Contexto — segunda historia 100 % Web; el backend está completo y `US-023` ya pagó la mitad de la infraestructura cliente

Esta es la historia que **salda la deuda de UI** que `US-018` (§4: botón `Submit`, `submitRequest()` en `lib/api.ts`, reload, banner `Request submitted for approval.`, errores en banner) y `US-019` (§4: botón `Cancel`, `cancelRequest()`, reload, banner `Request cancelled.`) reanotaron nominalmente para `US-024`. Verificado contra `main` (`8ada501`):

**Backend — existe y no se toca (cero ítems de Domain/Application/Infrastructure/API):**

- `GET /api/requests` (`ListVisibleRequestsHandler`) — para un **Employee** devuelve exactamente sus propias solicitudes, **en todos los estados**, ordenadas `CreatedAtUtc` descendente (**verificado en `RequestRepository.ListOwnedByAsync`: `OrderByDescending(request => request.CreatedAtUtc)`** — el orden más-reciente-primero viene del servidor). Para un **Manager** devuelve la unión propias ∪ `Submitted` del equipo, también descendente; el doc-comment del handler prescribe que **la pantalla deriva su vista filtrando por `employee.id`** (`US-020` `D3`) — para `S-04` el filtro es el inverso al de `/queue`: quedarse con `employee.id === me.id`.
- `RequestSummaryResponse` — `id`, `absenceType {id, code, name}`, `startDate`, `endDate`, `reason`, `state`, `employee {id, fullName}`, `createdAtUtc`. **Todo lo que la fila §3.3 necesita** (tipo, fechas, estado) ya viaja.
- `POST /api/requests/{id}/submit` (`SubmitRequestHandler`, `US-018`) — cuerpo vacío, `204` a éxito; errores `VF-REQ-002/004/005/006` ya mapeados en `ErrorStatusMap` (`004→403`, `005→409`, `006→404`) y probados de punta a punta.
- `POST /api/requests/{id}/cancel` (`CancelRequestHandler`, `US-019`) — cuerpo vacío, `204`; `Request.Cancel` admite **exactamente `Draft` y `Submitted`** (verificado en `Domain/Requests/Request.cs`) — la matriz de acciones de esta historia es la afordancia espejo de esa regla.
- `GET /api/requests/{id}` (`GetRequestByIdHandler`) — ya alimenta la página `[id]` a la que rutan `Edit` y `View`.
- `GET /auth/me` — identidad para el filtro del manager.

**Web — lo que existe hoy en `src/web/` (tras `US-023`):**

- `lib/api.ts` (único módulo con `fetch`, `ADR-013`) — **ya tiene** `getMe()`, `listRequests(): Promise<RequestSummary[]>`, además de `getRequest`, `createRequest`, `updateRequest`, `approveRequest`, `rejectRequest`. **Faltan** `submitRequest(id)` y `cancelRequest(id)` (ambos planes previos lo verificaron: no se escribieron por adelantado).
- `lib/types.ts` — **ya tiene** `RequestSummary` (espejo verificado del contrato) y `RequestState`. Nada que añadir.
- `app/(app)/requests/page.tsx` — **placeholder honesto de `S-04`** (título `My Requests` + botón `New request` + consumo de `consumePendingNotification()` para los banners `Draft created.` / `Changes saved.` que llegan navegando desde el formulario). **Esta historia lo reescribe entero** — es el único archivo existente que se reemplaza.
- `app/(app)/requests/new/page.tsx` y `app/(app)/requests/[id]/page.tsx` (`US-017`) — destino de `New request`, `Edit` y `View`. La página `[id]` ya distingue: `Draft` → formulario editable "Edit draft"; cualquier otro estado → "Request detail" solo lectura (vista interina hasta que `US-025` traiga el bloque DECISION de `S-06`). **`View` no necesita ruta nueva.**
- `app/(app)/queue/page.tsx` + `components/queue/QueueCard.tsx` (`US-023`) — precedentes directos: patrón de carga `fetchQueue()` a nivel de módulo + `.then(setState)` en el efecto (**satisface la regla ESLint `react-hooks/set-state-in-effect` que dio guerra en `US-023` — reutilizar el patrón tal cual, no redescubrirlo**), flag `deciding` para deshabilitar en vuelo, banner éxito/error en página, y estados de carga/vacío mínimos textuales. `QueueCard` **no se reutiliza como componente**: la fila de `S-04` es otro patrón visual de §3.3 (*List row*: fila compacta 16px/20px con badge y hasta 3 botones, sin bloque de razón; *Queue card*: 20px con nombre de empleado y razón). Se reutiliza su **forma** (presentacional puro, sin fetch, orquestación en la página).
- `components/feedback/Banner.tsx` — variantes `success`/`error` listas; `lib/session.ts` — `consumePendingNotification` (se conserva en la página nueva).
- `globals.css` — tokens §3.1, `btn-primary`/`btn-secondary` (11px/22px — talla de formulario) y `btn-approve`/`btn-reject` (8px/16px — talla de card de cola). **Faltan** las tres clases de botón de fila de `S-04` (8px/14px según el prototipo, líneas 164–167): outlined neutro (`Edit`/`View`), sólido accent (`Submit`) y outlined danger (`Cancel`).
- **No existen** `components/requests/RequestRow.tsx` ni `components/requests/StateBadge.tsx` — ambos prescritos con ese nombre por `SAD.md` §9.2.
- Sin test runner de frontend (ratificado `US-023` `D7`).

### 1.2 Narrativa

El backlog formula `US-024` sin narrativa propia, por comportamiento y visual. La intención la fija `FR-UIX-012` (*"A list of the employee's own requests showing absence type, start date, end date and state. Actions per state: …"* — con la misma tabla) y `SC-01`: darle al empleado la pantalla central del producto, desde la que crea, somete, cancela y consulta sus solicitudes. Es la pantalla que convierte en demo visible los backends de `US-018` y `US-019`.

### 1.3 Criterios de aceptación — verbatim (`Backlog.md` §EP-08 · `US-024`)

**Behavior**

| # | Criterio |
|---|---|
| `AC1` | "Given my request list, when it renders, then it shows only my own requests, most recent first." |
| `AC2` | "Given another employee's request, when my list renders, then it never appears." |

**Visual — `S-04`**

| # | Criterio |
|---|---|
| `AC3` | "Title row: `My Requests` at 24px/600 on the left, primary `New request` on the right, 24px below." |
| `AC4` | "One row card per request per §3.3: absence type name over `{start} → {end}`, then the state badge of §3.4, then the action buttons." |
| `AC5` | "Actions strictly by state:" — con la tabla verbatim:<br><br>\| State \| Buttons, in order \|<br>\|---\|---\|<br>\| `Draft` \| `Edit` · `Submit` · `Cancel` \|<br>\| `Submitted` \| `View` · `Cancel` \|<br>\| `Approved` · `Rejected` · `Cancelled` \| `View` \| |
| `AC6` | "`Submit` is the primary style; `Edit` and `View` are outlined; `Cancel` is outlined in the danger palette." |
| `AC7` | "No action that would be rejected for the current state is rendered — an affordance only; the API rejects it regardless." |

Patrones §3.3/§3.4, verbatim de `Backlog.md` (autoritativos junto con el markup del prototipo, líneas 157–168):

> **List row** (`S-04`) — white card, 1px border, 10px radius, 16px/20px padding, rows separated by 10px. Layout: type name (15px/600) over date range (13px secondary) on the left and flexible; state badge; action buttons. Wraps on narrow viewports.

> **[§3.4]** Pill, `999px` radius, 4px/12px padding, 12px/600. The label matches the persisted value. *(tabla de 5 estados con fondos/frentes oklch; `Draft` y `Cancelled` comparten fondo y se distinguen por etiqueta y acciones, `NFR-USA-007`)*

### 1.4 Alcance

**Entra**: `submitRequest`/`cancelRequest` en `lib/api.ts`; tres clases CSS de botón de fila; `StateBadge` (§3.4); `RequestRow` (§3.3 + matriz `AC5`); reescritura de `app/(app)/requests/page.tsx` — fila de título con `New request` (`AC3`), carga con el patrón `fetchQueue`-style filtrando `employee.id === me.id`, acciones `Edit`/`View` (navegación a `/requests/{id}`), `Submit` y `Cancel` (mutación + reload `FR-UIX-005` + banner §3.5), errores en banner de error, conservación del `consumePendingNotification` existente, y estados mínimos textuales de carga/vacío.

**No entra**

| Excluido | Por qué / destino |
|---|---|
| Skeleton de `S-04` (tres bloques de 64px) y empty state `S-04b` (card dashed, `You haven't created any requests yet`, CTA `Create request`) | **`US-032`** — sus criterios nombran `S-04`/`S-04b` verbatim (*"three 64px blocks on `S-04`"*, *"Given an employee with no requests, when `S-04` renders, then the `S-04b` empty state appears"*). Mismo diferimiento que `US-023` `D5`: aquí queda texto plano de carga y de vacío, que `US-032` reemplaza |
| Modal `S-08` de confirmación de cancelación (`Cancel this request?` · `Back`/`Yes, cancel`) | **`US-033`** — dueña por tabla de pantallas (`S-08 → US-033`); su criterio (*"Given I press `Cancel` on a row… then the `S-08` modal opens"*) describe la interceptación del clic que insertará entre el botón de esta historia y la llamada. Ver **`OQ-A`** |
| Shell S-03 (header, nav tabs, identidad) | **`US-030`** — la página se entrega dentro del `(app)/layout.tsx` mínimo actual, igual que `/queue` |
| Matriz completa del banner (fade, clear-on-navigate, `×` como criterio) | **`US-031`** — esta historia solo consume el componente existente |
| Bloque DECISION de `S-06` al pulsar `View` en `Approved`/`Rejected` | **`US-025`** (`Depends on: US-024` — posterior por diseño; `Backlog.md` §EP-08). `View` ruta a la página `[id]` actual, que ya renderiza "Request detail" solo lectura — vista interina anotada en su propio código |
| Badge `Approval Queue (N)` / navegación entre pantallas | **`US-035`** / **`US-030`** |
| Cualquier cambio en backend | Nada que cambiar — verificado archivo por archivo (§1.1): endpoints, orden descendente server-side, `ErrorStatusMap` completo para submit/cancel |

---

## 2. Cambios estructurales / de base

**No se requieren cambios de esquema, migraciones, configuración, variables de entorno, feature flags, dependencias nuevas (npm ni NuGet) ni cambios de seed.** Los únicos cambios "de base" son tres clases CSS nuevas en `globals.css` (ítem #2) — aditivas, sin tocar las existentes.

---

## 3. Plan ordenado por dependencia

**Sin ítems de Domain, Application, Infrastructure ni API** (§1.1). Todo es Web, de la base hacia la pantalla.

| # | Capa | Acción | Artefacto | Notas |
|---|---|---|---|---|
| 1 | Web | Modificar | `src/web/lib/api.ts` | Dos funciones sobre el helper `request<T>` existente: `submitRequest(id: string): Promise<void>` → `POST /requests/${id}/submit` · `cancelRequest(id: string): Promise<void>` → `POST /requests/${id}/cancel`. Sin cuerpo (los endpoints no bindean contrato — verificado en `RequestEndpoints.cs`); el `204 → undefined`, el mapeo `{ code, message }` y el redirect `VF-AUT-004` ya están resueltos en `request<T>`. `lib/types.ts` **no se toca**: `RequestSummary` ya existe |
| 2 | Web | Modificar | `src/web/app/globals.css` | Tres clases de botón de fila junto a `btn-approve`/`btn-reject`, valores verbatim del prototipo (líneas 164–167; el markup gana en detalle visual, §3.2): `.btn-row-outline` (`Edit`/`View` — `background: white; border: 1px solid oklch(85% 0.008 260)`) · `.btn-row-primary` (`Submit` — `background: oklch(52% 0.15 260); color: white; border: none`) · `.btn-row-danger` (`Cancel` — `background: white; border: 1px solid oklch(80% 0.1 25); color: oklch(45% 0.15 25)`); las tres con `padding: 8px 14px; border-radius: 7px; font-size: 13px; font-weight: 600; cursor: pointer` y `:disabled { cursor: default; opacity: 0.6 }` (precedente `btn-approve`/`btn-reject`). Justificación de creación: las clases existentes no encajan — `btn-primary`/`btn-secondary` son talla formulario (11px/22px) y `btn-reject` es 8px/16px; la fila usa 8px/14px (**`AC6`**) |
| 3 | Web | Crear | `src/web/components/requests/StateBadge.tsx` | Componente presentacional (nombre y carpeta prescritos por `SAD.md` §9.2). Pill `border-radius: 999px; padding: 4px 12px; font-size: 12px; font-weight: 600`, prop `state: RequestState`, mapa de los 5 pares fondo/frente **verbatim §3.4**; la etiqueta es el propio valor persistido (`Backlog.md` §2: *"the state labels shown in the interface now coincide exactly with the values persisted"*) — sin traducción. Justificación de creación: no existe ningún badge en el código; `US-025` (S-06) lo reutilizará |
| 4 | Web | Crear | `src/web/components/requests/RequestRow.tsx` | Componente presentacional (nombre prescrito por `SAD.md` §9.2; sin `fetch` ni estado — la página orquesta, mismo reparto que `QueueCard`). Fila §3.3 + prototipo línea 157: `display: flex; align-items: center; gap: 16px; padding: 16px 20px; background: var(--color-surface); border: 1px solid var(--color-border); border-radius: 10px; flex-wrap: wrap` (**`AC4`**, envuelve en viewport estrecho). Izquierda flexible (`flex: 1; min-width: 180px`): `absenceType.name` 15px/600 sobre `{startDate} → {endDate}` 13px secundario; luego `<StateBadge state={…} />`; luego grupo de acciones `display: flex; gap: 8px`. **Matriz estricta por estado (`AC5`/`AC7`)**: `Draft` → `Edit`(`.btn-row-outline`) · `Submit`(`.btn-row-primary`) · `Cancel`(`.btn-row-danger`); `Submitted` → `View`(`.btn-row-outline`) · `Cancel`(`.btn-row-danger`); `Approved`/`Rejected`/`Cancelled` → `View` — **ningún otro botón se renderiza jamás** (`FR-UIX-002`), orden exacto de la tabla. Props: `request: RequestSummary`, `onEdit`, `onView`, `onSubmit`, `onCancel: () => void`, `disabled: boolean` |
| 5 | Web | Modificar (reescritura) | `src/web/app/(app)/requests/page.tsx` | Página S-04 real (`'use client'`), reemplaza el placeholder. **Carga** (patrón `fetchQueue`-style de `(app)/queue/page.tsx`, que satisface `react-hooks/set-state-in-effect` — lección de `US-023`): función a nivel de módulo `fetchMyRequests(): Promise<RequestSummary[]>` = `Promise.all([getMe(), listRequests()])` → `requests.filter(r => r.employee.id === me.id)` (**`AC1`/`AC2`** — filtro inverso al de `/queue`, prescrito por `US-020` `D3`; para un Employee es identidad, para un Manager excluye las filas del equipo); el orden más-reciente-primero viene del servidor (**`AC1`**) — no se reordena. **Render**: fila de título `display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px` con `My Requests` 24px/600 y `New request` `.btn-primary` → `router.push('/requests/new')` (**`AC3`**, prototipo líneas 140–143); lista con gap 10px de `<RequestRow>`; carga/vacío mínimos textuales (`D4`). **Banners**: se conserva el lazy initializer con `consumePendingNotification()` del placeholder actual (los `Draft created.`/`Changes saved.` del formulario siguen aterrizando aquí — no perder esta pieza al reescribir) + banners en página para submit/cancel. **Acciones**: `onEdit`/`onView` → `router.push(\`/requests/${id}\`)` (la página `[id]` ya resuelve editable vs solo-lectura por estado); `onSubmit` → `submitRequest(id)`; `onCancel` → `cancelRequest(id)` (directo, sin confirmación — **`OQ-A`**); ambos con flag `acting` que deshabilita las filas en vuelo (precedente `US-023` `D6`), a éxito → re-`fetchMyRequests()` (**`FR-UIX-005`** — jamás mutación local) + banner success `Request submitted for approval.` / `Request cancelled.` **verbatim §3.5** (deuda `US-018`/`US-019`, ver `D3`); a `ApplicationError` → banner error con `error.message` del catálogo y reload igualmente (la fila puede haber cambiado de estado en otra pestaña) |
| 6 | Web | Verificar | `cd src/web && npm run lint && npm run typecheck && npm run depcruise && npm run build` | `depcruise` confirma que `fetch` sigue solo en `lib/api.ts`; sin test runner de frontend (`D5`) |
| 7 | Test | Verificar | `dotnet build VacaFlow.slnx` + `dotnet test VacaFlow.slnx` | Deben seguir verdes sin cambios — esta historia no toca el backend; `AC1`/`AC2` se apoyan en comportamiento ya probado por las suites de `US-020` (visibilidad y orden) y `US-018`/`US-019` (transiciones) |
| 8 | E2E | Verificar | Verificación manual §6 con la app corriendo (API + `npm run dev`) | Único punto donde `AC1`–`AC7` son demostrables juntos (DoD) |

**Dependencias:** 1 → 5 · 2 → {3, 4} → 5 · todo → {6, 7, 8}. **Paralelizable:** {1} con {2, 3, 4}. **Ruta crítica:** 2 → 4 → 5 → 8. `OQ-A` no bloquea ningún ítem (solo decide si el `onCancel` del ítem #5 llama directo o pasa por `window.confirm` provisional).

---

## 4. Casos de uso y tabla de trazabilidad

**Cero casos de uso nuevos en Application.** Esta historia añade la **superficie de consumo** de cuatro casos de uso existentes: *listar visibles* (`US-020`), *someter* (`US-018`), *cancelar* (`US-019`) y, por navegación, *obtener por id* (`US-016`/`US-017`).

| Historia | Criterio de aceptación (verbatim) | Ítems del plan que lo cubren | Cómo se verifica |
|---|---|---|---|
| `US-024` | "Given my request list, when it renders, then it shows only my own requests, most recent first." | #5 (filtro `employee.id === me.id` + orden heredado del servidor, verificado en `RequestRepository`) | §6 pasos 2–3; el orden y la visibilidad ya están probados server-side (`US-020`) |
| `US-024` | "Given another employee's request, when my list renders, then it never appears." | #5 (para un Employee el servidor solo envía propias — `FR-VIS-001`; para un Manager el filtro descarta las del equipo) | §6 pasos 3 y 8 (vista de manager) |
| `US-024` | "Title row: `My Requests` at 24px/600 on the left, primary `New request` on the right, 24px below." | #5 | Inspección visual §6 paso 2 (contra `05-my-requests.png`) |
| `US-024` | "One row card per request per §3.3: absence type name over `{start} → {end}`, then the state badge of §3.4, then the action buttons." | #3 (badge §3.4), #4 (fila §3.3) | Inspección visual §6 paso 2; los 5 badges contra la tabla §3.4 |
| `US-024` | "Actions strictly by state:" *(tabla `Draft` → `Edit` · `Submit` · `Cancel`; `Submitted` → `View` · `Cancel`; `Approved`/`Rejected`/`Cancelled` → `View`)* | #4 (matriz exhaustiva por estado, orden incluido) | §6 pasos 2 y 4–7 (un request en cada estado) |
| `US-024` | "`Submit` is the primary style; `Edit` and `View` are outlined; `Cancel` is outlined in the danger palette." | #2 (las tres clases), #4 (asignación por botón) | Inspección visual §6 paso 2 |
| `US-024` | "No action that would be rejected for the current state is rendered — an affordance only; the API rejects it regardless." | #4 (render condicional estricto — nada deshabilitado ni oculto por CSS: no se monta), backend ya existente (la mitad "the API rejects it regardless" está probada en `US-018`/`US-019`) | §6 paso 7 + suites backend existentes (`VF-REQ-005` en submit/cancel sobre estados finales) |

**Conteo: 7 criterios de entrada · 7 cubiertos.** Además esta historia **salda la deuda nominal reanotada** por `US-018` §4 (botón `Submit`, `submitRequest()`, reload, banner `Request submitted for approval.`, errores `VF-REQ-002/004/005` en banner de error) y `US-019` §4 (botón `Cancel` en filas `Draft`/`Submitted`, `cancelRequest()`, reload, banner `Request cancelled.`, errores en banner) — ambas listas quedan íntegramente cubiertas por los ítems #1, #2, #4 y #5.

---

## 5. Supuestos y decisiones

Sesión de planificación delegada (Fase 3 no interactiva): decisiones de arquitecto documentadas con su reversibilidad. **La única que merece ratificación del usuario está elevada a §7 (`OQ-A`).**

| # | Decisión | Justificación | Impacto si es incorrecta |
|---|---|---|---|
| `D1` | **Cero cambios en backend** | Verificado archivo por archivo en `main` (`8ada501`): `GET /requests` ya devuelve el conjunto correcto ordenado descendente (repo-side para Employee, re-sort en handler para Manager), submit/cancel existen con sus errores mapeados (`ErrorStatusMap`: `004→403`, `005→409`, `006→404`), y `Request.Cancel` admite exactamente `Draft`/`Submitted` — la matriz `AC5` es la afordancia espejo de reglas ya probadas | N/A — hecho del código |
| `D2` | **`Edit` y `View` rutan ambos a `/requests/{id}`; no hay ruta nueva ni componente de detalle nuevo** | La página `[id]` (US-017) ya bifurca por estado: `Draft` → formulario "Edit draft"; resto → "Request detail" solo lectura, anotado en su código como *"the interim view until US-025 delivers S-06's DECISION block"*. `US-025` (que **depende de `US-024`**) es la dueña declarada del bloque DECISION | Ninguno — `US-025` enriquece esa misma página sin tocar esta historia |
| `D3` | **Esta historia dispara los banners de éxito §3.5 (`Request submitted for approval.` / `Request cancelled.`) en página, con el componente `Banner` existente; los del formulario (`Draft created.`/`Changes saved.`) siguen llegando por `consumePendingNotification`** | Es la deuda reanotada explícitamente por `US-018` §4 y `US-019` §4 con destino `US-024`; mismo mecanismo que `US-023` `D3` usó para `Request approved.`/`Request rejected.` (ratificado en implementación). Submit/cancel no navegan → banner en página, sin `setPendingNotification` | Trivialmente reversible (una línea por rama) |
| `D4` | **Estados de carga y de lista vacía mínimos y textuales (sin skeleton §3.3 ni empty-state `S-04b`)** | `US-032` es la dueña explícita de ambos **nombrando `S-04`/`S-04b`** (*"three 64px blocks on `S-04`"*; título/cuerpo/CTA de `S-04b` en §3.5). Idéntico a `US-023` `D5` (ratificada). Un texto plano evita el área en blanco sin duplicar la otra historia | Ninguno — `US-032` sobrescribe exactamente esos dos bloques |
| `D5` | **Sin tests automatizados de frontend; verificación = lint + typecheck + depcruise + build + E2E manual** | Ratificación de `US-023` `D7`: sigue sin existir runner en `src/web/package.json` y estrenarlo excede una historia de pantalla (`TC-06`) | Si el usuario quiere estrenar runner aquí, se añade como ítem previo — ampliación, no corrección |
| `D6` | **`RequestRow` y `StateBadge` son componentes nuevos (no se reutiliza `QueueCard`), con los nombres del `SAD.md` §9.2** | El backlog define *List row* y *Queue card* como **dos patrones distintos** de §3.3 (fila compacta con badge y matriz de acciones vs card con nombre de empleado y bloque de razón); forzar un componente único parametrizado acoplaría dos pantallas con evoluciones independientes (`US-032`/`US-025` vs `US-034`). Lo que sí se replica es el reparto presentacional/orquestación y el patrón de fetch de `US-023`. `StateBadge` se separa de la fila porque `SAD.md` lo nombra como componente propio y `US-025` (S-06) lo necesitará | Si se prefiriera unificar, sería refactor posterior sin cambio de comportamiento |
| `D7` | **Tras un error de submit/cancel también se recarga la lista** | El error más probable (`VF-REQ-005`, `409`) significa que el estado real ya no es el que la fila muestra (p. ej. cancelado en otra pestaña) — recargar deja la matriz de acciones coherente con el servidor (`FR-UIX-002` en espíritu); `AC7` solo exige no ofrecer acciones inválidas para el estado **conocido** | Si se prefiere lista congelada al error (como `/queue`, donde la fila permanece por criterio explícito), se quita el re-fetch de la rama de error — cambio de una línea |
| `D8` | **El filtro del manager (`employee.id === me.id`) se aplica siempre, también para Employees (donde es identidad)** | Un solo camino de código sin bifurcar por rol (`FR-VIS-001`: el cliente no decide visibilidad, solo deriva la vista del payload — `US-020` `D3` prescribe exactamente este filtro para `S-04`); simetría exacta con `/queue` (filtro `!==`) | N/A — para Employee es un no-op verificable |
| `S1` | **La rama se crea desde `main` (`8ada501`) directamente** | Verificado: `US-023` (PR #20) mergeada; todo lo necesario está en `main` | Ninguno |

---

## 6. Verificación end-to-end

Con la API corriendo y `npm run dev` en `src/web/`, seed §3.6 (Laura manager; Carlos y Ana empleados asignados a ella).

| # | Paso | Resultado esperado |
|---|---|---|
| 1 | `cd src/web && npm run lint && npm run typecheck && npm run depcruise && npm run build` · `dotnet build` + `dotnet test` | Todo verde, 0 warnings; backend sin cambios |
| 2 | Sign in como Carlos con solicitudes en los 5 estados (crear/somenter/decidir con Laura según haga falta) → `/requests` — **`AC3`–`AC6`** | Fila de título `My Requests` 24px/600 + `New request` primario a la derecha, 24px de separación; una fila por solicitud: tipo 15px/600 sobre `{start} → {end}` 13px secundario, badge §3.4 con el color/etiqueta exactos del estado, botones según la matriz — contra `05-my-requests.png` |
| 3 | Crear un draft nuevo y volver — **`AC1`** | El draft recién creado aparece **primero** (más reciente); banner `Draft created.` aterriza vía `consumePendingNotification` (regresión del placeholder) |
| 4 | `Submit` sobre el draft — **`AC5`** + deuda `US-018` | Filas deshabilitadas en vuelo; a la vuelta la lista se recarga desde la API, la fila pasa a badge `Submitted` con acciones `View · Cancel`, y el banner lee `Request submitted for approval.` |
| 5 | `Cancel` sobre esa `Submitted` — deuda `US-019` (comportamiento según **`OQ-A`**) | Recarga; badge `Cancelled`, solo `View`; banner `Request cancelled.` |
| 6 | `Edit` sobre un `Draft` / `View` sobre una decidida — `D2` | `/requests/{id}`: formulario "Edit draft" editable vs "Request detail" solo lectura (interino hasta `US-025`) |
| 7 | Dos pestañas como Carlos sobre el mismo `Draft`: cancelarlo en una, `Submit` en la otra sin recargar — **`AC7`** (mitad API) + `D7` | La segunda recibe `409` `VF-REQ-005` en banner de error (`This request cannot move from Cancelled to Submitted.`) y la lista se recarga mostrando `Cancelled` con solo `View` |
| 8 | Sign in como Laura (Manager) con una `Submitted` de Carlos pendiente → `/requests` — **`AC2`** | Solo las solicitudes **propias** de Laura aparecen; la de Carlos no (sí aparece en `/queue`) — el filtro inverso funciona |
| 9 | Viewport estrecho (≈ 400px) — **`AC4`** ("Wraps on narrow viewports") | La fila envuelve (`flex-wrap`) sin romper el grupo de acciones |
| 10 | Visita a `/requests` sin sesión | Primer `GET` devuelve `VF-AUT-004` → redirect a `/sign-in` (`FR-UIX-007`, ya resuelto en `lib/api.ts`) |

---

## 7. Riesgos y preguntas abiertas

> ⚠️ **`OQ-A` — Pregunta abierta para el usuario (no bloquea ningún ítem; decide una línea del ítem #5):**
> El modal de confirmación `S-08` (`Cancel this request?` · `Back`/`Yes, cancel`) es íntegramente de **`US-033`** (tabla de pantallas `S-08 → US-033`; su criterio *"Given I press `Cancel` on a row… then the `S-08` modal opens"* es exactamente la interceptación del clic). Hasta que `US-033` llegue, ¿qué hace el botón `Cancel` de la fila?
>
> - **(a) — recomendada — Llama `cancelRequest(id)` directamente, sin confirmación.** Espejo exacto de `US-023` `D2` (los botones `Approve`/`Reject` deciden directo y `US-034` insertará su modal después — ratificado en implementación): `US-033` inserta el modal entre el `onClick` y la llamada existente sin retocar nada más. Contra: una cancelación irreversible a un clic durante la ventana entre historias (mitigado: es una app de demo y la secuencia del backlog ya acepta esta ventana para Approve/Reject, también irreversibles).
> - **(b) `window.confirm` provisional** (opción que el propio plan de `US-019` §7 dejó descrita). Protege del clic accidental, pero crea un artefacto que `US-033` debe retirar y un diálogo nativo que no cumple ningún criterio de `S-08`.
>
> **El plan asume (a) salvo indicación contraria.** Elegir (b) cambia una línea del ítem #5.

| Riesgo | Mitigación |
|---|---|
| `US-019 AC1` dice *"when I **confirm** cancellation"* y esta historia cancela sin confirmar | Es la misma relación `US-023`↔`US-034` ya aprobada: la "confirmación" es de `US-033` (dueña nominal con criterio verbatim); el backend de `US-019` quedó "backend done" con esa deuda repartida explícitamente entre `US-024` (botón/reload/banner — esta historia) y `US-033` (modal). `OQ-A` deja la elección explícita |
| Reescribir el placeholder pierde el aterrizaje de `Draft created.`/`Changes saved.` | Anotado expresamente en el ítem #5: el lazy initializer con `consumePendingNotification()` se conserva; §6 paso 3 lo verifica como regresión |
| La regla ESLint `react-hooks/set-state-in-effect` (error real sufrido en `US-023`) | Conocida de antemano: el ítem #5 replica el patrón `fetchQueue`-style (función async a nivel de módulo + `.then(setState)` en el efecto) que ya pasa lint en `(app)/queue/page.tsx` |
| Doble clic / carreras (submit y cancel simultáneos, dos pestañas) | Flag `acting` (precedente `US-023` `D6`) + el backend convierte la carrera en `VF-REQ-005`/`409` (probado en `US-018`/`US-019`); `D7` recarga tras el error para realinear la matriz de acciones |
| Tentación de construir skeleton/empty-state "ya que estamos" | `D4`: son criterios verbatim de `US-032` con `S-04`/`S-04b` nombrados — construirlos aquí duplica otra historia |
| Sin tests de frontend, `AC1`–`AC7` solo se demuestran manualmente | `D5` + §6; la semántica de datos (visibilidad, orden, transiciones, errores) ya está probada de punta a punta en las suites backend de `US-018`–`US-020` |

---

Siguiente paso — implementación:

```
/user-story-implement ".claude/implementation plans/US-024-plan.md"
```
