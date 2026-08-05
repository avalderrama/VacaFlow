# US-029 — Paquete de código fuente y video demo

**Must** · `M` · Depende de: todas las historias de aceptación · Traza: `Intent.md` §12 deliverables 3 y 4, `TC-18`

## 1. Entendimiento

**Descripción verbatim (Backlog.md):** *"The source ZIP contains no `node_modules`, `.next`, `bin` or `obj`, no database file and no real credentials."* / *"The video demonstrates `AC-01`–`AC-14` in sequence."*

**Criterios de aceptación (verbatim):**
- *"The source ZIP contains no `node_modules`, `.next`, `bin` or `obj`, no database file and no real credentials."*
- *"The video demonstrates `AC-01`–`AC-14` in sequence."*

**Contexto de los 14 escenarios de aceptación (`Intent.md` §13, verbatim):** `AC-01` Register an employee account · `AC-02` Log in · `AC-03` Create a Draft request · `AC-04` Reject an invalid date range (end before start) · `AC-05` Reject a start date in the past · `AC-06` Edit a Draft request · `AC-07` Submit a request · `AC-08` Prevent editing after submission · `AC-09` Log in as a manager · `AC-10` View Submitted requests assigned to that manager · `AC-11` Approve or reject with a comment — both decisions create an Approval record · `AC-12` Record the authenticated manager as responsible · `AC-13` Show the final decision to the employee · `AC-14` Block unauthorized operations (non-owner acting on a request; non-manager approving; manager approving their own request).

**Alcance — entra:**
- Un ZIP de código fuente (`VacaFlow-source.zip`) generado desde el árbol comiteado en `main`, sin `node_modules`, `.next`, `bin`, `obj`, archivo de base de datos ni credenciales reales.
- Un video demo (`VacaFlow-demo.mp4` o `.webm`) que recorre `AC-01`–`AC-14` en secuencia contra la API y la Web reales corriendo en local.
- Un guion cronometrado (documento de texto) que mapea cada segmento del video a su `AC`, pensado como base para una narración real si el usuario decide regrabar para el sponsor.
- Entrega de ambos artefactos al usuario vía `SendUserFile` (no se comitean — `.gitignore` ya excluye `*.zip`; el video tampoco se comitea).

**Alcance — no entra:**
- No se cambia código de `src/`.
- No se graba narración de audio real (decisión del usuario: automatizada y muda + guion complementario).
- No se ejecuta la "Acceptance session" (`WBS.md` 8.5) — eso es una sesión en vivo con el sponsor, fuera del alcance de esta historia.
- No se modifica `docs/Backlog.md` salvo que la implementación descubra una discrepancia (no se anticipa ninguna).

## 2. Cambios estructurales / de base

No se requieren cambios de esquema, configuración ni dependencias de producción. Es empaquetado y grabación sobre el sistema ya construido. La única pieza nueva es tooling de grabación, que vive enteramente en el scratchpad (no en el repositorio).

## 3. Plan ordenado por dependencia

| # | Capa | Acción | Artefacto | Notas |
|---|------|--------|-----------|-------|
| 1 | Verificación previa | — | — | Confirmar que `main` está verde: `dotnet build`, `dotnet test`, `npm run lint`/`typecheck`/`build` en `src/web`, y que el checkout usado para empaquetar es el `HEAD` de `main` recién mergeado (incluye US-028) |
| 2 | Empaquetado | Crear | `VacaFlow-source.zip` (scratchpad, no comiteado) | `git archive --format=zip -o ... HEAD` sobre el árbol de `main` — como todo lo excluido por `TC-18`/`LC-03` (`node_modules`, `.next`, `bin`, `obj`, `*.db`) ya está fuera de git vía `.gitignore`, `git archive` produce el ZIP correcto sin pasos de exclusión manual. Se verifica listando el contenido del ZIP contra la lista negra antes de entregar |
| 3 | Preparación de entorno | — | — | Levantar la API (`dotnet run --project src/BigSolutions.VacaFlow.Api --urls http://localhost:5217`) y la Web (`npm run dev` en `src/web`, puerto 3000) desde una base de datos recién reseteada, para que el seed de cuentas (`Laura Méndez`/manager, `Carlos Ruiz`/`Ana Torres`/empleados) esté en su estado inicial antes de grabar |
| 4 | Grabación | Crear | `VacaFlow-demo.webm` (scratchpad) | Harness Node + CDP (mismo patrón que `capture.js` de US-028) que conduce Chrome headless contra `http://localhost:3000` recorriendo los 14 escenarios en secuencia; captura vía `Page.startScreencast`; una segunda página headless reproduce los frames en un `<canvas>` y usa `MediaRecorder` (codec nativo de Chrome, sin `ffmpeg`) para producir el archivo de video, que se vuelca a disco vía un binding expuesto al Node host |
| 5 | Guion complementario | Crear | `VacaFlow-demo-script.md` (scratchpad) | Documento con marcas de tiempo aproximadas, una fila por `AC`, describiendo qué se ve en pantalla y una línea de narración sugerida — para que el usuario pueda regrabar con audio si lo necesita para el sponsor |
| 6 | Verificación | — | — | Reproducir el video generado y confirmar visualmente que las 14 escenas aparecen en el orden correcto y que cada bloqueo de `AC-14` se ve realmente rechazado (no solo intentado) |
| 7 | Entrega | — | — | `SendUserFile` con `VacaFlow-source.zip`, `VacaFlow-demo.webm` y `VacaFlow-demo-script.md` |

Ítem #2 depende de #1. Ítems #3–#6 son secuenciales (la grabación depende del entorno preparado). Ítem #7 depende de #2 y #6.

## 4. Casos de uso y tabla de trazabilidad

| Historia | Criterio (verbatim) | Ítems del plan que lo cubren | Cómo se verifica |
|---|---|---|---|
| US-029 | "The source ZIP contains no `node_modules`, `.next`, `bin` or `obj`, no database file and no real credentials." | #2 | Listar el contenido del ZIP (`unzip -l` / `Expand-Archive` + inspección) y confirmar ausencia de esas rutas y de cualquier archivo `*.db`/secretos |
| US-029 | "The video demonstrates `AC-01`–`AC-14` in sequence." | #3, #4, #6 | Reproducción del video verificando las 14 escenas en orden; guion (#5) referencia cada `AC` a su marca de tiempo |

Conteo: 2 criterios de entrada → 2 cubiertos.

## 5. Supuestos y decisiones

- **Empaquetado con `git archive` sobre `HEAD` de `main`**, no con copia manual de carpetas. Justificación: `.gitignore` ya implementa exactamente la lista negra de `TC-18`/`LC-03` (verificado: `node_modules/`, `.next/`, `bin/`, `obj/`, `*.db` nunca fueron trackeados), así que `git archive` es la forma más robusta de no depender de una exclusión manual que se pueda desactualizar. Impacto si fuera incorrecto: bajo — se verifica el contenido del ZIP explícitamente en el ítem #2 antes de entregar.
- **Video producido por automatización headless (CDP + `MediaRecorder` nativo de Chrome), sin narración de audio, más un guion complementario.** Decisión confirmada con el usuario vía `AskUserQuestion`. Justificación: reproducible, no depende de `ffmpeg` (no instalado en el entorno) ni de instalar Playwright (descarga de navegadores, requeriría permiso explícito), y dado el guion, el usuario puede regrabar con narración real si lo necesita para el sponsor.
- **Ni el ZIP ni el video se comitean al repositorio.** Mismo patrón que el ZIP de US-028: entrega directa al usuario vía `SendUserFile`. `.gitignore` ya excluye `*.zip`; se añadirá una entrada equivalente para el video si hiciera falta (a evaluar en implementación; si el video no queda dentro del árbol de trabajo — vive en el scratchpad — no se requiere entrada nueva en `.gitignore`).
- **La base de datos se resetea antes de grabar** para que el seed (3 empleados, 1 manager) esté en estado limpio y el recorrido sea reproducible. Igual precedente que la verificación E2E de historias anteriores.
- **No se ejecuta la "Acceptance session" (`WBS.md` 8.5)** — es una sesión en vivo posterior con el sponsor, no un artefacto que este skill pueda producir.

No quedan ambigüedades bloqueantes.
