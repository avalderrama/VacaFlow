# Plan de implementación — `US-035` · Pending count on the manager tab

| Campo | Valor |
|---|---|
| Historia | `US-035` — Pending count on the manager tab |
| Épica | `EP-03` — Application shell and feedback |
| Prioridad · Talla | **Should** · `S` |
| Pantalla | `S-03` (Application shell — header, nav) |
| Depende de | `US-030` (Application shell, mergeada — PR #23), `US-020` (listado visible para el manager, mergeado) — **sin precondiciones pendientes** |
| Traza | `Backlog.md` líneas 424-429 |
| Fuentes | `Backlog.md` v2.0 · código real verificado en `main` (commit `7c3061d` — `US-032` mergeada, PR #25), archivo por archivo en `src/web/` |
| Repositorio | `C:\Users\avald\OneDrive\Work\VacaFlow\repositories\vacaflow` |
| Rama sugerida | `feat/us-035-pending-count-manager-tab`, creada desde `main` (`7c3061d`) |
| Estado | Borrador presentado para aprobación — sin preguntas abiertas bloqueantes (ver §5 para la decisión arquitectónica principal) |

> **Este documento no implementa nada.** La implementación la ejecuta `/user-story-implement`.

---

## 1. Entendimiento

### 1.1 Contexto

Hoy, `NavTabs.tsx` (`src/web/components/shell/NavTabs.tsx`) renderiza la pestaña `Approval Queue` como texto fijo, sin ningún conteo:

```tsx
{role === 'Manager' && (
  <Link href="/queue" ...>Approval Queue</Link>
)}
```

`US-035` le agrega el número de solicitudes pendientes de decisión, entre paréntesis, cuando hay al menos una.

**De dónde sale "pendiente":** el backend ya resuelve esto exactamente. `ListVisibleRequestsHandler.cs` (líneas 30-34, verificado) construye la lista visible del manager como la unión de sus propias requests (`ListOwnedByAsync`) más **solo las `Submitted`** de su equipo (`ListPendingForManagerAsync` — el propio nombre y su doc-comment en `IRequestRepository.cs` confirman el filtro por estado en el repositorio, no en el cliente). El filtro cliente que ya usa `queue/page.tsx` (`request.employee.id !== me.id`) por lo tanto **ya aísla exactamente las pendientes de decisión** — no hace falta filtrar por estado en el cliente porque el backend nunca envía una fila ajena que no sea `Submitted`. El conteo de esta historia es, literalmente, `queue.length` tal como `queue/page.tsx` ya lo calcula.

**El problema real de esta historia no es el cálculo, es la propagación.** `AppHeader` (que renderiza `NavTabs`) y `QueuePage` (que decide sobre las requests) son **hermanos** en el árbol — ambos cuelgan de `(app)/layout.tsx`, uno como `<AppHeader/>` fijo y el otro como `{children}` de la ruta activa. `AppHeader` no vuelve a montarse al navegar entre rutas (es el shell persistente, `US-030`), así que:

- El conteo debe estar disponible en **cualquier pantalla**, no solo en `/queue` — un manager que aterriza en `/requests` ya debe ver `Approval Queue (3)` sin haber visitado la cola. Esto obliga a que el conteo se obtenga de forma **independiente** de `QueuePage`, no derivado de su estado local.
- Cuando una decisión (`approve`/`reject`) vacía la cola **estando en `/queue`**, el criterio exige que el número desaparezca **sin recargar la página** (`AC3`). Como `AppHeader` no se remonta al no haber navegación, `QueuePage` necesita una forma de **notificarle** que refresque su conteo — no hay recarga de página que lo haga por sí sola.

Esto es, en esencia, un problema de estado compartido entre componentes no emparentados directamente. La app no tiene hoy ningún mecanismo de estado compartido entre componentes (todo es `useState` por página) — **esta es la primera vez que se necesita uno**, y se documenta como tal en §5 (`D1`).

**Historia solo Web — cero backend.** El endpoint `GET /api/requests` y `ListPendingForManagerAsync` ya existen y ya filtran correctamente (`US-020`). Nada que agregar en Domain, Application, Infrastructure ni API.

### 1.2 Narrativa

La historia no trae narrativa "As a... I want... so that" explícita en `Backlog.md` (a diferencia de otras); solo trae los tres criterios de aceptación siguientes.

### 1.3 Criterios de aceptación — verbatim (`Backlog.md` líneas 427-429)

| # | Criterio |
|---|---|
| `AC1` | "Given a manager with pending requests, when the header renders, then the tab reads `Approval Queue (N)` with N the number of requests awaiting their decision." |
| `AC2` | "Given a manager with none pending, when the header renders, then the tab reads `Approval Queue` with no parenthetical." |
| `AC3` | "Given a decision that empties the queue, when it completes, then the count disappears without a page reload." |

### 1.4 Alcance

**Entra**: un mecanismo de estado compartido (Context de React) que calcula el conteo de pendientes una sola vez por sesión de navegación y lo expone tanto a `AppHeader`/`NavTabs` (para pintarlo) como a `QueuePage` (para refrescarlo tras una decisión); el cambio visual en `NavTabs` (paréntesis condicional).

**No entra**

| Excluido | Por qué / destino |
|---|---|
| Cambios en `GET /api/requests`, `ListVisibleRequestsHandler` o `ListPendingForManagerAsync` | Ya filtran correctamente para el manager (`US-020`) — verificado en §1.1 |
| Actualización en tiempo real ante cambios hechos por **otro** usuario (ej. un empleado que envía una request mientras el manager ya está en `/requests`) | Sin criterio que lo exija; `AC3` solo cubre el caso donde la propia decisión del manager vacía la cola. Push/polling en tiempo real es un feature distinto, no pedido aquí |
| Accesibilidad transversal (anuncio del cambio de conteo a lectores de pantalla) | `US-036`. El `<Link>` ya es un texto plano — su cambio de contenido es detectable por defecto sin `aria-live`, que no está pedido por ningún criterio de esta historia |

---

## 2. Cambios estructurales / de base

**No se requieren cambios de esquema, migraciones, configuración, variables de entorno, feature flags, dependencias nuevas (npm ni NuGet) ni cambios de contrato de API.** El backend ya expone exactamente los datos necesarios (`GET /api/requests`, verificado en `main`).

---

## 3. Plan ordenado por dependencia

**Sin ítems de Domain, Application, Infrastructure ni API.** Todo es Web.

| # | Capa | Acción | Artefacto | Notas |
|---|---|---|---|---|
| 1 | Web | Crear | `src/web/components/shell/PendingQueueCountProvider.tsx` | Context de React: `PendingQueueCountProvider` (componente) + hook `usePendingQueueCount()` → `{ count: number \| null; refresh: () => Promise<void> }`. Al montar, si `me.role === 'Manager'` llama `getMe()` + `listRequests()` y cuenta `requests.filter(r => r.employee.id !== me.id).length` (idéntico al filtro de `fetchQueue` en `queue/page.tsx`, D1); si es `Employee`, `count` queda en `null` sin llamar a la API (D3). `refresh()` repite el mismo cálculo bajo demanda |
| 2 | Web | Modificar | `src/web/app/(app)/layout.tsx` | Envolver `<AppHeader/>` y `{children}` dentro de `<PendingQueueCountProvider>`, para que ambos hermanos compartan el mismo estado |
| 3 | Web | Modificar | `src/web/components/shell/AppHeader.tsx` | Consumir `usePendingQueueCount()` y pasar `count` a `<NavTabs role={me.role} pendingCount={count} />` |
| 4 | Web | Modificar | `src/web/components/shell/NavTabs.tsx` | Aceptar `pendingCount: number \| null`; renderizar `Approval Queue (${pendingCount})` cuando `pendingCount` es un número `> 0`, y `Approval Queue` a secas en cualquier otro caso (`0`, `null` o aún cargando) — cubre `AC1`/`AC2` con una sola condición (D2) |
| 5 | Web | Modificar | `src/web/app/(app)/queue/page.tsx` | En `decide()`, después del refetch existente de la cola (`setQueue(await fetchQueue())`), llamar también a `refresh()` del hook — mismo bloque `try/catch` no bloqueante que ya usa el refetch de la cola (`console.error` en fallo, sin pisar el banner de éxito) |
| 6 | Web | Verificar | `cd src/web && npm run lint && npm run typecheck && npm run depcruise && npm run build` | Regresión estática |
| 7 | Test | Verificar | `dotnet build VacaFlow.slnx` + `dotnet test VacaFlow.slnx` | Regresión pura — cero cambios backend |
| 8 | E2E | Verificar | Verificación manual §6 con la app corriendo (API + `npm run dev`) | Único punto donde `AC1`–`AC3` son demostrables juntos |

**Dependencias:** 1 → 2 → 3 → 4; 1 → 5 (puede hacerse en paralelo con 3/4). **Ruta crítica:** 1 → 2 → 3 → 4 → 8.

---

## 4. Casos de uso y tabla de trazabilidad

**Cero casos de uso nuevos en Application.** El conteo se deriva de datos que `ListVisibleRequestsHandler` ya devuelve.

| Historia | Criterio de aceptación (verbatim) | Ítems del plan que lo cubren | Cómo se verifica |
|---|---|---|---|
| `US-035` | "Given a manager with pending requests, when the header renders, then the tab reads `Approval Queue (N)` with N the number of requests awaiting their decision." | #1, #2, #3, #4 | §6 paso 2: manager con N pendientes en cualquier pantalla (no solo `/queue`) → pestaña lee `Approval Queue (N)` |
| `US-035` | "Given a manager with none pending, when the header renders, then the tab reads `Approval Queue` with no parenthetical." | #1, #3, #4 | §6 paso 3: manager sin pendientes → pestaña lee `Approval Queue` sin paréntesis |
| `US-035` | "Given a decision that empties the queue, when it completes, then the count disappears without a page reload." | #1, #5 | §6 paso 4: aprobar/rechazar la última pendiente estando en `/queue` → el conteo desaparece de la pestaña sin navegar ni recargar |

**Conteo: 3 criterios de entrada · 3 cubiertos.**

---

## 5. Supuestos y decisiones

| # | Decisión | Justificación | Impacto si es incorrecta |
|---|---|---|---|
| `D1` | **Se introduce el primer Context de React de la app** (`PendingQueueCountProvider`), en vez de un bus de eventos del DOM (`window.dispatchEvent`) o de duplicar el fetch en cada página | `AppHeader` y `QueuePage` son hermanos, no padre-hijo — no hay forma de compartir estado sin prop-drilling a través de todo el layout o sin un mecanismo de suscripción. Context es el mecanismo idiomático de React para exactamente este problema (estado compartido entre componentes no emparentados) y es más testeable/legible que un `EventEmitter` casero. Alternativa de polling por intervalo fue descartada por no estar atada a un evento real (`AC3` es event-driven: "when it completes") | Cambia el patrón establecido de "todo es `useState` local por página" — es una extensión legítima, no una violación, porque ningún patrón previo cubre estado compartido entre hermanos. Si el usuario prefiere otro mecanismo, es un cambio local a este único archivo nuevo |
| `D2` | **La condición de render es `pendingCount > 0`** (no `pendingCount !== null` ni `!== 0`) | Cubre `AC1` (`N > 0` → paréntesis) y `AC2` (`N === 0` → sin paréntesis) con una sola rama; el estado transitorio "aún cargando" (`null`, antes de que `getMe()`/`listRequests()` resuelvan) cae naturalmente en "sin paréntesis", que es el estado más conservador (nunca muestra un número incorrecto o parpadea) | N/A — hecho de la implementación, sin ambigüedad de criterio |
| `D3` | **El Provider no llama a la API si el rol es `Employee`** | Evita una llamada a `GET /api/requests` innecesaria para el 100% de las pantallas que ve un empleado (que nunca ve la pestaña `Approval Queue`, gateada por rol en `NavTabs` desde `US-030`) | Ninguno — optimización pura, sin afectar ningún criterio |
| `D4` | **El conteo reutiliza el mismo filtro que `fetchQueue`** (`employee.id !== me.id`), sin filtrar además por `state` en el cliente | Verificado en `ListVisibleRequestsHandler.cs` (§1.1): el backend garantiza que toda fila ajena en la respuesta ya es `Submitted` — filtrar por estado en el cliente sería código redundante contra una invariante que el propio backend documenta y aplica | Si esa invariante backend cambiara alguna vez (por ejemplo, si se agregara otro estado visible al manager), este conteo se rompería junto con el propio `/queue` actual — mismo riesgo compartido, no uno nuevo |
| `D5` | **`refresh()` se llama solo tras una decisión exitosa**, en el mismo bloque no bloqueante que ya usa `setQueue(await fetchQueue())` | Consistente con el manejo de errores ya establecido en `decide()` (`US-023`/`US-031`): un fallo al refrescar el conteo no debe pisar el banner de éxito de la decisión ya tomada | Si `refresh()` fallara silenciosamente, el conteo quedaría desactualizado hasta la próxima navegación — mismo riesgo que ya acepta el refetch de la cola misma |
| `D6` | **Sin tests automatizados de frontend** | Ratificación de `US-023 D7`/.../`US-032` (sigue sin existir runner en `src/web/package.json`) | Si se estrena runner, se añade como ítem previo |
| `S1` | **La rama se crea desde `main` (`7c3061d`) directamente** | `US-032` mergeada (PR #25); `origin/main` al día; árbol limpio verificado | Ninguno |

---

## 6. Verificación end-to-end

Con la API corriendo y `npm run dev` en `src/web/`, seed §3.6 (Laura manager; Carlos y Ana empleados asignados a ella).

| # | Paso | Resultado esperado |
|---|---|---|
| 1 | `dotnet build` + `dotnet test` · `cd src/web && npm run lint && npm run typecheck && npm run depcruise && npm run build` | Todo verde, 0 warnings |
| 2 | Como Carlos: crear y enviar (`Submit`) una o más requests para que Laura tenga pendientes. Como Laura: aterrizar directamente en `/requests` (no en `/queue`) — **`AC1`** | La pestaña lee `Approval Queue (N)` con N = cantidad real de `Submitted` de su equipo, visible sin haber entrado nunca a `/queue` |
| 3 | Como Laura: decidir (aprobar o rechazar) todas las pendientes hasta vaciar la cola, sin salir de `/queue` — **`AC3`** | El número en la pestaña baja en cada decisión y, al llegar a 0, la pestaña pasa a leer `Approval Queue` sin paréntesis — todo sin recargar la página ni navegar |
| 4 | Con la cola en 0 (paso 3), navegar a `/requests` y volver a `/queue` — **`AC2`** | La pestaña sigue leyendo `Approval Queue` sin paréntesis en ambas pantallas |
| 5 | Como Carlos: enviar una nueva request. Como Laura, **sin recargar**, navegar de `/requests` a `/queue` (navegación normal de la SPA) | La pestaña ya refleja `Approval Queue (1)` al llegar — confirma que el conteo inicial no quedó pegado en 0 de una carga previa |

---

## 7. Riesgos y preguntas abiertas

**Sin preguntas abiertas bloqueantes.** La decisión de introducir un Context (`D1`) es, en la práctica, la única solución idiomática al problema real de esta historia (estado compartido entre hermanos) — se documenta prominentemente en §5 en vez de plantearse como pregunta, porque las alternativas (bus de eventos del DOM, polling) son estrictamente peores para el mismo resultado, no lecturas igualmente válidas del criterio.

| Riesgo | Mitigación |
|---|---|
| Doble llamada a `getMe()` en el mismo montaje de página (una desde `AppHeader`, otra desde el nuevo Provider) | Ya es el patrón establecido en esta app — cada página y `AppHeader` llaman a `getMe()` de forma independiente (`requests/page.tsx`, `queue/page.tsx`, `AppHeader.tsx` ya lo hacen cada uno por su lado). No es una regresión, es consistencia con lo existente |
| `refresh()` y el refetch de la propia cola (`fetchQueue`) hacen dos llamadas a `GET /api/requests` casi simultáneas tras cada decisión | Aceptado — mismo patrón de duplicación ya presente entre páginas distintas; unificar ambos fetches en una sola fuente de verdad es una refactorización mayor sin criterio que la exija |
| El conteo podría parpadear a `null` (sin paréntesis) durante una fracción de segundo en cada `refresh()` antes de resolver | Coherente con `D2`: el estado transitorio nunca muestra un número incorrecto, solo omite el paréntesis brevemente — no viola ningún criterio |

---

Siguiente paso — implementación:

```
/user-story-implement ".claude/implementation plans/US-035-plan.md"
```
