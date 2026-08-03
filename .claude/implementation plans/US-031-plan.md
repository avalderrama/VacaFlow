# Plan de implementación — `US-031` · Notification banner

| Campo | Valor |
|---|---|
| Historia | `US-031` — Notification banner |
| Épica | `EP-03` — Application shell and feedback |
| Prioridad · Talla | **Must** · `S` |
| Pantalla | `S-03` (Application shell — header, nav, banner) — codueña con `US-030`/`US-035` (`Backlog.md` §3.2, fila `S-03 → US-030, US-031, US-035`) |
| Depende de | `US-030` (Application shell, mergeada — PR #23, commit `cff4009`) — **sin precondiciones pendientes** |
| Trazas | `FR-UIX-003` (*"Every API error is displayed to the user with its message. No error is silently swallowed, and no failure is presented as a success."*) · `NFR-USA-002` (*"Create, edit, submit, cancel, approve and reject each produce a perceptible result… No action completes silently."*) · `Backlog.md` §3.3 (**Banner**) y §3.5 (catálogo de mensajes y `aria-label="Dismiss notification"`) |
| Fuentes | `Backlog.md` v2.0 · `FRD.md` · `NFR.md` · código real verificado en **`main` (commit `cff4009` — `US-030` mergeada, PR #23)**, archivo por archivo en `src/web/` · planes previos `US-017` (origen del componente `Banner`), `US-018`, `US-019`, `US-023`, `US-024`, `US-030` |
| Repositorio | `C:\Users\avald\OneDrive\Work\VacaFlow\repositories\vacaflow` |
| Rama sugerida | `feat/us-031-notification-banner`, creada desde `main` (`cff4009`) |
| Estado | Borrador presentado para aprobación (sesión de planificación delegada; **dos preguntas abiertas en §7 — `OQ-A` reduced motion, `OQ-B` apilamiento de banners**) |

> **Este documento no implementa nada.** La implementación la ejecuta `/user-story-implement`.

---

## 1. Entendimiento

### 1.1 Contexto — historia de **verificación y compleción**, no de construcción

Esta historia es deliberadamente pequeña y el plan lo respeta: **el componente `Banner` ya existe desde `US-017` y ya lo consumen todas las páginas que lo necesitan.** Su propio doc-comment lo anticipó, verbatim (`src/web/components/feedback/Banner.tsx`, líneas 3–6, verificado en `main` `cff4009`):

> *"Minimal form of the banner (Backlog.md §3.3). The full Given/When/Then matrix — clear-on-navigate, 150ms fade — is US-031's to verify; this component is written once, against the spec, so US-031 completes it instead of rewriting it (US-017 plan D4)."*

Ese comentario es la frontera de alcance real: **`US-031` verifica la matriz completa contra §3.3/§3.5 y completa lo único que falta.** Auditoría cláusula a cláusula del spec §3.3 (**Banner** — *"inside the content column, 12px/16px padding, 8px radius, `role="status"`, message on the left and a `×` dismiss button on the right, 150ms fade-in. Success: background `oklch(93% 0.06 150)`, text `oklch(30% 0.12 150)`. Error: background `oklch(95% 0.03 25)`, text `oklch(35% 0.15 25)`."*) contra el componente actual:

| Cláusula §3.3 / §3.5 | Estado en `main` (`cff4009`) |
|---|---|
| *inside the content column* | ✅ Cada página lo renderiza como primer hijo de su contenido, dentro del `<main>` del shell (`requests/page.tsx` L84–87, `queue/page.tsx` L68–71, `requests/[id]/page.tsx` L68–70) |
| *12px/16px padding* | ✅ `padding: '12px 16px'` (`Banner.tsx` L28) |
| *8px radius* | ✅ `borderRadius: 'var(--radius-control)'` = `8px` (`globals.css`) |
| *`role="status"`* | ✅ `Banner.tsx` L23 |
| *message on the left and a `×` dismiss button on the right* | ✅ flex `space-between`; botón `×` (L36–50) |
| *`aria-label="Dismiss notification"`* (§3.5) | ✅ `Banner.tsx` L39, verbatim |
| paleta success (`oklch(93% 0.06 150)` / `oklch(30% 0.12 150)`) | ✅ vía `--color-success-bg`/`--color-success-text`, valores verbatim en `globals.css` L10–11 |
| paleta error (`oklch(95% 0.03 25)` / `oklch(35% 0.15 25)`) | ✅ vía `--color-error-bg`/`--color-error-text`, valores verbatim en `globals.css` L13/L15 |
| ***150ms fade-in*** | ❌ **No existe.** `grep` de `transition`/`animation`/`@keyframes`/`fade` en `src/web/app/globals.css` y en el componente: cero resultados. El banner aparece instantáneo. **Es el único trabajo de código de esta historia** |

**Clear-on-navigate (criterio 4) — ya se cumple por construcción, se verifica aquí:**

- El estado del banner (`notification`/`error`/`cancelError`) vive como `useState` **dentro de cada página**. En el App Router de Next.js, navegar a otra ruta **desmonta el componente de página** (el layout/shell persiste, las páginas no) — el estado muere con la página y volver atrás monta una instancia fresca con estado `null`. No hay carry-over posible de estado en memoria.
- El único transporte deliberado entre rutas es `lib/session.ts`: `setPendingNotification` / `consumePendingNotification` sobre `sessionStorage` (clave `vacaflow.pendingNotification`), usado exactamente en tres puntos — `RequestForm.tsx` L125 (`'Draft created.'`), L128 (`'Changes saved.'`) y `requests/[id]/page.tsx` L33 (`'Request cancelled.'`) — siempre inmediatamente antes de un redirect intencional a `/requests`, que lo consume en `requests/page.tsx` L37–39 con un lazy initializer. **`consumePendingNotification` borra la clave al leerla** (`session.ts` L12–14): un solo consumo, sin reaparición al volver a navegar. Esto es el mecanismo de *"appears at the top of the content column"* tras una acción que redirige — no una violación del criterio de limpieza, que gobierna banners **ya mostrados** que no deben viajar.
- Distinción clave del plan: `pendingNotification` = carry **hacia** la pantalla destino de una acción (una vez); estado de página = banner **mostrado**, que muere al navegar. Ambas mitades ya se comportan según el criterio; esta historia lo demuestra en §6.

**Cobertura actual de mensajes §3.5 (auditada página por página):** `Draft created.` y `Changes saved.` (`RequestForm` → redirect → `/requests`) · `Request submitted for approval.` y `Request cancelled.` inline en `/requests` (L73) · `Request cancelled.` desde el detalle vía redirect · `Request approved.` / `Request rejected.` inline en `/queue` (L53) · errores de API con `variant="error"` en las tres páginas. `Account created. Welcome to VacaFlow!` y `Signed in as {name}.` **no están cableados** — pertenecen a `US-014`/`US-013` (pantallas de registro/sign-in completas, no mergeadas; el sign-in actual es el stub de `US-009` que solo hace `router.push('/requests')`). No son alcance de `US-031`, cuyo mecanismo (`setPendingNotification` antes del redirect) quedará listo para que esas historias lo usen.

**Historia solo Web — cero backend, verificado explícitamente:** el banner es una preocupación de renderizado del cliente sobre respuestas que los endpoints existentes ya devuelven (`ApplicationError.apiError.message` en `lib/api.ts`). Como `US-023`/`US-024`/`US-025`/`US-030`: ningún ítem de Domain, Application, Infrastructure ni API.

### 1.2 Narrativa (verbatim)

> "As a user, I want every action to tell me what happened, so that no operation completes silently."

### 1.3 Criterios de aceptación — verbatim (`Backlog.md` líneas 408–412)

| # | Criterio |
|---|---|
| `AC1` | "Given a successful action, when it completes, then the success banner of §3.5 appears at the top of the content column with `role="status"`." |
| `AC2` | "Given a rejected action, when the error returns, then the error banner shows the message from §3.5 in the error palette." |
| `AC3` | "Given a banner, when I press `×`, then it disappears." |
| `AC4` | "Given a banner, when I navigate to another screen, then it is cleared rather than carried over." |
| `AC5` | "Given a banner, when it appears, then it fades in over 150ms." |

### 1.4 Alcance

**Entra**: fade-in de 150ms en `Banner` (única brecha de código, `AC5`); actualización del doc-comment del componente (la matriz deja de estar "pendiente de `US-031`"); verificación demostrativa de `AC1`–`AC4` contra el código y en E2E (§6).

**No entra**

| Excluido | Por qué / destino |
|---|---|
| Banners `Account created…` y `Signed in as {name}.` | `US-014`/`US-013` (pantallas de auth completas, no mergeadas). El mecanismo que usarán ya existe |
| Centralizar el banner en el shell (`(app)/layout.tsx`) o en un contexto/hook compartido | Sin criterio que lo pida. El patrón por página ya satisface §3.3 (*"inside the content column"*) y `AC4` sale gratis del desmontaje por página; centralizarlo obligaría a implementar la limpieza al navegar a mano (un listener de ruta) — más código para el mismo comportamiento. Ratifica `US-030 OQ-A(a)` |
| Reemplazar `alert-general` del error de carga del detalle (`requests/[id]/page.tsx` L49–58) por un `Banner` | `AC2` gobierna *"a rejected action"*; el fallo de **carga** de la página no es una acción rechazada. El patrón `alert-general` (`role="alert"`) viene de `US-017` y sigue siendo válido — `D5` |
| Modales de confirmación/decisión | `US-033`/`US-034` |
| Backend | Cero cambios — verificado (§1.1) |

---

## 2. Cambios estructurales / de base

**No se requieren cambios de esquema, migraciones, configuración, variables de entorno, feature flags, dependencias nuevas (npm ni NuGet), cambios de seed ni cambios de contrato de API.** El único artefacto tocado es CSS + un componente cliente existente.

---

## 3. Plan ordenado por dependencia

**Sin ítems de Domain, Application, Infrastructure ni API.** Todo es Web (más su verificación). El plan es deliberadamente corto: la historia completa un componente existente, no construye uno.

| # | Capa | Acción | Artefacto | Notas |
|---|---|---|---|---|
| 1 | Web | Modificar | `src/web/app/globals.css` | Añadir `@keyframes banner-fade-in { from { opacity: 0; } to { opacity: 1; } }` junto al catálogo de clases existente (mismo racional que `.btn-*`/`.alert-general`: definiciones compartidas viven aquí; `@keyframes` no puede declararse inline). Sujeto a `OQ-A`: guard `@media (prefers-reduced-motion: reduce)` que anule la animación |
| 2 | Web | Modificar | `src/web/components/feedback/Banner.tsx` | (a) Añadir `animation: 'banner-fade-in 150ms ease-out'` al style del contenedor (`AC5`, valor verbatim del spec); (b) actualizar el doc-comment: la matriz Given/When/Then deja de ser "US-031's to verify" — pasa a documentar que el componente está completo contra §3.3 y cómo se satisface clear-on-navigate (estado por página + consumo único de `pendingNotification`); (c) el comentario de la prop `variant` ("'error' is unreachable code today") ya es falso desde `US-018`/`US-023` — corregirlo de paso, es el mismo bloque |
| 3 | Web | Verificar | `cd src/web && npm run lint && npm run typecheck && npm run depcruise && npm run build` | Regresión estática; sin imports nuevos, `depcruise` no debería inmutarse |
| 4 | Test | Verificar | `dotnet build VacaFlow.slnx` + `dotnet test VacaFlow.slnx` | Regresión pura — cero cambios backend |
| 5 | E2E | Verificar | Verificación manual §6 con la app corriendo (API + `npm run dev`) | Único punto donde `AC1`–`AC5` son demostrables juntos; incluye la demostración explícita de `AC4` (clear-on-navigate) y la re-verificación de `Draft created.` vía `sessionStorage` (riesgo StrictMode, §7) |

**Dependencias:** 1 → 2 → {3, 4, 5}. **Ruta crítica:** 1 → 2 → 5. `OQ-A` decide un bloque de 3 líneas en #1; `OQ-B` no toca ningún ítem (ratifica el comportamiento actual o añade una línea en `requests/page.tsx`).

---

## 4. Casos de uso y tabla de trazabilidad

**Cero casos de uso nuevos en Application.** La historia completa la superficie de feedback que consumen los casos de uso existentes (create/edit/submit/cancel/approve/reject).

| Historia | Criterio de aceptación (verbatim) | Ítems del plan que lo cubren | Cómo se verifica |
|---|---|---|---|
| `US-031` | "Given a successful action, when it completes, then the success banner of §3.5 appears at the top of the content column with `role="status"`." | Ya satisfecho en `main` (§1.1: `role="status"` en `Banner.tsx` L23; los 6 mensajes §3.5 alcanzables cableados); #5 lo demuestra | §6 pasos 2–5: submit/cancel en `/requests`, approve/reject en `/queue`, create/edit vía redirect — banner verde primero en la columna, `role="status"` en inspección |
| `US-031` | "Given a rejected action, when the error returns, then the error banner shows the message from §3.5 in the error palette." | Ya satisfecho en `main` (§1.1: `variant="error"` con `--color-error-bg`/`--color-error-text` verbatim; mensajes del catálogo vía `ApplicationError.apiError.message`); #5 lo demuestra | §6 paso 6: provocar `VF-REQ-002` (submit de draft con fecha pasada) y un rechazo de decisión — banner rojo con el mensaje exacto del catálogo §3.5 |
| `US-031` | "Given a banner, when I press `×`, then it disappears." | Ya satisfecho en `main` (`onDismiss` → estado `null` en las tres páginas); #5 lo demuestra | §6 paso 7: pulsar `×` en un banner de cada variante; no reaparece al volver a la ruta (estado fresco + `pendingNotification` ya consumida) |
| `US-031` | "Given a banner, when I navigate to another screen, then it is cleared rather than carried over." | Ya satisfecho por construcción (§1.1: desmontaje de página en App Router + consumo único de `sessionStorage`); #2b lo documenta en el componente; #5 lo demuestra | §6 pasos 8–9: banner visible en `/requests` → navegar a `/queue` y volver → sin banner; ídem desde `/queue` |
| `US-031` | "Given a banner, when it appears, then it fades in over 150ms." | **#1, #2a** (única brecha de código) | §6 paso 10: fade perceptible al aparecer; inspección de `animation-duration: 150ms` en devtools |

**Conteo: 5 criterios de entrada · 5 cubiertos.** Cuatro se cubren con verificación demostrativa sobre código ya en `main` (más la actualización de documentación #2b); uno (`AC5`) requiere código nuevo (#1, #2a). El plan no fabrica trabajo donde el criterio ya está pagado.

---

## 5. Supuestos y decisiones

Sesión de planificación delegada (Fase 3 no interactiva): decisiones de arquitecto documentadas con su reversibilidad. **Las dos que merecen ratificación del usuario están elevadas a §7 (`OQ-A`, `OQ-B`).**

| # | Decisión | Justificación | Impacto si es incorrecta |
|---|---|---|---|
| `D1` | **Historia Web-only: cero ítems de backend** | El banner renderiza mensajes que la API ya devuelve (`ApplicationError` en `lib/api.ts`); ningún endpoint nuevo ni cambiado | N/A — hecho del código |
| `D2` | **Completar el `Banner` existente, no reescribirlo ni centralizarlo** | El doc-comment de `US-017` prescribe exactamente esto (*"US-031 completes it instead of rewriting it"*); 8 de 9 cláusulas §3.3 ya se cumplen carácter a carácter (§1.1). Crear un provider/contexto duplicaría el sistema sin criterio que lo respalde | N/A — hecho verificado |
| `D3` | **Fade-in como `@keyframes` en `globals.css` + `animation` inline de 150ms en el componente** | `@keyframes` no puede vivir inline; `globals.css` es ya el catálogo de definiciones compartidas (`.btn-*`, `.alert-general`, tokens). `animation` (no `transition`) porque el banner aparece por montaje condicional (`{notification && …}`) — no hay estado previo del que transicionar. `ease-out` como curva por defecto razonable (el spec solo fija la duración) | Cosmético — cambiar curva o mecanismo es local |
| `D4` | **`AC4` se satisface con la arquitectura actual (estado por página + consumo único), sin listener de rutas** | Verificado: navegar desmonta la página y su estado; `consumePendingNotification` borra la clave al leer (`session.ts` L12–14). Añadir un listener de `pathname` sería código para un comportamiento que ya existe | Si E2E (§6 pasos 8–9) revelara un carry-over real, se reabre — pero el mecanismo está trazado línea a línea |
| `D5` | **El error de carga del detalle (`alert-general`) no se convierte a `Banner`** | `AC2` dice *"a rejected action"*; el fallo de carga no es una acción del usuario rechazada. `alert-general` (`role="alert"`) es el patrón de error de formulario/página de `US-017` y sigue vigente. `/requests` y `/queue` sí usan `Banner` para su error de carga — heterogeneidad preexistente, sin criterio que la unifique | Si el usuario quiere uniformar, es un swap local de una línea — ampliación, no corrección |
| `D6` | **Los banners `Account created…` / `Signed in as {name}.` quedan fuera** | Pertenecen a `US-014`/`US-013` (no mergeadas; el sign-in actual es el stub de `US-009`). `US-031` entrega el mecanismo, no esos flujos | Ninguno — trazado en §1.4 |
| `D7` | **Sin tests automatizados de frontend; verificación = lint + typecheck + depcruise + build + E2E manual** | Ratificación de `US-023 D7`/`US-024 D5`/`US-025 D8`/`US-030 D10`: sigue sin existir runner en `src/web/package.json` (verificado) | Si el usuario quiere estrenar runner aquí, se añade como ítem previo |
| `S1` | **La rama se crea desde `main` (`cff4009`) directamente** | `US-030` mergeada (PR #23); `origin/main` al día; árbol limpio verificado | Ninguno |

---

## 6. Verificación end-to-end

Con la API corriendo y `npm run dev` en `src/web/`, seed §3.6 (Laura manager; Carlos empleado asignado a ella).

| # | Paso | Resultado esperado |
|---|---|---|
| 1 | `dotnet build` + `dotnet test` · `cd src/web && npm run lint && npm run typecheck && npm run depcruise && npm run build` | Todo verde, 0 warnings |
| 2 | Como Carlos: crear un draft (`/requests/new` → `Create request`) — **`AC1`** + riesgo StrictMode | Aterrizaje en `/requests` con banner verde `Draft created.` como primer elemento de la columna de contenido; `role="status"` y `aria-label="Dismiss notification"` en inspección. **Verificar expresamente en `npm run dev` (StrictMode activo)** — ver riesgo §7 |
| 3 | Editar el draft y guardar — **`AC1`** | Redirect a `/requests` con `Changes saved.` |
| 4 | `Submit` del draft en `/requests`; luego `Cancel` — **`AC1`** | Banners `Request submitted for approval.` y `Request cancelled.` inline, sin recarga |
| 5 | Como Laura en `/queue`: aprobar una request y rechazar otra — **`AC1`** | `Request approved.` / `Request rejected.` inline |
| 6 | Como Carlos: draft con fecha de inicio pasada (seed o data vieja) → `Submit` — **`AC2`** | Banner rojo con paleta error y el mensaje verbatim de `VF-REQ-002`: `The start date cannot be in the past.` |
| 7 | Pulsar `×` en un banner verde y en uno rojo — **`AC3`** | Desaparecen; recargar/volver a la ruta no los resucita (`pendingNotification` ya consumida; estado fresco) |
| 8 | Provocar un banner en `/requests` (p. ej. paso 4) y, **sin pulsar `×`**, navegar a `/queue` (o a `/requests/new`) y volver — **`AC4`** | El banner **no** está: ni en la pantalla destino ni al volver. Nada en `sessionStorage` (`vacaflow.pendingNotification` ausente en devtools) |
| 9 | Ídem desde `/queue`: banner de decisión visible → navegar a `/requests` y volver — **`AC4`** | Sin carry-over en ninguna dirección |
| 10 | Observar la aparición de cualquier banner (throttling de CPU en devtools si hace falta) — **`AC5`** | Fade-in de opacidad; `animation: banner-fade-in 150ms ease-out` visible en el panel Computed. Si `OQ-A(a)`: con `prefers-reduced-motion: reduce` emulado, aparece sin animación |
| 11 | Regresión del shell (`US-030`) | Header/nav intactos; el banner sigue **dentro** de la columna de contenido (§3.3), no entre header y main |

---

## 7. Riesgos y preguntas abiertas

> ⚠️ **`OQ-A` — Pregunta abierta para el usuario (decide 3 líneas en el ítem #1):**
> §3.3 fija *"150ms fade-in"* sin excepción, pero el proyecto traza `NFR-USA-*` (accesibilidad) y la práctica estándar es respetar `prefers-reduced-motion`.
>
> - **(a) — recomendada — Añadir el guard:** `@media (prefers-reduced-motion: reduce) { animation: none }` (o duración 0). El banner aparece instantáneo para quien pidió menos movimiento; el spec se cumple para todos los demás. Coste: 3 líneas de CSS.
> - **(b) Literalidad estricta:** siempre 150ms, sin guard. Fiel a la letra de §3.3; ignora la preferencia del sistema del usuario.
>
> **El plan asume (a) salvo indicación contraria.**

> ⚠️ **`OQ-B` — Pregunta abierta para el usuario (ratifica el comportamiento actual o añade una línea):**
> En `/requests` pueden coexistir **dos** banners apilados en un caso límite real: llegar con `Draft created.` pendiente (éxito) y que el fetch de la lista falle (error) — hoy se renderizan ambos, éxito arriba y error debajo. Ningún criterio ni §3.3 limita a un banner; ambos mensajes son ciertos (`FR-UIX-003` prohíbe además presentar un fallo como éxito, y prohíbe tragarse el error).
>
> - **(a) — recomendada — Mantener el apilamiento:** ambos hechos se comunican; cero cambios. Es también el comportamiento vigente tras `US-018`/`US-024` sin queja de spec.
> - **(b) Solo un banner a la vez (el error gana):** al setear `error` se anula `notification`. Una línea en `requests/page.tsx`, pero silencia un éxito real (tensión con `NFR-USA-002`).
>
> **El plan asume (a) salvo indicación contraria.**

| Riesgo | Mitigación |
|---|---|
| **StrictMode y el lazy initializer impuro de `/requests`** (`useState(() => consumePendingNotification())`, `requests/page.tsx` L37–39): en dev, React StrictMode doble-invoca los initializers; el primer consumo borra la clave de `sessionStorage` y la segunda invocación devolvería `null` — según qué resultado retenga React, el banner `Draft created.` podría perderse **solo en dev**. En `next build`/producción no hay doble invocación | §6 paso 2 lo verifica expresamente en `npm run dev`. Si el banner se pierde: mover el consumo a un `useEffect` de montaje (`setNotification(consumePendingNotification())`) — cambio local de 4 líneas dentro del alcance de esta historia (es la mecánica de `AC1`). Si se muestra bien, no se toca |
| Navegación detalle→detalle (`/requests/{a}` → `/requests/{b}`) reutilizaría la instancia de página y `cancelError` sobreviviría al cambio de `id` | No alcanzable desde la UI actual (siempre se pasa por `/requests`, que desmonta). Se anota; si `US-032+` añadiera navegación directa entre detalles, limpiar el estado en el `useEffect` de `params.id` |
| El fade se aplique también a re-renders del mismo banner (p. ej. mensaje cambia sin desmontar) | La animación corre al montar el nodo; los tres estados se anulan (`null`) antes de cada acción (`setError(null)`/`setNotification(null)` en `act`/`decide`), forzando desmontaje/montaje — el fade corre en cada aparición real |
| Sin tests de frontend, `AC1`–`AC5` solo se demuestran manualmente | `D7` + §6; la lógica de servidor que alimenta los mensajes ya está probada (suites funcionales de EP-01/EP-02) |

---

Siguiente paso — implementación:

```
/user-story-implement ".claude/implementation plans/US-031-plan.md"
```
