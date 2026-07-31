# Plan de implementación — `US-016` · Edit a Draft request

| Campo | Valor |
|---|---|
| Historia | `US-016` — Edit a Draft request |
| Épica | `EP-05` — Request authoring |
| Prioridad · Talla | **Must** · `M` |
| Pantalla | `S-05` (formulario en modo edición) — **sin superficie web todavía**; esta historia entrega el comportamiento del agregado y el endpoint (ver §1.4 y `D8`) |
| Depende de | `US-015` (**mergeada en `main`** — agregado `Request`, `CreateRequestHandler`, `POST /api/requests`, tabla `Requests`, verificado en el código) |
| Trazas | `RULE-01` · `RULE-02` · `RULE-03` · `RULE-04` · `AC-06` · `AC-08` (parcial) · `AC-14` · `FR-REQ-005`–`FR-REQ-007` · `FR-CAT-003` · `FR-AUT-010` · `FRD.md` §5.3, §6.3, §7 · `SAD.md` §5.3, §6.2, §6.3, §8.3 · `WBS.md` paquete 5.3 |
| Fuentes | `Backlog.md` §EP-05, §3.5 · `FRD.md` · `SAD.md` · código real verificado en `src/` y `tests/` |
| Repositorio | `C:\Users\avald\OneDrive\Work\VacaFlow\repositories\vacaflow` |
| Rama sugerida | `feat/us-016-edit-draft-request`, creada **desde `main`** (todas las dependencias mergeadas — no hay apilado esta vez, a diferencia de `US-015` `D10`) |
| Estado | Aprobado el 2026-07-30 (decisiones de arquitecto documentadas en §5; sesión sin interlocutor humano) |

> **Este documento no implementa nada.** La implementación la ejecuta `/user-story-implement`.

---

## 1. Entendimiento

### 1.1 Contexto — por qué ahora

`US-015` creó el agregado `Request` con **solo** `Create` (decisión `D5` de aquel plan: cada método llega con su historia). Esta es la historia que trae el segundo comportamiento: **editar un borrador**, con las dos reglas transversales que estrenan el resto del ciclo de vida — `RULE-03` (solo `Draft` es editable) y `RULE-04` (solo el dueño actúa sobre su solicitud).

Hallazgos de grounding, todos verificados contra el código en `main`:

- **`Request` existe y está donde el plan de `US-015` lo dejó** (`src/BigSolutions.VacaFlow.Domain/Requests/Request.cs`): propiedades con setter privado, ctor privado para EF, `Create` estático que recibe `today` y `nowUtc` como parámetros (el dominio nunca lee el reloj — test de arquitectura `Domain_And_Application_Should_Not_Read_The_Clock_Directly`). Su propio doc-comment anota que `UpdateDetails` llega con `US-016`. `MaxReasonLength = 500` es una constante privada reutilizable por el método nuevo.
- **`DateRange` ya hace `RULE-01` inquebrantable por construcción** (`DateRange.Create` → `VF-REQ-001`): la edición lo **reutiliza tal cual**, no se toca.
- **`IRequestRepository` tiene una sola operación** (`Add`) y su doc-comment ya anuncia: *"GetByIdAsync/listing operations arrive with their first consumer (US-016/US-018/US-020)"*. Esta historia es ese primer consumidor — el puerto crece una operación (`CA-INF-004`).
- **`RequestErrors` declara solo los errores de `US-015`** (`VF-REQ-001`, `VF-REQ-002`, cuatro variantes `VF-VAL-001`). Faltan los tres de esta historia: `VF-REQ-003`, `VF-REQ-004`, `VF-REQ-006`. `ErrorStatusMap` tampoco los tiene — sin entrada, el test `Every_Domain_Error_Code_Should_Have_A_Status_Mapping` falla y el código caería a `500`.
- **`IAbsenceTypeRepository.ExistsActiveAsync` ya existe** (creada por `US-015` `D6`, que anticipó textualmente: *"Si `US-016` (edit) necesita la misma comprobación, reutiliza la operación tal cual"*). Se reutiliza, no se crea nada en el catálogo.
- **`CreateRequestHandler` fija el patrón del handler de edición**: `Validate()` primero, `DateRange.Create`, `ExistsActiveAsync`, reloj de `TimeProvider` convertido a `today`/`nowUtc`, `SaveChangesAsync` del `IUnitOfWork` devolviendo `Result`. `SAD.md` §6.2 ilustra además la forma exacta del handler que carga y muta (`GetByIdAsync` → null check → ownership → método de dominio → save).
- **`RequestEndpoints` ya monta el grupo `/api/requests`** con `MapPost`; el `MapPut` nuevo se añade al mismo grupo. `ResultExtensions.ToHttpResult()` ya devuelve `204 No Content` para un `Result` exitoso — es exactamente lo que `ADR-012` pide para update (ver `D2`).
- **La tabla `Requests` no cambia**: `UpdateDetails` solo escribe columnas existentes (`AbsenceTypeId`, `StartDate`, `EndDate`, `Reason`, `UpdatedAtUtc`). La migración `AddRequests` (20260731004548) ya está aplicada. **Sin migración nueva.**
- **Tests existentes reutilizables**: `FakeRequestRepository`, `FixedTimeProvider`, `FakeCurrentUser`, `FakeUnitOfWork`, `FakeAbsenceTypeRepository` (Application.UnitTests); `SqliteDatabaseFixture` (IntegrationTests); `VacaFlowApiFactory` y el patrón de `IdentityIgnoredTests` (FunctionalTests). El `FakeRequestRepository` necesita `GetByIdAsync` al crecer el puerto.
- **No existe aplicación web** (`src/web/` no existe, verificado). Los fragmentos visuales de los criterios (banner `Changes saved.`, botón `Edit`, título `Edit draft`, botón `Save changes`) se difieren a `US-017`, que es la historia del formulario `S-05` y depende explícitamente de `US-015` + `US-016` (ver `D8`).

### 1.2 Narrativa

El backlog formula `US-016` por criterios. La intención la fijan `EP-05` y `FR-REQ-005`–`FR-REQ-007`: el dueño de una solicitud en estado `Draft` puede modificar su tipo, fechas y motivo; cualquier otro estado y cualquier otro usuario son rechazados; toda edición re-evalúa las mismas validaciones de la creación.

### 1.3 Criterios de aceptación — verbatim (`Backlog.md` §EP-05 · `US-016`)

| # | Criterio |
|---|---|
| `AC1` | "Given my own `Draft`, when I press `Edit` and save, then the type, dates and reason are updated and the banner reads `Changes saved.`" |
| `AC2` | "Given a request in any other state, when an edit is attempted, then `VF-REQ-003` is returned." |
| `AC3` | "Given another employee's request, when an edit is attempted, then `VF-REQ-004` is returned." |
| `AC4` | "Given an edit violating `RULE-01` or `RULE-02`, when saved, then the same field messages as on creation appear." |

Nota visual del backlog (verbatim): *"**Visual** — the form title reads `Edit draft`; the primary button reads `Save changes`."* — diferida con el resto de la superficie web (`D8`).

Reglas y errores implicados, verbatim del catálogo (`FRD.md` §7 = `Backlog.md` §3.5):

| Código | HTTP | Mensaje | Regla |
|---|---|---|---|
| `VF-REQ-003` | 409 | `Only Draft requests can be edited.` | `RULE-03` (`FR-REQ-005`) |
| `VF-REQ-004` | 403 | `You can only act on your own requests.` | `RULE-04` (`FR-REQ-006`) |
| `VF-REQ-006` | 404 | `The request was not found.` | — (contrato `PUT /requests/{id}`, `FRD.md` §6.3) |
| `VF-REQ-001` | 400 | `The end date cannot be earlier than the start date.` | `RULE-01` re-evaluada (`FR-REQ-007`) — **ya declarado**, se reutiliza |
| `VF-REQ-002` | 400 | `The start date cannot be in the past.` | `RULE-02` re-evaluada (`FR-REQ-007`) — **ya declarado**, se reutiliza |
| `VF-VAL-001` | 400 | mensajes de campo de §3.5 | `FR-REQ-004` re-evaluada (`FR-REQ-007`) — **ya declarados**, se reutilizan |
| `VF-CAT-001` | 400 | `The selected absence type does not exist or is not available.` | `FR-CAT-003` — ver `D3` |
| `VF-AUT-004` | 401 | `You must be signed in to perform this action.` | `FR-AUT-011` (resuelto de extremo a extremo por `TE-011`) |

Contrato del endpoint, verbatim de `FRD.md` §6.3 (con el delta de `ADR-012`/`SAD.md` §18 sobre el cuerpo de éxito — ver `D2`):

> **`PUT /requests/{id}`** · Request: `{ absenceTypeId, startDate, endDate, reason }` · Success `200`: the updated request · Errors: `VF-VAL-001` `400` · `VF-REQ-001` `400` · `VF-REQ-002` `400` · `VF-REQ-004` `403` · `VF-REQ-006` `404` · `VF-REQ-003` `409`

### 1.4 Alcance

**Entra**

- El comportamiento `UpdateDetails` en el agregado `Request` (`RULE-03` + `RULE-02` + backstop de reason, `SAD.md` §5.3) y los tres errores nuevos en `RequestErrors`.
- El caso de uso `UpdateRequestHandler` con `UpdateRequestCommand.Validate()`, y la operación nueva `GetByIdAsync` en `IRequestRepository` (crecimiento operación a operación, `CA-INF-004`).
- Persistencia: implementación de `GetByIdAsync` en `RequestRepository`. **Sin migración** — el esquema no cambia.
- API: contrato `UpdateRequestContract`, endpoint `PUT /api/requests/{id}` (`204`), tres entradas nuevas en `ErrorStatusMap` (`409`/`403`/`404`).
- Tests: unitarios de dominio (`UpdateDetails`), unitarios del handler, integración del repositorio (`GetByIdAsync`), funcionales del endpoint (incluidos el `403` con dos cuentas reales y el `409` forzando estado por SQL directo).

**No entra**

| Excluido | Por qué |
|---|---|
| `Submit`, `Cancel`, `Decide`, `Approval`, `ApprovalPolicy`, `VF-REQ-005` | Son `US-018`, `US-019`, `US-021`+. Mismo criterio que `US-015` `D5`: cada método llega con su historia y sus tests |
| `GET /requests` y `GET /requests/{id}` | `US-020`/`FR-VIS-*`. La edición no necesita leer por API: el handler carga por el puerto. La UI de `US-017` decidirá con `US-020` de dónde saca los datos del formulario |
| Toda la superficie web de los criterios (botón `Edit`, banner `Changes saved.`, título `Edit draft`, botón `Save changes`, controles deshabilitados si no es `Draft`) | No existe `src/web/` (verificado). Igual que `US-015` `D9`: el backend entrega códigos, mensajes verbatim y `field` para pintar "the same field messages as on creation"; el consumo queda trazado a `US-017` (ver `D8`) |
| Concurrencia optimista (dos ediciones simultáneas del mismo borrador) | Ningún requisito del MVP la pide; `TC-06` prohíbe la maquinaria sin requisito. Última escritura gana |
| Historial de cambios / auditoría de la edición | Fuera del FRD; `UpdatedAtUtc` es el único rastro exigido |

---

## 2. Cambios estructurales / de base

**No se requieren cambios de esquema, migraciones, configuración, variables de entorno, permisos, feature flags ni dependencias nuevas.** `UpdateDetails` escribe únicamente columnas que la migración `AddRequests` (`20260731004548`) ya creó. `ErrorStatusMap` (Api, no base de datos) gana tres entradas — ítem #7.

---

## 3. Plan ordenado por dependencia

De adentro hacia afuera (Domain → Application → Infrastructure → API → tests). Prosa en español, identificadores en inglés.

| # | Capa | Acción | Artefacto | Notas |
|---|---|---|---|---|
| 1 | Domain | Modificar | `src/BigSolutions.VacaFlow.Domain/Requests/Errors/RequestErrors.cs` | Añadir tres errores, mensajes verbatim de §3.5, **sin `Field`** (son errores de la operación, no de un campo — la UI de `US-017` los pinta en el alert general, ver `D6`): `OnlyDraftEditable` (`VF-REQ-003`, "Only Draft requests can be edited.") · `NotOwner` (`VF-REQ-004`, "You can only act on your own requests.") · `NotFound` (`VF-REQ-006`, "The request was not found.") — nombres de `SAD.md` §5.5/§6.2 |
| 2 | Domain | Modificar | `src/BigSolutions.VacaFlow.Domain/Requests/Request.cs` | Añadir `public Result UpdateDetails(AbsenceTypeId absenceTypeId, DateRange period, string? reason, DateOnly today, DateTime nowUtc)` (`SAD.md` §5.3, adaptado al código real: `reason` nullable con el mismo backstop que `Create`): (1) `State is not RequestState.Draft` → `OnlyDraftEditable` (**`RULE-03`**, primero — el estado manda sobre el contenido); (2) `period.Start < today` → `StartDateInPast` (**`RULE-02`** re-evaluada, `FR-REQ-007`; `RULE-01` ya es imposible: `period` es un `DateRange` construido); (3) reason nulo/blanco o `Trim().Length > MaxReasonLength` → `ReasonRequired` (backstop, mismo criterio que `Create`); éxito → asigna `AbsenceTypeId`, `Period`, `Reason` trimmed y `UpdatedAtUtc = nowUtc` (`CreatedAtUtc` intacto). Sin setter público, sin lectura del reloj. Actualizar el doc-comment de la clase (ya no "only exercises Create") |
| 3 | Application | Modificar | `src/BigSolutions.VacaFlow.Application/Abstractions/IRequestRepository.cs` | Añadir `Task<Request?> GetByIdAsync(RequestId id, CancellationToken cancellationToken);` — primer consumidor de la operación anunciada por el propio doc-comment del puerto (`CA-INF-004`, `SAD.md` §6.3). Actualizar ese comentario (US-018/US-020 siguen pendientes de sus listados) |
| 4 | Application | Crear | `src/BigSolutions.VacaFlow.Application/Requests/UpdateRequestCommand.cs` | `public sealed record UpdateRequestCommand(Guid RequestId, Guid? AbsenceTypeId, DateOnly? StartDate, DateOnly? EndDate, string? Reason)` con `Validate()` idéntico en estructura al de `CreateRequestCommand` (mismos cuatro checks, mismos errores `VF-VAL-001` — así `AC4`/"same field messages as on creation" sale gratis). `RequestId` viene de la ruta, no nullable. **Sin `employeeId`** — `No_Contract_Or_Command_Should_Carry_An_Identity_Field` barre `*Command.cs`; los tokens prohibidos son `EmployeeId`/`ManagerId`/`ResponsibleManagerId`, `RequestId` no lo es (ver `D5`) |
| 5 | Application | Crear | `src/BigSolutions.VacaFlow.Application/Requests/UpdateRequestHandler.cs` | `public sealed class UpdateRequestHandler(ICurrentUser currentUser, IAbsenceTypeRepository absenceTypes, IRequestRepository requests, IUnitOfWork unitOfWork, TimeProvider timeProvider)` → `Task<Result> Handle(UpdateRequestCommand command, CancellationToken cancellationToken)`. Secuencia (patrón `SAD.md` §6.2 + orden barato-primero de `CreateRequestHandler`): (1) `command.Validate()`; (2) `DateRange.Create(...)` → `VF-REQ-001`; (3) `await requests.GetByIdAsync(new RequestId(command.RequestId), ...)` → null → `RequestErrors.NotFound` (`VF-REQ-006`); (4) `request.OwnerId != currentUser.EmployeeId` → `RequestErrors.NotOwner` (**`RULE-04`**, `FR-REQ-006` — Application, y única comparación de identidad permitida: contra `ICurrentUser`); (5) `await absenceTypes.ExistsActiveAsync(...)` → `AbsenceTypeErrors.NotAvailable` (`VF-CAT-001`, ver `D3`); (6) `request.UpdateDetails(...)` con `today`/`nowUtc` derivados de `timeProvider.GetUtcNow()` (mismo cálculo que `CreateRequestHandler`) → `VF-REQ-003`/`VF-REQ-002`/`VF-VAL-001`; (7) `await unitOfWork.SaveChangesAsync(...)`; (8) `Result.Success()`. Sin `IIdGenerator` (no se crea nada). Devuelve `Result` sin valor — el endpoint responde `204` (`D2`) |
| 6 | Application | Modificar | `src/BigSolutions.VacaFlow.Application/DependencyInjection.cs` | `services.AddScoped<UpdateRequestHandler>();` |
| 7 | Infrastructure | Modificar | `src/BigSolutions.VacaFlow.Infrastructure/Persistence/Repositories/RequestRepository.cs` | Implementar `GetByIdAsync`: `dbContext.Requests.FirstOrDefaultAsync(request => request.Id == id, cancellationToken)` — entidad rastreada; el `SaveChangesAsync` del `UnitOfWork` persiste la mutación sin `Update()` explícito (patrón EF change-tracking ya usado por `CredentialStore`) |
| 8 | API | Modificar | `src/BigSolutions.VacaFlow.Api/ErrorHandling/ErrorStatusMap.cs` | `["VF-REQ-003"] = StatusCodes.Status409Conflict` · `["VF-REQ-004"] = StatusCodes.Status403Forbidden` · `["VF-REQ-006"] = StatusCodes.Status404NotFound` — exactamente los HTTP de `FRD.md` §7; obligatorio para `Every_Domain_Error_Code_Should_Have_A_Status_Mapping` |
| 9 | API | Crear | `src/BigSolutions.VacaFlow.Api/Contracts/UpdateRequestContract.cs` | `public sealed record UpdateRequestContract(Guid? AbsenceTypeId, DateOnly? StartDate, DateOnly? EndDate, string? Reason)` — espejo de `FRD.md` §6.3 (mismo cuerpo que create), **sin `employeeId` ni `requestId`** (el id viaja en la ruta; un `employeeId` inyectado lo descarta el binding JSON, patrón `IdentityIgnoredTests`). Record propio en lugar de reutilizar `CreateRequestContract` — ver `D4` |
| 10 | API | Modificar | `src/BigSolutions.VacaFlow.Api/Endpoints/RequestEndpoints.cs` | En el grupo existente `/api/requests`, añadir `group.MapPut("/{id:guid}", ...)`: construye `UpdateRequestCommand` desde `id` (ruta) + contrato, invoca `UpdateRequestHandler` y devuelve `result.ToHttpResult()` → `204 No Content` (`ADR-012`, ver `D2`; la extensión ya existe y ya hace exactamente eso). **`.RequireAuthorization()` en el `MapPut`** (test `Every_Endpoint_Should_State_Its_Authorization_Explicitly`). Recibe, delega, mapea — cero condicionales (`CA-PRE-001`). Un `id` no-Guid ni siquiera matchea la ruta (`{id:guid}`) → `404` del framework, coherente con `VF-REQ-006` |
| 11 | Test | Modificar | `tests/BigSolutions.VacaFlow.Domain.UnitTests/Requests/RequestTests.cs` | Añadir bloque `UpdateDetails` (fechas fijas por parámetro, determinismo `TE-004`): (a) `Draft` + datos válidos → éxito, `AbsenceTypeId`/`Period`/`Reason` (trimmed) actualizados, `UpdatedAtUtc = nowUtc` nuevo, `CreatedAtUtc` y `State` intactos; (b) cada estado no-`Draft` (`Submitted`, `Approved`, `Rejected`, `Cancelled` — vía reflexión no: construir el estado con los métodos cuando existan; hoy solo `Draft` es alcanzable por dominio, así que usar el truco del test de integración **no** — ver nota) → `VF-REQ-003`; (c) `period.Start == today` → éxito (frontera `FR-REQ-003`); (d) `period.Start == today - 1` → `VF-REQ-002`; (e) reason nulo/blanco/501 → `VF-VAL-001` reason; (f) reason de 500 → éxito. **Nota (b):** como el dominio aún no expone `Submit`, el estado no-`Draft` no es alcanzable por API pública del agregado; el caso `VF-REQ-003` de dominio se ejercita marcando el estado vía el mecanismo que el implementador juzgue mínimo (p. ej. instancia rehidratada por EF en el test de integración #13, y en unitario un helper interno de test solo si ya existiera — **no añadir** `InternalsVisibleTo` ni setters para esto; si no hay vía limpia, la cobertura unitaria de (b) se traslada íntegra al funcional #14c, que fuerza el estado por SQL — ver `D7`) |
| 12 | Test | Crear | `tests/BigSolutions.VacaFlow.Application.UnitTests/Requests/UpdateRequestHandlerTests.cs` (+ ampliar `Requests/Fakes/FakeRequestRepository.cs` con `GetByIdAsync` sobre su lista interna) | Con los fakes existentes y reloj fijo: (a) éxito → campos mutados, `SaveChanges` invocado; (b) id inexistente → `VF-REQ-006` y nada guardado; (c) `FakeCurrentUser` con otro `EmployeeId` que el dueño → `VF-REQ-004`; (d) cada campo ausente → su `VF-VAL-001` con el `Field` correcto; (e) `end < start` → `VF-REQ-001`; (f) `start` ayer → `VF-REQ-002`; (g) tipo inexistente/inactivo → `VF-CAT-001`; (h) `start == today` → éxito (frontera) |
| 13 | Test | Modificar | `tests/BigSolutions.VacaFlow.Infrastructure.IntegrationTests/Persistence/RequestRepositoryTests.cs` | Añadir: (a) `GetByIdAsync` de un request sembrado → agregado completo (ids tipados, `Period` owned, `State`); (b) Guid aleatorio → `null`; (c) roundtrip de edición: cargar, `UpdateDetails`, `SaveChangesAsync`, releer con contexto limpio → campos nuevos y `UpdatedAtUtc` cambiado persisten sin `Update()` explícito (valida el change-tracking del ítem #7) |
| 14 | Test | Modificar | `tests/BigSolutions.VacaFlow.Api.FunctionalTests/Endpoints/RequestEndpointTests.cs` | Contra `VacaFlowApiFactory` (pipeline real, cookie real; crear el draft previo vía `POST /api/requests` real): (a) **`AC1`** — `PUT /api/requests/{id}` del propio draft con tipo/fechas/reason nuevos → `204`, y una verificación de persistencia sobre la base de la factory (no hay `GET` todavía); (b) **`AC3`** — segunda cuenta registrada en el test edita el draft de la primera → `403` `{ code: "VF-REQ-004" }`; (c) **`AC2`** — forzar `State = 1` (`Submitted`) por SQL directo (patrón `US-014` fila desactivada) y editar → `409` `{ code: "VF-REQ-003" }`; (d) **`AC4`** — `endDate < startDate` → `400` `VF-REQ-001` `field: "endDate"`; `startDate` ayer → `400` `VF-REQ-002` `field: "startDate"`; campo ausente → `400` `VF-VAL-001` con su `field` — **los mismos cuerpos que emite el create** (aserción literal de "the same field messages as on creation"); (e) id inexistente → `404` `VF-REQ-006`; (f) tipo Guid aleatorio → `400` `VF-CAT-001`; (g) sin sesión → `401` `VF-AUT-004`; (h) payload con `employeeId` ajeno → `204` y el dueño no cambia (patrón `IdentityIgnoredTests`) |
| 15 | Test | Verificar | `tests/BigSolutions.VacaFlow.ArchitectureTests/` + suites completas | **Sin cambios, comprobar en verde**: `UpdateRequestCommand`/`UpdateRequestContract` sin tokens de identidad; `MapPut` con autorización explícita; `VF-REQ-003/004/006` mapeados; sin lectura directa del reloj; dependencias de anillos intactas |

**Dependencias:** 1 → 2 → {3, 5} · 3 → {5, 7, 12} · 4 → 5 → 6 · {1} → 8 · {5} → 9 → 10 · 2 → 11 · {3–6} → 12 · {7} → 13 · {8–10} → 14 · todo → 15. Sin ítem de `Program.cs`: el grupo ya está mapeado. Paralelizable: #8 (tras #1) y la rama de tests de dominio (#11, tras #2) con el resto. **Ruta crítica:** 1→2→5→10→14.

---

## 4. Casos de uso y tabla de trazabilidad

Caso de uso único de Application: **editar un borrador propio** (`UpdateRequestHandler`), consumido por `PUT /api/requests/{id}`. Actor: el dueño autenticado de la solicitud (`RULE-04`); cualquier otro usuario recibe `VF-REQ-004`, cualquier estado no-`Draft` recibe `VF-REQ-003`. El dueño se compara siempre contra `ICurrentUser.EmployeeId` (`FR-AUT-010`).

| Historia | Criterio de aceptación (verbatim) | Ítems del plan que lo cubren | Cómo se verifica |
|---|---|---|---|
| `US-016` | "Given my own `Draft`, when I press `Edit` and save, then the type, dates and reason are updated and the banner reads `Changes saved.`" | #2 (`UpdateDetails` muta tipo, fechas y reason), #3, #5, #7, #9, #10. **Parte visual diferida** (botón `Edit`, banner `Changes saved.`): no existe `src/web/`; trazado a `US-017` (ver `D8`) | Dominio #11a · handler #12a · integración #13c (roundtrip real de edición) · funcional #14a (`204` real con cookie real y persistencia verificada) |
| `US-016` | "Given a request in any other state, when an edit is attempted, then `VF-REQ-003` is returned." | #1 (`OnlyDraftEditable`), #2 (**`RULE-03`** primera guarda de `UpdateDetails`), #5, #8 (`409`) | Dominio #11b (con la nota de alcanzabilidad, ver `D7`) · funcional #14c (`Submitted` forzado por SQL → `409` `VF-REQ-003`) |
| `US-016` | "Given another employee's request, when an edit is attempted, then `VF-REQ-004` is returned." | #1 (`NotOwner`), #5 (**`RULE-04`** en el handler — `FR-REQ-006` es Application), #8 (`403`) | Handler #12c (fake con otra identidad) · funcional #14b (dos cuentas reales, `403`) |
| `US-016` | "Given an edit violating `RULE-01` or `RULE-02`, when saved, then the same field messages as on creation appear." | `DateRange` existente **reutilizado sin cambios** (`RULE-01`), #2 (`RULE-02` re-evaluada, `FR-REQ-007`), #4 (`Validate()` estructural idéntico al de create → mismos `VF-VAL-001`), #5 | Dominio #11c/#11d (fronteras hoy/ayer) · handler #12e/#12f · funcional #14d (aserción de cuerpos **idénticos** a los del create: mismo `code`, `message` y `field`) |

**Conteo: 4 criterios de entrada · 4 cubiertos** (los fragmentos de UI de `AC1` y la nota Visual del backlog diferidos con destino explícito `US-017`, decisión `D8` — mismo tratamiento aprobado en `US-014-plan` `D5` y `US-015-plan` `D9`).

Deuda de UI acumulada para `US-017` (reanotada): select y placeholder de `US-014`; formulario, banner `Draft created.` y retorno a `S-04` de `US-015`; y ahora botón `Edit`, banner `Changes saved.`, título `Edit draft`, botón `Save changes` y el estado deshabilitado para no-borradores de esta historia.

---

## 5. Supuestos y decisiones

Sesión de planificación sin interlocutor humano (Fase 3 no interactiva): las ambigüedades se resolvieron con criterio de arquitecto y quedan documentadas con su reversibilidad.

| # | Decisión | Justificación | Impacto si es incorrecta |
|---|---|---|---|
| `D1` | **La ruta es `PUT /api/requests/{id}`**, aunque el backlog y `FRD.md` §6.3 escriben `/requests/{id}` | Convención vigente del repo, ya ratificada dos veces (`US-014-plan` `D1`, `US-015-plan` `D1`): todo cuelga de `/api`; el grupo `/api/requests` ya existe en `RequestEndpoints` | Renombrar el grupo es una línea más tests |
| `D2` | **`204 No Content` en el éxito; el handler devuelve `Result` sin valor** — no se devuelve "the updated request" pese al literal de `FRD.md` §6.3 (`200`) | `ADR-012` y el delta de `SAD.md` §18 corrigen expresamente al FRD: los command endpoints devuelven `204`, la UI refetchea tras cada mutación (`FR-UIX-005`). `ResultExtensions.ToHttpResult()` ya implementa exactamente esto — cero código nuevo de mapeo. Mismo razonamiento aprobado en `US-015-plan` `D2` | Si el frontend necesitara el objeto, `US-020` (`GET`) lo entrega; cambiar el `204` por un cuerpo sería local al endpoint |
| `D3` | **La edición re-valida el tipo de ausencia con `ExistsActiveAsync` → `VF-CAT-001`**, aunque `FRD.md` §6.3 no lista `VF-CAT-001` entre los errores de `PUT /requests/{id}` y `FR-REQ-007` no cita `FR-CAT-003` | La edición **cambia el tipo** (`AC1`: "the type… are updated"): aceptar un `absenceTypeId` inexistente o inactivo violaría `FR-CAT-003`, que es general ("A request referencing an unknown or inactive absence type is rejected") y no está acotado al create. Sin la comprobación, un tipo inexistente reventaría en la FK con un `500` opaco (la FK es red de seguridad, no regla — `CA-INF-003`) y un tipo desactivado se colaría en silencio. La omisión en §6.3 se lee como descuido del FRD, no como permiso. La operación ya existe — `US-015` `D6` anticipó textualmente esta reutilización | Si producto decidiera que un draft puede conservar un tipo ya desactivado, la matización sería "validar solo si el tipo cambió" — un condicional en el handler; el error y el test cambian poco. Anotado como pendiente de incorporar `VF-CAT-001 400` a `FRD.md` §6.3 (`PUT`) |
| `D4` | **`UpdateRequestContract` es un record propio**, aunque su forma es idéntica a `CreateRequestContract` | Se evaluó la reutilización (regla "reutilizar por defecto"): se descarta porque los contratos son la superficie pública por endpoint y evolucionan por separado (`CA-APP-006`, "explicit boundary DTOs" — un cambio futuro en create no debe arrastrar al update ni viceversa); el costo es un record de una línea. El precedente del repo es un contrato por operación (`RegisterAccountContract` vs `SignInContract`, ambos con campos solapados) | Cero: unificar después sería mecánico, en la dirección fácil |
| `D5` | **El `RequestId` viaja en la ruta y el command lo carga como `Guid RequestId` no-nullable** | La ruta `{id:guid}` es la forma REST del FRD (§6.3) y el constraint hace imposible un id malformado (el framework responde `404` antes de tocar el handler, coherente con `VF-REQ-006`). Meterlo en el command (en vez de un parámetro suelto del `Handle`) mantiene el patrón "un command con `Validate()`" de `ADR-011`; `RequestId` no es token prohibido del barrido de identidad (`EmployeeId`/`ManagerId`/`ResponsibleManagerId`) ni es identidad del actor — identifica el recurso, y la autorización real es la comparación contra `ICurrentUser` en el handler | Si se prefiriera `Handle(Guid id, command)` (forma de `SAD.md` §6.2), el cambio es cosmético y local |
| `D6` | **`OnlyDraftEditable`, `NotOwner` y `NotFound` no llevan `Field`** | No señalan un campo del formulario sino la operación entera; `Backlog.md` `US-017` ya define dónde se pintan: "A general error, when present, renders in an alert block at the top of the card". Los errores de campo (`VF-REQ-001/002`, `VF-VAL-001`) ya llevan su `Field` de `US-015` y se reutilizan intactos — eso es exactamente "the same field messages as on creation" | Añadir un `Field` después es aditivo en el record `Error` ya existente |
| `D7` | **El caso `VF-REQ-003` se garantiza en el funcional (#14c, estado forzado por SQL) y la guarda vive en el dominio; el unitario de dominio no fabrica estados inalcanzables con trucos** | Hoy el único estado alcanzable por API pública del agregado es `Draft` (Submit llega con `US-018`). Abrir el agregado para el test (setter interno, `InternalsVisibleTo`, reflexión) debilitaría `CA-DOM-002` por conveniencia de test. El funcional ejercita la guarda real contra una fila real `Submitted` (rehidratada por EF, vía legítima), y cuando `US-018` entregue `Submit`, el unitario (b) de #11 se completa trivialmente encadenando `Submit` + `UpdateDetails` — anotado como entrada para la planificación de `US-018` | Riesgo mínimo y temporal: la guarda es una línea (`State is not RequestState.Draft`) cubierta end-to-end desde el día uno; la cobertura unitaria llega una historia después |
| `D8` | **Toda la superficie web de los criterios se difiere a `US-017` y queda trazada en §4** | No existe `src/web/` (verificado). `US-017` ("Request form screen") depende explícitamente de `US-015` **y** `US-016` — el backlog ya reparte así el trabajo: esta historia entrega el `PUT` con códigos, mensajes y `field`, y `US-017` construye el formulario dual crear/editar | Igual que `US-015` `D9`: la historia queda "backend done" hasta `US-017`; el plan no cambia |
| `D9` | **"Today" para la re-evaluación de `RULE-02` es la fecha UTC del `TimeProvider`**, calculada en el handler y pasada al dominio como parámetro — mismo `D4` de `US-015`, sin re-litigar | Coherencia estricta con create: si create y update usaran relojes distintos, un mismo payload sería válido en uno e inválido en el otro. `AS-04` (sin zonas horarias en el MVP) sigue vigente | Cambiaría en los dos handlers a la vez; el dominio y sus tests no se tocan |
| `S1` | El orden del handler (estructural → rango → cargar → dueño → catálogo → dominio) devuelve **un** error por petición, el primero encontrado | Patrón vigente (`US-015` `S1`, `FR-ERR-002`: un solo `{ code, message, field? }`). El orden `NotFound` antes que `NotOwner` es el único posible (sin cargar no hay dueño que comparar); catálogo tras dueño para no gastar I/O en peticiones que van a fallar por autorización | Cambiar el orden es local al handler; el contrato de error no cambia |
| `S2` | Editar un `Draft` cuyo start ya pasó **falla con `VF-REQ-002` aunque las fechas enviadas sean las originales** — no hay exención para "no cambié las fechas" | `FR-REQ-007` es taxativo: `FR-REQ-003` "re-evaluated on every edit, with the same errors as on creation". Es además coherente con `US-018` (`OQ-04`): ese draft tampoco podría submitirse; el usuario debe mover las fechas | Si producto quisiera permitir ediciones que no tocan fechas vencidas, sería un cambio de `FR-REQ-007`, no de esta historia |
| `S3` | La edición concurrente del mismo borrador se resuelve por última escritura (sin token de concurrencia) | Único actor posible: el dueño (`RULE-04`); el escenario es el mismo usuario en dos pestañas, sin daño de integridad (el agregado re-valida todo en cada `PUT`). `TC-06` prohíbe la maquinaria sin requisito | Añadir `RowVersion` después es una migración pequeña y un `409` nuevo — aditivo |

---

## 6. Verificación end-to-end

| # | Paso | Resultado esperado |
|---|---|---|
| 1 | `dotnet build VacaFlow.slnx` | Compila con **0 warnings** (`TreatWarningsAsErrors`) |
| 2 | `dotnet test VacaFlow.slnx` | Suite completa verde, incluidos #11–#14 y los tests de arquitectura sin modificar |
| 3 | `dotnet run --project src/BigSolutions.VacaFlow.Api --urls http://localhost:5080` | Arranca; sin migración nueva que aplicar |
| 4 | Login `employee@vacaflow.test` / `Employee123!` · crear un draft (`POST /api/requests`) y capturar el `id` del `Location` | `201` |
| 5 | `PUT /api/requests/{id}` con `{ absenceTypeId: <otro tipo activo>, startDate: hoy+5, endDate: hoy+7, reason: "Updated trip" }` | `204`; en la base, la fila tiene los valores nuevos, `UpdatedAtUtc` cambiado y `CreatedAtUtc`/`State` intactos |
| 6 | Igual con `endDate` < `startDate` | `400` `{ "code": "VF-REQ-001", "field": "endDate" }` — cuerpo idéntico al del create |
| 7 | Igual con `startDate` = ayer | `400` `{ "code": "VF-REQ-002", "field": "startDate" }` |
| 8 | Igual sin `reason` (y variantes sin tipo / sin fecha) | `400` `VF-VAL-001` con el `field` del campo ausente |
| 9 | Igual con `absenceTypeId` aleatorio | `400` `{ "code": "VF-CAT-001", … }` |
| 10 | `PUT /api/requests/{guid aleatorio}` | `404` `{ "code": "VF-REQ-006", "message": "The request was not found." }` |
| 11 | Login con `manager@vacaflow.test` e intentar el `PUT` del paso 5 | `403` `{ "code": "VF-REQ-004", "message": "You can only act on your own requests." }` |
| 12 | `UPDATE Requests SET State = 1 WHERE Id = '<id>'` por SQL directo y repetir el paso 5 como dueño | `409` `{ "code": "VF-REQ-003", "message": "Only Draft requests can be edited." }` |
| 13 | `PUT` sin cookie | `401` `{ "code": "VF-AUT-004", … }` |
| 14 | `PUT` del paso 5 añadiendo `"employeeId": "<guid ajeno>"` al payload | `204`; `Requests.EmployeeId` no cambia |

---

## 7. Riesgos

| Riesgo | Mitigación |
|---|---|
| El estado `Submitted` no es alcanzable por dominio hasta `US-018`, así que la guarda `RULE-03` queda sin unitario de dominio directo (`D7`) | Cubierta end-to-end por #14c (fila real `Submitted` por SQL, agregado rehidratado por EF); anotada como entrada obligatoria para el plan de `US-018` (completar #11b encadenando `Submit` + `UpdateDetails`) |
| `D3` contradice el literal de `FRD.md` §6.3 (que omite `VF-CAT-001` en `PUT`) | Decisión documentada con su lectura (omisión, no permiso) y anotada como pendiente de corregir en `FRD.md` §6.3; revertirla es borrar una guarda y un test |
| El change-tracking implícito (#7: sin `Update()` explícito) podría no persistir la mutación del owned type `Period` si el tracking se comporta distinto con `OwnsOne` | #13c es exactamente ese roundtrip (cargar → mutar `Period` → save → releer con contexto limpio); si fallara, el ajuste vive solo en `RequestRepository`/configuración |
| `S2` (draft con fechas vencidas ineditables aun sin tocar fechas) puede sorprender al usuario | Comportamiento exigido por `FR-REQ-007` y coherente con `OQ-04`; el mensaje `VF-REQ-002` bajo `Start date` le dice exactamente qué mover. Si molesta en demo, es cambio de FRD |
| Edición concurrente del mismo draft (dos pestañas) — última escritura gana (`S3`) | Sin requisito en el MVP; documentado. `RowVersion` sería aditivo si el sponsor lo pidiera |
| Los fragmentos de UI diferidos (`D8`) podrían perderse al cerrar `EP-05` | Trazados nominalmente en §4 con destino `US-017`, junto con la deuda heredada de `US-014` y `US-015` — entrada obligatoria para la planificación de `US-017` |

---

Siguiente paso — implementación:

```
/user-story-implement ".claude/implementation plans/US-016-plan.md"
```
