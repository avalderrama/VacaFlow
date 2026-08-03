# Plan de implementación — `US-023` · Approval Queue screen

| Campo | Valor |
|---|---|
| Historia | `US-023` — Approval Queue screen |
| Épica | `EP-07` — Manager decision |
| Prioridad · Talla | **Must** · `M` |
| Pantalla | `S-07` (Approval Queue) — **esta historia es la dueña nominal** (`Backlog.md` tabla de pantallas, fila `S-07 → US-023`; screenshot `10-manager-queue.png`). `S-09` (Decision modal) **queda fuera**: dueña `US-034` (ver `D2`) |
| Depende de | `US-020` (listado, PR #17) · `US-021` (approve, PR #18) · `US-022` (reject, PR #19) — **las tres mergeadas en `main`** |
| Trazas | `FR-UIX-014` (Manager queue — *Traces: US-023*) · `FR-UIX-002`/`003`/`005` · `FR-VIS-001` · `FR-DEC-008` · `Backlog.md` §EP-07 `US-023`, §3.3 (*Queue card*, *Banner*), §3.5 (banners `Request approved.` / `Request rejected.`) · `SAD.md` §9 (árbol web: `(app)/queue/page.tsx # S-07`, `components/queue/QueueCard`) · `ADR-013` (solo `lib/api.ts` llama `fetch`) · prototipo `VacaFlow.dc.html` líneas 190–208 (markup exacto de la card) |
| Fuentes | `Backlog.md` v2.0 · `FRD.md` · `SAD.md` v2.0 · código real verificado en **`main` (commit `c25b3b8` — `US-022` mergeada)**, backend y `src/web/` archivo por archivo · planes `US-020` (`D2`/`D3`/`S1`), `US-021` (`D9`), `US-022` (`D3`) — deuda nominal anotada para esta historia |
| Repositorio | `C:\Users\avald\OneDrive\Work\VacaFlow\repositories\vacaflow` |
| Rama sugerida | `feat/us-023-approval-queue`, creada desde `main` (`c25b3b8`) — sin precondiciones pendientes |
| Estado | Borrador presentado para aprobación (sesión de planificación delegada; **una pregunta abierta en §7 — `OQ-A`, el acceso a la ruta antes del shell**) |

> **Este documento no implementa nada.** La implementación la ejecuta `/user-story-implement`.

---

## 1. Entendimiento

### 1.1 Contexto — el backend está completo; esta es la primera historia 100 % Web

Las tres dependencias entregaron y dejaron **explícitamente anotado** que la superficie `S-07` es de esta historia (patrón de diferimiento séptuple `US-014`→`US-022`, mantenido en implementación todas las veces). Verificado contra `main` (`c25b3b8`):

**Backend — existe y no se toca (cero ítems de Domain/Application/Infrastructure/API):**

- `GET /api/requests` (`ListVisibleRequestsHandler`) — sin parámetros (`FR-VIS-001`: *"the caller cannot influence this through parameters"*). Para un Manager devuelve **la unión** de sus propias solicitudes (todos los estados) ∪ las `Submitted` de sus asignados, ordenada `CreatedAtUtc` descendente en una sola secuencia (`US-020` `D3`/`S1`). El doc-comment de `RequestSummaryResponse` lo prescribe verbatim para esta historia: *"a consumer deriving 'the approval queue' (US-023) must exclude rows where `Employee.Id` equals the caller's own id (from `GET /auth/me`)"*. La porción ajena es, por construcción, exactamente las `Submitted` del equipo — no hace falta filtrar por estado.
- `RequestSummaryResponse` — `id`, `absenceType {id, code, name}`, `startDate`, `endDate`, `reason`, `state`, `employee {id, fullName}`, `createdAtUtc`. **Todo lo que la card de §3.3 necesita** (nombre del empleado, tipo, fechas, razón) ya viaja; el `reason` completo incluido.
- `POST /api/requests/{id}/approve` y `POST /api/requests/{id}/reject` — mismo `DecideRequestHandler`/`ApprovalPolicy`, cuerpo `{ comment? }` (opcional, `FR-DEC-008`), éxito `204 No Content` (`ADR-012`), errores `VF-DEC-001`–`005`/`VF-REQ-006`/`VF-VAL-001` ya mapeados y probados de punta a punta. Una decisión completada hace que la solicitud deje de ser `Submitted` → **el servidor ya la expulsa de la porción de cola** en el siguiente `GET` (probado en funcional de `US-021`/`US-022`).
- `GET /auth/me` — devuelve `AuthenticatedUserResponse` (`id`, `fullName`, `email`, `role`), ya autenticado por cookie.

**Web — lo que existe hoy en `src/web/` (US-013/US-017):**

- `lib/api.ts` — único módulo con `fetch` (`ADR-013`, regla `only-lib-api-may-fetch` de dependency-cruiser). Ya maneja: parse del error `{ code, message, field? }`, redirect a sign-in solo en `VF-AUT-004` (`FR-UIX-007`), y `204 → undefined`. **Faltan** las funciones `getMe`, `listRequests`, `approveRequest`, `rejectRequest`.
- `lib/types.ts` — espejos TS de los contratos C#. **Falta** el espejo de `RequestSummaryResponse`.
- `components/feedback/Banner.tsx` — ya implementa el patrón §3.3 con `role="status"`, dismiss `aria-label="Dismiss notification"` y **la variante `'error'` lista pero sin llamador** (su doc-comment: *"it exists so a future error-banner caller has the variant ready"* — el llamador llega con esta historia, criterio `AC6`).
- `lib/session.ts` — `setPendingNotification`/`consumePendingNotification` para banners que cruzan navegación (no se necesita aquí: la decisión no navega, el banner se muestra en la misma página).
- `app/(app)/requests/page.tsx` — placeholder de S-04 (sin lista; la lista real es `US-024`). `app/(app)/layout.tsx` — contenedor mínimo, **no** el shell S-03 (`US-030`). **No existe** `(app)/queue/page.tsx` ni `components/queue/`.
- `globals.css` — tokens §3.1 (incluye `--color-danger`) y clases `btn-primary`/`btn-secondary`. **Faltan** los estilos de los botones de la card: `Approve` sólido success y `Reject` outlined danger (colores exactos en el prototipo, líneas 202–203: sólido `oklch(55% 0.14 150)` blanco; outlined borde `oklch(80% 0.1 25)` texto `oklch(45% 0.15 25)`).
- `package.json` web — scripts `lint`, `typecheck`, `depcruise`, `build`. **No hay test runner de frontend** (ver `D7`).

### 1.2 Narrativa

El backlog formula `US-023` por criterios visuales y de comportamiento. La intención la fija `FR-UIX-014` (*"A list of `Submitted` requests of the manager's assigned employees… Each row offers Approve and Reject… After a decision the list reloads and the request leaves the queue"*): darle al manager la pantalla desde la que ejercita, con dos clics, los casos de uso ya entregados por `US-020`–`US-022`. Es la primera pantalla del producto con acciones que mutan estado ajeno.

### 1.3 Criterios de aceptación — verbatim (`Backlog.md` §EP-07 · `US-023`)

**Visual — `S-07`**

| # | Criterio |
|---|---|
| `AC1` | "Page title `Approval Queue`, 24px/600, 24px below." |
| `AC2` | "One card per request per §3.3: employee name, then `{type} · {start} → {end}` in 13px secondary; `Reject` (outlined, danger) and `Approve` (solid, success) on the right, in that order." |
| `AC3` | "A top-bordered block below shows the full reason at 14px." |
| `AC4` | "Cards ordered most recent first; the action group does not shrink on narrow viewports." |

**Comportamiento**

| # | Criterio |
|---|---|
| `AC5` | "Given a completed decision, when it returns, then the list reloads from the API and the request leaves the queue." |
| `AC6` | "Given a failed decision, when the error returns, then it appears in an error banner and the request stays in the queue." |

Patrón *Queue card*, verbatim de `Backlog.md` §3.3 (autoritativo junto con el markup del prototipo):

> **Queue card** (`S-07`) — white card, 20px padding. Top line: employee name (15px/600) over `type · start → end` (13px secondary); actions on the right. Then a top-bordered block, 12px above and below, with the reason at 14px.

### 1.4 Alcance

**Entra**: tipos y funciones de API cliente (`RequestSummary`, `getMe`, `listRequests`, `approveRequest`, `rejectRequest`), estilos de los dos botones de decisión, componente `QueueCard`, página `(app)/queue/page.tsx` con derivación de la cola por `employee.id`, disparo de las decisiones **sin comentario** (`{ comment: null }` — ver `D2`), reload desde la API tras cada decisión (`FR-UIX-005`), banner de éxito §3.5 (`Request approved.` / `Request rejected.` — deuda nominal de `US-021`/`US-022`, ver `D3`) y banner de error con el mensaje del catálogo (`AC6`, `FR-UIX-003`).

**No entra**

| Excluido | Por qué / destino |
|---|---|
| Modal `S-09` (título §3.5, textarea `Comment (optional)`, confirmación) | **`US-034`** — dueña por tabla de pantallas (`S-09 → US-034`) y por §EP-07; sus criterios (*"Given I press `Approve` or `Reject`, when it activates, then the `S-09` modal opens"*) describen exactamente la interceptación del clic que `US-034` insertará entre el botón y la llamada. Ver `D2` |
| Shell S-03: header, nav tabs (`My Requests` / `Approval Queue`), identidad visible | **`US-030`** (los tres primeros criterios lo nombran verbatim). `US-023` no depende de `US-030` en el backlog — la página se entrega sin tab; ver `OQ-A` |
| Matriz completa del banner (fade 150ms, clear-on-navigate, `×` verificado como criterio) | **`US-031`** — el componente `Banner` ya existe y esta historia lo consume tal cual; los Given/When/Then del banner son de `US-031` |
| Skeleton de carga (dos bloques de 96px) y empty state de cola vacía (card dashed sin CTA) | **`US-032`** — sus criterios nombran `S-07` expresamente (*"two 96px blocks on `S-07`"*, *"a manager with an empty queue"*). Esta historia deja un estado de carga/vacío mínimo textual, no el patrón §3.3 (ver `D5`) |
| Badge `Approval Queue (N)` con conteo | **`US-035`** |
| Lista real de S-04 / My Requests | **`US-024`** |
| Cualquier cambio en backend (endpoints, contratos, handler, discriminador `isOwn`) | Nada que cambiar: la derivación por `employee.id` está prescrita en el propio contrato (`US-020` `D3` ratificada — no se re-decide aquí) |

---

## 2. Cambios estructurales / de base

**No se requieren cambios de esquema, migraciones, configuración, variables de entorno, feature flags, dependencias nuevas ni cambios de seed.** Tampoco dependencias npm nuevas: todo se construye con React/Next ya presentes. Los únicos cambios "de base" son dos clases CSS nuevas en `globals.css` (ítem #3) — aditivas, sin tocar las existentes.

---

## 3. Plan ordenado por dependencia

**Sin ítems de Domain, Application, Infrastructure ni API** (§1.1). Todo es Web, de la base hacia la pantalla.

| # | Capa | Acción | Artefacto | Notas |
|---|---|---|---|---|
| 1 | Web | Modificar | `src/web/lib/types.ts` | Espejo de `RequestSummaryResponse` (verificado contra `Contracts/RequestSummaryResponse.cs`): `export interface RequestSummary { id: string; absenceType: { id: string; code: string; name: string }; startDate: string; endDate: string; reason: string; state: RequestState; employee: { id: string; fullName: string }; createdAtUtc: string; }`. Fechas como string `yyyy-MM-dd` sin parsear (convención `S1` del plan `US-017`, ya documentada en el header del archivo) |
| 2 | Web | Modificar | `src/web/lib/api.ts` | Cuatro funciones sobre el helper `request<T>` existente (`ADR-013` — ningún otro módulo toca `fetch`): `getMe(): Promise<AuthenticatedUser>` → `GET /auth/me` · `listRequests(): Promise<RequestSummary[]>` → `GET /requests` · `approveRequest(id: string, comment: string \| null): Promise<void>` → `POST /requests/${id}/approve` body `{ comment }` · `rejectRequest(id, comment)` → ídem `/reject`. El `204 → undefined` y el mapeo de errores ya están resueltos en `request<T>` |
| 3 | Web | Modificar | `src/web/app/globals.css` | Dos clases nuevas junto a `btn-primary`/`btn-secondary`: `.btn-approve` (sólido success — `background: oklch(55% 0.14 150); color: white; border: none`) y `.btn-reject` (outlined danger — `background: white; border: 1px solid oklch(80% 0.1 25); color: oklch(45% 0.15 25)`); ambas `padding: 8px 16px; border-radius: 7px; font-size: 13px; font-weight: 600; cursor: pointer` — valores verbatim del prototipo (líneas 202–203), que gana en detalle visual (§3.2). Justificación de creación: no existe ninguna clase success-sólida ni danger-outlined en el CSS actual (verificado) |
| 4 | Web | Crear | `src/web/components/queue/QueueCard.tsx` | Componente presentacional (carpeta `components/queue/` prescrita por `SAD.md` §9). Card blanca 20px padding, borde 1px, radio 10px (prototipo línea 195). Cabecera flex `justify-content: space-between; flex-wrap: wrap; gap: 16px`: izquierda nombre del empleado 15px/600 sobre `{absenceType.name} · {startDate} → {endDate}` 13px secundario (**`AC2`**); derecha grupo de acciones `flex-shrink: 0` (**`AC4`**) con `Reject` (`.btn-reject`) **antes de** `Approve` (`.btn-approve`) — orden verbatim de `AC2` y del prototipo. Debajo, bloque `border-top` 12px arriba/abajo con `reason` completo a 14px (**`AC3`**). Props: `request: RequestSummary`, `onApprove: () => void`, `onReject: () => void`, `disabled: boolean` (decisión en vuelo, `D6`). Sin `fetch` ni estado propio — la página orquesta |
| 5 | Web | Crear | `src/web/app/(app)/queue/page.tsx` | Página S-07 (`'use client'`, ruta prescrita por `SAD.md` §9). **Carga**: `Promise.all([getMe(), listRequests()])`; deriva la cola filtrando `row.employee.id !== me.id` (prescripción literal del contrato, §1.1); el orden más-reciente-primero ya viene del servidor (**`AC4`**, `US-020` `S1`) — no se reordena. **Render**: título `Approval Queue` 24px/600 con 24px de margen inferior (**`AC1`**, mismo patrón del placeholder de S-04); cards con gap 10px; estados mínimos de carga/vacío textuales (`D5`). **Decisión**: `onApprove`/`onReject` llaman `approveRequest(id, null)` / `rejectRequest(id, null)` (sin comentario — `D2`), con botones deshabilitados mientras hay una decisión en vuelo (`D6`); a éxito → re-`listRequests()` desde la API (**`AC5`**, `FR-UIX-005` — jamás mutación local) + `Banner` success con `Request approved.` / `Request rejected.` verbatim §3.5 (`D3`); a `ApplicationError` → `Banner` variant `'error'` con `error.message` del catálogo (**`AC6`**, `FR-UIX-003` — primer llamador real de la variante) y la lista se mantiene (opcionalmente re-fetch: la solicitud sigue `Submitted` server-side, permanece en la cola en ambos casos). Un empleado que visite la ruta obtiene una cola derivada vacía (su payload solo trae propias) — sin acción inválida ofrecida (`FR-UIX-002`) |
| 6 | Web | Verificar | `cd src/web && npm run lint && npm run typecheck && npm run depcruise && npm run build` | `depcruise` verifica que `fetch` sigue solo en `lib/api.ts` (los componentes nuevos consumen `lib/api`); no hay test runner de frontend que ejecutar (`D7`) |
| 7 | Test | Verificar | `dotnet build VacaFlow.slnx` + `dotnet test VacaFlow.slnx` | Deben seguir verdes sin cambios — esta historia no toca el backend; los criterios `AC5`/`AC6` se apoyan en comportamiento ya probado por las suites de `US-020`–`US-022` |
| 8 | E2E | Verificar | Verificación manual §6 con la app corriendo (API + `npm run dev`) | Único punto donde `AC1`–`AC6` son demostrables juntos (DoD: *"the acceptance criteria are demonstrable in the running application"*) |

**Dependencias:** `OQ-A` no bloquea ningún ítem (solo añadiría un enlace provisional si se elige (b)) · 1 → 2 → 5 · 3 → 4 → 5 · todo → {6, 7, 8}. **Paralelizable:** {1, 2} con {3, 4}. **Ruta crítica:** 1 → 2 → 5 → 8.

---

## 4. Casos de uso y tabla de trazabilidad

**Cero casos de uso nuevos en Application.** Esta historia añade la **superficie de consumo** de tres casos de uso existentes: *listar visibles* (`ListVisibleRequestsHandler`, `US-020`) y *decidir* en sus dos verbos (`DecideRequestHandler`, `US-021`/`US-022`).

| Historia | Criterio de aceptación (verbatim) | Ítems del plan que lo cubren | Cómo se verifica |
|---|---|---|---|
| `US-023` | "Page title `Approval Queue`, 24px/600, 24px below." | #5 | Inspección visual §6 paso 2 (contra `10-manager-queue.png`) |
| `US-023` | "One card per request per §3.3: employee name, then `{type} · {start} → {end}` in 13px secondary; `Reject` (outlined, danger) and `Approve` (solid, success) on the right, in that order." | #1 (los datos), #3 (los dos estilos de botón), #4 (la card con el orden Reject→Approve) | Inspección visual §6 paso 2; `typecheck` fija la forma de los datos |
| `US-023` | "A top-bordered block below shows the full reason at 14px." | #4 | Inspección visual §6 paso 2 (razón larga en seed/creada ad hoc) |
| `US-023` | "Cards ordered most recent first; the action group does not shrink on narrow viewports." | #5 (orden heredado del servidor, sin reordenar), #4 (`flex-shrink: 0` en el grupo de acciones) | §6 paso 3 (dos solicitudes con orden conocido) + paso 7 (viewport estrecho) |
| `US-023` | "Given a completed decision, when it returns, then the list reloads from the API and the request leaves the queue." | #2 (`approveRequest`/`rejectRequest`/`listRequests`), #5 (re-fetch tras `204` — `FR-UIX-005`; el servidor ya expulsa lo no-`Submitted`, probado en `US-021`/`US-022`) | §6 pasos 4–5 |
| `US-023` | "Given a failed decision, when the error returns, then it appears in an error banner and the request stays in the queue." | #2 (`ApplicationError` con `{ code, message }`), #5 (`Banner` variant `'error'` — primer llamador de la variante ya escrita) | §6 paso 6 (provocando `VF-DEC-005` real con dos pestañas) |

**Conteo: 6 criterios de entrada · 6 cubiertos.** Además esta historia **salda la deuda nominal** de los banners de `US-021 AC1` (`Request approved.`) y `US-022 AC1` (`Request rejected.`), anotada en ambos planes como *"infraestructura `US-031`, disparo `US-023`"* — el disparo llega aquí (`D3`).

---

## 5. Supuestos y decisiones

Sesión de planificación delegada (Fase 3 no interactiva): decisiones de arquitecto documentadas con su reversibilidad. **La única que merece ratificación del usuario está elevada a §7 (`OQ-A`).**

| # | Decisión | Justificación | Impacto si es incorrecta |
|---|---|---|---|
| `D1` | **Cero cambios en backend** | Verificado archivo por archivo en `main` (`c25b3b8`): endpoints, contratos, handler, errores y orden del listado ya cubren todo lo que la pantalla necesita (§1.1). El propio contrato documenta cómo derivar la cola | N/A — hecho del código |
| `D2` | **Los botones deciden directamente con `{ comment: null }` — sin modal ni input de comentario; `S-09` es íntegramente de `US-034`** | Tres fuentes convergen: (1) los criterios de `US-023` **no mencionan** comentario ni modal — solo botones, reload y banner de error; (2) `Backlog.md` v2.0 separó expresamente `US-034` (*"Given I press `Approve` or `Reject`, when it activates, then the `S-09` modal opens"* — la interceptación del clic es suya, con dependencia propia `US-030`) y la tabla de pantallas asigna `S-09 → US-034`; (3) el comentario es **opcional** por `FR-DEC-008` verbatim (*"optional for both approval and rejection"*) y por `US-022 AC2`, de modo que `comment: null` es una decisión válida y completa hoy. La frase de `FR-UIX-014` *"each allowing an optional comment before confirmation"* traza a la pareja `US-023`+`US-034` en conjunto: la porción "optional comment before confirmation" la entrega `US-034` (sus *Traces* son exactamente `AC-11`, `FR-DEC-008`) | Ninguno estructural: `US-034` inserta el modal entre el `onClick` y la llamada existente (`approveRequest(id, comment)` ya acepta el parámetro desde el día uno — ítem #2) sin tocar contrato ni endpoint |
| `D3` | **Esta historia dispara los banners de éxito §3.5 (`Request approved.` / `Request rejected.`) usando el componente `Banner` existente** | Los planes de `US-021` (`D9`, trazabilidad `AC1`) y `US-022` (`D3`, trazabilidad `AC1`) difirieron esos textos con dueñas *"infraestructura `US-031`, disparo `US-023`"* — el disparo es de aquí. El componente ya existe y ya renderiza éxitos en `/requests`; no disparar el banner dejaría la decisión silenciosa (contra `NFR-USA-002`/`FR-UIX-003` en espíritu) teniendo el componente a un import. La matriz Given/When/Then del banner (fade, clear-on-navigate) sigue siendo de `US-031` | Si el usuario prefiere diferir también el disparo a `US-031`, se elimina una línea por rama de éxito — trivialmente reversible |
| `D4` | **La cola se deriva en cliente por `employee.id !== me.id` (vía `GET /auth/me`), sin discriminador nuevo en el contrato** | No es una decisión nueva: es la ratificación de `US-020` `D3`, escrita en el doc-comment de `RequestSummaryResponse` como instrucción literal para esta historia. La porción ajena del payload de un manager es exactamente las `Submitted` del equipo (por construcción del handler — verificado), así que no hace falta filtrar además por `state` (hacerlo sería inofensivo pero redundante) | Si en el futuro el payload cambiara, `US-020` dejó anotado que un `isOwn` sería aditivo — no bloquea nada hoy |
| `D5` | **Estados de carga y de cola vacía mínimos y textuales (sin skeleton §3.3 ni empty-state dashed)** | `US-032` es la dueña explícita de ambos patrones **nombrando `S-07`** (*"two 96px blocks on `S-07`"*, *"a manager with an empty queue… empty state… no action button"*). Construirlos aquí duplicaría la historia; dejar un área en blanco violaría `FR-UIX-004` (SHOULD). Punto medio: un texto plano de carga y otro de vacío, que `US-032` reemplaza por los patrones §3.3 | Ninguno — `US-032` sobrescribe exactamente esos dos bloques |
| `D6` | **Los botones de decisión se deshabilitan mientras hay una decisión en vuelo** | Evita el doble-submit desde UI (el backend ya lo tolera: la segunda llegaría a `VF-DEC-005`/`409` — carrera probada en `US-021`); `btn-primary:disabled` ya sienta el precedente de estilo. Alcance mínimo: un flag por página, no por card | Si se prefiere granularidad por card, es un cambio local del ítem #5 |
| `D7` | **Sin tests automatizados de frontend; la verificación de UI es lint + typecheck + depcruise + build + E2E manual (§6)** | Verificado en `src/web/package.json`: no existe runner (ni Jest, ni Vitest, ni Playwright) ni precedente en ninguna historia web previa (`US-013`, `US-017`). Introducir la infraestructura de testing de frontend es una decisión de proyecto que excede una historia de pantalla (`TC-06`) | Si el usuario quiere estrenar runner aquí, se añade como ítem previo — ampliación, no corrección |
| `S1` | **La rama se crea desde `main` (`c25b3b8`) directamente** | Verificado: `US-022` (PR #19) mergeada; los criterios dependen solo de código ya en `main` | Ninguno |

---

## 6. Verificación end-to-end

Con la API corriendo y `npm run dev` en `src/web/`, seed §3.6 (Laura manager; Ana y Carlos asignados a ella).

| # | Paso | Resultado esperado |
|---|---|---|
| 1 | `cd src/web && npm run lint && npm run typecheck && npm run depcruise && npm run build` · `dotnet build` + `dotnet test` | Todo verde, 0 warnings; backend sin cambios |
| 2 | Sign in como Carlos → crear y someter una solicitud con razón larga. Sign in como Laura → navegar a `/queue` — **`AC1`–`AC3`** | Título `Approval Queue` 24px/600; card con `Carlos …` 15px/600, `{tipo} · {start} → {end}` 13px secundario, `Reject` outlined danger y `Approve` sólido success en ese orden a la derecha; bloque top-bordered con la razón completa a 14px — estructura contra `10-manager-queue.png` |
| 3 | Someter una segunda solicitud (Ana) y recargar `/queue` — **`AC4`** | La de Ana (más reciente) aparece primero |
| 4 | `Approve` sobre la de Ana — **`AC5`** + deuda `US-021 AC1` | Botones deshabilitados durante el vuelo; a la vuelta la lista se recarga desde la API, la card de Ana desaparece y el banner success lee `Request approved.` |
| 5 | `Reject` sobre la de Carlos — **`AC5`** + deuda `US-022 AC1` | Ídem; banner `Request rejected.` |
| 6 | Dos pestañas como Laura sobre la misma solicitud `Submitted`: aprobar en una, luego rechazar en la otra (sin recargar) — **`AC6`** | La segunda recibe `409` `VF-DEC-005`; banner de error con `This request has already been decided.` en paleta error; la card permanece hasta el siguiente reload |
| 7 | Viewport estrecho (≈ 400px) sobre una card — **`AC4`** | La cabecera envuelve (`flex-wrap`) pero el grupo `Reject`/`Approve` no se encoge (`flex-shrink: 0`) |
| 8 | Sign in como Carlos (Employee) → navegar a `/queue` a mano | Cola derivada vacía (su payload solo trae propias); texto de vacío mínimo (`D5`); ninguna acción ofrecida (`FR-UIX-002`) |
| 9 | La propia solicitud `Submitted` de Laura (creada por ella) | **No** aparece en `/queue` (filtro `employee.id`); sí viaja en su payload — la distinción prescrita por el contrato funciona |
| 10 | Visita a `/queue` sin sesión | Primer `GET` devuelve `VF-AUT-004` → redirect a `/sign-in` (`FR-UIX-007`, ya resuelto en `lib/api.ts`) |

---

## 7. Riesgos y preguntas abiertas

> ⚠️ **`OQ-A` — Pregunta abierta para el usuario (no bloquea ningún ítem; afecta solo cómo se llega a la página en la demo):**
> El tab de navegación `Approval Queue` pertenece a `US-030` (shell S-03), de la que `US-023` **no** depende según el backlog. Hasta que `US-030` llegue, ¿cómo se accede a `/queue`?
>
> - **(a) — recomendada — Solo por URL directa (`/queue`).** Cero artefactos provisionales que `US-030` tendría que retirar; los criterios de `US-023` no piden navegación; el DoD ("demonstrable in the running application") se satisface navegando a la URL. Mismo espíritu que el placeholder de S-04.
> - **(b) Enlace provisional en `/requests` visible solo para managers.** Mejora la demo, pero exige `getMe` en el placeholder de S-04 (que `US-024` va a reescribir entero) y crea trabajo desechable doble.
>
> **El plan asume (a) salvo indicación contraria.** Elegir (b) añade un ítem pequeño sobre `app/(app)/requests/page.tsx`.

| Riesgo | Mitigación |
|---|---|
| `FR-UIX-014` menciona *"optional comment before confirmation"* y esta historia decide sin comentario | `D2` con las tres citas: los criterios de `US-023` no lo piden, `US-034` es la dueña declarada del modal (sus *Traces*: `AC-11`, `FR-DEC-008`) y el comentario es opcional por norma — la decisión sin comentario es válida hoy y el modal se inserta después sin retocar nada de esta historia (`approveRequest`/`rejectRequest` ya aceptan `comment` desde el ítem #2) |
| Doble clic / decisiones concurrentes desde la UI | `D6` (deshabilitar en vuelo) + el backend ya convierte la carrera en `VF-DEC-005` (probado en `US-021`) — el peor caso es el banner de error de `AC6`, que es comportamiento especificado |
| Tentación de construir skeleton/empty-state §3.3 "ya que estamos" | `D5`: son criterios verbatim de `US-032` con `S-07` nombrado — construirlos aquí es duplicar otra historia |
| La página queda "huérfana" de navegación hasta `US-030` | Es la secuencia declarada por el propio backlog (§ *Increment 3*: el shell se construye una vez y las pantallas "drop into an existing frame"); `OQ-A` deja la elección explícita |
| Sin tests de frontend, `AC5`/`AC6` solo se demuestran manualmente | `D7` + §6 pasos 4–6; la semántica de datos de ambos criterios ya está probada de punta a punta en las suites backend de `US-020`–`US-022` |

---

Siguiente paso — implementación:

```
/user-story-implement ".claude/implementation plans/US-023-plan.md"
```
