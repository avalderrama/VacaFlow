# Plan de implementación — `US-034` · Decision modal

| Campo | Valor |
|---|---|
| Historia | `US-034` — Decision modal |
| Épica | `EP-07` — Manager decision |
| Prioridad · Talla | **Must** · `S` |
| Pantalla | `S-09` (Decision modal) |
| Depende de | `US-030` (Application shell, mergeada) — **sin precondiciones pendientes** |
| Traza | `Backlog.md` líneas 566–577 |
| Fuentes | `Backlog.md` v2.0 · código real verificado en `main` (commit `faf9ebb` — `US-013` mergeada, PR #30), archivo por archivo en `src/web/` |
| Repositorio | `C:\Users\avald\OneDrive\Work\VacaFlow\repositories\vacaflow` |
| Rama sugerida | `feat/us-034-decision-modal`, creada desde `main` (`faf9ebb`) |
| Estado | Borrador presentado para aprobación — sin preguntas abiertas |

> **Este documento no implementa nada.** La implementación la ejecuta `/user-story-implement`.

---

## 1. Entendimiento

### 1.1 Contexto

`US-021`/`US-022` (Approve/Reject, mergeadas) ya implementaron la decisión de punta a punta en el backend: `POST /requests/{id}/approve` y `POST /requests/{id}/reject`, ambos con un parámetro `comment: string | null` ya aceptado por el contrato. `US-023` (Approval Queue screen, mergeada) ya construyó `QueueCard.tsx` con los botones `Approve`/`Reject` y `queue/page.tsx` con la función `decide()`. **Nada de esto cambia.**

El propio código actual declara la brecha explícitamente — `queue/page.tsx` línea 3–7:

> *"S-07 Approval Queue (US-023). Decisions are sent without a comment (comment: null) — the S-09 modal with the optional comment field is US-034's job; approveRequest/rejectRequest already accept a comment parameter so that story only inserts the modal between the click and the existing call, nothing here changes."*

Esta historia inserta el modal `S-09` entre el click en `Approve`/`Reject` y la llamada ya existente — igual patrón que `US-033` (Cancel confirmation modal) insertó `S-08` delante de la cancelación. `US-033` ya construyó el shell genérico `Modal.tsx` (`src/web/components/modals/Modal.tsx`) — overlay, cierre por click externo/`Escape`, click interno no propaga, `role="dialog"`, foco inicial — **precisamente para que `US-034` lo reutilice sin reconstruirlo**, como su propio comentario declara: *"US-034 reuses this shell for its own content; it does not build another."*

**Historia solo Web.** El backend de `US-021`/`US-022` está completo y probado; ningún archivo de `Domain`, `Application`, `Infrastructure` ni `API` cambia.

### 1.2 Narrativa (verbatim)

> "As a manager, I want to add an optional comment before deciding, so that the employee understands the outcome."

### 1.3 Criterios de aceptación — verbatim (`Backlog.md` líneas 571–577)

| # | Criterio |
|---|---|
| `AC1` | "Given I press `Approve` or `Reject`, when it activates, then the `S-09` modal opens with the matching title from §3.5." |
| `AC2` | "Given the modal, when it renders, then it contains a labelled `Comment (optional)` textarea, 3 rows, `maxlength=500`." |
| `AC3` | "Given the approve modal, when it renders, then the confirm button reads `Approve` in the success palette; for reject it reads `Reject` in the danger palette." |
| `AC4` | "Given `Cancel`, an overlay click or `Escape`, when triggered, then the modal closes with no decision recorded." |
| `AC5` | "Given the modal is reopened, when it renders, then the comment field is empty rather than retaining the previous text." |
| `V1` | "420px panel, actions right-aligned." |

Copy exacto de los títulos (`Backlog.md` §3.5, tabla de modales):

| Modal | Título | Acciones |
|---|---|---|
| `S-09` approve | `Approve this request?` | `Cancel` · `Approve` |
| `S-09` reject | `Reject this request?` | `Cancel` · `Reject` |

Label del campo (`Backlog.md` §3.5, tabla de forms): `Comment (optional)` — sin helper.

### 1.4 Alcance

**Entra**: componente `DecisionModal` (reutiliza `Modal.tsx` de `US-033`) parametrizado por `decision: 'approve' | 'reject'`, con título/botón/paleta dinámicos; enganchar `QueueCard`'s `onApprove`/`onReject` en `queue/page.tsx` para que abran el modal en vez de decidir de inmediato; `decide()` pasa a recibir el comentario capturado por el modal en vez de `null` fijo.

**No entra**

| Excluido | Por qué / destino |
|---|---|
| Cambios al endpoint de decisión, `DecideRequestHandler` o la transición de dominio | Ya existen y están probados (`US-021`/`US-022`) — esta historia es una capa de confirmación con comentario delante de una acción que no cambia |
| Cambios a `QueueCard.tsx`'s botones `Approve`/`Reject` en sí, o al layout de `S-07` | Ya correctos (`US-023`), sin criterio de esta historia que los toque — solo cambia qué hace `onApprove`/`onReject` |
| Cambios al mensaje `Request approved.`/`Request rejected.` o a cualquier otro banner | Ya correcto (`US-021`/`US-022`), sin criterio que lo toque |
| Reconstruir el shell `Modal` genérico | Ya existe (`US-033`) y su propio comentario declara que `US-034` debe reutilizarlo, no reconstruirlo |
| Trampa de foco (`focus trap`) | Mismo criterio que `US-033 D6` — ningún AC de esta historia la exige |
| Tests automatizados de frontend | Sin runner en `src/web/package.json` — ratificación de `US-013 D6` y anteriores |

---

## 2. Cambios estructurales / de base

**No se requieren cambios de esquema, migraciones, configuración, variables de entorno, feature flags, dependencias nuevas (npm ni NuGet) ni cambios de contrato de API.** `POST /requests/{id}/approve` y `POST /requests/{id}/reject` ya aceptan `comment: string | null` y no cambian; `lib/api.ts`'s `approveRequest`/`rejectRequest` ya tienen la firma correcta.

---

## 3. Plan ordenado por dependencia

**Sin ítems de Domain, Application, Infrastructure ni API.** Todo es Web.

| # | Capa | Acción | Artefacto | Notas |
|---|---|---|---|---|
| 1 | Web | Crear | `src/web/components/modals/DecisionModal.tsx` | Usa `Modal` (de `US-033`) con `maxWidth={420}` (`V1`). Props: `{ decision: 'approve' \| 'reject'; isOpen; onClose; onConfirm: (comment: string \| null) => void; confirming }`. Título dinámico (`Approve this request?` / `Reject this request?`, `AC1`); `<textarea>` con `<label>` `Comment (optional)`, `rows={3}`, `maxLength={500}` (`AC2`), estado local `comment` inicializado en `''` — al desmontarse (Modal retorna `null` cuando `!isOpen`) el estado se resetea solo, satisfaciendo `AC5` sin lógica extra; botón `Cancel` (`.btn-secondary`) y botón de confirmación dinámico: `Approve`/`.btn-approve` (paleta success) o `Reject`/`.btn-danger` (paleta danger, mismo precedente de `US-033 D3` que ya usa `.btn-danger` sólido para la acción destructiva primaria de un modal) — `AC3`. `Cancel`/overlay/`Escape` ya cierran sin decidir vía el `Modal` compartido (`AC4`) — `onConfirm(comment.trim() || null)` es la única vía que ejecuta la decisión |
| 2 | Web | Modificar | `src/web/app/(app)/queue/page.tsx` | Agregar estado `decisionTarget: { id: string; action: 'approve' \| 'reject' } \| null`; `decide(id, action, comment)` ahora recibe el comentario y lo pasa a `approveRequest`/`rejectRequest` en vez de `null` fijo; `QueueCard`'s `onApprove`/`onReject` pasan a `() => setDecisionTarget({ id: request.id, action: 'approve' \| 'reject' })` en vez de llamar `decide` directo; renderizar `<DecisionModal isOpen={decisionTarget !== null} decision={decisionTarget?.action ?? 'approve'} onClose={() => setDecisionTarget(null)} onConfirm={(comment) => { const target = decisionTarget; setDecisionTarget(null); if (target) void decide(target.id, target.action, comment); }} confirming={deciding} />` (`AC1`, `AC4`) |
| 3 | Web | Verificar | `cd src/web && npm run lint && npm run typecheck && npm run depcruise && npm run build` | Regresión estática |
| 4 | Test | Verificar | `dotnet build VacaFlow.slnx` + `dotnet test VacaFlow.slnx` | Regresión pura — cero cambios backend |
| 5 | E2E | Verificar | Verificación manual §6 con la app corriendo (API + `npm run dev`) | Único punto donde los 5 criterios son demostrables juntos, en ambos flujos (aprobar y rechazar) |

**Dependencias:** 1 → 2 → {3, 4, 5}. **Ruta crítica:** 1 → 2 → 5.

---

## 4. Casos de uso y tabla de trazabilidad

**Cero casos de uso nuevos en Application.** La decisión en sí (`DecideRequestHandler`) es de `US-021`/`US-022` y no cambia.

| Historia | Criterio de aceptación (verbatim) | Ítems del plan que lo cubren | Cómo se verifica |
|---|---|---|---|
| `US-034` | "Given I press `Approve` or `Reject`, when it activates, then the `S-09` modal opens with the matching title from §3.5." (`AC1`) | #1, #2 | §6 pasos 2 y 3: click en `Approve` y en `Reject` por separado, título exacto en cada caso |
| `US-034` | "Given the modal, when it renders, then it contains a labelled `Comment (optional)` textarea, 3 rows, `maxlength=500`." (`AC2`) | #1 | §6 paso 2: inspección del árbol de accesibilidad (label asociado, `rows`, `maxlength`) |
| `US-034` | "Given the approve modal… confirm button reads `Approve` in the success palette; for reject it reads `Reject` in the danger palette." (`AC3`) | #1 | §6 pasos 2 y 3: color/clase del botón en cada modal |
| `US-034` | "Given `Cancel`, an overlay click or `Escape`… the modal closes with no decision recorded." (`AC4`) | #1 (vía `Modal` de `US-033`) | §6 paso 4: las 3 formas de cierre, verificando que la request no cambió de estado |
| `US-034` | "Given the modal is reopened… the comment field is empty rather than retaining the previous text." (`AC5`) | #1 | §6 paso 5: escribir un comentario, cerrar sin confirmar, reabrir, campo vacío |
| `US-034` (visual) | "420px panel, actions right-aligned." (`V1`) | #1 | §6 paso 2: inspección visual/estructural |

**Conteo: 6 criterios de entrada (5 `AC` + 1 visual) · 6 cubiertos.**

---

## 5. Supuestos y decisiones

| # | Decisión | Justificación | Impacto si es incorrecta |
|---|---|---|---|
| `D1` | **Reutilizar `Modal.tsx` de `US-033` sin modificarlo** | Su propio comentario declara que `US-034` es su segundo consumidor previsto; overlay/`Escape`/click-interno/`role="dialog"`/foco inicial ya cumplen `AC4` sin código nuevo | Si el spec de `S-09` exigiera un comportamiento distinto al de `S-08` (no lo hace — `Backlog.md` §3.3 los unifica), extender `Modal` sería un cambio local |
| `D2` | **Botón de confirmación de `Reject` usa `.btn-danger` (sólido)**, no `.btn-reject` (outline) | Mismo precedente que `US-033 D3`: la acción primaria destructiva de un modal usa la paleta sólida, distinta del botón outline de una fila/lista. El spec dice "danger palette" sin especificar outline vs. sólido — ya resuelto una vez en esta app | Cosmético — cambiar a outline es una clase CSS, sin impacto funcional |
| `D3` | **El estado del comentario vive dentro de `DecisionModal`, no en `queue/page.tsx`** | Como `Modal` retorna `null` cuando `isOpen` es `false`, el subárbol se desmonta — el estado local del comentario se resetea solo en cada apertura, satisfaciendo `AC5` sin lógica de limpieza explícita | N/A — consecuencia directa del ciclo de vida de React ya usado por `CancelConfirmationModal` |
| `D4` | **Comentario vacío o solo espacios se envía como `null`**, no como cadena vacía | El campo es opcional (`Comment (optional)`); el backend ya trata `comment: null` como "sin comentario" (`US-022`: "the comment is optional") — enviar `''` en vez de `null` sería una distinción sin significado que además ensucia el registro de `Approval` | Ninguno — comportamiento ya validado por los tests backend existentes de `US-022` |
| `D5` | **Sin trampa de foco (`focus trap`)** | Mismo criterio que `US-033 D6`; ningún `AC` de `US-034` la exige | Si una auditoría futura la exige, es una extensión local al `Modal` genérico |
| `D6` | **Sin tests automatizados de frontend** | Ratificación de `US-013 D6`…`US-012 D6`: sigue sin existir runner en `src/web/package.json` | Si se estrena runner, se añade como ítem previo |
| `S1` | **La rama se crea desde `main` (`faf9ebb`) directamente** | `US-013` mergeada (PR #30); `origin/main` al día | Ninguno |

---

## 6. Verificación end-to-end

Con la API corriendo y `npm run dev` en `src/web/`, seed §3.6 (Laura manager; Carlos empleado asignado a ella). Requiere una request `Submitted` en la cola de Laura — crear una nueva desde Carlos si no queda ninguna disponible tras sesiones previas.

| # | Paso | Resultado esperado |
|---|---|---|
| 1 | `dotnet build` + `dotnet test` · `cd src/web && npm run lint && npm run typecheck && npm run depcruise && npm run build` | Todo verde, 0 warnings |
| 2 | Como Laura, en `/queue`: click en `Approve` de una tarjeta — **`AC1`, `AC2`, `AC3`, `V1`** | Modal 420px abre, título `Approve this request?`, textarea `Comment (optional)` (3 filas, `maxlength=500`, label asociado por `for`/`id`), botones `Cancel` y `Approve` (paleta success) alineados a la derecha |
| 3 | `Back`/cerrar; click en `Reject` de la misma tarjeta — **`AC1`, `AC3`** | Modal con título `Reject this request?`, botón de confirmación `Reject` en paleta danger (`.btn-danger` sólido) |
| 4 | Con el modal de `Reject` abierto: probar `Cancel`, click en el overlay y tecla `Escape`, cada uno por separado — **`AC4`** | Las tres formas cierran el modal sin cambiar el estado de la request (la tarjeta sigue en la cola) |
| 5 | Abrir el modal de `Approve`, escribir un comentario, cerrar sin confirmar (`Cancel`), reabrir — **`AC5`** | El campo de comentario aparece vacío, no conserva el texto anterior |
| 6 | Abrir el modal de `Approve`, escribir un comentario, confirmar — **`AC1` end-to-end** | La decisión se ejecuta con el comentario capturado (verificar en el detalle de la request como empleado: bloque `DECISION` con el comentario exacto), banner `Request approved.`, modal se cierra, la request sale de la cola |
| 7 | Repetir el paso 6 con `Reject` y comentario vacío | La decisión se ejecuta sin comentario (`comment: null`), banner `Request rejected.`, sin bloque de comentario en el detalle |
| 8 | Regresión: repetir el flujo de aprobar/rechazar verificando que el mensaje, el endpoint y la transición de estado siguen siendo exactamente los de `US-021`/`US-022` | Sin cambios de comportamiento fuera de la confirmación con comentario agregada |

---

## 7. Riesgos y preguntas abiertas

**Sin preguntas abiertas.** `D1` y `D2` son reutilización directa de decisiones ya tomadas y justificadas en `US-033`, no lecturas ambiguas del requerimiento nuevo.

| Riesgo | Mitigación |
|---|---|
| Confundir el modal de decisión con una reconstrucción del shell genérico | El reporte final declara explícitamente que `Modal.tsx` no se tocó |
| Enviar `comment: ''` en vez de `null` y que el backend lo trate distinto | `D4` fuerza `null` explícitamente; el paso 7 de §6 verifica que un comentario vacío no deja rastro en el detalle |

---

Siguiente paso — implementación:

```
/user-story-implement ".claude/implementation plans/US-034-plan.md"
```
