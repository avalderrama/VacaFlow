# Plan de implementación — `US-032` · List loading and empty states

| Campo | Valor |
|---|---|
| Historia | `US-032` — List loading and empty states |
| Épica | `EP-03` — Application shell and feedback |
| Prioridad · Talla | **Must** · `S` |
| Pantallas | `S-04` (My Requests — list), `S-04b` (My Requests — empty), `S-07` (Approval Queue) |
| Depende de | `US-024` (My Requests screen, mergeada — PR #21), `US-020` (listado visible para el manager, mergeado) — **sin precondiciones pendientes** |
| Traza | `FR-UIX-004`, `NFR-USA-008` · `Backlog.md` §3.3 ("Loading skeleton", "Empty state") y §3.5 (copy de estados vacíos, líneas 219-220) |
| Fuentes | `Backlog.md` v2.0 · código real verificado en `main` (commit `a04baf1` — `US-031` mergeada, PR #24), archivo por archivo en `src/web/` |
| Repositorio | `C:\Users\avald\OneDrive\Work\VacaFlow\repositories\vacaflow` |
| Rama sugerida | `feat/us-032-list-loading-empty-states`, creada desde `main` (`a04baf1`) |
| Estado | Borrador presentado para aprobación — una pregunta abierta ya resuelta por el usuario (ver §5, `D-loadFailed`) |

> **Este documento no implementa nada.** La implementación la ejecuta `/user-story-implement`.

---

## 1. Entendimiento

### 1.1 Contexto

Hoy, tanto `/requests` (`src/web/app/(app)/requests/page.tsx`) como `/queue` (`src/web/app/(app)/queue/page.tsx`) usan placeholders de texto plano mientras cargan o cuando no hay datos:

- `/requests`: `{requests === null && !loadFailed && <p>Loading…</p>}` y `{requests !== null && requests.length === 0 && <p>You haven't created any requests yet.</p>}`.
- `/queue`: `{queue === null && error === null && <p>Loading…</p>}` y `{queue !== null && queue.length === 0 && <p>No requests are waiting on your decision.</p>}`.

`US-032` reemplaza ambos placeholders por los patrones visuales que `Backlog.md` §3.3 especifica **verbatim**:

> *"Loading skeleton — grey blocks `oklch(94% 0.004 250)`, 10px radius, stacked with a 10px gap. Three blocks of 64px for `S-04`; two blocks of 96px for `S-07`."*
> *"Empty state — white card with a **dashed** border, 12px radius, centered, 64px/24px padding: a 16px/600 title, a 14px secondary line, and a primary call-to-action button where an action is available."*

Copy exacto de los estados vacíos (`Backlog.md` líneas 219-220):

| Pantalla | Título | Cuerpo | Botón |
|---|---|---|---|
| `S-04b` (My Requests, vacío) | `You haven't created any requests yet` | `Create your first absence request to get started.` | `Create request` |
| `S-07` vacío (Approval Queue, vacío) | `No pending requests` | `When an employee assigned to you submits a request, it will appear here.` | — (sin botón) |

**Reutilización, no creación paralela**: ambos componentes nuevos (`ListSkeleton`, `EmptyState`) son presentacionales puros, sin fetch ni estado — mismo patrón que `RequestRow`/`QueueCard`/`StateBadge`. Se crean una sola vez en `src/web/components/feedback/` (junto a `Banner`, el otro componente de feedback transversal) y los consumen ambas páginas con props distintas (conteo de bloques, copy, presencia o no del botón).

**Hallazgo colateral verificado en el código actual**: `/queue` conserva el bug detectado en la revisión de calidad de `US-023` (guard `queue === null && error === null`, que hace reaparecer "Loading…" si se descarta el banner de error sin que haya un fetch en curso) — nunca corregido porque quedó fuera de alcance de `US-023`/`US-024`/`US-030`/`US-031` y se registró como tarea de fondo (`task_a790a23a`). Esta historia **reescribe exactamente esa línea** para insertar el skeleton, así que el usuario confirmó aplicar aquí el mismo fix `loadFailed` que ya tiene `/requests` desde `US-024` (ver §5, `D-loadFailed`). La tarea de fondo se da por resuelta al cerrar esta historia.

**Historia solo Web — cero backend**: el skeleton y el estado vacío son estados derivados de datos que la API ya devuelve (`requests === null` mientras carga, `.length === 0` cuando no hay filas). Sin endpoints, DTOs ni cambios de contrato.

### 1.2 Narrativa (verbatim)

> "As a user, I want the list to tell me when it is loading and when there is nothing to show, so that I never face an ambiguous blank area."

### 1.3 Criterios de aceptación — verbatim (`Backlog.md` líneas 419-422)

| # | Criterio |
|---|---|
| `AC1` | "Given a list being fetched, when it renders, then the skeleton of §3.3 is shown — three 64px blocks on `S-04`, two 96px blocks on `S-07`." |
| `AC2` | "Given an employee with no requests, when `S-04` renders, then the `S-04b` empty state appears with its title, body and the `Create request` button." |
| `AC3` | "Given a manager with an empty queue, when `S-07` renders, then the empty state appears with its own copy and **no** action button." |
| `AC4` | "Given the empty state card, when it renders, then its border is dashed, distinguishing it from a populated row." |

### 1.4 Alcance

**Entra**: componente `ListSkeleton` (bloques grises, radio 10px, gap 10px, cantidad y alto parametrizables); componente `EmptyState` (card blanca, borde dashed, radio 12px, padding 64px/24px, título/cuerpo/botón opcional); reemplazo de los placeholders de texto en `/requests` y `/queue`; fix `loadFailed` en `/queue` (mismo patrón que `/requests`, D-loadFailed §5).

**No entra**

| Excluido | Por qué / destino |
|---|---|
| Modal de confirmación de cancelación | `US-033` |
| Modal de decisión con comentario | `US-034` |
| Contador `(N)` en la pestaña del manager | `US-035` |
| Accesibilidad transversal (skip link, foco, `Escape`) | `US-036` — el skeleton/empty-state de esta historia no necesita nada de eso para cumplir sus 4 criterios |
| Backend | Cero cambios — los datos que disparan cada estado (`null` mientras carga, arreglo vacío) ya existen |

---

## 2. Cambios estructurales / de base

**No se requieren cambios de esquema, migraciones, configuración, variables de entorno, feature flags, dependencias nuevas (npm ni NuGet), cambios de seed ni cambios de contrato de API.** Todo el trabajo es Web: dos componentes presentacionales nuevos, dos páginas modificadas y una entrada nueva de token de color en `globals.css`.

---

## 3. Plan ordenado por dependencia

**Sin ítems de Domain, Application, Infrastructure ni API.** Todo es Web (más su verificación).

| # | Capa | Acción | Artefacto | Notas |
|---|---|---|---|---|
| 1 | Web | Modificar | `src/web/app/globals.css` | Agregar `--color-skeleton: oklch(94% 0.004 250);` al bloque de tokens `:root` (valor verbatim del spec, no coincide con ningún token existente) y una clase `.skeleton-block { background: var(--color-skeleton); border-radius: 10px; }` reutilizable por alto vía `style` inline (el alto varía por pantalla: 64px vs 96px, no amerita una clase por variante) |
| 2 | Web | Crear | `src/web/components/feedback/ListSkeleton.tsx` | Presentacional puro: `{ count: number; blockHeight: number }` → `count` bloques `.skeleton-block` de `blockHeight`px, apilados con `gap: '10px'` (`AC1`) |
| 3 | Web | Crear | `src/web/components/feedback/EmptyState.tsx` | Presentacional puro: `{ title: string; body: string; actionLabel?: string; onAction?: () => void }` → card con borde dashed 1px, radio 12px, centrado, padding `64px 24px`; título 16px/600, cuerpo 14px secundario; botón `.btn-primary` solo si `actionLabel`/`onAction` están presentes (`AC2`, `AC3`, `AC4`) |
| 4 | Web | Modificar | `src/web/app/(app)/requests/page.tsx` | Reemplazar `{requests === null && !loadFailed && <p>Loading…</p>}` por `<ListSkeleton count={3} blockHeight={64} />`; reemplazar `<p>You haven't created any requests yet.</p>` por `<EmptyState title="You haven't created any requests yet" body="Create your first absence request to get started." actionLabel="Create request" onAction={() => router.push('/requests/new')} />` (`AC1`, `AC2`) |
| 5 | Web | Modificar | `src/web/app/(app)/queue/page.tsx` | (a) Agregar estado `loadFailed` y su seteo en el `.catch` del `useEffect` inicial, mismo patrón de `requests/page.tsx` (D-loadFailed, §5); (b) reemplazar `{queue === null && error === null && <p>Loading…</p>}` por `{queue === null && !loadFailed && <ListSkeleton count={2} blockHeight={96} />}` (`AC1`); (c) reemplazar `<p>No requests are waiting on your decision.</p>` por `<EmptyState title="No pending requests" body="When an employee assigned to you submits a request, it will appear here." />` sin `actionLabel` ni `onAction` (`AC3`, `AC4`) |
| 6 | Web | Verificar | `cd src/web && npm run lint && npm run typecheck && npm run depcruise && npm run build` | Regresión estática |
| 7 | Test | Verificar | `dotnet build VacaFlow.slnx` + `dotnet test VacaFlow.slnx` | Regresión pura — cero cambios backend |
| 8 | E2E | Verificar | Verificación manual §6 con la app corriendo (API + `npm run dev`) | Único punto donde `AC1`–`AC4` son demostrables juntos, incluida la regresión del fix `loadFailed` en `/queue` |

**Dependencias:** 1 → 2, 3 → 4, 5 → {6, 7, 8}. **Ruta crítica:** 1 → 2/3 → 4/5 → 8.

---

## 4. Casos de uso y tabla de trazabilidad

**Cero casos de uso nuevos en Application.** La historia completa la superficie de feedback visual sobre datos que los casos de uso existentes (`ListVisibleRequestsHandler`) ya devuelven.

| Historia | Criterio de aceptación (verbatim) | Ítems del plan que lo cubren | Cómo se verifica |
|---|---|---|---|
| `US-032` | "Given a list being fetched, when it renders, then the skeleton of §3.3 is shown — three 64px blocks on `S-04`, two 96px blocks on `S-07`." | #1, #2, #4, #5b | §6 pasos 2 y 6: recarga con throttling de red, contar bloques y su alto en devtools |
| `US-032` | "Given an employee with no requests, when `S-04` renders, then the `S-04b` empty state appears with its title, body and the `Create request` button." | #1, #3, #4 | §6 paso 3: cuenta sin requests → título, cuerpo y botón exactos; click navega a `/requests/new` |
| `US-032` | "Given a manager with an empty queue, when `S-07` renders, then the empty state appears with its own copy and **no** action button." | #1, #3, #5c | §6 paso 7: cuenta manager sin pendientes → copy exacto, sin botón en el DOM |
| `US-032` | "Given the empty state card, when it renders, then its border is dashed, distinguishing it from a populated row." | #3 | §6 pasos 3 y 7: inspección de `border-style: dashed` en devtools, contraste visual con una fila poblada (`border-style: solid` de `RequestRow`/`QueueCard`) |

**Conteo: 4 criterios de entrada · 4 cubiertos.**

---

## 5. Supuestos y decisiones

| # | Decisión | Justificación | Impacto si es incorrecta |
|---|---|---|---|
| `D1` | **Historia Web-only: cero ítems de backend** | El skeleton y el empty-state son estados derivados de `null`/`.length === 0`, ya producidos por el código existente | N/A — hecho del código |
| `D2` | **`ListSkeleton` y `EmptyState` viven en `components/feedback/`, junto a `Banner`** | Son componentes de feedback transversal consumidos por ambas páginas de listado, mismo criterio de ubicación que ya agrupa `Banner` allí desde `US-017` | Cosmético — mover un archivo es local |
| `D3` | **Un solo componente `ListSkeleton` parametrizado por `count`/`blockHeight`, no uno por pantalla** | `S-04` y `S-07` comparten la misma regla visual (bloques grises, 10px radio, 10px gap) y solo difieren en cantidad y alto — crear dos componentes duplicaría el marcado sin criterio que lo exija | Si una pantalla futura necesita una variante visual distinta, se extiende con una prop nueva, no se duplica |
| `D4` | **`EmptyState` decide el botón por presencia de `actionLabel`/`onAction`, no por una prop `variant`** | `S-04b` lo necesita, `S-07` vacío explícitamente no (`AC3`, "**no** action button") — una prop opcional expresa la ausencia con más precisión que un enum de dos variantes que solo difieren en eso | N/A — hecho verificado contra el criterio |
| `D5` | **Token nuevo `--color-skeleton`, no reutilizar `--color-border`** | El spec fija `oklch(94% 0.004 250)` verbatim para el skeleton; `--color-border` es `oklch(90% 0.006 250)` — valores distintos aunque visualmente cercanos, y ya existe precedente de tokens `--color-*` dedicados por caso de uso (`--color-success-bg` vs `--color-error-bg`) | Cosmético — fusionar tokens después es un cambio de una línea |
| `D-loadFailed` | **Se corrige en `/queue` el mismo bug que `/requests` ya resolvió en `US-024`** (guard `queue === null && error === null` → separar en `loadFailed`) | Confirmado por el usuario en esta sesión: esta historia reescribe exactamente esa línea para insertar el skeleton, así que dejar el bug intacto sería tocar el código sin corregir algo ya diagnosticado en el mismo lugar. La tarea de fondo `task_a790a23a` se da por resuelta al cerrar `US-032` | Si el usuario prefiriera no tocarlo, el ítem #5a se retira y el bug queda para la tarea de fondo — ya descartado por la respuesta recibida |
| `D6` | **Sin tests automatizados de frontend** | Ratificación de `US-023 D7`/`US-024 D5`/`US-025 D8`/`US-030 D10`/`US-031 D7`: sigue sin existir runner en `src/web/package.json` | Si el usuario quiere estrenar runner aquí, se añade como ítem previo |
| `S1` | **La rama se crea desde `main` (`a04baf1`) directamente** | `US-031` mergeada (PR #24); `origin/main` al día; árbol limpio verificado | Ninguno |

---

## 6. Verificación end-to-end

Con la API corriendo y `npm run dev` en `src/web/`, seed §3.6 (Laura manager; Carlos empleado asignado a ella).

| # | Paso | Resultado esperado |
|---|---|---|
| 1 | `dotnet build` + `dotnet test` · `cd src/web && npm run lint && npm run typecheck && npm run depcruise && npm run build` | Todo verde, 0 warnings |
| 2 | Como Carlos, con requests existentes: recargar `/requests` con throttling de red (devtools) — **`AC1`** | Durante la carga: 3 bloques grises de 64px, radio 10px, gap 10px, antes de que aparezca la lista real |
| 3 | Como un empleado sin requests (o cancelar/eliminar todas las visibles del seed para probar), cargar `/requests` — **`AC2`, `AC4`** | Card con borde dashed, radio 12px, título "You haven't created any requests yet", cuerpo "Create your first absence request to get started.", botón "Create request" que navega a `/requests/new` |
| 4 | Provocar un error de carga en `/requests` (detener la API momentáneamente) y descartar el banner de error | "Loading…"/skeleton no reaparece sin un fetch en curso (regresión del guard `loadFailed` ya existente) |
| 5 | Como Laura, con requests pendientes en la cola: recargar `/queue` con throttling — **`AC1`** | 2 bloques grises de 96px, radio 10px, gap 10px |
| 6 | Provocar un error de carga en `/queue` (detener la API momentáneamente) y descartar el banner de error — regresión del fix `D-loadFailed` | El skeleton no reaparece espontáneamente sin un fetch en curso — mismo comportamiento que el paso 4, ahora también en `/queue` |
| 7 | Como un manager sin requests pendientes (decidir todas las visibles del seed), cargar `/queue` — **`AC3`, `AC4`** | Card con borde dashed, título "No pending requests", cuerpo "When an employee assigned to you submits a request, it will appear here.", **sin ningún botón** en el DOM |
| 8 | Inspeccionar en devtools una fila poblada (`RequestRow`/`QueueCard`) junto a la card vacía — **`AC4`** | `border-style: solid` en la fila vs `border-style: dashed` en la card, confirmando el contraste que exige el criterio |

---

## 7. Riesgos y preguntas abiertas

**Sin preguntas abiertas pendientes.** La única ambigüedad real de esta historia (`D-loadFailed`) ya fue resuelta por el usuario antes de escribir este documento: se corrige el bug de `/queue` como parte del ítem #5.

| Riesgo | Mitigación |
|---|---|
| Vaciar la cola/lista del seed para probar los estados vacíos (pasos 3 y 7 de §6) altera datos de desarrollo compartidos | Se trabaja contra la base SQLite local de desarrollo, recreable con el seeder (`DatabaseSeeder`, `TE-003`); no es un entorno compartido |
| `EmptyState` sin `actionLabel` podría interpretarse como "botón oculto por CSS" en vez de "nunca renderizado" | El ítem #3 lo resuelve por ausencia condicional del `<button>` en el JSX (`{actionLabel && onAction && <button>…</button>}`), no por `display: none` — verificable en el DOM real, no solo visualmente |
| Confundir el zócalo del skeleton con el de una card real durante el throttling de red | El bloque no lleva texto ni interacción; su único propósito es ocupar espacio con la altura correcta mientras se resuelve el fetch |

---

Siguiente paso — implementación:

```
/user-story-implement ".claude/implementation plans/US-032-plan.md"
```
