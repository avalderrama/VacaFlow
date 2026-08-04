# Plan de implementación — `US-036` · Accessibility baseline

| Campo | Valor |
|---|---|
| Historia | `US-036` — Accessibility baseline |
| Épica | `EP-03` — Application shell and feedback (última historia de la épica) |
| Prioridad · Talla | **Should** · `S` |
| Depende de | `US-030` (Application shell, mergeada — PR #23) — **sin precondiciones pendientes** |
| Traza | `TC-15`, `NFR-USA-004`–`007` · `Backlog.md` líneas 431-438 |
| Fuentes | `Backlog.md` v2.0 · código real verificado en `main` (commit `aef00c0` — `US-035` mergeada, PR #26), archivo por archivo en `src/web/` |
| Repositorio | `C:\Users\avald\OneDrive\Work\VacaFlow\repositories\vacaflow` |
| Rama sugerida | `feat/us-036-accessibility-baseline`, creada desde `main` (`aef00c0`) |
| Estado | Borrador presentado para aprobación — una pregunta abierta ya resuelta por el usuario (ver §5, `D3`) |

> **Este documento no implementa nada.** La implementación la ejecuta `/user-story-implement`.

---

## 1. Entendimiento

### 1.1 Contexto — historia de **auditoría**, con una sola brecha real de código

Igual que `US-032` y `US-035`, esta historia audita una base de código ya construida a lo largo de 8 historias previas contra un checklist final de accesibilidad. La auditoría, cláusula por cláusula, da:

| Criterio | Estado en `main` (`aef00c0`) |
|---|---|
| `AC1` — skip link `Skip to main content` | ❌ **No existe.** `grep` de "skip" en `src/web` no arroja resultados. El destino (`id="main-content"`) sí existe desde `US-030` (`(app)/layout.tsx` línea 14), pero nada salta hacia él. **Es el único trabajo de código de esta historia.** |
| `AC2` — anillo de foco 2px accent, 2px offset | ✅ **Ya existe**, verbatim, desde `US-030`: `globals.css`, regla `a:focus-visible, button:focus-visible, input:focus-visible, select:focus-visible, textarea:focus-visible { outline: 2px solid var(--color-accent); outline-offset: 2px; }` |
| `AC3` — `Escape` cierra un modal abierto | ⏸️ **No aplicable todavía.** Ningún modal existe en la app (`US-033`, cancelación, y `US-034`, decisión, no están implementados). Ver `D3` — el usuario confirmó que esta historia no construye nada para `AC3`; lo hereda cada historia de modal, que ya trae el mismo criterio verbatim en su propia lista de aceptación (`Backlog.md` líneas 517 y 574) |
| `AC4` — todo control de formulario con `label` asociado por `for`/`id`, sin `placeholder` como label | ✅ **Ya existe.** `RequestForm.tsx` (4 campos) y `sign-in/page.tsx` (2 campos) usan `<label htmlFor="…">` con `id` correspondiente en cada control; `grep` de `placeholder=` en todo `src/web` no arroja resultados |
| `AC5` — estado de la lista identificable en escala de grises (no solo por color) | ✅ **Ya existe por construcción.** `StateBadge.tsx` (`US-024`) siempre renderiza el nombre del estado como texto (`{state}`) además del color — su propio comentario ya cita `NFR-USA-007` y explica que `Draft`/`Cancelled` comparten fondo por diseño, distinguidos por label y por las acciones ofrecidas, nunca por color solo |

**Conclusión de la auditoría: 4 de 5 criterios ya se cumplen carácter a carácter.** El único trabajo de código nuevo es el skip link (`AC1`); todo lo demás es verificación demostrativa contra código ya en `main`.

**Historia solo Web — cero backend**, como todas las de `EP-03`.

### 1.2 Narrativa

La historia no trae narrativa "As a... I want... so that" explícita en `Backlog.md` (a diferencia de otras); solo trae los cinco criterios de aceptación siguientes.

### 1.3 Criterios de aceptación — verbatim (`Backlog.md` líneas 434-438)

| # | Criterio |
|---|---|
| `AC1` | "Given any page, when I press Tab from the top, then the first stop is a `Skip to main content` link that becomes visible on focus and jumps to `#main-content`." |
| `AC2` | "Given any interactive element, when focused, then a 2px accent outline with 2px offset is visible." |
| `AC3` | "Given an open modal, when I press `Escape`, then it closes." |
| `AC4` | "Given any form control, when inspected, then it has a visible label associated by `for`/`id`; no placeholder acts as a label." |
| `AC5` | "Given the request list rendered in greyscale, when read, then every state remains identifiable by its text label." |

### 1.4 Alcance

**Entra**: el skip link (`AC1`) — único código nuevo; verificación demostrativa de `AC2`, `AC4`, `AC5` contra código ya en `main`; documentación explícita de por qué `AC3` queda diferida.

**No entra**

| Excluido | Por qué / destino |
|---|---|
| `AC3` (Escape cierra modal) | Sin modal en la app hoy. `US-033`/`US-034` construirán sus propios modales y ya cargan este mismo criterio verbatim en su propia lista de aceptación — decisión confirmada por el usuario (`D3`) |
| Skip link en `sign-in/page.tsx` (fuera del shell `(app)`) | La página de sign-in es un formulario único sin la estructura de columna de contenido (`#main-content`) que el shell `(app)` sí tiene desde `US-030`; no hay destino natural al que saltar. Si una futura historia le da estructura de múltiples secciones, se agrega ahí |
| Auditoría de accesibilidad automatizada con herramienta dedicada (axe, Lighthouse) | Sin criterio que la pida; la verificación de esta historia es manual contra los 5 criterios verbatim, igual que el resto de historias de `EP-03` sin runner de frontend |
| Cambios en `StateBadge`, `RequestForm` o cualquier componente ya conforme | `AC4`/`AC5` ya se cumplen — tocar código conforme sin motivo sería una historia inventada |

---

## 2. Cambios estructurales / de base

**No se requieren cambios de esquema, migraciones, configuración, variables de entorno, feature flags, dependencias nuevas (npm ni NuGet) ni cambios de contrato de API.** El único artefacto tocado es un enlace nuevo en el shell + su clase CSS.

---

## 3. Plan ordenado por dependencia

**Sin ítems de Domain, Application, Infrastructure ni API.** Todo es Web (más su verificación).

| # | Capa | Acción | Artefacto | Notas |
|---|---|---|---|---|
| 1 | Web | Modificar | `src/web/app/globals.css` | Agregar clase `.skip-link`: posicionado fuera de la pantalla (`position: absolute`, desplazado) por defecto, y traído a la vista con `:focus` (mismo mecanismo que el resto de la industria para este patrón — visualmente oculto sin `display: none`/`visibility: hidden`, que impedirían el foco por teclado) |
| 2 | Web | Modificar | `src/web/app/(app)/layout.tsx` | Agregar `<a href="#main-content" className="skip-link">Skip to main content</a>` como **primer hijo**, antes de `<AppHeader/>` — así es la primera parada de `Tab` desde el principio de cualquier página del shell (`AC1`) |
| 3 | Web | Verificar | `cd src/web && npm run lint && npm run typecheck && npm run depcruise && npm run build` | Regresión estática |
| 4 | Test | Verificar | `dotnet build VacaFlow.slnx` + `dotnet test VacaFlow.slnx` | Regresión pura — cero cambios backend |
| 5 | E2E | Verificar | Verificación manual §6 con la app corriendo (API + `npm run dev`) | Único punto donde `AC1`, `AC2`, `AC4`, `AC5` son demostrables juntos; `AC3` se documenta como diferido, no se intenta verificar sin modal |

**Dependencias:** 1 → 2 → {3, 4, 5}. **Ruta crítica:** 1 → 2 → 5.

---

## 4. Casos de uso y tabla de trazabilidad

**Cero casos de uso nuevos en Application.** Esta historia completa la base de accesibilidad transversal que las pantallas ya construidas en `EP-03`/`EP-04`/`EP-06`/`EP-07` deben respetar.

| Historia | Criterio de aceptación (verbatim) | Ítems del plan que lo cubren | Cómo se verifica |
|---|---|---|---|
| `US-036` | "Given any page, when I press Tab from the top, then the first stop is a `Skip to main content` link that becomes visible on focus and jumps to `#main-content`." | #1, #2 | §6 paso 2: `Tab` desde el principio de `/requests` y `/queue` → el link es la primera parada, visible al enfocarse, `Enter` salta a `#main-content` |
| `US-036` | "Given any interactive element, when focused, then a 2px accent outline with 2px offset is visible." | Ya satisfecho en `main` (§1.1) | §6 paso 3: tabular por botones, links, inputs y selects de varias pantallas, inspeccionar `outline`/`outline-offset` computados |
| `US-036` | "Given an open modal, when I press `Escape`, then it closes." | **Diferido — sin ítems, `D3`** | No verificable: no existe ningún modal en `main` hoy. Queda trazado a `US-033`/`US-034`, que ya cargan este criterio verbatim |
| `US-036` | "Given any form control, when inspected, then it has a visible label associated by `for`/`id`; no placeholder acts as a label." | Ya satisfecho en `main` (§1.1) | §6 paso 4: inspeccionar cada campo de `RequestForm` y `sign-in` — `label[for]` con `id` correspondiente, cero `placeholder` en el árbol |
| `US-036` | "Given the request list rendered in greyscale, when read, then every state remains identifiable by its text label." | Ya satisfecho en `main` (§1.1) | §6 paso 5: emular escala de grises (devtools) en `/requests` con filas de varios estados — cada una identificable por su texto, no solo por el color de fondo |

**Conteo: 5 criterios de entrada · 4 cubiertos con verificación (uno ya satisfecho por código existente + uno nuevo) · 1 diferido explícitamente con justificación (`AC3`).** Ningún criterio queda cubierto por omisión silenciosa.

---

## 5. Supuestos y decisiones

| # | Decisión | Justificación | Impacto si es incorrecta |
|---|---|---|---|
| `D1` | **Historia Web-only: cero ítems de backend** | Los 5 criterios son de presentación/interacción del cliente | N/A — hecho del código |
| `D2` | **El skip link vive solo en `(app)/layout.tsx`, no en `sign-in/page.tsx`** | El shell `(app)` es la única pantalla con una columna de contenido nombrada (`#main-content`, desde `US-030`) a la que saltar; `sign-in` es un formulario de una sola sección sin destino natural distinto del propio formulario | Si una futura historia le da estructura de secciones a `sign-in`, se agrega el mismo patrón ahí — cambio local, aislado |
| `D3` | **`AC3` (Escape cierra modal) queda diferido, sin código en esta historia** | Confirmado por el usuario: no existe ningún modal en `main` hoy; `US-033` y `US-034` ya cargan este mismo criterio verbatim en su propia lista de aceptación (`Backlog.md` líneas 517, 574) — construir un mecanismo genérico ahora sería anticipar trabajo sin nada real a lo que engancharlo, contra el patrón de esta sesión de no construir por adelantado | Si el usuario prefiriera un hook reutilizable ahora, es una historia aparte que se puede insertar antes de `US-033` |
| `D4` | **El skip link usa `position: absolute` + desplazamiento, no `display: none`/`visibility: hidden`** | Un elemento con `display: none` o `visibility: hidden` nunca puede recibir foco por teclado — el patrón estándar de accesibilidad para "oculto hasta enfocado" exige que el elemento permanezca en el árbol de accesibilidad y solo se mueva visualmente fuera de la pantalla | Si se implementara con `display`/`visibility`, `AC1` fallaría en la práctica pese a "existir" el link en el DOM |
| `D5` | **Sin tests automatizados de frontend** | Ratificación de `US-023 D7`/.../`US-035 D6`: sigue sin existir runner en `src/web/package.json` | Si se estrena runner, se añade como ítem previo |
| `S1` | **La rama se crea desde `main` (`aef00c0`) directamente** | `US-035` mergeada (PR #26); `origin/main` al día; árbol limpio verificado | Ninguno |

---

## 6. Verificación end-to-end

Con la API corriendo y `npm run dev` en `src/web/`, seed §3.6 (Laura manager; Carlos y Ana empleados asignados a ella).

| # | Paso | Resultado esperado |
|---|---|---|
| 1 | `dotnet build` + `dotnet test` · `cd src/web && npm run lint && npm run typecheck && npm run depcruise && npm run build` | Todo verde, 0 warnings |
| 2 | Sign in, luego en `/requests` y en `/queue`: presionar `Tab` desde el foco inicial de la página (ej. tras un refresh) — **`AC1`** | El primer elemento enfocado es el link "Skip to main content", invisible hasta el foco; al presionar `Enter` el foco salta a `#main-content` (verificable con `document.activeElement` o el siguiente `Tab` continuando dentro del contenido, no en el header) |
| 3 | Tabular por botones (`New request`, `Approve`/`Reject`, `Sign out`), links (`My Requests`, `Approval Queue`) e inputs/selects del formulario de creación — **`AC2`** | Cada uno muestra el anillo de foco `outline: 2px solid var(--color-accent)` con `outline-offset: 2px` en el panel Computed de devtools |
| 4 | Inspeccionar el DOM de `RequestForm` (`/requests/new`) y de `sign-in` — **`AC4`** | Cada campo tiene `<label for="X">` con un control `id="X"` correspondiente; cero atributos `placeholder` en el árbol |
| 5 | En `/requests` con filas de al menos 3 estados distintos (ej. `Draft`, `Submitted`, `Approved`), emular escala de grises (devtools → Rendering → Emulate vision deficiencies → Achromatopsia, o CSS `filter: grayscale(1)` manual) — **`AC5`** | Cada fila sigue siendo identificable por el texto de su `StateBadge` (`Draft`, `Submitted`, `Approved`…), no solo por el tono de fondo |
| 6 | Documentar `AC3` como diferido en el reporte final — sin paso de navegador, no hay modal que abrir | N/A — trazado a `US-033`/`US-034` |

---

## 7. Riesgos y preguntas abiertas

**Sin preguntas abiertas pendientes.** La única ambigüedad real de esta historia (`D3`, alcance de `AC3`) ya fue resuelta por el usuario antes de escribir este documento.

| Riesgo | Mitigación |
|---|---|
| El skip link podría interferir visualmente si el CSS de "oculto hasta enfocado" está mal calibrado (ej. quedar parcialmente visible o solapar el header) | Verificación visual explícita en el paso 2 de §6, no solo funcional |
| Confundir "criterio diferido con justificación" con "criterio incumplido" en el reporte final | El reporte de Fase 7 declara `AC3` explícitamente como diferido con su motivo, nunca como completado ni omitido en silencio |

---

Siguiente paso — implementación:

```
/user-story-implement ".claude/implementation plans/US-036-plan.md"
```
