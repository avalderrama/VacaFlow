# Plan de implementación — `US-033` · Cancel confirmation modal

| Campo | Valor |
|---|---|
| Historia | `US-033` — Cancel confirmation modal |
| Épica | `EP-06` — Request lifecycle (última historia pendiente de la épica) |
| Prioridad · Talla | **Must** · `S` |
| Pantalla | `S-08` (Cancel confirmation modal) |
| Depende de | `US-030` (Application shell, mergeada — PR #23) — **sin precondiciones pendientes** |
| Traza | `NFR-USA-009` · `Backlog.md` líneas 511-521 |
| Fuentes | `Backlog.md` v2.0 · código real verificado en `main` (commit `c91c4f0` — `US-036` mergeada, PR #27), archivo por archivo en `src/web/` |
| Repositorio | `C:\Users\avald\OneDrive\Work\VacaFlow\repositories\vacaflow` |
| Rama sugerida | `feat/us-033-cancel-confirmation-modal`, creada desde `main` (`c91c4f0`) |
| Estado | Borrador presentado para aprobación — sin preguntas abiertas |

> **Este documento no implementa nada.** La implementación la ejecuta `/user-story-implement`.

---

## 1. Entendimiento

### 1.1 Contexto

`US-019` (cancelar una request, mergeada) ya implementó la acción de cancelación de punta a punta: el endpoint `POST /requests/{id}/cancel`, el botón `Cancel` en cada fila de `/requests` (`RequestRow.tsx`) y el botón `Cancel request` en el detalle (`RequestForm.tsx`, visible solo para requests `Submitted`). **Ambos disparan la cancelación de inmediato**, sin ningún paso de confirmación — verificado en `app/(app)/requests/page.tsx` (`RequestRow`'s `onCancel` llama a `act(request.id, 'cancel')` directo) y en `app/(app)/requests/[id]/page.tsx` (`onCancelRequest` llama a `handleCancelRequest()` directo).

`US-019` ya declaraba esta brecha en su propia lista de dependencias (`Backlog.md` línea 504: "Depends on: `US-018`, `US-033`") — la acción se construyó primero, la confirmación llega ahora. `US-033` no cambia **qué** hace la cancelación (el endpoint, `CancelRequestHandler`, la transición de estado y el mensaje `Request cancelled.` ya están correctos y probados) — únicamente inserta un modal de confirmación **entre el click y la llamada** que hoy ya existe en ambos puntos.

**Primer modal de la aplicación.** Ningún componente de tipo overlay/modal existe hoy en `src/web` (`grep` de "modal" en `src/web/components` no arroja resultados). El spec de `Backlog.md` §3.3 unifica `S-08` (este modal, 400px) y `S-09` (modal de decisión, 420px, `US-034`) bajo una única definición de comportamiento:

> *"Modal — fixed overlay `oklch(0% 0 0 / 0.4)`, centered white panel, 12px radius, 28px padding, max-width 400px (`S-08`) or 420px (`S-09`). Actions right-aligned. Closes on overlay click and on `Escape`; a click inside the panel does not close it."*

Esto justifica un único componente `Modal` genérico (overlay + panel + cierre por overlay/`Escape` + el click interno no propaga) del que `US-033` es el primer consumidor — no una anticipación de `US-034`, sino la lectura directa del propio spec, que ya describe el comportamiento como compartido. `US-034` construirá su propio contenido (`S-09`) reutilizando este mismo `Modal`, sin que este plan le adelante nada de su contenido específico (el campo de comentario, `Approve`/`Reject`).

**Cierra una brecha diferida de `US-036`.** El plan de `US-036` (Accessibility baseline) documentó `AC3` ("`Escape` cierra un modal abierto") como diferido porque no existía ningún modal en la app; esta historia es la primera vez que hay uno real al que enganchar ese comportamiento — el manejo de `Escape` del `Modal` genérico satisface ese criterio, ahora demostrable.

**Historia solo Web — cero backend.** El endpoint de cancelación, `CancelRequestHandler` y la transición de dominio ya existen y están probados (`US-019`). Nada que agregar en Domain, Application, Infrastructure ni API.

### 1.2 Narrativa (verbatim)

> "As an employee, I want to confirm before cancelling, so that I do not lose a request by a stray click."

### 1.3 Criterios de aceptación — verbatim (`Backlog.md` líneas 516-519)

| # | Criterio |
|---|---|
| `AC1` | "Given I press `Cancel` on a row or `Cancel request` on the detail, when it activates, then the `S-08` modal opens with the title and body of §3.5." |
| `AC2` | "Given the modal, when I press `Back`, click the overlay or press `Escape`, then it closes with no change." |
| `AC3` | "Given the modal, when I press `Yes, cancel`, then the cancellation executes and the modal closes." |
| `AC4` | "Given a click inside the modal panel, when it happens, then the modal does not close." |

Copy exacto del modal (`Backlog.md` línea 226, tabla de §3.5):

| Título | Cuerpo | Acciones |
|---|---|---|
| `Cancel this request?` | `This action cannot be undone. The request will move to the Cancelled state.` | `Back` · `Yes, cancel` |

### 1.4 Alcance

**Entra**: componente genérico `Modal` (overlay, panel, cierre por overlay/`Escape`, click interno no propaga); componente `CancelConfirmationModal` con el copy exacto de `S-08`; enganchar ambos puntos de disparo existentes (`RequestRow` en `/requests`, `Cancel request` en el detalle) para que abran el modal en vez de cancelar de inmediato.

**No entra**

| Excluido | Por qué / destino |
|---|---|
| Contenido del modal de decisión (`S-09`, campo de comentario, `Approve`/`Reject`) | `US-034` — este plan solo construye el `Modal` genérico que `US-034` reutilizará, no su contenido |
| Cambios al endpoint de cancelación, `CancelRequestHandler` o la transición de dominio | Ya existen y están probados (`US-019`) — esta historia es una capa de confirmación delante de una acción que no cambia |
| Cambios al mensaje `Request cancelled.` o a cualquier otro banner | Ya correcto (`US-019`/`US-031`), sin criterio que lo toque |
| Trampa de foco (`focus trap`) dentro del modal | Sin criterio que lo pida explícitamente en `US-033`; `NFR-USA-004`–`007` (`US-036`) cubren foco visible y `Escape`, no trampa de foco. Si una futura auditoría de accesibilidad lo exige, es una extensión local al `Modal` genérico |

---

## 2. Cambios estructurales / de base

**No se requieren cambios de esquema, migraciones, configuración, variables de entorno, feature flags, dependencias nuevas (npm ni NuGet) ni cambios de contrato de API.** El endpoint `POST /requests/{id}/cancel` ya existe y no cambia.

---

## 3. Plan ordenado por dependencia

**Sin ítems de Domain, Application, Infrastructure ni API.** Todo es Web.

| # | Capa | Acción | Artefacto | Notas |
|---|---|---|---|---|
| 1 | Web | Modificar | `src/web/app/globals.css` | Agregar `.modal-overlay` (fixed, inset 0, `background: oklch(0% 0 0 / 0.4)`, flex centrado), `.modal-panel` (blanco, `border-radius: var(--radius-card)`, `padding: 28px`), y `.btn-danger` (sólido, `background: var(--color-danger)`, texto blanco — "danger palette" del spec, distinto de `.btn-row-danger`/`.btn-reject` que son outline) |
| 2 | Web | Crear | `src/web/components/modals/Modal.tsx` | Genérico: `{ isOpen, onClose, maxWidth, children }`. Overlay con `onClick={onClose}`; panel con `onClick={(e) => e.stopPropagation()}` (`AC4`) y `style={{ maxWidth }}`; `useEffect` con listener de `keydown` para `Escape` → `onClose` mientras `isOpen` (`AC2`, cierra la brecha diferida de `US-036 AC3`). Retorna `null` si `!isOpen` |
| 3 | Web | Crear | `src/web/components/modals/CancelConfirmationModal.tsx` | Usa `Modal` con `maxWidth={400}` (`S-08`, `AC1`); título `Cancel this request?`, cuerpo `This action cannot be undone. The request will move to the Cancelled state.` (copy exacto de §3.5); botones right-aligned: `Back` (`.btn-secondary`, `onClick={onClose}`, `AC2`) y `Yes, cancel` (`.btn-danger`, `onClick={onConfirm}`, deshabilitado mientras `confirming`, `AC3`) |
| 4 | Web | Modificar | `src/web/app/(app)/requests/page.tsx` | Agregar estado `cancelTargetId: string \| null`; `RequestRow`'s `onCancel` pasa a `() => setCancelTargetId(request.id)` en vez de llamar `act(request.id, 'cancel')` directo; renderizar `<CancelConfirmationModal isOpen={cancelTargetId !== null} onClose={() => setCancelTargetId(null)} onConfirm={() => { act(cancelTargetId!, 'cancel'); setCancelTargetId(null); }} confirming={acting} />` (`AC1`, `AC3`) |
| 5 | Web | Modificar | `src/web/app/(app)/requests/[id]/page.tsx` | Agregar estado `showCancelModal: boolean`; `RequestForm`'s `onCancelRequest` pasa a `() => setShowCancelModal(true)` en vez de `handleCancelRequest` directo; renderizar `<CancelConfirmationModal isOpen={showCancelModal} onClose={() => setShowCancelModal(false)} onConfirm={() => { setShowCancelModal(false); handleCancelRequest(); }} confirming={cancelling} />` (`AC1`, `AC3`) |
| 6 | Web | Verificar | `cd src/web && npm run lint && npm run typecheck && npm run depcruise && npm run build` | Regresión estática |
| 7 | Test | Verificar | `dotnet build VacaFlow.slnx` + `dotnet test VacaFlow.slnx` | Regresión pura — cero cambios backend |
| 8 | E2E | Verificar | Verificación manual §6 con la app corriendo (API + `npm run dev`) | Único punto donde los 4 criterios son demostrables juntos, en ambos puntos de disparo (fila y detalle) |

**Dependencias:** 1 → 2 → 3 → {4, 5} → {6, 7, 8}. **Ruta crítica:** 1 → 2 → 3 → 4/5 → 8.

---

## 4. Casos de uso y tabla de trazabilidad

**Cero casos de uso nuevos en Application.** La cancelación en sí (`CancelRequestHandler`) es de `US-019` y no cambia.

| Historia | Criterio de aceptación (verbatim) | Ítems del plan que lo cubren | Cómo se verifica |
|---|---|---|---|
| `US-033` | "Given I press `Cancel` on a row or `Cancel request` on the detail, when it activates, then the `S-08` modal opens with the title and body of §3.5." | #1, #2, #3, #4, #5 | §6 pasos 2 y 3: click en `Cancel` de una fila `Draft`/`Submitted` en `/requests`, y en `Cancel request` del detalle de una `Submitted` — el modal abre con el título y cuerpo exactos en ambos casos |
| `US-033` | "Given the modal, when I press `Back`, click the overlay or press `Escape`, then it closes with no change." | #2, #3 | §6 paso 4: las tres formas de cierre, verificando que la request no cambió de estado |
| `US-033` | "Given the modal, when I press `Yes, cancel`, then the cancellation executes and the modal closes." | #2, #3, #4, #5 | §6 paso 5: `Yes, cancel` ejecuta la cancelación real (banner `Request cancelled.`, estado pasa a `Cancelled`) y el modal se cierra |
| `US-033` | "Given a click inside the modal panel, when it happens, then the modal does not close." | #2 | §6 paso 6: click sobre el texto del panel (no sobre `Back`/`Yes, cancel`/overlay) — el modal permanece abierto |

**Conteo: 4 criterios de entrada · 4 cubiertos.**

---

## 5. Supuestos y decisiones

| # | Decisión | Justificación | Impacto si es incorrecta |
|---|---|---|---|
| `D1` | **Un único componente `Modal` genérico, no una implementación ad hoc solo para cancelar** | El propio spec de `Backlog.md` §3.3 unifica `S-08`/`S-09` bajo una sola definición de comportamiento (overlay, `Escape`, click interno no propaga) — construir dos implementaciones duplicaría esa lógica sin motivo. `US-034` reutiliza este `Modal`, no su contenido | Si `US-034` necesitara un comportamiento distinto (poco probable dado el spec compartido), extender el `Modal` es un cambio local |
| `D2` | **`US-033` cierra la brecha diferida `AC3` de `US-036`** (Escape cierra un modal) | `US-036` documentó explícitamente que no había ningún modal en la app para verificar ese criterio; este es el primero. Se declara en el reporte de esta historia, no se re-abre el plan de `US-036` | N/A — hecho de secuencia, no de código |
| `D3` | **Nueva clase `.btn-danger` (sólida), distinta de `.btn-row-danger`/`.btn-reject` (outline)** | El spec dice "`Yes, cancel` in the danger palette" sin especificar outline — el patrón visual establecido en la app usa sólido para la acción primaria de un modal/formulario (`.btn-primary`) y outline para acciones secundarias en listas; un modal de confirmación de una acción destructiva se alinea mejor con un botón sólido que comunique peso visual, consistente con cómo `.btn-approve` (sólido) se diferencia de `.btn-reject` (outline) en el patrón de dos acciones de la cola | Cosmético — cambiar a outline es una clase CSS, sin impacto funcional |
| `D4` | **La ejecución real de la cancelación no cambia** — el modal es una capa de confirmación delante de las llamadas ya existentes (`act(id, 'cancel')`, `handleCancelRequest()`) | Ambas ya están implementadas, probadas y correctas desde `US-019` | N/A — hecho verificado en el código actual |
| `D5` | **El estado del modal (`cancelTargetId`/`showCancelModal`) vive en cada página como `useState` local, no en un contexto compartido** | Mismo patrón ya establecido en toda la app (cada página gestiona su propio estado de UI); no hay necesidad de compartir el estado del modal entre `/requests` y el detalle, que son pantallas distintas | N/A — consistente con el resto del código |
| `D6` | **Sin trampa de foco (`focus trap`)** | Ningún criterio de `US-033` la exige; `US-036` (`NFR-USA-004`–`007`) cubre foco visible y `Escape`, no trampa de foco | Si una auditoría futura la exige, es una extensión local al `Modal` genérico |
| `D7` | **Sin tests automatizados de frontend** | Ratificación de `US-023 D7`/…/`US-036 D5`: sigue sin existir runner en `src/web/package.json` | Si se estrena runner, se añade como ítem previo |
| `S1` | **La rama se crea desde `main` (`c91c4f0`) directamente** | `US-036` mergeada (PR #27); `origin/main` al día | Ninguno |

---

## 6. Verificación end-to-end

Con la API corriendo y `npm run dev` en `src/web/`, seed §3.6 (Laura manager; Carlos empleado asignado a ella).

| # | Paso | Resultado esperado |
|---|---|---|
| 1 | `dotnet build` + `dotnet test` · `cd src/web && npm run lint && npm run typecheck && npm run depcruise && npm run build` | Todo verde, 0 warnings |
| 2 | Como Carlos, en `/requests`: click en `Cancel` de una fila `Draft` o `Submitted` — **`AC1`** | Modal abre: título `Cancel this request?`, cuerpo `This action cannot be undone. The request will move to the Cancelled state.`, botones `Back` y `Yes, cancel` alineados a la derecha, panel ≤400px |
| 3 | Abrir el detalle de una request `Submitted` propia y click en `Cancel request` — **`AC1`** | Mismo modal, mismo copy exacto |
| 4 | Con el modal abierto (desde el paso 2 o 3): probar `Back`, click en el overlay (fuera del panel) y tecla `Escape`, cada uno por separado — **`AC2`** | Las tres formas cierran el modal sin cambiar el estado de la request (recargar la lista/detalle confirma que sigue `Draft`/`Submitted`) |
| 5 | Abrir el modal y presionar `Yes, cancel` — **`AC3`** | La cancelación se ejecuta (banner `Request cancelled.`, estado pasa a `Cancelled`), el modal se cierra |
| 6 | Abrir el modal y hacer click sobre el texto del cuerpo (no sobre un botón ni el overlay) — **`AC4`** | El modal permanece abierto |
| 7 | Regresión: repetir el flujo de cancelación completo verificando que el mensaje, el endpoint y la transición de estado siguen siendo exactamente los de `US-019` | Sin cambios de comportamiento fuera de la confirmación agregada |

---

## 7. Riesgos y preguntas abiertas

**Sin preguntas abiertas.** El único punto de diseño no trivial (`D1`, un `Modal` genérico compartido) se justifica directamente por el propio spec de `Backlog.md` §3.3, que ya unifica el comportamiento de `S-08`/`S-09` — no es una lectura ambigua.

| Riesgo | Mitigación |
|---|---|
| El listener de `keydown` para `Escape` podría quedar activo si el componente se desmonta con el modal abierto (ej. navegación mientras está abierto) | El `useEffect` que registra el listener retorna su propia función de limpieza (`removeEventListener`), patrón estándar de React — se verifica en la ronda de revisión de calidad |
| Doble submit si el usuario hace click repetido en `Yes, cancel` antes de que la petición resuelva | `confirming`/`cancelling` ya existe como estado en ambas páginas (reutilizado, no nuevo) y deshabilita el botón mientras la petición está en curso — mismo patrón que el resto de acciones asíncronas de la app |
| Confundir el `Modal` genérico con una implementación ya lista para `US-034` | El reporte final de esta historia declara explícitamente que solo se construyó el shell genérico, no el contenido de `S-09` |

---

Siguiente paso — implementación:

```
/user-story-implement ".claude/implementation plans/US-033-plan.md"
```
