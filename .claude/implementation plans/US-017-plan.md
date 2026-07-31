# Plan de implementación — `US-017` · Request form screen

| Campo | Valor |
|---|---|
| Historia | `US-017` — Request form screen |
| Épica | `EP-05` — Request authoring |
| Prioridad · Talla | **Must** · `M` (talla del backlog; ver riesgo de talla en §7 — esta historia además **funda la aplicación web**) |
| Pantalla | `S-05` — Request form, create and edit ([`06-new-request-form.png`](../../docs/prototype/screenshots/06-new-request-form.png)) |
| Depende de | `US-015` (**mergeada en `main`**) · `US-016` (**rama `feat/us-016-edit-draft-request` empujada; PR pendiente de crear/mergear** — ver `D9`) |
| Trazas | `Backlog.md` §EP-05 `US-017`, §3.1–§3.5, §7 filas 8–9 · `FRD.md` §8 (`FR-UIX-002`–`FR-UIX-007`), §6.2, §6.3, §7 · `SAD.md` §9 (`ADR-009`, `ADR-013`), §8.5, §17 · `NFR-USA-004`–`007` · deuda de UI de `US-014-plan` `D5`, `US-015-plan` `D9`, `US-016-plan` `D8` |
| Fuentes | `Backlog.md` v2.0 · `SAD.md` v2.0 · `FRD.md` · código real verificado en `src/` (rama `feat/us-016-edit-draft-request`) |
| Repositorio | `C:\Users\avald\OneDrive\Work\VacaFlow\repositories\vacaflow` |
| Rama sugerida | `feat/us-017-request-form-screen`, creada **desde `feat/us-016-edit-draft-request`** (la dependencia `US-016` aún no está en `main` — ver `D9`) |
| Estado | Aprobado el 2026-07-30 (decisiones de arquitecto documentadas en §5; sesión sin interlocutor humano) |

> **Este documento no implementa nada.** La implementación la ejecuta `/user-story-implement`.

---

## 1. Entendimiento

### 1.1 Contexto — por qué esta historia es distinta

`US-017` es la **primera historia del proyecto que toca la superficie web**. Verificado contra el árbol real: `src/web/` **no existe**; los únicos proyectos son los cuatro backend (`Domain`, `Application`, `Infrastructure`, `Api`). Los planes de `US-014`, `US-015` y `US-016` verificaron lo mismo y difirieron **toda** su superficie visual a esta historia. En consecuencia, `US-017` tiene dos entregas inseparables:

1. **Fundacional** — crear la aplicación web (`src/web/`) con el stack que `SAD.md` ya decidió: **Next.js (App Router) + TypeScript**, proxy de `/api` hacia el API .NET (`ADR-009` — un solo origen, cookie first-party, **cero CORS**), frontera única `lib/api.ts` como el único módulo que llama al servidor (`ADR-013`), y las reglas de frontend validadas con `dependency-cruiser` (`SAD.md` §9.3). El stack **no se inventa aquí: ya está comprometido por el SAD** (§3 "web (Next.js)", §4.1 `src/web/`, §9.2 estructura completa, §17 `npm run dev`).
2. **Funcional** — la pantalla `S-05`: un único formulario para crear y editar borradores, con sus tres modos (`New request` · `Edit draft` · `Request detail` deshabilitado), consumiendo los endpoints reales ya implementados.

> **Nota de fuentes.** El documento `docs/reglas-diseno-uiux-web.md` referido en conversaciones previas **no existe en el repositorio** (verificado por glob). Las reglas UI/UX vigentes son `Backlog.md` §3 (tokens, patrones, microcopy — autoritativo para toda cadena de texto) y `FRD.md` §8 (`FR-UIX-*`). El prototipo `docs/prototype/VacaFlow.dc.html` es autoritativo para layout e interacción; su copy español **no** se replica (`Backlog.md` §2).

#### Grounding — API real verificada (rama `feat/us-016-edit-draft-request`)

El formulario integra contra **este** contrato, leído del código, no del FRD:

| Endpoint real | Auth | Éxito | Contrato |
|---|---|---|---|
| `POST /api/auth/login` | anónimo | `200` + `AuthenticatedUserResponse(Id, FullName, Email, Role)` + cookie `VacaFlow.Session` (`HttpOnly`, `SameSite=Lax`, 8 h sliding) | `SignInContract(Email, Password)` |
| `POST /api/auth/register` | anónimo | `201` + mismo response + cookie | `RegisterAccountContract(FullName, Email, Password, Role)` |
| `POST /api/auth/logout` | cookie | `204` | — |
| `GET /api/auth/me` | cookie | `200` + `AuthenticatedUserResponse` | — |
| `GET /api/absence-types` | cookie | `200` + `AbsenceTypeResponse(Id, Code, Name)[]` (solo activos) | — |
| `POST /api/requests` | cookie | `201` + `Location: /api/requests/{id}` + cuerpo `{ id }` (`ADR-012`) | `CreateRequestContract(AbsenceTypeId?, StartDate?, EndDate?, Reason?)` |
| `PUT /api/requests/{id:guid}` | cookie | `204` sin cuerpo (`ADR-012`) | `UpdateRequestContract(AbsenceTypeId?, StartDate?, EndDate?, Reason?)` |

Errores: **siempre** `{ code, message, field? }` (`TE-005`); `401` devuelve JSON `VF-AUT-004` (no redirect — `Program.cs` `OnRedirectToLogin`); `FallbackPolicy` exige sesión en todo endpoint que no opte por `AllowAnonymous`. Los códigos que este formulario pintará: `VF-VAL-001` (con `field`), `VF-REQ-001` (`field: "endDate"`), `VF-REQ-002` (`field: "startDate"`), `VF-CAT-001`, `VF-REQ-003` (409), `VF-REQ-004` (403), `VF-REQ-006` (404), `VF-AUT-004` (401).

**Hueco detectado (decisión mayor de esta historia):** no existe **ningún `GET` de solicitudes** — ni lista (`US-020`/`US-024`) ni detalle (`US-025`). El modo edición de `S-05` necesita cargar el borrador (tipo, fechas, reason, **estado** — el criterio "not a `Draft` → controles deshabilitados" es indecidible sin leer el estado). `IRequestRepository.GetByIdAsync` **ya existe** (creado por `US-016`). Esta historia añade el mínimo endpoint de lectura `GET /api/requests/{id}` restringido al dueño — ver `D3`.

Otras piezas verificadas: puerto de API en desarrollo `http://localhost:5217` (`launchSettings.json`) — destino del proxy; `AuthenticatedUserResponse` documenta ya en su doc-comment que sirve a `/me`; los tests funcionales (`VacaFlowApiFactory`) fijan el patrón para el endpoint nuevo; `TreatWarningsAsErrors` activo en el backend.

#### Deuda de UI acumulada que esta historia debe saldar

Cross-referencia obligatoria — cada plan previo difirió aquí, nominalmente, estos fragmentos:

| Origen | Fragmento diferido | Dónde lo cierra este plan |
|---|---|---|
| `US-014-plan` `D5` | El `<select>` `Absence type` poblado desde `GET /absence-types`, "never hardcoded" (`AC2`) | Ítem #13 (`RequestForm`) |
| `US-014-plan` `D5` | Primera opción: placeholder deshabilitado `Select…` (`AC3`) | Ítem #13 |
| `US-015-plan` `D9` | El formulario de creación completo (`S-05`) | Ítems #13, #17 |
| `US-015-plan` `D9` | Retorno a `S-04` tras crear + banner `Draft created.` | Ítems #12, #16, #17 (ver `D5` — `S-04` es aún un placeholder) |
| `US-015-plan` `D9` | Mensajes de validación pintados "beneath that field" desde `{ code, message, field? }` | Ítem #13 |
| `US-016-plan` `D8` | Modo edición del formulario (cargar el draft y guardar) | Ítems #14, #18 |
| `US-016-plan` `D8` | Banner `Changes saved.` | Ítems #12, #16, #18 |
| `US-016-plan` `D8` | Título `Edit draft` · botón `Save changes` | Ítems #13, #18 |
| `US-016-plan` `D8` | Estado deshabilitado cuando la solicitud no es `Draft` (botón secundario `Back`, sin botón primario) | Ítems #13, #18 |
| `US-016-plan` `D8` | Errores de operación (`VF-REQ-003/004/006`) sin `field` pintados en el alert general de la tarjeta | Ítem #13 |

Lo que **no** entra pese a estar cerca (queda con su historia): botón `Edit` en la fila de la lista (`US-024`), pantalla `S-04` real (`US-024`), shell completo `S-03` (`US-030`), banner con toda su matriz de criterios (`US-031`), skeletons y empty states (`US-032`) — ver §1.4 y `D4`–`D6`.

### 1.2 Narrativa (verbatim)

> "As an employee, I want one form for creating and editing, so that the experience is consistent."

### 1.3 Criterios de aceptación — verbatim (`Backlog.md` §EP-05 · `US-017`)

El backlog formula esta historia como criterios visuales sobre `S-05`:

| # | Criterio (**Visual — `S-05`**) |
|---|---|
| `AC1` | "Header row: a `←` back button with `aria-label="Back to my requests"`, then the title — `New request`, `Edit draft` or `Request detail`." |
| `AC2` | "White card, max-width 560px, 32px padding, fields with an 18px gap." |
| `AC3` | "Order: `Absence type` select · a row with `Start date` and `End date` side by side, each min-width 180px, wrapping on narrow viewports · `Reason` textarea, 4 rows, `maxlength=500`, vertically resizable." |
| `AC4` | "`Start date` carries `min` set to today; `End date` carries `min` set to the chosen start date. This is an affordance — the API validates regardless." |
| `AC5` | "The `Reason` label row shows a live `N/500` counter, right-aligned, 12px secondary." |
| `AC6` | "Action row 28px below the card content: primary `Save draft` or `Save changes`, then a secondary `Cancel`." |
| `AC7` | "A general error, when present, renders in an alert block at the top of the card." |
| `AC8` | "Given a request that is not a `Draft`, when the form opens, then every control is disabled, the primary save button is absent and the secondary button reads `Back`." |

Además, esta historia hereda como criterios propios los fragmentos de UI diferidos (tabla de §1.1), cuyos verbatim originales están en los planes citados; los dos operativos centrales son:

- `US-015` `AC1` (fragmento): "…I return to `S-04` and the banner reads `Draft created.`"
- `US-016` `AC1` (fragmento) + nota Visual: "…the banner reads `Changes saved.`" · "the form title reads `Edit draft`; the primary button reads `Save changes`."

Microcopy vinculante (`Backlog.md` §3.5): labels `Absence type` / `Start date` / `End date` / `Reason`; placeholder `Select…`; contador `N/500`; botones `Save draft` · `Save changes` · `Cancel` · `Back`; `aria-label="Back to my requests"`; mensajes de validación y catálogo de errores §7 del FRD tal como los emite el API.

### 1.4 Alcance

**Entra**

- **Bootstrap de `src/web/`**: Next.js (App Router) + TypeScript, proxy `/api` → `http://localhost:5217` (`ADR-009`), tokens de diseño de `Backlog.md` §3.1 como CSS custom properties, fuentes IBM Plex (ver `D2`), `.gitignore` para `node_modules`/`.next`.
- **Frontera `lib/api.ts`** (único módulo con `fetch`), `lib/types.ts` (espejos de los contratos reales), `lib/session.ts` (transporte del mensaje de banner entre navegaciones) — estructura literal de `SAD.md` §9.2.
- **Reglas de frontend**: `.dependency-cruiser.js` + regla de lint que prohíbe `fetch` fuera de `lib/api.ts` (`SAD.md` §9.3).
- **`S-05` completo**: componente `RequestForm` dual (crear/editar) con los tres títulos, select poblado del API, afordances de fechas, contador, errores por campo y alert general, modo deshabilitado no-`Draft`.
- **Rutas**: `(auth)/sign-in` mínima funcional (ver `D6`), `(app)/layout.tsx` mínimo con slot de banner (ver `D4`), `(app)/requests` placeholder de aterrizaje (ver `D5`), `(app)/requests/new`, `(app)/requests/[id]`.
- **Banner mínimo** (`components/feedback/Banner.tsx`) para `Draft created.` / `Changes saved.` y para errores de operación fuera del formulario (ver `D4`).
- **Backend mínimo de lectura**: `GET /api/requests/{id}` restringido al dueño (`GetRequestByIdHandler` + `RequestDetailResponse`), con sus tests — ver `D3`.

**No entra**

| Excluido | Por qué / destino |
|---|---|
| Shell `S-03` real (header con wordmark, nav tabs, nombre/rol, `Sign out`) | `US-030`. El `(app)/layout.tsx` de esta historia es un contenedor mínimo (ancho 1100px, slot de banner) que `US-030` reemplaza/enriquece |
| Matriz completa del banner (fade 150ms, limpiar al navegar, paleta de error, criterios Given/When/Then) | `US-031`. Aquí el banner nace con lo imprescindible para los dos mensajes de éxito heredados; `US-031` lo completa y verifica formalmente |
| Lista `S-04` real (filas, badges, acciones por estado) y empty state `S-04b` | `US-024` / `US-032`. Aquí `/requests` es un placeholder honesto con el botón `New request` (necesario para navegar al formulario y aterrizar el banner) |
| Skeletons de carga | `US-032` |
| Pantallas `S-01`/`S-02` con sus criterios visuales | `US-013` / `US-012`. El sign-in de esta historia es scaffolding funcional mínimo (ver `D6`) |
| `GET /requests` (lista servidor, filtro por rol) | `US-020` |
| Detalle `S-06` con bloque `DECISION` | `US-025`. El modo deshabilitado de `S-05` (`AC8`) cubre el interín para solicitudes no-`Draft` abiertas desde el formulario |
| `Submit`/`Cancel` de solicitudes, modales | `US-018`, `US-019`, `US-033` |
| Tests unitarios de componentes React (Jest/Testing Library/Playwright) | Ningún documento del set los exige para el web (la pirámide de `SAD.md` §14 es backend; §9.3 exige `depcruise` + lint). Añadir un framework de test de UI es decisión de proyecto que no toma esta historia — ver `D8` |
| CORS | Innecesario por diseño: proxy same-origin (`ADR-009`) |

---

## 2. Cambios estructurales / de base

Sin cambios de esquema de base de datos ni migraciones (el `GET` nuevo solo lee columnas existentes). Los cambios estructurales de esta historia son de **repositorio y tooling**:

1. **Proyecto nuevo `src/web/`** (Next.js + TypeScript). No entra en `VacaFlow.slnx` — no es un proyecto MSBuild; se construye con `npm`. Dependencias nuevas: `next`, `react`, `react-dom`, `typescript` y dev-deps (`eslint`, `eslint-config-next`, `dependency-cruiser`). Ninguna librería de UI ni de estado (ver `D2`).
2. **`next.config.mjs`** con el rewrite `/api/:path*` → `http://localhost:5217/api/:path*` (`ADR-009`; puerto real de `launchSettings.json`). La cookie `VacaFlow.Session` queda first-party; `SameSite=Lax` funciona sin tocar el API.
3. **`.gitignore`** raíz: añadir `node_modules/`, `src/web/.next/` (coherente con `US-029`: el ZIP no lleva `node_modules` ni `.next`).
4. **`.dependency-cruiser.js` + regla ESLint** (`no-restricted-globals` para `fetch` fuera de `lib/api.ts`) — `SAD.md` §9.3.
5. **Backend**: un handler, un DTO, un response record y un `MapGet` nuevos (ítems #1–#5). Sin cambios de configuración, variables de entorno, permisos ni feature flags.

---

## 3. Plan ordenado por dependencia

De adentro hacia afuera. El tramo backend (#1–#6) precede al web porque `lib/api.ts` se escribe contra endpoints reales. Prosa en español, identificadores en inglés.

| # | Capa | Acción | Artefacto | Notas |
|---|---|---|---|---|
| 1 | Application | Crear | `src/BigSolutions.VacaFlow.Application/Requests/RequestDetailDto.cs` | `public sealed record RequestDetailDto(Guid Id, Guid AbsenceTypeId, DateOnly StartDate, DateOnly EndDate, string Reason, string State)` — mapeo explícito a mano desde el agregado (`CA-APP-006`, `CA-APP-011`); `State` como `ToString()` del enum (coincide con los labels de §3.4) |
| 2 | Application | Crear | `src/BigSolutions.VacaFlow.Application/Requests/GetRequestByIdHandler.cs` | `public sealed class GetRequestByIdHandler(ICurrentUser currentUser, IRequestRepository requests)` → `Task<Result<RequestDetailDto>> Handle(Guid requestId, CancellationToken ct)`: (1) `requests.GetByIdAsync(new RequestId(requestId), ct)` — **reutiliza el puerto existente de `US-016`, sin crecerlo**; (2) null → `RequestErrors.NotFound` (`VF-REQ-006`); (3) `request.OwnerId != currentUser.EmployeeId` → `RequestErrors.NotOwner` (`VF-REQ-004`, `RULE-04` — ver `D3` para el alcance dueño-solo); (4) mapear a DTO. Query sin `IUnitOfWork` (no muta) y sin command record (`ADR-011` aplica a commands; el único input es el id de la ruta — precedente: `GetCurrentUserHandler` sin command) |
| 3 | Application | Modificar | `src/BigSolutions.VacaFlow.Application/DependencyInjection.cs` | `services.AddScoped<GetRequestByIdHandler>();` |
| 4 | API | Crear | `src/BigSolutions.VacaFlow.Api/Contracts/RequestDetailResponse.cs` | `public sealed record RequestDetailResponse(Guid Id, Guid AbsenceTypeId, DateOnly StartDate, DateOnly EndDate, string Reason, string State)` — espejo campo a campo del DTO, nunca la entidad (`CA-PRE-003`) |
| 5 | API | Modificar | `src/BigSolutions.VacaFlow.Api/Endpoints/RequestEndpoints.cs` | En el grupo `/api/requests`: `group.MapGet("/{id:guid}", ...)` → `GetRequestByIdHandler`, `result.ToOkResult(ToResponse)` (extensión existente) → `200`. **`.RequireAuthorization()` explícito** (test `Every_Endpoint_Should_State_Its_Authorization_Explicitly`). Errores ya mapeados en `ErrorStatusMap` por `US-016` (`VF-REQ-004`→403, `VF-REQ-006`→404) — **`ErrorStatusMap` no se toca**. Nota: la `Location` del `POST` (`/api/requests/{id}`) por fin resuelve a un recurso real |
| 6 | Test | Crear/Modificar | `tests/BigSolutions.VacaFlow.Application.UnitTests/Requests/GetRequestByIdHandlerTests.cs` · ampliar `tests/BigSolutions.VacaFlow.Api.FunctionalTests/Endpoints/RequestEndpointTests.cs` | Unit (fakes existentes): (a) dueño → DTO con todos los campos y `State = "Draft"`; (b) id inexistente → `VF-REQ-006`; (c) otra identidad → `VF-REQ-004`. Funcional (`VacaFlowApiFactory`): (d) crear draft real por `POST` y `GET` del `Location` → `200` con cuerpo completo; (e) segunda cuenta → `403`; (f) Guid aleatorio → `404`; (g) sin cookie → `401` `VF-AUT-004` |
| 7 | Web | Crear | `src/web/` — scaffold Next.js: `package.json`, `tsconfig.json`, `next.config.mjs`, `app/layout.tsx` (raíz), `app/globals.css` | App Router + TypeScript, sin Tailwind (ver `D2`). `next.config.mjs`: `rewrites()` → `{ source: '/api/:path*', destination: 'http://localhost:5217/api/:path*' }` (`ADR-009`). `globals.css`: custom properties con los tokens **literales** de `Backlog.md` §3.1 (`--color-bg: oklch(98% 0.004 250)`, `--color-accent: oklch(52% 0.15 260)`, radios, focus ring 2px accent offset 2px…). Fuentes `IBM Plex Sans`/`IBM Plex Mono` vía `next/font/google` (self-hosted en build). Layout raíz aplica fondo, fuente y color de texto |
| 8 | Web | Modificar | `.gitignore` (raíz del repo) | `node_modules/` · `src/web/.next/` · `src/web/next-env.d.ts` según convención |
| 9 | Web | Crear | `src/web/lib/types.ts` | Espejos TS de los contratos **reales** de §1.1: `AuthenticatedUser`, `AbsenceType { id, code, name }`, `RequestDetail { id, absenceTypeId, startDate, endDate, reason, state }`, `RequestPayload { absenceTypeId, startDate, endDate, reason }`, `ApiError { code, message, field? }` (camelCase — serialización JSON por defecto de ASP.NET Core, verificada en los tests funcionales) |
| 10 | Web | Crear | `src/web/lib/api.ts` | **El único módulo que llama `fetch`** (`ADR-013`, `SAD.md` §9.3). Helper interno que fija `credentials: 'same-origin'`, `Content-Type: application/json`, parsea `{ code, message, field? }` en fallo y lo lanza/devuelve tipado como `ApiError`. Funciones: `signIn(email, password)` → `AuthenticatedUser` · `listAbsenceTypes()` → `AbsenceType[]` · `createRequest(payload)` → `{ id }` (lee el cuerpo `{ id }` del `201`) · `updateRequest(id, payload)` → `void` (`204`) · `getRequest(id)` → `RequestDetail`. Ante `401` (`VF-AUT-004`): redirige a `/sign-in` (`FR-UIX-007`) — ver `D7` |
| 11 | Web | Crear | `src/web/lib/session.ts` | Transporte del mensaje de banner entre navegaciones (`sessionStorage`): `setPendingNotification(message)` / `consumePendingNotification()`. Es lo que permite "I return to `S-04` and the banner reads `Draft created.`" sin estado global ni librería (ver `D4`) |
| 12 | Web | Crear | `src/web/components/feedback/Banner.tsx` | Banner de §3.3 en su forma mínima: `role="status"`, paleta success (`oklch(93% 0.06 150)` / `oklch(30% 0.12 150)`) y error, botón `×` con `aria-label="Dismiss notification"`. Los criterios Given/When/Then completos del banner los verifica `US-031`; aquí nace el componente para no escribirlo dos veces (§5 `D4`) |
| 13 | Web | Crear | `src/web/components/requests/RequestForm.tsx` | **El corazón de la historia.** Client component con props `{ mode: 'create' } \| { mode: 'edit', initial: RequestDetail }`. Cubre: tarjeta blanca 560px/32px, gap 18px (`AC2`); orden select → fila de fechas (min-width 180px, wrap) → textarea 4 filas `maxLength=500` redimensionable (`AC3`); select poblado con `listAbsenceTypes()` al montar, **nunca hardcodeado**, primera opción `<option value="" disabled>Select…</option>` (deuda `US-014`); `min` de `Start date` = hoy y `min` de `End date` = start elegido (`AC4` — afordance; el API valida igual); contador vivo `N/500` alineado a la derecha, 12px secundario (`AC5`); fila de acciones a 28px: primario `Save draft` (create) / `Save changes` (edit) + secundario `Cancel` (`AC6`); submit → `createRequest`/`updateRequest`; errores con `field` pintados bajo su campo con `role="alert"`, 13px, `oklch(50% 0.18 25)` (`FR-UIX-006`, patrón §3.3 "Inline field error"; mapeo `field` → control por nombre camelCase del API: `absenceTypeId`, `startDate`, `endDate`, `reason`); errores **sin** `field` (`VF-REQ-003/004/006`, `VF-CAT-001`) en el alert block al tope de la tarjeta (`AC7`); botón primario deshabilitado mientras guarda (patrón `US-007`); **modo deshabilitado**: si `mode === 'edit'` y `initial.state !== 'Draft'` → todos los controles `disabled`, sin botón primario, secundario `Back` (`AC8`). Labels visibles `for`/`id` en todos los controles (`FR-UIX-006`, `NFR-USA-006`). Éxito → `setPendingNotification('Draft created.' \| 'Changes saved.')` + navegación a `/requests` |
| 14 | Web | Crear | `src/web/components/requests/RequestFormHeader.tsx` *(o inline en las páginas si resulta trivial)* | Header row de `S-05`: botón `←` con `aria-label="Back to my requests"` navegando a `/requests`, y título `New request` / `Edit draft` / `Request detail` (`AC1`) |
| 15 | Web | Crear | `src/web/app/(auth)/sign-in/page.tsx` | **Scaffolding funcional mínimo** (ver `D6`): email + password + botón `Sign in` llamando `signIn()`; éxito → `/requests`; error → mensaje del API. Sin pretensión de cumplir los criterios visuales de `S-01` — esos son de `US-013` y quedan trazados allí. Necesario porque sin cookie ninguna ruta `(app)` funciona |
| 16 | Web | Crear | `src/web/app/(app)/layout.tsx` | Contenedor mínimo del grupo autenticado: columna de contenido 1100px centrada, 32px padding, y el slot del `Banner` (consume `consumePendingNotification()` al montar la página destino). **No** es el shell `S-03` (sin header/nav/identidad) — ese es `US-030` (ver `D4`) |
| 17 | Web | Crear | `src/web/app/(app)/requests/page.tsx` | **Placeholder honesto de `S-04`** (ver `D5`): título `My Requests` + botón primario `New request` → `/requests/new`, y el `Banner` cuando hay notificación pendiente. Sin lista (llega con `US-024`/`US-020`). Es el destino de "I return to `S-04`" de la deuda de `US-015`/`US-016` |
| 18 | Web | Crear | `src/web/app/(app)/requests/new/page.tsx` · `src/web/app/(app)/requests/[id]/page.tsx` | `new`: header (`New request`) + `RequestForm mode='create'`. `[id]`: client page que llama `getRequest(id)`; título y comportamiento según estado — `Draft` → `Edit draft` + form editable; no-`Draft` → `Request detail` + form deshabilitado (`AC8`); `403`/`404` → alert general con el mensaje del API. Rutas literales de `SAD.md` §9.2 |
| 19 | Web | Crear | `src/web/.dependency-cruiser.js` + regla ESLint en la config de lint del scaffold | Las tres reglas de `SAD.md` §9.3 (`only-lib-api-may-fetch`, `components-do-not-import-pages`, `no-circular`) + `no-restricted-globals` para el identificador `fetch` fuera de `lib/api.ts`. Script `npm run depcruise` en `package.json` |
| 20 | Web | Modificar | `src/web/app/page.tsx` (raíz `/`) | Redirect a `/requests` (que a su vez cae a `/sign-in` sin sesión vía `D7`) — evita una home huérfana del scaffold |
| 21 | Test | Verificar | Suites backend completas + `npm run lint` + `npm run depcruise` + `npm run build` en `src/web` | Backend intacto en verde (arquitectura incluida — el handler nuevo es `sealed` y termina en `Handler`); el build de producción de Next.js compila sin errores de tipos; `depcruise` sin violaciones |

**Dependencias:** 1 → 2 → 3 · {1,2} → 4 → 5 → 6 · 7 → {8, 9, 19, 20} · 9 → 10 → {13, 15, 18} · 11 → {12, 13} · {10, 11} → 12 → {16, 17} · 13 → {14} → 18 · {5, 10} → 18 · todo → 21. **Paralelizable:** el tramo backend (#1–#6) y el bootstrap web (#7–#9) son independientes hasta #18 (la página `[id]` necesita el `GET` real). **Ruta crítica:** 7 → 9 → 10 → 13 → 18 → 21.

---

## 4. Casos de uso y tabla de trazabilidad

Casos de uso de Application implicados: `ListAbsenceTypesHandler` (existente, se consume), `CreateRequestHandler` (existente, se consume), `UpdateRequestHandler` (existente, se consume) y **uno nuevo**: `GetRequestByIdHandler` (leer una solicitud propia — `D3`). En el cliente, el caso de uso es único: *autorar un borrador con un solo formulario* en tres modos.

| Historia | Criterio de aceptación (verbatim) | Ítems del plan que lo cubren | Cómo se verifica |
|---|---|---|---|
| `US-017` | "Header row: a `←` back button with `aria-label="Back to my requests"`, then the title — `New request`, `Edit draft` or `Request detail`." | #14, #18 | Inspección visual + inspección del `aria-label` en el DOM (§6 pasos 6, 9, 12) |
| `US-017` | "White card, max-width 560px, 32px padding, fields with an 18px gap." | #7 (tokens), #13 | Visual contra `06-new-request-form.png` (layout; copy en inglés — `Backlog.md` §2) |
| `US-017` | "Order: `Absence type` select · a row with `Start date` and `End date` side by side, each min-width 180px, wrapping on narrow viewports · `Reason` textarea, 4 rows, `maxlength=500`, vertically resizable." | #13 | Visual + DOM (`maxLength`, `rows`, `resize: vertical`); wrap comprobado estrechando el viewport |
| `US-017` | "`Start date` carries `min` set to today; `End date` carries `min` set to the chosen start date. This is an affordance — the API validates regardless." | #13 (afordance) · la validación real ya existe en backend (`US-015`/`US-016`, intacta) | DOM: atributos `min`; y §6 paso 8 demuestra que el API rechaza igualmente un payload inválido |
| `US-017` | "The `Reason` label row shows a live `N/500` counter, right-aligned, 12px secondary." | #13 | Teclear y ver el contador avanzar (§6 paso 6) |
| `US-017` | "Action row 28px below the card content: primary `Save draft` or `Save changes`, then a secondary `Cancel`." | #13 | Visual en ambos modos (§6 pasos 6 y 9) |
| `US-017` | "A general error, when present, renders in an alert block at the top of the card." | #13 (errores sin `field` → alert block) | §6 paso 13 (`VF-REQ-003` real → alert general) |
| `US-017` | "Given a request that is not a `Draft`, when the form opens, then every control is disabled, the primary save button is absent and the secondary button reads `Back`." | #2/#5 (el `GET` expone `state`), #13, #18 | §6 paso 12: forzar `State = 1` por SQL, abrir `/requests/{id}` → controles `disabled`, sin primario, secundario `Back`, título `Request detail` |
| `US-014` (deuda `D5`) | "Given the request form, when it loads, then the `Absence type` select is populated from this endpoint and never hardcoded." | #10 (`listAbsenceTypes`), #13 | §6 paso 6: los tres tipos sembrados aparecen; DevTools muestra la llamada a `/api/absence-types` |
| `US-014` (deuda `D5`) | "Given the select, when it renders, then the first option is the disabled-value placeholder `Select…`." | #13 | DOM: `<option value="" disabled>Select…</option>` seleccionada por defecto en modo create |
| `US-015` (deuda `D9`) | "…then a request is created in `Draft` owned by the authenticated user, I return to `S-04` and the banner reads `Draft created.`" *(fragmento UI)* | #11, #12, #13, #16, #17 (aterrizaje en el placeholder de `S-04` — `D5`) | §6 paso 7: guardar → navegación a `/requests` + banner `Draft created.` |
| `US-015` (deuda `D9`) | "…then `VF-REQ-001` appears beneath `End date`." / "…`VF-REQ-002` appears beneath `Start date`." / "…the corresponding validation message appears beneath that field." *(fragmentos UI)* | #13 (mapeo `field` → mensaje bajo el control, `role="alert"`) | §6 paso 8: fechas invertidas → mensaje del catálogo bajo `End date`; campo vacío → su mensaje bajo el campo |
| `US-016` (deuda `D8`) | "…when I press `Edit` and save, then the type, dates and reason are updated and the banner reads `Changes saved.`" *(fragmento UI; el botón `Edit` de la fila es de `US-024`)* | #2, #5, #10, #13, #18 (cargar draft real, editar, guardar) + #11, #12, #16, #17 (banner) | §6 pasos 9–11: abrir `/requests/{id}` de un draft propio, ver valores precargados, cambiar, guardar → `/requests` + `Changes saved.` |
| `US-016` (deuda `D8`, nota Visual) | "the form title reads `Edit draft`; the primary button reads `Save changes`." | #13, #14, #18 | §6 paso 9 |

**Conteo: 8 criterios propios + 6 fragmentos de deuda heredada = 14 filas · 14 cubiertas.** Queda diferido con destino explícito (no es deuda de esta historia sino alcance de otras): botón `Edit` en la fila (`US-024`), matriz completa del banner (`US-031`), shell (`US-030`), lista y empty states (`US-024`/`US-032`), visuales de `S-01` (`US-013`).

---

## 5. Supuestos y decisiones

Sesión de planificación sin interlocutor humano (Fase 3 no interactiva): las ambigüedades se resolvieron con criterio de arquitecto y quedan documentadas con su reversibilidad. `D3`–`D6` son las de mayor calado; revisarlas primero si se re-planifica.

| # | Decisión | Justificación | Impacto si es incorrecta |
|---|---|---|---|
| `D1` | **Stack web: Next.js (App Router) + TypeScript en `src/web/`, sin Tailwind, sin librería de UI ni de estado; CSS plano con los tokens de §3.1** | El stack **no es elección de esta historia**: `SAD.md` lo compromete (§3, §4.1, §9.2 con rutas App Router literales, §17, `ADR-009`, `ADR-013`). Lo único que el SAD no fija es el styling: se elige CSS plano con custom properties porque los tokens de `Backlog.md` §3.1 son valores `oklch` literales y el prototipo usa estilos directos — una utility-library añadiría una capa de traducción sin requisito (`TC-06` en espíritu). Sin gestor de estado: el estado es local por pantalla y la app refetchea tras cada mutación (`FR-UIX-005`) | Cambiar el styling después es local a componentes; cambiar el framework sería re-litigar el SAD, no este plan |
| `D2` | **Fuentes IBM Plex vía `next/font/google`** (self-hosted en build, sin request en runtime) | §3.1 exige IBM Plex Sans/Mono con pesos concretos; `next/font` es el mecanismo nativo del stack ya decidido, sin dependencia nueva ni FOUC | Si el build sin red fuera un requisito (no lo es hoy), se cambia a `@fontsource/*` en npm — cambio local a `layout.tsx` |
| `D3` | **Se añade `GET /api/requests/{id}` (dueño-solo) en esta historia**: `GetRequestByIdHandler` + `RequestDetailResponse`, reutilizando `IRequestRepository.GetByIdAsync` existente | Sin lectura no hay modo edición ni `AC8` (decidir "not a `Draft`" exige leer `state`). `US-016-plan` §1.4 dejó explícitamente abierto de dónde sacaría los datos el formulario ("La UI de `US-017` decidirá con `US-020`"): se decide **no** esperar a `US-020` (lista con filtro por rol — problema distinto y mayor) y entregar el mínimo: por id, solo el dueño (`RULE-04`), errores ya mapeados (`VF-REQ-004`/`VF-REQ-006` existen en `ErrorStatusMap` desde `US-016` — cero cambios allí). El endpoint además hace resolvible por fin la `Location` que el `POST` emite desde `US-015`. `US-025` (detalle `S-06`) lo extenderá o reutilizará; `US-020` mantiene intacta su lista | Si `US-025`/`US-020` necesitan visibilidad de manager sobre el detalle, el cambio es aditivo en el handler (política de visibilidad), no un breaking change; el contrato `RequestDetailResponse` crecería campos (nombre del tipo, decisión) de forma también aditiva |
| `D4` | **El banner y el layout `(app)` nacen aquí en versión mínima; `US-030`/`US-031` los completan** | La deuda de `US-015`/`US-016` asigna los mensajes `Draft created.`/`Changes saved.` a esta historia — imposible sin un banner y un layout donde vivan. El backlog secuenciaba el shell (incremento 3) antes que las pantallas, pero el orden real de implementación llegó aquí primero; construir el shell completo dentro de `US-017` inflaría la historia y pisaría los criterios de `US-030`/`US-031`. Se aplica el patrón ya ratificado tres veces en sentido inverso (backend primero, UI después): se entrega el **mínimo verificable** y el resto queda trazado a su historia. El componente `Banner` se escribe una vez, per spec §3.3, para que `US-031` verifique en lugar de reescribir | `US-030` reemplaza el layout mínimo por el shell real — el formulario "drops into an existing frame" tal como el backlog quería, solo que el frame llega una historia después. Riesgo bajo: el layout mínimo no tiene API pública que romper |
| `D5` | **`/requests` es un placeholder honesto de `S-04`** (título + `New request` + banner), no la lista | "I return to `S-04`" necesita un destino de navegación **hoy**; la lista real exige `GET /requests` (`US-020`) y los criterios de `US-024`. Un placeholder con el botón de entrada al formulario es el mínimo que hace la historia demostrable de extremo a extremo sin robarle alcance a `US-024` | `US-024` sobreescribe el cuerpo de la página; la ruta y el aterrizaje del banner no cambian |
| `D6` | **Se incluye una página `/sign-in` mínima funcional, sin los criterios visuales de `S-01`** | Sin cookie no se puede ni abrir `S-05` (FallbackPolicy). Las alternativas — exigir un `curl` manual para obtener cookie, o construir `S-01` completa — son peores: la primera hace la historia indemostrable para un revisor (`Definition of Done`: "demonstrable in the running application"), la segunda se come `US-013`. El scaffolding queda marcado en el código como provisional con destino `US-013` | `US-013` reemplaza la página; `lib/api.signIn` ya queda escrito y se reutiliza tal cual |
| `D7` | **Ante `401` (`VF-AUT-004`), `lib/api.ts` redirige a `/sign-in`** (`FR-UIX-007`) | El API ya devuelve JSON `401` sin redirect (verificado en `Program.cs`); la redirección es responsabilidad del cliente y `FR-UIX-007` la exige ("Unauthenticated redirection · MUST"). Centralizarla en `lib/api.ts` evita repetirla por página. El matiz "with an explanation" (`SAD.md` §9.5) se completará cuando `US-031` defina el transporte de banners de error entre rutas | Si `US-013`/`US-031` prefieren un mensaje contextual, el cambio es local al helper |
| `D8` | **Sin framework de tests de UI en esta historia; la verificación web es lint + depcruise + build + verificación manual de §6** | Ningún documento del set exige tests de componentes para el web; la pirámide de `SAD.md` §14 es backend y §9.3 define exactamente qué se automatiza en el cliente (reglas de dependencia). Introducir Jest/Playwright es una decisión de proyecto con costo recurrente que ninguna historia pidió (`TC-06` en espíritu). El backend nuevo (#1–#5) **sí** lleva sus tests completos (#6) | Si el proyecto decide añadir tests de UI después, nada de esta historia lo obstaculiza — los componentes son funciones puras de props salvo `lib/api` |
| `D9` | **La rama parte de `feat/us-016-edit-draft-request`** (PR #de `US-016` aún sin crear en GitHub — sin `gh` en este entorno) | `US-017` depende de `US-016` (el `PUT` y `GetByIdAsync` del puerto). `main` no la contiene todavía (verificado: `git log main..feat/us-016-edit-draft-request` = 1 commit). Mismo patrón de apilado que `US-015-plan` `D10` | Si el PR de `US-016` se mergea antes de empezar, partir de `main` directamente; si `US-016` cambia en review, rebase — el solape de archivos es mínimo (`RequestEndpoints.cs`) |
| `D10` | **El título del modo deshabilitado es `Request detail`** y la navegación de `Back`/`←`/`Cancel` va siempre a `/requests` | `AC1` lista los tres títulos y `AC8` describe el modo; el tercer título solo tiene sentido en el modo deshabilitado (los otros dos están asignados por la nota Visual de `US-016`). `S-06` real (con bloque `DECISION`) es `US-025`; hasta entonces, el modo deshabilitado de `S-05` es la única vista de una solicitud no editable | `US-025` decidirá si `/requests/[id]` no-Draft pasa a renderizar `S-06`; la ruta no cambia |
| `D11` | **`Cancel` del formulario navega a `/requests` sin confirmación** | El botón `Cancel` de `S-05` es "abandonar el formulario", no "cancelar la solicitud" (eso es `US-019`/`US-033` con su modal). El prototipo no confirma el abandono del formulario y ningún criterio lo pide | Si producto quisiera confirmar el descarte de cambios, sería aditivo |
| `S1` | La serialización JSON del API es camelCase (`field: "endDate"`, cuerpo `{ id }`) y `DateOnly` viaja como `"yyyy-MM-dd"` | Comportamiento por defecto de ASP.NET Core minimal APIs, ya afirmado por los tests funcionales existentes (`code`, `field` en camelCase); `input[type=date]` produce exactamente `yyyy-MM-dd` — sin conversión en el cliente | Si algún caso no casara, el ajuste vive en `lib/api.ts`/`lib/types.ts`, un solo lugar |
| `S2` | El "today" de la afordance `min` es la fecha local del navegador; la validación real (`RULE-02`) sigue siendo la del servidor | `AC4` lo dice verbatim: "This is an affordance — the API validates regardless". Un desfase navegador/servidor cerca de medianoche produce, a lo sumo, un `VF-REQ-002` del API pintado bajo `Start date` — el flujo correcto | Ninguno estructural |
| `S3` | El grupo `(app)` no valida sesión en el servidor Next (sin middleware); la protección real es el API + redirección de `D7` | La regla del cliente es "no business rule in the frontend" (`SAD.md` §9.5); un middleware de sesión duplicaría la autoridad del cookie que solo el API puede validar. La página edit recibe `401` en su primer `getRequest` y redirige | Si el parpadeo pre-redirect molestara, un middleware de conveniencia sería aditivo (`US-030`+) |

---

## 6. Verificación end-to-end

| # | Paso | Resultado esperado |
|---|---|---|
| 1 | `dotnet build VacaFlow.slnx` && `dotnet test VacaFlow.slnx` | Compila con 0 warnings; suite completa verde incluida #6 |
| 2 | `cd src/web && npm install && npm run lint && npm run depcruise && npm run build` | Sin errores; reglas de §9.3 en verde |
| 3 | Terminal 1: `dotnet run --project src/BigSolutions.VacaFlow.Api` (puerto 5217) · Terminal 2: `cd src/web && npm run dev` | API y web arriba; `http://localhost:3000` redirige a `/requests` y de ahí a `/sign-in` (sin cookie) |
| 4 | Sign in con `employee@vacaflow.test` / `Employee123!` | Aterriza en `/requests` (placeholder `My Requests` + `New request`); la cookie `VacaFlow.Session` es first-party (DevTools) y **ninguna petición es cross-origin** |
| 5 | Pulsar `New request` | `S-05` modo create: `←` con `aria-label="Back to my requests"`, título `New request`, tarjeta 560px, select con `Select…` deshabilitado + los 3 tipos sembrados (petición real a `/api/absence-types` visible en red), fechas con `min` = hoy, textarea 4 filas | 
| 6 | Teclear en `Reason` | El contador `N/500` avanza en vivo, alineado a la derecha |
| 7 | Rellenar válido y `Save draft` | `POST /api/requests` → `201`; navegación a `/requests`; banner verde `Draft created.` con `×` |
| 8 | Repetir con `endDate` < `startDate` (forzando el `min` vía DevTools si hace falta) y con `Reason` vacío | `400` del API; `The end date cannot be earlier than the start date.` **bajo `End date`** (`role="alert"`); `The reason is required (1 to 500 characters).` bajo `Reason`; sin banner |
| 9 | Abrir `/requests/{id}` del draft del paso 7 (id del `Location`/cuerpo `{ id }`) | `GET /api/requests/{id}` → `200`; título `Edit draft`, valores precargados (tipo, fechas, reason), primario `Save changes` |
| 10 | Cambiar tipo y reason, `Save changes` | `PUT` → `204`; `/requests` + banner `Changes saved.` |
| 11 | Sign in como `manager@vacaflow.test` y abrir la misma URL `/requests/{id}` | `403` `VF-REQ-004` → alert general `You can only act on your own requests.`; con Guid aleatorio → `404` `VF-REQ-006` |
| 12 | Como dueño: `UPDATE Requests SET State = 1 WHERE Id = '{id}'` por SQL y reabrir `/requests/{id}` | Título `Request detail`; todos los controles `disabled`; sin botón primario; secundario `Back` → `/requests` (`AC8`) |
| 13 | Con la fila aún `Submitted`, forzar un `PUT` (p. ej. re-habilitando el form por DevTools) | `409` `VF-REQ-003` → alert general `Only Draft requests can be edited.` (`AC7`) |
| 14 | Borrar la cookie y navegar a `/requests/new` | El primer fetch devuelve `401` → redirección a `/sign-in` (`FR-UIX-007`) |
| 15 | Tab por el formulario | Focus ring 2px accent con offset 2px en cada control (`NFR-USA-005`); labels visibles asociados por `for`/`id` |

---

## 7. Riesgos

| Riesgo | Mitigación |
|---|---|
| **Talla subestimada**: el backlog da `M` a la pantalla, pero la historia carga además el bootstrap del web entero + un endpoint de lectura | El bootstrap es scaffold estándar (`create-next-app`) y el endpoint reutiliza puerto y mapeos existentes; aun así, tratarla como `M` grande / `L` corta al agendar. Los tramos #1–#6 y #7–#17 son paralelizables |
| `US-016` sin mergear: el PR aún no existe en GitHub (`D9`) | Rama apilada sobre `feat/us-016-edit-draft-request`; si el review de `US-016` toca `RequestEndpoints.cs` o el puerto, rebase con conflicto acotado a ese archivo |
| `D3` añade superficie backend en una historia "de pantalla" — posible fricción con `US-020`/`US-025` al planificarlas | Alcance mínimo y documentado (dueño-solo, DTO plano); ambas historias lo extienden aditivamente. Anotado como **entrada obligatoria** para los planes de `US-020` y `US-025` |
| El layout/banner mínimos (`D4`) podrían divergir de lo que `US-030`/`US-031` exijan | El `Banner` se escribe ya contra la spec §3.3 (paletas, `role="status"`, dismiss) y el layout mínimo no expone API; ambos quedan trazados como base a completar, no como hecho consumado |
| El scaffolding de `/sign-in` (`D6`) podría "colar" como pantalla definitiva y desactivar `US-013` | Marcado provisional en código y aquí; `US-013`/`US-012` conservan íntegros sus criterios visuales |
| Serialización de fechas/campos entre `input[type=date]`, TS y `DateOnly` (`S1`) | Formato único `yyyy-MM-dd` en ambos extremos; si un caso falla, el fix vive solo en `lib/api.ts`. El paso §6.8 ejercita el roundtrip de error con `field` real |
| El rewrite del proxy apunta a un puerto fijo (5217) que podría cambiar | Puerto leído de `launchSettings.json` real; si cambiara, es una línea en `next.config.mjs` (documentado en §2.2). El README (`US-026`) documentará el arranque a dos procesos |
| `next/font/google` requiere red en el primer build (`D2`) | Aceptado para el MVP local; alternativa `@fontsource` documentada en `D2` |

---

Siguiente paso — implementación:

```
/user-story-implement ".claude/implementation plans/US-017-plan.md"
```
