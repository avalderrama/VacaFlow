# Plan de implementación — `US-030` · Application shell

| Campo | Valor |
|---|---|
| Historia | `US-030` — Application shell |
| Épica | `EP-03` — Application shell and feedback |
| Prioridad · Talla | **Must** · `M` |
| Pantalla | `S-03` (Application shell — header, nav, banner) — **dueña nominal junto con `US-031`/`US-035`** (`Backlog.md` §3.2, fila `S-03 → US-030, US-031, US-035`; visible en screenshots `05`–`11`) |
| Depende de | `US-010` (`GET /auth/me`, mergeada hace tiempo) — **sin precondiciones pendientes** |
| Desbloquea | `US-031` (Notification banner) · `US-033` (Cancel confirmation modal) · `US-034` (Decision modal) · `US-035` (Pending count on the manager tab) · `US-036` (Accessibility baseline) — verificado en las líneas de dependencia del propio backlog: `US-031` "Depends on: US-030" (línea 404), `US-033` "Depends on: US-030" (línea 512), `US-034` "Depends on: US-030" (línea 567), `US-035` "Depends on: US-030, US-020" (línea 425), `US-036` "Depends on: US-030" (línea 432). Es la historia fundacional del hito 3 — Shell (`Backlog.md` §"Milestones") |
| Trazas | `SC-01` · `FR-UIX-001` (*"Once signed in, every screen displays the current user's name and role (UX-PRN-002)."*) · `FR-UIX-002` (afordancia del tab manager-only) · `Backlog.md` §3.3 (*Application header*, *Nav tab*), §3.5 (*Navigation and shell*) · prototipo `VacaFlow.dc.html` líneas 105–137 (markup exacto del header, nav, bloque de identidad y `Cerrar sesión`) · `SAD.md` §9.2 (`(app)/layout.tsx # S-03 shell: header, nav, banner slot`; `components/shell/ AppHeader · NavTabs · SkipLink`) · deuda de `US-009` (mitad cliente de Sign out) y de `US-013` (*"the header of S-03 shows the current user's name and role on every screen"*) |
| Fuentes | `Backlog.md` v2.0 · `FRD.md` · `SAD.md` v2.0 · código real verificado en **`main` (commit `ae00345` — `US-025` mergeada, PR #22)**, `src/web/` archivo por archivo y `AuthEndpoints.cs` · planes `US-017` (`D4` — layout mínimo diferido a esta historia), `US-023`/`US-024`/`US-025` (diferimientos del shell reanotados aquí) |
| Repositorio | `C:\Users\avald\OneDrive\Work\VacaFlow\repositories\vacaflow` |
| Rama sugerida | `feat/us-030-app-shell`, creada desde `main` (`ae00345`) |
| Estado | Borrador presentado para aprobación (sesión de planificación delegada; **tres preguntas abiertas en §7 — `OQ-A` slot del banner, `OQ-B` granularidad de componentes del shell, `OQ-C` tab activo en rutas anidadas**) |

> **Este documento no implementa nada.** La implementación la ejecuta `/user-story-implement`.

---

## 1. Entendimiento

### 1.1 Contexto — primera historia del shell; historia **solo Web** (cero backend)

Verificado contra `main` (`ae00345`):

**Lo que existe hoy en `src/web/`:**

- `app/(app)/layout.tsx` — **sigue siendo el contenedor mínimo**, tal como lo declararon los planes de `US-017`/`US-023`/`US-024`/`US-025`. Su comentario lo dice verbatim: *"Minimal container for the authenticated route group — not the S-03 shell (no header, nav, or identity display); US-030 replaces this (US-017 plan D4)."* Hoy solo renderiza `<div style={{ maxWidth: 'var(--content-width-main)', margin: '0 auto', padding: '32px' }}>{children}</div>`. Es un **server component** (sin `'use client'`).
- Las cuatro páginas autenticadas — `requests/page.tsx` (S-04), `requests/new/page.tsx` (S-05 create), `requests/[id]/page.tsx` (S-05 edit / S-06 detail) y `queue/page.tsx` (S-07) — **viven todas bajo `app/(app)/` y renderizan dentro de `(app)/layout.tsx`**. Adoptar el shell en el layout las envuelve automáticamente: **ninguna página necesita cambios** (verificado: ninguna renderiza header, nav ni identidad propia; sus `<h1>` son títulos de contenido, no del shell).
- `lib/api.ts` — `getMe(): Promise<AuthenticatedUser>` existe (`GET /auth/me`) y ya lo consumen `requests/page.tsx` y `queue/page.tsx`. **No existe wrapper de sign-out** (`grep logout` en `src/web` no devuelve nada) — hay que añadirlo.
- `lib/types.ts` — `AuthenticatedUser { id, fullName, email, role }` con `role: 'Employee' | 'Manager'`. Todo lo que el header necesita ya viaja.
- `components/` — carpetas `feedback/`, `queue/`, `requests/`. **No existe `components/shell/`** — el árbol del `SAD.md` §9.2 la nombra (`AppHeader · NavTabs · SkipLink`) y dice verbatim: *"the shell components exist because US-030, US-031 and US-033–US-035 named them"*. `SkipLink` pertenece a `US-036`, no a esta historia.
- `globals.css` — tokens §3.1 completos como variables CSS (`--color-*`, `--content-width-main: 1100px`, fuentes `--font-ibm-plex-sans`/`--font-ibm-plex-mono` cargadas en `app/layout.tsx`); catálogo de clases de botón. **No hay clases de nav-pill ni de header** — se añaden aquí.
- Nadie usa `usePathname` todavía (`grep` vacío) — el resaltado del tab activo lo estrena esta historia.

**Backend — lo que existe (nada que crear):**

- `POST /api/auth/logout` — existe desde `US-009` (`AuthEndpoints.cs` línea 64): `SignOutAsync` de la cookie + `204 No Content`, con `RequireAuthorization()`. La mitad servidor de Sign out está pagada; **la mitad cliente (botón + redirect a `S-01`) quedó huérfana y esta historia la salda** (el criterio de `US-009` nombra el botón `Sign out` de `S-03`, pantalla que no existía).
- `GET /api/auth/me` — existe desde `US-010`; devuelve `{ id, fullName, email, role }`.

**Prototipo (`VacaFlow.dc.html` líneas 105–137, autoritativo para markup junto con §3.3):** header = barra blanca `padding: 14px 32px`, `border-bottom: 1px solid oklch(90% 0.006 250)`; izquierda: wordmark `VacaFlow` en mono `18px/600, letter-spacing -0.02em` + `<nav>` con `gap: 6px`, separados del wordmark por `gap: 36px`; el botón del queue va envuelto en el condicional `isManager` (línea 112 — **el tab es manager-only en el propio markup**, la misma afordancia que `FR-UIX-002` exige); derecha: nombre `14px/600` sobre rol `12px` secundario, alineados a la derecha, + botón `Sign out` bordeado (`background: white; border: 1px solid oklch(85% 0.008 260); padding: 8px 16px; border-radius: 8px; font-size: 13px; font-weight: 600`). Nav tab (§3.3): pill `8px/16px padding, 8px radius`; activo `background: oklch(93% 0.03 260), texto oklch(35% 0.1 260)`; inactivo transparente, texto `oklch(45% 0.02 260)`. `<main>` con `flex: 1; padding: 32px; max-width: 1100px; margin: 0 auto` (línea 137) — exactamente lo que el layout actual ya hace con su `<div>`.

### 1.2 Narrativa (verbatim)

> "As a signed-in user, I want a consistent header showing where I am and who I am, so that I never have to wonder about either."

### 1.3 Criterios de aceptación — verbatim (`Backlog.md` §EP-03 · `US-030`)

| # | Criterio |
|---|---|
| `AC1` | "Given any signed-in screen, when it renders, then the header shows the wordmark, the navigation, my name, my role and `Sign out`." |
| `AC2` | "Given an Employee, when the header renders, then the navigation contains only `My Requests`." |
| `AC3` | "Given a Manager, when the header renders, then the navigation contains `My Requests` and `Approval Queue`." |
| `AC4` | "Given the active tab, when it renders, then it uses the active pill style of §3.3 and the other uses the inactive style." |
| `AC5` | "Given a Manager, when they open `My Requests`, then they see their own requests as any employee would." |

Nota visual verbatim de la historia:

> **Visual** — header per §3.3; main content constrained to 1100px, centered, 32px padding.

Copy §3.5 (autoritativo sobre el prototipo en español): wordmark `VacaFlow` · nav `My Requests` · `Approval Queue (N)` — *"the count is omitted when zero"* (el conteo `N` es `US-035`; aquí el tab se rotula `Approval Queue` a secas, ver `D7`) · rol `Employee` · `Manager` · botón `Sign out`.

Además esta historia **salda la deuda cliente de `US-009`** (verbatim, criterio 1):

> "Given a signed-in user, when I press `Sign out`, then the session is invalidated and I return to `S-01` with no banner carried over."

y da soporte a `US-013` (*"after signing in, the header of S-03 shows the current user's name and role on every screen"*) y a `FR-UIX-001`.

### 1.4 Alcance

**Entra**: wrapper `signOut()` en `lib/api.ts`; componentes de shell (`components/shell/` — ver `OQ-B`); reemplazo del contenido de `app/(app)/layout.tsx` por header + `<main>`; clases CSS del header/nav en `globals.css`; resaltado del tab activo por pathname; gating del tab `Approval Queue` por rol vía `getMe()`; botón `Sign out` funcional.

**No entra**

| Excluido | Por qué / destino |
|---|---|
| Banner centralizado en el shell (matriz completa, `role="status"`, fade-in, limpieza al navegar) | **`US-031`** (depende de esta historia). Hoy cada página renderiza su propio `Banner` dentro de la columna de contenido — visualmente ya aparece "at the top of the content column" como §3.3 pide. Ver `OQ-A` sobre si el shell deja algún hueco estructural |
| Conteo `(N)` en el tab del manager y su refresco sin recarga | **`US-035`** (depende de esta historia + `US-020`). El tab se rotula `Approval Queue` a secas — `D7` |
| `SkipLink` / `Skip to main content`, foco y `Escape` en modales | **`US-036`**. El `<main>` del shell sí lleva `id="main-content"` desde ya (una prop gratuita que `US-036` necesitará; no es implementar su criterio) — `D8` |
| Skeletons y empty states | **`US-032`** |
| Guard de ruta: bloquear `/queue` a un Employee que teclea la URL | Ningún criterio lo pide; `FR-UIX-002` es afordancia (*"the API rejects it regardless"*) y `SAD.md` §9.5 lo ratifica. El comportamiento actual de `/queue` para un Employee (lista vacía) no cambia — `D4` |
| Backend | Cero cambios: `GET /auth/me` y `POST /auth/logout` existen y bastan. **Verificado explícitamente — historia Web-only como `US-023`/`US-024`** |
| Redirección al sign-in sin sesión | Ya resuelta por `lib/api.ts` (`VF-AUT-004` → `/sign-in`); el shell no añade session-check propio (mismo racional del layout actual) |

---

## 2. Cambios estructurales / de base

**No se requieren cambios de esquema, migraciones, configuración, variables de entorno, feature flags, dependencias nuevas (npm ni NuGet), cambios de seed ni cambios de contrato de API.** Historia íntegramente Web; los dos endpoints que consume existen desde `US-009`/`US-010`. La única novedad estructural del cliente es la carpeta `components/shell/`, ya prescrita por `SAD.md` §9.2.

---

## 3. Plan ordenado por dependencia

**Sin ítems de Domain, Application, Infrastructure ni API.** Todo es Web (más su verificación).

| # | Capa | Acción | Artefacto | Notas |
|---|---|---|---|---|
| 1 | Web | Modificar | `src/web/lib/api.ts` | Añadir `export function signOut(): Promise<void>` → `request<void>('/auth/logout', { method: 'POST' })` (el endpoint devuelve `204`; la rama `204 → undefined` del helper ya existe). Mismo patrón que `submitRequest`/`cancelRequest` |
| 2 | Web | Crear | `src/web/globals.css` → clases en `src/web/app/globals.css` | Clases del shell (mismo racional que `.btn-*`: valores verbatim del prototipo, una definición): `.nav-tab` (pill `8px 16px`, radius `8px`, transparente, texto `oklch(45% 0.02 260)`, `font-size: 14px; font-weight: 600; cursor: pointer; border: none`) y `.nav-tab-active` (`background: oklch(93% 0.03 260)`, texto `oklch(35% 0.1 260)`); `.btn-signout` (`background: var(--color-surface); border: 1px solid var(--color-border-input); padding: 8px 16px; border-radius: 8px; font-size: 13px; font-weight: 600; cursor: pointer` — prototipo línea 124) |
| 3 | Web | Crear | `src/web/components/shell/NavTabs.tsx` | **(Sujeto a `OQ-B` — el plan asume la opción (a): dos componentes, como nombra `SAD.md` §9.2.)** `'use client'`. Props: `role: EmployeeRole`. Renderiza `<nav>` con `gap: 6px`: tab `My Requests` → `/requests` siempre; tab `Approval Queue` → `/queue` **solo si `role === 'Manager'`** (`AC2`/`AC3` — el condicional espeja la línea 112 del prototipo). Tabs como `<Link>` de `next/link` estilizados con `.nav-tab`/`.nav-tab-active` (navegación client-side real con URL, no botones de estado como el prototipo — `D3`). Activo por `usePathname()`: `/queue` → Approval Queue; cualquier otra ruta del grupo (`/requests`, `/requests/new`, `/requests/{id}`) → My Requests (`AC4`, prefix matching — ver `OQ-C`/`D6`) |
| 4 | Web | Crear | `src/web/components/shell/AppHeader.tsx` | `'use client'`. Header §3.3 / prototipo líneas 107–126: `<header>` flex space-between, `padding: 14px 32px`, `background: var(--color-surface)`, `border-bottom: 1px solid var(--color-border)`. Izquierda (flex, `gap: 36px`): wordmark `VacaFlow` (`fontFamily: var(--font-ibm-plex-mono)`, `18px/600`, `letterSpacing: -0.02em`) + `<NavTabs role={me.role} />`. Derecha (flex, `gap: 16px`): bloque identidad alineado a la derecha — `me.fullName` `14px/600` sobre `me.role` `12px` `var(--color-text-secondary)` (`AC1`, `FR-UIX-001`) — + botón `Sign out` (`.btn-signout`). Datos: `getMe()` en un `useEffect` al montar (mismo patrón de fetch-on-mount de las páginas — `D5`); mientras carga, header con wordmark y el resto vacío (sin layout shift brusco: reservar altura con el propio padding); si `getMe()` falla con `VF-AUT-004`, `api.ts` ya redirige solo. `Sign out`: flag en vuelo (botón deshabilitado) + `await signOut()` → `window.location.href = '/sign-in'` (navegación dura: descarta todo estado en memoria y no siembra `pendingNotification` — *"no banner carried over"* de `US-009` por construcción); si `signOut()` lanza, el error se muestra (sin tragarlo — `FR-UIX-003`) con el patrón mínimo disponible en el header (ver `D9`) |
| 5 | Web | Modificar | `src/web/app/(app)/layout.tsx` | Reemplazar el contenedor mínimo: `<AppHeader />` + `<main id="main-content" style={{ maxWidth: 'var(--content-width-main)', margin: '0 auto', padding: '32px' }}>{children}</main>` (nota visual verbatim: 1100px, centered, 32px padding — los mismos valores que ya tiene el `<div>` actual, ahora en un `<main>` semántico con el id que `US-036` apuntará — `D8`). El layout **sigue siendo server component** (solo compone; el estado vive en `AppHeader`). Actualizar el comentario obsoleto (*"US-030 replaces this"* — esta historia lo salda). **Ninguna página bajo `(app)/` se toca**: las cuatro rutas ya renderizan dentro de este layout y heredan el shell gratis (verificado §1.1) |
| 6 | Web | Verificar | `cd src/web && npm run lint && npm run typecheck && npm run depcruise && npm run build` | `depcruise` confirma que `fetch` sigue solo en `lib/api.ts` y que `components/shell` no importa de `app/` (regla `components-do-not-import-pages`) |
| 7 | Test | Verificar | `dotnet build VacaFlow.slnx` + `dotnet test VacaFlow.slnx` | Regresión pura — cero cambios backend; las suites existentes de `US-009`/`US-010` (logout/me funcionales) ya cubren los dos endpoints que el shell consume |
| 8 | E2E | Verificar | Verificación manual §6 con la app corriendo (API + `npm run dev`) | Único punto donde `AC1`–`AC5` son demostrables juntos contra `05-my-requests.png`/`10-manager-queue.png` |

**Dependencias:** 1 → 4 · 2 → {3, 4} · 3 → 4 → 5 · todo → {6, 7, 8}. **Paralelizable:** {1, 2} entre sí; 3 con 1. **Ruta crítica:** 2 → 3 → 4 → 5 → 8. `OQ-A` no bloquea ningún ítem (su "sí" añadiría un hueco estructural en #5); `OQ-B` decide si #3 y #4 son uno o dos archivos, no su contenido; `OQ-C` decide una condición de una línea en #3.

---

## 4. Casos de uso y tabla de trazabilidad

**Cero casos de uso nuevos en Application.** Esta historia añade la superficie de shell que consume dos casos de uso existentes: *obtener el usuario actual* (`US-010`) y *cerrar sesión* (`US-009`, cuya mitad cliente se salda aquí).

| Historia | Criterio de aceptación (verbatim) | Ítems del plan que lo cubren | Cómo se verifica |
|---|---|---|---|
| `US-030` | "Given any signed-in screen, when it renders, then the header shows the wordmark, the navigation, my name, my role and `Sign out`." | #4 (header completo con identidad de `getMe()`), #5 (presente en las cuatro rutas vía layout) | §6 pasos 2–4 y 6 (header idéntico en `S-04`, `S-05`, `S-06`, `S-07`) contra screenshots `05`–`11` |
| `US-030` | "Given an Employee, when the header renders, then the navigation contains only `My Requests`." | #3 (tab queue condicionado a `role === 'Manager'`), #4 (pasa el rol real) | §6 paso 2 (como Carlos: un solo tab) |
| `US-030` | "Given a Manager, when the header renders, then the navigation contains `My Requests` and `Approval Queue`." | #3, #4 | §6 paso 5 (como Laura: dos tabs) |
| `US-030` | "Given the active tab, when it renders, then it uses the active pill style of §3.3 and the other uses the inactive style." | #2 (estilos verbatim §3.3), #3 (`usePathname` decide) | §6 pasos 5–7 (swap del pill al navegar) + inspección contra `05`/`10` |
| `US-030` | "Given a Manager, when they open `My Requests`, then they see their own requests as any employee would." | #3 (el tab navega a `/requests`; la página existente ya filtra `employee.id === me.id` — `US-024`, sin cambios) | §6 paso 6; regresión de `US-024` |
| `US-009` (deuda) | "Given a signed-in user, when I press `Sign out`, then the session is invalidated and I return to `S-01` with no banner carried over." | #1 (`signOut()` → `POST /auth/logout` existente), #4 (botón + navegación dura a `/sign-in` sin sembrar banner) | §6 pasos 8–9 (redirect a `S-01` limpio; llamada posterior → `VF-AUT-004`) — cierra la mitad cliente huérfana de `US-009` |

**Conteo: 5 criterios de entrada de `US-030` · 5 cubiertos** (+ 1 criterio diferido de `US-009` que esta historia salda, trazado aparte). La nota visual (1100px/centered/32px) la cubre #5 y ya es cierta hoy (regresión, no cambio).

---

## 5. Supuestos y decisiones

Sesión de planificación delegada (Fase 3 no interactiva): decisiones de arquitecto documentadas con su reversibilidad. **Las tres que merecen ratificación del usuario están elevadas a §7 (`OQ-A`, `OQ-B`, `OQ-C`).**

| # | Decisión | Justificación | Impacto si es incorrecta |
|---|---|---|---|
| `D1` | **Historia Web-only: cero ítems de backend** | Verificado explícitamente en `main`: `GET /auth/me` (`US-010`) devuelve `{ id, fullName, email, role }` — todo lo que el header muestra — y `POST /auth/logout` (`US-009`) invalida la cookie con `204`. El gating por rol es afordancia pura sobre el `role` que ya viaja | N/A — hecho del código |
| `D2` | **El shell vive en `(app)/layout.tsx` como composición; el estado vive en `AppHeader` (`'use client'`); las páginas no se tocan** | `SAD.md` §9.2 asigna el shell a `(app)/layout.tsx` verbatim; las cuatro rutas ya renderizan dentro de él (verificado), así que el shell les llega gratis. Mantener el layout como server component que compone un client component es el patrón Next.js estándar y no obliga a convertir nada más | N/A — hecho arquitectónico verificado |
| `D3` | **Los tabs son `<Link>` de `next/link` a `/requests` y `/queue`, no botones con estado** | A diferencia del prototipo (single-page con `navTab` en estado), esta app tiene rutas reales por pantalla (`SAD.md` §9.2); un `<Link>` da URL compartible, back button y prefetch gratis, y `usePathname` deriva el activo sin estado duplicado | Cosmético — cambiar el elemento es local al componente |
| `D4` | **Sin guard de ruta para `/queue`: ocultar el tab es toda la protección cliente** | `FR-UIX-002` verbatim: la no-oferta es afordancia, *"the API rejects it regardless"*; `SAD.md` §9.5 primera regla. Un Employee que teclea `/queue` ve hoy una lista vacía (su `listRequests` solo trae las propias y el filtro las excluye) — sin fuga de datos. Añadir un guard sería alcance de nadie | Si el usuario quiere redirect, es un `useEffect` en `queue/page.tsx` — ampliación menor |
| `D5` | **`AppHeader` llama `getMe()` por su cuenta al montar; sin contexto global ni caché de sesión** | Patrón vigente de la app: cada página ya hace su propio `getMe()` (S-04, S-07) y `SAD.md` §9.1 rechaza infraestructura extra para un cliente de cinco pantallas (`TC-06`). El header monta una vez por carga completa (las navegaciones internas son client-side y no lo desmontan), así que el coste real es una llamada por sesión de pestaña, no por página | Si se prefiere centralizar (contexto `CurrentUser`), es refactor aditivo posterior; `US-035` podría motivarlo al necesitar el conteo compartido |
| `D6` | **Tab activo por prefijo de `usePathname()`: `/queue*` → `Approval Queue`; todo lo demás del grupo → `My Requests`** | `AC4` habla de "the active tab" y "the other" — binario. Las rutas anidadas (`/requests/new`, `/requests/{id}`) pertenecen conceptualmente al flujo de My Requests (el prototipo no tiene URLs; su `navTab` permanece en `requests` durante form y detalle — verificado en su estado) | Ver `OQ-C` — cambiar la regla es una condición de una línea |
| `D7` | **El tab del manager se rotula `Approval Queue` a secas, sin `(N)`** | El conteo es el criterio íntegro de `US-035` (*"the tab reads `Approval Queue (N)`"*), historia separada que depende de esta. §3.5 anota *"the count is omitted when zero"* — rotular sin conteo es además el estado legítimo de conteo cero | Ninguno — `US-035` inserta el conteo sobre este tab |
| `D8` | **El `<main>` del shell lleva `id="main-content"` desde ya** | `US-036` (Skip link) apunta verbatim a `#main-content`; ponerle el id al elemento que esta historia crea evita que `US-036` tenga que reabrir el layout. Es un atributo inerte, no implementa ningún criterio ajeno (el `SkipLink` en sí queda fuera) | Ninguno — si molesta, quitarlo es una palabra |
| `D9` | **Error de `signOut()`: se muestra, no se traga** | `FR-UIX-003` verbatim: *"No error is silently swallowed."* El header no tiene banner propio (es `US-031`); el patrón mínimo es un mensaje inline/`alert-general` junto al botón o reutilizar `Banner` dentro del header. Caso límite real: si la sesión ya expiró, `api.ts` redirige solo a `/sign-in` (que es a donde el usuario quería ir) | Cosmético — `US-031` centralizará la superficie de error del shell |
| `D10` | **Sin tests automatizados de frontend; verificación = lint + typecheck + depcruise + build + E2E manual** | Ratificación de `US-023 D7`/`US-024 D5`/`US-025 D8`: sigue sin existir runner en `src/web/package.json` (verificado: scripts `dev/build/start/lint/typecheck/depcruise`) | Si el usuario quiere estrenar runner aquí, se añade como ítem previo — ampliación, no corrección |
| `S1` | **La rama se crea desde `main` (`ae00345`) directamente** | Verificado: `US-010` mergeada hace tiempo; `origin/main` al día; sin precondiciones pendientes | Ninguno |

---

## 6. Verificación end-to-end

Con la API corriendo y `npm run dev` en `src/web/`, seed §3.6 (Laura manager; Carlos y Ana empleados asignados a ella).

| # | Paso | Resultado esperado |
|---|---|---|
| 1 | `dotnet build` + `dotnet test` · `cd src/web && npm run lint && npm run typecheck && npm run depcruise && npm run build` | Todo verde, 0 warnings |
| 2 | Sign in como Carlos (Employee) → `/requests` — **`AC1`/`AC2`** | Header en barra blanca con borde inferior: wordmark `VacaFlow` mono 18px; nav con **solo** `My Requests` (pill activo); a la derecha `Carlos …` 14px/600 sobre `Employee` 12px secundario y botón `Sign out` bordeado — contra `05-my-requests.png` |
| 3 | Navegar a `New request` (`/requests/new`) y a un detalle (`/requests/{id}`) — **`AC1`/`AC4`** | El header persiste idéntico en ambas rutas; `My Requests` sigue con el pill activo (`OQ-C`/`D6`); contenido centrado a 1100px con 32px de padding |
| 4 | Como Carlos, teclear `/queue` a mano — regresión `D4` | Sin tab `Approval Queue` en el header; la página muestra su estado vacío actual; ninguna fuga de datos |
| 5 | Sign out y sign in como Laura (Manager) — **`AC3`/`AC4`** | Nav con `My Requests` y `Approval Queue` (sin `(N)` — `D7`); al pulsar `Approval Queue` la URL pasa a `/queue`, su pill se activa y el de `My Requests` pasa a inactivo — contra `10-manager-queue.png` |
| 6 | Como Laura, pulsar `My Requests` — **`AC5`** | `/requests` muestra **solo las requests propias de Laura** (filtro de `US-024` intacto), con el pill de `My Requests` activo |
| 7 | Alternar tabs varias veces — **`AC4`** | El swap activo/inactivo es consistente con la URL; back button del navegador también actualiza el pill (bondad de `D3`) |
| 8 | Pulsar `Sign out` — deuda `US-009` | Botón deshabilitado en vuelo; `POST /api/auth/logout` → `204`; aterrizaje en `/sign-in` (`S-01`) **sin banner** de ningún tipo |
| 9 | Tras el paso 8, `GET /api/requests` directo (devtools/curl con la cookie ya invalidada) | `401` `VF-AUT-004` — la sesión quedó invalidada de verdad, no solo la navegación |
| 10 | Recargar `/requests` sin sesión | `api.ts` redirige a `/sign-in` (regresión `FR-UIX-007`; el shell no rompió el flujo del 401) |

---

## 7. Riesgos y preguntas abiertas

> ⚠️ **`OQ-A` — Pregunta abierta para el usuario (no bloquea ítems; decide si #5 deja un hueco estructural):**
> `SAD.md` §9.2 anota el layout como *"S-03 shell: header, nav, **banner slot**"*, pero **ningún criterio de `US-030` menciona el banner** — la conducta completa del banner (matriz éxito/error, `role="status"`, dismiss, limpieza al navegar, fade-in) es `US-031`, que depende de esta historia. Hoy el banner se renderiza por página (componente `Banner` dentro de la columna de contenido), lo que ya satisface visualmente "at the top of the content column".
>
> - **(a) — recomendada — Sin trabajo de slot en `US-030`.** El shell entrega header + `<main>`; `US-031` decide si centraliza el banner en el layout o mantiene el patrón por página. Contra: `US-031` podría reabrir `layout.tsx` (coste trivial).
> - **(b) Dejar un placeholder estructural** (un hueco vacío entre header y `<main>`, sin lógica). Contra: markup muerto sin criterio que lo respalde; el prototipo dibuja el banner **dentro** de la columna de contenido (línea 129), no entre header y main, así que el hueco podría nacer equivocado.
>
> **El plan asume (a) salvo indicación contraria.**

> ⚠️ **`OQ-B` — Pregunta abierta para el usuario (decide si los ítems #3/#4 son uno o dos archivos, no su contenido):**
> `SAD.md` §9.2 nombra `components/shell/ AppHeader · NavTabs · SkipLink` (SkipLink es `US-036`). `NavTabs` por separado son ~30 líneas.
>
> - **(a) — recomendada — Dos componentes (`AppHeader` + `NavTabs`)**, fieles al árbol del SAD (*"the component tree maps one-to-one…"*). `US-035` tocará solo `NavTabs` para el conteo.
> - **(b) Solo `AppHeader`** con el nav inline. Menos archivos, pero se aparta del árbol nombrado del SAD sin ganancia real.
>
> **El plan asume (a) salvo indicación contraria.**

> ⚠️ **`OQ-C` — Pregunta abierta para el usuario (decide una condición de una línea en #3):**
> `AC4` define activo/inactivo pero el backlog no dice qué tab está activo en las rutas **anidadas** (`/requests/new`, `/requests/{id}`) — el prototipo no tiene URLs y su estado `navTab` permanece en `requests` durante formulario y detalle.
>
> - **(a) — recomendada — Prefix matching:** `/queue*` → `Approval Queue` activo; todo lo demás del grupo → `My Requests` activo (siempre hay exactamente un tab activo, como en el prototipo).
> - **(b) Match exacto:** solo `/requests` y `/queue` activan; en `/requests/new` y `/requests/{id}` ningún tab estaría activo. Contra: contradice el prototipo y deja la nav "apagada" en dos pantallas.
>
> **El plan asume (a) salvo indicación contraria.**

| Riesgo | Mitigación |
|---|---|
| Doble `getMe()` en la primera carga (header + página) | `D5`: coste trivial (una llamada extra por carga completa, no por navegación interna); centralizar es refactor posterior si `US-035` lo motiva |
| Flash del header sin identidad/tabs mientras `getMe()` resuelve | El header renderiza estructura estable (wordmark + padding fijo) y rellena identidad/tabs al resolver; sin layout shift del contenido. Si se quisiera SSR de la identidad haría falta otra arquitectura de sesión — fuera de alcance (`TC-06`) |
| `Sign out` con la sesión ya expirada | `signOut()` recibiría `401 VF-AUT-004` y `api.ts` redirige solo a `/sign-in` — exactamente el destino deseado; sin estado raro |
| El comentario del layout (*"US-030 replaces this"*) queda obsoleto | Ítem #5 lo actualiza expresamente |
| Sin tests de frontend, `AC1`–`AC5` solo se demuestran manualmente | `D10` + §6; los endpoints consumidos ya están probados server-side (`US-009`/`US-010` funcionales) |
| Regresión visual en páginas existentes al cambiar `<div>` por `<main>` | Mismos valores de estilo (verificado carácter a carácter en §1.1); §6 pasos 2–6 recorren las cuatro rutas |

---

Siguiente paso — implementación:

```
/user-story-implement ".claude/implementation plans/US-030-plan.md"
```
