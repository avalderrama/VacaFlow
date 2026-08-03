# Plan de implementación — `US-022` · Reject a request with a comment

| Campo | Valor |
|---|---|
| Historia | `US-022` — Reject a request with a comment |
| Épica | `EP-07` — Manager decision |
| Prioridad · Talla | **Must** · `M` |
| Pantallas | `S-07` (Approval Queue — **dueña: `US-023`**) · `S-09` (Decision modal — **dueña: `US-034`**). **Ambas superficies se difieren a sus historias dueñas**, mismo patrón que `US-021` `D9` (ver `D3`) |
| Depende de | `US-021` (Approve — **mergeada en `main`**, PR #18) |
| Trazas | `RULE-08` · `AC-11` · `FR-DEC-001`–`FR-DEC-009` (`FRD.md` §5.5, en particular `FR-DEC-005`/`FR-DEC-008` que nombran `US-022`) · `FRD.md` §4.2 (transición `T4`) · §6.3 (contrato compartido de `approve`/`reject`) · §7 (`VF-DEC-001`–`VF-DEC-005`, ya en código) · `SAD.md` §6.1 (*"approve and reject share DecideRequestHandler"*), §8.2, `ADR-012` · `Backlog.md` §EP-07 `US-022`, §3.5 (banner `Request rejected.`) |
| Fuentes | `Backlog.md` v2.0 · `FRD.md` · `SAD.md` v2.0 · código real verificado en `src/` y `tests/` (**`main`, commit `1983539`** — `US-021` mergeada) · plan `US-021` §4 (deuda declarada para esta historia) |
| Repositorio | `C:\Users\avald\OneDrive\Work\VacaFlow\repositories\vacaflow` |
| Rama sugerida | `feat/us-022-reject-request`, creada desde `main` (`1983539`) — sin precondiciones pendientes |
| Estado | Borrador presentado para aprobación (sesión de planificación delegada; **una pregunta abierta en §7 — `OQ-A`, la forma del contrato**) |

> **Este documento no implementa nada.** La implementación la ejecuta `/user-story-implement`.

---

## 1. Entendimiento

### 1.1 Contexto — por qué esta historia es delgada por diseño

`US-021` construyó **deliberadamente genérico** todo lo que la decisión necesita (su `D3`, ratificado por `SAD.md` §6.1: *"Approve and reject share `DecideRequestHandler`"*). Verificado contra `main` (`1983539`), **ya existe y no se toca**:

- **Domain completo**: `DecisionType` (con `Rejected = 2`), `ApprovalId`, la entidad hija `Approval` (misma clase, misma factoría para ambas decisiones), `Request.Decide(ApprovalId, EmployeeId, DecisionType, string? comment, DateTime nowUtc)` (transiciones `T3` **y `T4`** en el mismo método), `ApprovalPolicy` (las cuatro ramas de autorización son agnósticas al tipo de decisión), `RequestErrors.OnlySubmittedDecidable`/`AlreadyDecided` y `ApprovalErrors` — los cinco `VF-DEC-*` del catálogo §7.
- **Application completo**: `DecideRequestCommand(Guid, DecisionType, string? Comment)` con `Validate()` (≤ 500, `FR-DEC-008` — que el FRD marca *"optional for both approval and rejection"*), `DecideRequestHandler` registrado en DI. El doc del endpoint `approve` ya anuncia: *"reject (US-022) will wire Rejected onto the same DecideRequestHandler"* (`RequestEndpoints.cs:99-102`).
- **Infrastructure completo**: mapeo owned de `Approval` en `RequestConfiguration` (tabla `Approvals`, `UNIQUE(RequestId)`), migración `AddApprovals` aplicada, traducción de la carrera concurrente en `UnitOfWork` → `VF-DEC-005`. **La tabla no distingue decisiones**: la columna `Decision` (int) es la única diferencia entre una fila de aprobación y una de rechazo — el criterio de "paridad estructural" de esta historia es **verdadero por construcción**.
- **API parcial**: `ErrorStatusMap` ya tiene las cinco entradas `VF-DEC-*` y `SourceRuleTests` ya las pinea — **nada que añadir ahí**.
- **Tests que ya ejercitan `Rejected`**: dominio (`RequestTests.cs:357-363` — camino feliz `Rejected` con estado, `Approval.Decision` y comentario asertados; theories de re-decisión y de `Cancel` post-decisión con ambos tipos), handler (`DecideRequestHandlerTests.cs:254` — `VF-DEC-005` con command `Rejected`), integración (`RequestRepositoryTests.cs:533` — persiste un `Decide` `Rejected` real contra SQLite).

**Lo único que no existe** para `US-022`: (1) el contrato del cuerpo del reject (ver `OQ-A`), (2) el endpoint `POST /api/requests/{id}/reject`, (3) los tests que prueban que **el mismo código** produce `DecisionType.Rejected` de punta a punta sobre HTTP. Exactamente lo que el plan de `US-021` dejó anotado en su §4 (*"deuda que esta historia añade"*): *"endpoint `POST /{id:guid}/reject` con `DecideRequestCommand(id, DecisionType.Rejected, comment)`, contrato, tests de comentario presente/ausente y de paridad estructural del registro. Ni Domain, ni policy, ni tabla, ni `ErrorStatusMap` cambian."*

**Sin contradicciones sobre el comentario**: `FR-DEC-008` (*"optional for both approval and rejection"*), el criterio 2 de esta historia (*"the comment is optional"*) y el modal `S-09` de `US-034` (`Comment (optional)`) coinciden — el comentario del rechazo es **opcional**, igual que el de la aprobación. No hay ninguna divergencia de validación entre ambos endpoints.

### 1.2 Narrativa

El backlog formula `US-022` por criterios. La intención la fijan `EP-07` y `FR-DEC-005` (*"An approval or a rejection creates exactly one `Approval` record"*): el rechazo es **la misma operación de decisión** que la aprobación — misma autorización, mismo registro, misma transacción — distinguida únicamente por el valor de `Decision` y por el comentario que el manager elija dejar. La historia entrega el segundo consumidor del caso de uso ya existente.

### 1.3 Criterios de aceptación — verbatim (`Backlog.md` §EP-07 · `US-022`)

| # | Criterio |
|---|---|
| `AC1` | "Given a `Submitted` request assigned to me, when I reject it with a comment, then it becomes `Rejected`, one `Approval` record carries the comment, and the banner reads `Request rejected.`" |
| `AC2` | "Given a rejection with no comment, when submitted, then it succeeds — the comment is optional." |
| `AC3` | "Given a rejection, when the record is inspected, then it is structurally identical to an approval except for decision and comment." |
| `AC4` | "All authorization criteria of `US-021` apply identically." |

Contrato del endpoint, verbatim de `FRD.md` §6.3 (con la enmienda de `ADR-012` sobre el éxito):

> **`POST /requests/{id}/approve` and `POST /requests/{id}/reject`** · Request: `{ comment? }` — "**no `responsibleManagerId`**" (`FR-DEC-006`) · Success: `204 No Content` (`ADR-012` nombra reject expresamente: *"Create, update, submit, cancel, approve and reject return a status with no body"*) · Errors: `VF-VAL-001` `400` · `VF-DEC-002`/`003`/`004` `403` · `VF-REQ-006` `404` · `VF-DEC-001`/`005` `409`

El FRD define **un solo bloque de contrato para ambos verbos** — mismo cuerpo, mismos errores, mismo éxito. De ahí la pregunta `OQ-A`.

### 1.4 Alcance

**Entra**: el contrato del cuerpo (según `OQ-A`), el endpoint `POST /api/requests/{id}/reject` (espejo del `approve` con `DecisionType.Rejected` cableado), y los tests que faltan: camino feliz del handler con `Rejected` + comentario, funcionales del endpoint (comentario presente/ausente, paridad de autorización, identidad ignorada, `401`) y la inspección de paridad estructural del registro.

**No entra**

| Excluido | Por qué / destino |
|---|---|
| Botones `Reject` de `S-07`, reload de la cola | **`US-023`** (*Depends on: US-020, US-021, US-022*) — dueña nominal por tabla de pantallas |
| Modal `S-09` en modo reject (textarea `Comment (optional)`) | **`US-034`** — mismo diferimiento pure-UI que `US-021` `D9`; **sin arruga nueva**: el comentario es opcional también al rechazar (`FR-DEC-008`), el modal no distingue modos en su validación |
| Banner `Request rejected.` (texto catalogado en `Backlog.md` §3.5) | Infraestructura `US-031`, disparo `US-023` — deuda nominal idéntica a la del banner de `US-021` `AC1` |
| Bloque `approval?` en `GET /requests` (ver el comentario/decision del rechazo por HTTP) | **`US-025`** (*Depends on: US-021, US-022, US-024*) — diferimiento ya ratificado en `OQ-A` de `US-021` |
| Cualquier cambio en Domain, Application, Infrastructure, `ErrorStatusMap` o seed | Nada que cambiar: todo nació genérico en `US-021` (`D3` de aquel plan) — verificado archivo por archivo en §1.1 |

---

## 2. Cambios estructurales / de base

**No se requieren cambios de esquema, migraciones, configuración, variables de entorno, feature flags, dependencias nuevas ni cambios de seed.** La tabla `Approvals`, su `UNIQUE(RequestId)` y el mapeo owned ya soportan ambos valores de `Decision`; `ErrorStatusMap` ya contiene los cinco `VF-DEC-*`.

---

## 3. Plan ordenado por dependencia

De adentro hacia afuera. **Sin ítems de Domain, Application ni Infrastructure** (§1.1) y **sin ítems de Web** (§1.4, `D3`).

| # | Capa | Acción | Artefacto | Notas |
|---|---|---|---|---|
| 1 | API | Crear | `src/BigSolutions.VacaFlow.Api/Contracts/RejectRequestContract.cs` | `public sealed record RejectRequestContract(string? Comment)` — espejo del bloque único `{ comment? }` de §6.3, doc-comment idéntico al de `ApproveRequestContract` (sin `responsibleManagerId`, `FR-DEC-006` por forma). **Condicionado a `OQ-A`** — si el usuario prefiere compartir el tipo, este ítem se convierte en renombrar `ApproveRequestContract` → `DecideRequestContract` (2 usos en `src`, 12 en tests) |
| 2 | API | Modificar | `src/BigSolutions.VacaFlow.Api/Endpoints/RequestEndpoints.cs` | `group.MapPost("/{id:guid}/reject", …)`: sibling literal del `approve` (líneas 103-113) — bind del contrato, `new DecideRequestCommand(id, DecisionType.Rejected, contract.Comment)` → `DecideRequestHandler.Handle` → `result.ToHttpResult()` (`204`, `ADR-012`), **`.RequireAuthorization()` explícito**. El `DecisionType` es la única diferencia con el approve y es cableado, no condicional (`CA-PRE-001`). Actualizar el doc-comment del approve (su "reject (US-022) will wire…" — llegó) |
| 3 | Test | Modificar | `tests/BigSolutions.VacaFlow.Application.UnitTests/Requests/DecideRequestHandlerTests.cs` | El único hueco del handler: camino feliz con `DecisionType.Rejected` — (a) con comentario ⇒ `Success`, `request.State == Rejected`, `Approval.Decision == Rejected`, `Approval.Comment` con el texto (**`AC1`** — "one `Approval` record carries the comment"), `ResponsibleManagerId == FakeCurrentUser.EmployeeId`; (b) sin comentario ⇒ `Success`, `Comment == null` (**`AC2`**). El resto de la matriz (autorización, estados, re-decisión) ya está probado sobre el handler compartido — no se duplica (`D2`) |
| 4 | Test | Modificar | `tests/BigSolutions.VacaFlow.Infrastructure.IntegrationTests/Persistence/RequestRepositoryTests.cs` | Paridad estructural (**`AC3`**) contra SQLite real: persistir una aprobación y un rechazo (el arnés de `RequestRepositoryTests.cs:530-536` ya monta ambos), recargar y asertar que ambas filas de `Approvals` pueblan **las mismas columnas** (`Id`, `RequestId`, `ResponsibleManagerId`, `DecidedAtUtc` no nulos en ambas) y difieren **solo** en `Decision`/`Comment` — el criterio verbatim convertido en asserts. Si el test existente ya lo cubre al ampliar sus asserts, ampliar en vez de crear |
| 5 | Test | Modificar | `tests/BigSolutions.VacaFlow.Api.FunctionalTests/Endpoints/RequestEndpointTests.cs` | Contra `VacaFlowApiFactory` (pipeline real, cookie real, seed §3.6): (a) **`AC1`** — Carlos crea y somete; Laura `POST …/reject` con `{ "comment": "No coverage that week" }` ⇒ `204`; `GET /api/requests/{id}` ⇒ `state == "Rejected"`; la solicitud sale de la porción de cola de Laura; (b) **`AC2`** — reject sin comentario (`null`) ⇒ `204` y `state == "Rejected"`; (c) **`AC4`** paridad de autorización sobre la ruta nueva (subconjunto representativo, `D2`): Draft ⇒ `409` `VF-DEC-001` · Carlos (rol Employee) rechaza ⇒ `403` `VF-DEC-002` · Laura rechaza su propia Submitted ⇒ `403` `VF-DEC-004` · **cruce approve→reject**: solicitud ya aprobada, Laura la rechaza ⇒ `409` `VF-DEC-005` (y viceversa implícito por simetría del guard); (d) sin cookie ⇒ `401` `VF-AUT-004`; (e) comentario de 501 ⇒ `400` `VF-VAL-001` field `comment` (mismo `Validate()` compartido) |
| 6 | Test | Modificar | `tests/BigSolutions.VacaFlow.Api.FunctionalTests/Endpoints/IdentityIgnoredTests.cs` | Sibling del caso approve (`IdentityIgnoredTests.cs:90-127`): payload con `responsibleManagerId` inyectado sobre `POST …/reject` ⇒ `204` y la decisión registrada con la identidad autenticada — `FR-DEC-006`/`FR-AUT-010` sobre la ruta nueva |
| 7 | Test | Verificar | `dotnet build VacaFlow.slnx` + `dotnet test VacaFlow.slnx` · `cd src/web && npm run lint && npm run depcruise && npm run build` (sin cambios web — deben seguir verdes) | Arquitectura: el contrato nuevo pasa `No_Contract_Or_Command_Should_Carry_An_Identity_Field` por construcción; endpoint con autorización explícita; `Every_Domain_Error_Code_Should_Have_A_Status_Mapping` sin cambios (cero códigos nuevos) |

**Dependencias:** `OQ-A` → 1 → 2 → {5, 6} · 3 y 4 son independientes de 1-2 (prueban el código ya existente) · todo → 7. **Paralelizable:** {3, 4} con {1, 2}. **Ruta crítica:** 1 → 2 → 5.

---

## 4. Casos de uso y tabla de trazabilidad

**Cero casos de uso nuevos en Application.** Esta historia añade el **segundo consumidor** del caso de uso existente *decidir una solicitud* (`DecideRequestHandler`), con `DecisionType.Rejected` cableado en el endpoint — tal como `US-021` `D3` lo dejó previsto. La cadena de guards (input → existencia → `ApprovalPolicy` → `Request.Decide` → transacción única) es byte a byte la misma.

| Historia | Criterio de aceptación (verbatim) | Ítems del plan que lo cubren | Cómo se verifica |
|---|---|---|---|
| `US-022` | "Given a `Submitted` request assigned to me, when I reject it with a comment, then it becomes `Rejected`, one `Approval` record carries the comment, and the banner reads `Request rejected.`" | #1/#2 (endpoint que cablea `Rejected` sobre el handler existente), #3a (el registro lleva el comentario). El **banner** es UI: texto ya catalogado en §3.5, dueñas `US-031`/`US-023` — misma deuda nominal que el banner de `US-021` (`D3`) | Handler #3a · funcional #5a · §6 pasos 3-4 |
| `US-022` | "Given a rejection with no comment, when submitted, then it succeeds — the comment is optional." | #2 (mismo `Validate()` compartido — `FR-DEC-008`: opcional para ambos), #3b, #5b | Handler #3b · funcional #5b · §6 paso 5 |
| `US-022` | "Given a rejection, when the record is inspected, then it is structurally identical to an approval except for decision and comment." | **Verdadero por construcción** (misma entidad `Approval`, misma factoría, misma tabla, mismas columnas — §1.1); #4 lo convierte en asserts sobre filas reales | Integración #4 · §6 paso 6 |
| `US-022` | "All authorization criteria of `US-021` apply identically." | #2 (el endpoint delega en el **mismo** `DecideRequestHandler` + `ApprovalPolicy` — identidad de código, no re-implementación), #5c/#5d (paridad probada sobre la ruta HTTP nueva), #6 (identidad ignorada) | Funcional #5c-e, #6 · §6 pasos 7-9 · los 7 criterios de `US-021` siguen verdes sobre su propia suite (`D2`) |

**Conteo: 4 criterios de entrada · 4 cubiertos.** La única porción diferida es el **banner** de `AC1` (render de UI — patrón séptuple ya establecido, ver `D3`).

---

## 5. Supuestos y decisiones

Sesión de planificación delegada (Fase 3 no interactiva): decisiones de arquitecto documentadas con su reversibilidad. **La única que merece ratificación del usuario está elevada a §7 (`OQ-A`).**

| # | Decisión | Justificación | Impacto si es incorrecta |
|---|---|---|---|
| `D1` | **Cero cambios en Domain/Application/Infrastructure/`ErrorStatusMap`/seed** | Verificado archivo por archivo en `main` (`1983539`): todo lo que el rechazo necesita nació genérico en `US-021` (`D3` de aquel plan, prescrito por SAD §6.1) y ya está probado con ambos `DecisionType` en dominio e integración. Añadir algo sería maquinaria sin requisito (`TC-06`) | N/A — es un hecho del código, no una apuesta |
| `D2` | **La paridad de autorización (`AC4`) se prueba con un subconjunto funcional representativo sobre `/reject` (4 códigos: `VF-DEC-001`/`002`/`004`/`005` — incluido el cruce approve→reject), no duplicando la matriz completa de 7 casos de `US-021`** | El endpoint no contiene lógica: cablea `DecisionType.Rejected` y delega en el mismo handler/policy cuya matriz completa ya está verde (dominio rama a rama, handler los 7 criterios, funcional los 7 sobre `/approve`). El subconjunto prueba lo único nuevo — que la ruta llega al mismo código — sin re-probar el código mismo. `VF-DEC-003` (manager no asignado) y la rama fail-closed quedan cubiertos por identidad de código; duplicarlos exigiría re-montar el segundo manager al vuelo para probar una línea cableada | Si el usuario quiere la matriz completa espejo, son 2 tests funcionales más (segundo manager al vuelo + dueño sin manager) — aditivo, ~1 h |
| `D3` | **Toda la superficie web (S-07 botón Reject, S-09 modal en modo reject, banner) se difiere a sus dueñas `US-023`/`US-034`/`US-031`; esta historia no toca `src/web/`** | Mismo diferimiento que `US-021` `D9` (precedente séptuple `US-014`→`US-021`, mantenido en implementación todas las veces). **Sin arruga nueva específica del reject**: el comentario es opcional también al rechazar (`FR-DEC-008` verbatim, criterio `AC2` de esta historia, textarea `Comment (optional)` de `S-09`) — el modal no necesita distinguir modos en validación, solo en título/botón, que son de `US-034` | Ninguno — nada de esa UI existe aún y sus historias dependen de esta |
| `D4` | **`204 No Content` en el éxito, no el `200` con cuerpo del FRD §6.3** | `ADR-012` nombra reject expresamente (*"…approve and reject return a status with no body"*); precedente uniforme en los seis command-endpoints existentes. El detalle del rechazo llegará por `GET` cuando `US-025` entregue el bloque `approval?` | N/A — enmienda documentada del SAD §18 |
| `S1` | **La rama se crea desde `main` (`1983539`) directamente** | Verificado: `US-021` (PR #18) mergeada; working tree limpio; rama actual `main` | Ninguno |

---

## 6. Verificación end-to-end

| # | Paso | Resultado esperado |
|---|---|---|
| 1 | `dotnet build VacaFlow.slnx` | Compila con **0 warnings** (`TreatWarningsAsErrors`) |
| 2 | `dotnet test VacaFlow.slnx` | Suite completa verde, incluidos #3–#6 y los tests de arquitectura sin cambios |
| 3 | Login Carlos · crear draft A y someterlo · Login Laura · `POST /api/requests/{A}/reject` con `{ "comment": "No coverage that week" }` — **`AC1`** | `204 No Content` |
| 4 | `GET /api/requests/{A}` como Carlos · `GET /api/requests` como Laura — **`AC1`** | `state == "Rejected"`; A ya no aparece en la porción de cola de Laura; fila única en `Approvals` con `Decision = 2` y el comentario |
| 5 | Repetir con draft B sometido y `{ "comment": null }` — **`AC2`** | `204`; `state == "Rejected"`; `Comment` `NULL` en la fila |
| 6 | Inspeccionar en la base las filas de `Approvals` de una aprobación (de la suite de `US-021`) y del rechazo de A — **`AC3`** | Mismas columnas pobladas (`Id`, `RequestId`, `ResponsibleManagerId`, `DecidedAtUtc`); difieren solo `Decision` y `Comment` |
| 7 | Laura rechaza un Draft · Carlos rechaza la de Ana · Laura rechaza su propia Submitted — **`AC4`** | `409` `VF-DEC-001` · `403` `VF-DEC-002` · `403` `VF-DEC-004` |
| 8 | Laura rechaza la solicitud ya aprobada en la verificación de `US-021` — **`AC4`** (cruce) | `409` `VF-DEC-005`; sigue habiendo una sola fila en `Approvals` con `Decision = 1` |
| 9 | `POST /{A}/reject` sin cookie · con `responsibleManagerId` inyectado · con comentario de 501 | `401` `VF-AUT-004` · `204` con la identidad autenticada registrada · `400` `VF-VAL-001` field `comment` |
| 10 | `cd src/web && npm run lint && npm run depcruise && npm run build` | Verdes sin cambios (esta historia no toca el web) |

---

## 7. Riesgos y preguntas abiertas

> ⚠️ **`OQ-A` — Pregunta abierta para el usuario (bloquea el ítem #1 — nada más):**
> `FRD.md` §6.3 define **un solo bloque de contrato para ambos verbos** — verbatim: *"**`POST /requests/{id}/approve`** and **`POST /requests/{id}/reject`** · Request: `{ comment? }`"* — mismo cuerpo, mismos errores. ¿Qué tipo bindea el endpoint reject?
>
> - **(a) — recomendada — `RejectRequestContract(string? Comment)` nuevo.** Es lo que el plan de `US-021` dejó prescrito en su deuda de §4; sigue el precedente de nomenclatura un-contrato-por-operación (`CreateRequestContract`, `UpdateRequestContract`); cero toques a código existente. Coste: un record de una propiedad casi duplicado (mitigado con doc-comment cruzado).
> - **(b) `DecideRequestContract` compartido (renombrar `ApproveRequestContract`).** Refleja más fielmente el bloque único del FRD y la familia `Decide*` (`DecideRequestCommand`/`Handler`). Coste: contradice el sketch del SAD §8.2, que nombra `ApproveRequestContract` literalmente, y toca 14 usos existentes (2 en `src`, 12 en tests) — un rename mecánico pero que ensancha el diff de una historia pensada para ser mínima.
> - **(c) Reutilizar `ApproveRequestContract` tal cual en `/reject`.** Cero tipos nuevos, pero un `ApproveRequestContract` bindeado en el endpoint de rechazo (y posteado en sus tests) es semánticamente confuso — descartada como recomendación.
>
> **El plan asume (a) salvo indicación contraria.** Cambiar a (b) convierte el ítem #1 en un rename y no altera ningún otro ítem.

| Riesgo | Mitigación |
|---|---|
| `AC1` menciona el banner y esta historia no construye UI | Deuda nominal idéntica al banner de `US-021`: texto ya catalogado en §3.5, dueñas `US-031`/`US-023` dependen de esta historia — anotado en §4 |
| Tentación de re-probar la matriz completa de autorización sobre `/reject` (ceremonia) o de no probarla en absoluto (hueco) | `D2` fija el punto medio: subconjunto representativo de 4 códigos sobre la ruta nueva + identidad de código para el resto — ampliable si el usuario lo pide |
| El criterio de paridad estructural podría interpretarse como que exige código nuevo | No lo exige: es verdadero por construcción (misma entidad/tabla). El ítem #4 lo demuestra con asserts, que es todo lo que el criterio pide (*"when the record is inspected"*) |

---

Siguiente paso — implementación:

```
/user-story-implement ".claude/implementation plans/US-022-plan.md"
```
