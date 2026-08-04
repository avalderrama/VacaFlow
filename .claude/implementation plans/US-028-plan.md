# US-028 — Prototipo HTML funcional en inglés

**Must** · `M` · Depende de: `US-017`, `US-023`, `US-024`, `US-025` · Traza: §12 deliverable 2

## 1. Entendimiento

**Descripción verbatim (Backlog.md §EP-09):** *"The prototype exists and is the design source for this backlog, but its copy is Spanish and the product ships in English (§2)."*

**Criterios de aceptación (verbatim):**
- *"Given the delivered prototype, when opened, then every string matches the §3.5 catalog."*
- *"Given the delivered prototype, when opened, then layout, spacing, palette and interactions are unchanged from the current version."*
- *"Given the ZIP, when extracted, then it opens in a browser with no server and no build step."*
- *"Given the screenshots in `docs/prototype/`, when the English version is cut, then they are regenerated so the documentation matches what ships."*

**Hallazgo bloqueante, ya resuelto con el usuario:** `docs/prototype/VacaFlow.dc.html` carga `<script src="./support.js">`, y ese archivo no existe en el repositorio. El prototipo actual depende de un runtime propietario (`x-dc`, `DCLogic`) que no está disponible, así que hoy no abre en un navegador sin ese archivo faltante — contradice directamente el tercer criterio. Decisión confirmada: reconstruir el prototipo como HTML/CSS/JS autocontenido, sin dependencias externas, en vez de intentar recuperar `support.js`.

**Qué se preserva del archivo actual (fuente de verdad para layout y comportamiento, per Backlog.md §3.2: "the markup wins for visual detail"):**
- Los 4 estados de sesión: login, registro, lista de solicitudes / bandeja de aprobación, formulario de solicitud.
- Los 2 modales (confirmación de cancelación, decisión aprobar/rechazar).
- Toda la lógica cliente: datos semilla en `localStorage`, validaciones, transiciones de estado, reglas de autorización (`RULE-04`–`RULE-07`), banners de éxito/error, skeleton de carga, estado vacío.
- Los valores exactos de estilo inline (colores `oklch`, paddings, radios, tipografía) — se copian literalmente, no se reinterpretan.

**Qué cambia:**
- Toda cadena visible se reemplaza por el catálogo verbatim de Backlog.md §3.5 (labels, botones, banners de éxito, mensajes de error `VF-*`, mensajes de validación, estados vacíos, textos de modales, bloque de decisión, bloque de cuentas de prueba).
- El runtime `x-dc`/`sc-if`/`sc-for`/`DCLogic` se reemplaza por JavaScript vanilla (una función de render que reconstruye el DOM según el estado) — sin custom elements, sin script externo. Es un cambio de implementación, no de comportamiento observable: no hay ningún consumidor del runtime `x-dc` fuera de este archivo, así que no hay compatibilidad que romper.
- Los nombres de personas se mantienen en español donde son datos de dominio (`Laura Méndez`, `Carlos Ruiz`, `Ana Torres`) pero los nombres de los tipos de ausencia sí se traducen (`Vacaciones`→`Vacation`, `Permiso personal`→`Personal Leave`, `Incapacidad médica`→`Sick Leave`), igual que hace `Backlog.md §3.6` para el seed real del backend.

**Alcance — entra:**
- Un archivo HTML autocontenido nuevo en inglés, funcional sin servidor ni build step.
- Las 11 capturas de pantalla regeneradas en inglés, mismos nombres de archivo que hoy.
- Empaquetado en un ZIP, entregado al usuario (no comiteado — `.gitignore` ya excluye `*.zip`).

**Alcance — no entra:**
- No se modifica `docs/prototype/VacaFlow.dc.html` (el original en español permanece como fuente de diseño histórica).
- No se intenta recuperar o reconstruir `support.js`.
- No se cambia código de `src/`.
- No se actualiza el `README.md` raíz.

## 2. Cambios estructurales / de base

No se requieren cambios de esquema, configuración ni dependencias. Son archivos estáticos nuevos.

## 3. Plan ordenado por dependencia

| # | Capa | Acción | Artefacto | Notas |
|---|------|--------|-----------|-------|
| 1 | Diseño/Prototipo | Crear | `docs/prototype/en/VacaFlow.en.html` | HTML/CSS/JS autocontenido; traduce todo el copy visible según §3.5; reescribe el runtime `x-dc` como JS vanilla; preserva estilos inline, datos semilla y lógica de negocio cliente-side idénticos al original |
| 2 | Verificación | — | — | Abrir el archivo en el navegador (Claude Browser) desde el sistema de archivos (`file://`), recorrer los 9 escenarios de pantalla y validar cada string contra §3.5 |
| 3 | Diseño/Prototipo | Crear | `docs/prototype/en/screenshots/01-login.png` … `11-approve-decision-modal.png` | Mismos 11 nombres de archivo que hoy (`02` y `04` son duplicados del login en el set actual — se preservan como tales) |
| 4 | Entrega | — | `VacaFlow-prototype-en.zip` | Empaquetado del contenido de `docs/prototype/en/` (HTML + screenshots); entregado al usuario vía `SendUserFile`, no comiteado a git |

Ítem #2 depende de #1. Ítem #3 depende de #2. Ítem #4 depende de #1 y #3.

## 4. Casos de uso y tabla de trazabilidad

| Historia | Criterio (verbatim) | Ítems del plan que lo cubren | Cómo se verifica |
|---|---|---|---|
| US-028 | "every string matches the §3.5 catalog" | #1, #2 | Recorrido manual en navegador comparando cada string visible contra la tabla §3.5, pantalla por pantalla |
| US-028 | "layout, spacing, palette and interactions are unchanged from the current version" | #1 | Comparación de valores de estilo inline (colores `oklch`, paddings, radios) copiados literalmente del archivo original; verificación visual contra las 11 capturas actuales |
| US-028 | "the ZIP... opens in a browser with no server and no build step" | #1, #4 | Abrir el HTML extraído del ZIP directamente vía `file://` sin ningún servidor ni paso de compilación |
| US-028 | "screenshots... are regenerated so the documentation matches what ships" | #3 | Los 11 archivos se recapturan desde la versión en inglés, mismos nombres |

Conteo: 4 criterios de entrada → 4 cubiertos.

## 5. Supuestos y decisiones

- **Reescritura completa en vez de recuperar `support.js`:** confirmado con el usuario. Justificación: es la opción robusta ante la ausencia del runtime original y elimina el riesgo de que el problema se repita.
- **Nombres de personas se mantienen en español; nombres de tipos de ausencia se traducen.** Sigue exactamente el precedente que `Backlog.md §3.6` ya fija para el seed real del backend. Impacto si fuera incorrecto: bajo, es consistente con una decisión ya tomada en el propio backlog.
- **El ZIP no se comitea a git** — `.gitignore:*.zip` ya lo excluye; se entrega directamente al usuario. El HTML y las capturas fuente sí se comitean en `docs/prototype/en/`, para que el ZIP sea reproducible.
- **`02-login-reset.png` y `04-login.png` son duplicados de `01-login.png` en el set actual** (verificado visualmente) — la regeneración preserva la misma estructura de 11 archivos por paridad de nombres con el original, aunque solo hay 9 estados de pantalla distintos.
- **No se toca `docs/prototype/VacaFlow.dc.html` original (español)** — `Backlog.md` lo sigue citando como fuente de diseño autoritativa para layout; se deja intacto.

No quedan ambigüedades bloqueantes.
