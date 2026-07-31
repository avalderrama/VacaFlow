# Plan de implementación — `US-018` · Submit a request

| Campo | Valor |
|---|---|
| Historia | `US-018` — Submit a request |
| Épica | `EP-06` — Request lifecycle |
| Prioridad · Talla | **Must** · `M` |
| Pantalla | `S-04` — My Requests ([`05-my-requests.png`](../../docs/prototype/screenshots/05-my-requests.png)) — la pantalla real es de `US-024`, que **depende de esta historia**; la superficie web se difiere allí (ver `D2`) |
| Depende de | `US-015` (**mergeada en `main`**) · `TE-011` (**mergeada en `main`**). Además `US-016`/`US-017` ya están en `main` (PR #13, #14) — el `PUT`, el `GET /requests/{id}` y `src/web/` existen |
| Trazas | `RULE-02` (re-evaluada, `OQ-04`) · `RULE-03` · `RULE-04` · `AC-07` · `AC-08` · `FR-LFC-001`–`FR-LFC-004` · `FRD.md` §4.2 (transición `T1`), §6.3, §7 · `SAD.md` §5.3 (`Submit`), §5.5 (`InvalidTransition`), §6.1 (`SubmitRequestHandler`), §18 (`ADR-012`) · `Backlog.md` §EP-06 `US-018`, §3.5 · deuda de test de `US-016-plan` `D7` |
| Fuentes | `Backlog.md` v2.0 · `FRD.md` · `SAD.md` v2.0 · código real verificado en `src/` y `tests/` (rama `main`, commit `4069a94`) |
| Repositorio | `C:\Users\avald\OneDrive\Work\VacaFlow\repositories\vacaflow` |
| Rama sugerida | `feat/us-018-submit-request`, creada **desde `main`** (todas las dependencias mergeadas) |
| Estado | Aprobado el 2026-07-30 (decisiones de arquitecto documentadas en §5; sesión sin interlocutor humano — revisar `D2` y `D5` antes de implementar) |

> **Este documento no implementa nada.** La implementación la ejecuta `/user-story-implement`.

---

## 1. Entendimiento

### 1.1 Contexto — por qué ahora y qué hay ya

`US-018` estrena `EP-06` (Request lifecycle): la **primera transición de estado** del agregado `Request`. Hasta hoy el agregado solo sabe `Create` y `UpdateDetails`; su propio doc-comment anota *"Submit/Cancel/Decide arrive with their own stories (US-018/US-019)"*. Esta es esa historia para `Submit`.

Hallazgos de grounding, todos verificados contra `main` (`4069a94`, con `US-016` y `US-017` mergeadas):

- **`Request` está listo para recibir `Submit`**: `State` (`RequestState` con los cinco valores), `SubmittedAtUtc` ya declarado (nullable, **columna ya creada** por la migración `AddRequests` — verificado en `RequestConfiguration.cs:60` y en la migración `20260731004548`), `UpdatedAtUtc`, y `IsEditable => State is RequestState.Draft` (la guarda `RULE-03` que `AC5` re-verifica). `SAD.md` §5.3 trae el sketch exacto del método `Submit(DateOnly today, DateTime nowUtc)` — no se inventa nada.
- **`RequestErrors` ya declara casi todo**: `StartDateInPast` (`VF-REQ-002`, `Field: "startDate"`), `NotOwner` (`VF-REQ-004`), `NotFound` (`VF-REQ-006`), `OnlyDraftEditable` (`VF-REQ-003`). **Falta únicamente `VF-REQ-005`** — y no es un `static readonly` sino una **factory** `InvalidTransition(from, to)`, porque su mensaje interpola: `This request cannot move from {current} to {target}.` (`TE-005` criterio 3, `SAD.md` §5.5 lo escribe literal).
- **`ErrorStatusMap` no tiene `VF-REQ-005`** → hay que añadir `409` (FRD §7) o `Every_Domain_Error_Code_Should_Have_A_Status_Mapping` falla (el test escanea los literales `"VF-REQ-*"` en los `*Errors.cs` de Domain, factory incluida). El test hermano `Known_Error_Codes_Should_Map_To_Their_Documented_Status` gana una fila `InlineData`.
- **`IRequestRepository` no crece**: `GetByIdAsync` existe desde `US-016` y es todo lo que Submit necesita (`CA-INF-004` — cero cambios en el puerto, cero en `RequestRepository`, cero en Infrastructure).
- **El patrón del handler está fijado dos veces**: `UpdateRequestHandler` (cargar → `NotFound` → dueño vs `ICurrentUser.EmployeeId` → método de dominio → `SaveChangesAsync`) y `GetRequestByIdHandler` (handler **sin command record** cuando el único input es el id de la ruta). Submit no lleva cuerpo (`FRD.md` §6.3: "Request: empty"), así que sigue la segunda forma — ver `D4`.
- **El endpoint está decidido por el FRD**: `POST /requests/{id}/submit` (§6.3 fila 9). Con la convención `/api` vigente: `POST /api/requests/{id}/submit`, en el grupo ya montado de `RequestEndpoints`. Éxito `204` vía `ToHttpResult()` existente (`ADR-012` corrige el `200` del FRD — precedente `US-016` `D2`).
- **Dos deudas de test explícitas de `US-016` vencen aquí** (anotadas en aquel plan como "entrada obligatoria para `US-018`"):
  1. `US-016-plan` `D7`: el unitario de dominio de `RULE-03` sobre un estado no-`Draft` quedó pendiente porque `Submitted` no era alcanzable por API pública del agregado. Con `Submit` real, se completa encadenando `Create` → `Submit` → `UpdateDetails` → `VF-REQ-003`. **Eso es exactamente `AC5`.**
  2. `RequestEndpointTests` (comentario final del archivo, verbatim): el `409` de edición a nivel HTTP quedó diferido porque forzar `State` por SQL out-of-band contra la base de la `WebApplicationFactory` está **reproduciblemente roto** en este proyecto, "or (b) Submit(), which does not exist until US-018". Ahora existe: el funcional produce el `Submitted` por la vía legítima (`POST …/submit`) y ejercita el `PUT` → `409 VF-REQ-003` de extremo a extremo. El comentario se actualiza.
- **Web**: `src/web/` existe desde `US-017`, pero `/requests` es el **placeholder honesto de `S-04`** (título + botón `New request` + banner — sin lista, sin filas, sin acciones). El botón `Submit` vive, según `US-024`, en la fila de la lista (`Draft` → `Edit · Submit · Cancel`), y la lista necesita `GET /requests` (`US-020`). Ver `D2`: la superficie web de esta historia se difiere a `US-024`, que depende de `US-018` y ya tiene asignados la fila, sus acciones y el reload.
- **`TimeProvider` real en los funcionales**: `VacaFlowApiFactory` no sustituye el reloj, así que el caso "el start date ya pasó" no se produce esperando — ver la estrategia de #9g y `D6`.

### 1.2 Narrativa

El backlog formula `US-018` por criterios. La intención la fijan `EP-06` y `FR-LFC-001`–`FR-LFC-004`: el dueño de un borrador lo somete a aprobación (`Draft` → `Submitted`, transición `T1` de `FRD.md` §4.2, sellando `SubmittedAtUtc`); la fecha de inicio se re-valida en ese momento (`OQ-04`, resuelto: sí); cualquier otro estado, y cualquier usuario que no sea el dueño, son rechazados; y una vez sometida, la solicitud es inmutable para el empleado (`FR-LFC-004`).

### 1.3 Criterios de aceptación — verbatim (`Backlog.md` §EP-06 · `US-018`)

| # | Criterio |
|---|---|
| `AC1` | "Given my own `Draft`, when I press `Submit`, then it becomes `Submitted`, the list reloads and the banner reads `Request submitted for approval.`" |
| `AC2` | "Given a `Draft` whose start date has since passed, when I submit, then `VF-REQ-002` is returned in an error banner. *(`OQ-04`, confirmed by the prototype.)*" |
| `AC3` | "Given a request that is not a `Draft`, when a submit is attempted, then `VF-REQ-005` is returned." |
| `AC4` | "Given another employee's request, when a submit is attempted, then `VF-REQ-004` is returned." |
| `AC5` | "Given a submitted request, when an edit is attempted, then it is rejected." |

Reglas y errores implicados, verbatim del catálogo (`FRD.md` §7 = `Backlog.md` §3.5):

| Código | HTTP | Mensaje | Regla |
|---|---|---|---|
| `VF-REQ-005` | 409 | `This request cannot move from {current} to {target}.` | `FR-LFC-001` / §4.2 — **nuevo**, con interpolación (`TE-005` criterio 3) |
| `VF-REQ-002` | 400 | `The start date cannot be in the past.` | `RULE-02` re-evaluada al someter (`FR-LFC-003`, `OQ-04`) — **ya declarado**, se reutiliza |
| `VF-REQ-004` | 403 | `You can only act on your own requests.` | `RULE-04` (`FR-LFC-002`) — **ya declarado**, se reutiliza |
| `VF-REQ-006` | 404 | `The request was not found.` | contrato del endpoint (`FRD.md` §6.3) — **ya declarado**, se reutiliza |
| `VF-REQ-003` | 409 | `Only Draft requests can be edited.` | `RULE-03`/`FR-LFC-004` (`AC5`) — **ya implementado** en `UpdateDetails` + `UpdateRequestHandler`; aquí solo gana los tests que faltaban |
| `VF-AUT-004` | 401 | `You must be signed in to perform this action.` | `FR-AUT-011` (resuelto por `TE-011`) |

Contrato del endpoint, verbatim de `FRD.md` §6.3 (con el delta `ADR-012` sobre el cuerpo de éxito — ver `D3`):

> **`POST /requests/{id}/submit`** · Request: empty · Success `200`: the updated request · Errors: `VF-REQ-002` `400` · `VF-REQ-004` `403` · `VF-REQ-006` `404` · `VF-REQ-005` `409`

Banner de éxito (§3.5, verbatim): `Request submitted for approval.` — su render pertenece a la superficie diferida (`D2`).

### 1.4 Alcance

**Entra**

- El comportamiento `Submit(today, nowUtc)` en el agregado `Request` (transición `T1`: guarda de estado → re-validación `RULE-02` → `State = Submitted`, `SubmittedAtUtc`, `UpdatedAtUtc`), con la forma exacta de `SAD.md` §5.3.
- La factory `RequestErrors.InvalidTransition(RequestState from, RequestState to)` (`VF-REQ-005` interpolado con los nombres de estado de §3.4 — el `ToString()` del enum ya coincide).
- El caso de uso `SubmitRequestHandler` (sin command record — sin cuerpo que validar, ver `D4`), reutilizando `IRequestRepository.GetByIdAsync` **sin crecer el puerto**.
- API: `POST /api/requests/{id:guid}/submit` → `204` (`ADR-012`), entrada `VF-REQ-005 → 409` en `ErrorStatusMap`.
- Tests: unitarios de dominio (`Submit` + el `UpdateDetails`-tras-`Submit` que salda `US-016` `D7` y cubre `AC5`), unitarios del handler, funcionales del endpoint (incluido el `PUT` → `409 VF-REQ-003` a nivel HTTP que `RequestEndpointTests` dejó documentadamente pendiente), fila nueva en el pin de estados de `SourceRuleTests`.

**No entra**

| Excluido | Por qué / destino |
|---|---|
| El botón `Submit` en la fila de `S-04`, el reload de la lista y el banner `Request submitted for approval.` | `US-024` (My Requests screen) es la dueña de la fila, su matriz de acciones por estado (`Draft` → `Edit · Submit · Cancel`) y el reload tras cada acción — y **depende de `US-018`**. `/requests` es hoy un placeholder sin lista que recargar (verificado). Mismo patrón "backend primero, pantalla con su historia" ratificado en `US-015` `D9` / `US-016` `D8` → `US-017`. Ver `D2` — **decisión mayor de este plan** |
| `submitRequest()` en `src/web/lib/api.ts` | Sería código muerto hasta `US-024` (nadie lo llama); `US-024` lo añade con su botón. `ADR-013` no exige escribir la función antes de su primer consumidor |
| `GET /requests` (lista con filtro por rol) | `US-020` |
| `Cancel`, `Decide`, `Approval`, `ApprovalPolicy`, errores `VF-DEC-*` | `US-019`, `US-021`+ — cada método llega con su historia (`US-015` `D5`) |
| Modal de confirmación al someter | Ningún criterio lo pide (el modal es de Cancel, `S-08`/`US-033`); el prototipo somete directo |
| Concurrencia optimista (doble submit simultáneo) | Sin requisito (`TC-06`); el segundo submit recibe `VF-REQ-005` por la guarda de estado — resultado correcto sin maquinaria |
| Notificación al manager | `OS-12`/`FUT-*` — fuera del MVP |

---

## 2. Cambios estructurales / de base

**No se requieren cambios de esquema, migraciones, configuración, variables de entorno, permisos, feature flags ni dependencias nuevas.** `Submit` escribe únicamente columnas que la migración `AddRequests` (`20260731004548`) ya creó — `State`, `SubmittedAtUtc` (nullable, verificada en el snapshot) y `UpdatedAtUtc`. `ErrorStatusMap` (Api, no base de datos) gana una entrada — ítem #5.

---

## 3. Plan ordenado por dependencia

De adentro hacia afuera (Domain → Application → API → tests). Sin ítems de Infrastructure ni de Web (ver §1.4 y `D2`). Prosa en español, identificadores en inglés.

| # | Capa | Acción | Artefacto | Notas |
|---|---|---|---|---|
| 1 | Domain | Modificar | `src/BigSolutions.VacaFlow.Domain/Requests/Errors/RequestErrors.cs` | Añadir la factory de `SAD.md` §5.5, literal: `public static Error InvalidTransition(RequestState from, RequestState to) => new("VF-REQ-005", $"This request cannot move from {from} to {to}.");` — sin `Field` (error de operación, como sus hermanos `VF-REQ-003/004/006`). Los `ToString()` del enum coinciden con los labels de §3.4 (`Draft`, `Submitted`, …), que es lo que `TE-005` criterio 3 exige interpolar. Es el primer miembro no-`static readonly` del archivo: el doc-comment lo anota (mensaje paramétrico → factory) |
| 2 | Domain | Modificar | `src/BigSolutions.VacaFlow.Domain/Requests/Request.cs` | Añadir `public Result Submit(DateOnly today, DateTime nowUtc)` (forma de `SAD.md` §5.3, transición `T1`): (1) `State is not RequestState.Draft` → `RequestErrors.InvalidTransition(State, RequestState.Submitted)` (`FR-LFC-001` — el estado manda primero, mismo orden que `UpdateDetails`); (2) `Period.Start < today` → `RequestErrors.StartDateInPast` (`FR-LFC-003`/`OQ-04` — **misma comparación y mismo error que `Create`/`UpdateDetails`**, así `AC2` devuelve el `VF-REQ-002` del catálogo con su `field`); éxito → `State = RequestState.Submitted`, `SubmittedAtUtc = nowUtc`, `UpdatedAtUtc = nowUtc` (`CreatedAtUtc` y `Reason`/`Period`/`AbsenceTypeId` intactos). Sin lectura del reloj (parámetros, `TE-004`). Tras la transición, `IsEditable` pasa a `false` solo — la inmutabilidad de `FR-LFC-004` ya está escrita en `UpdateDetails` y no se toca. Actualizar el doc-comment de la clase (Cancel/Decide siguen pendientes con `US-019`/`US-021`) |
| 3 | Application | Crear | `src/BigSolutions.VacaFlow.Application/Requests/SubmitRequestHandler.cs` | `public sealed class SubmitRequestHandler(ICurrentUser currentUser, IRequestRepository requests, IUnitOfWork unitOfWork, TimeProvider timeProvider)` → `public async Task<Result> Handle(Guid requestId, CancellationToken cancellationToken)`. Secuencia (patrón `UpdateRequestHandler`, menos pasos porque no hay cuerpo): (1) `requests.GetByIdAsync(new RequestId(requestId), …)` → null → `RequestErrors.NotFound` (`VF-REQ-006`); (2) `request.OwnerId != currentUser.EmployeeId` → `RequestErrors.NotOwner` (**`RULE-04`**, `FR-LFC-002` — única comparación de identidad, contra `ICurrentUser`); (3) `request.Submit(today, nowUtc)` con `today`/`nowUtc` derivados de `timeProvider.GetUtcNow()` (mismo cálculo que create/update) → `VF-REQ-005`/`VF-REQ-002`; (4) `unitOfWork.SaveChangesAsync(…)`. **Sin command record ni `Validate()`** — no hay payload (ver `D4`; precedente: `GetRequestByIdHandler.Handle(Guid, ct)`). Sin `IAbsenceTypeRepository` (no cambia el tipo — ver `D7`) ni `IIdGenerator` (no crea nada). El nombre `SubmitRequestHandler` es el que `SAD.md` §6.1 ya lista |
| 4 | Application | Modificar | `src/BigSolutions.VacaFlow.Application/DependencyInjection.cs` | `services.AddScoped<SubmitRequestHandler>();` |
| 5 | API | Modificar | `src/BigSolutions.VacaFlow.Api/ErrorHandling/ErrorStatusMap.cs` | `["VF-REQ-005"] = StatusCodes.Status409Conflict` — el HTTP de `FRD.md` §7; obligatorio para `Every_Domain_Error_Code_Should_Have_A_Status_Mapping` (el escáner de literales ve el `"VF-REQ-005"` de la factory #1) |
| 6 | API | Modificar | `src/BigSolutions.VacaFlow.Api/Endpoints/RequestEndpoints.cs` | En el grupo existente `/api/requests`, añadir `group.MapPost("/{id:guid}/submit", …)`: sin contrato de entrada (cuerpo vacío por `FRD.md` §6.3), invoca `SubmitRequestHandler.Handle(id, ct)` y devuelve `result.ToHttpResult()` → `204 No Content` (`ADR-012`, ver `D3`; la extensión ya hace exactamente eso). **`.RequireAuthorization()` explícito** (test `Every_Endpoint_Should_State_Its_Authorization_Explicitly`). Recibe, delega, mapea — cero condicionales (`CA-PRE-001`). Un id no-Guid no matchea `{id:guid}` → `404` del framework, coherente con `VF-REQ-006` |
| 7 | Test | Modificar | `tests/BigSolutions.VacaFlow.Domain.UnitTests/Requests/RequestTests.cs` | Bloque `Submit` (fechas por parámetro, determinismo `TE-004`): (a) `Draft` con `Period.Start` futuro → éxito, `State == Submitted`, `SubmittedAtUtc == nowUtc`, `UpdatedAtUtc == nowUtc`, `CreatedAtUtc`/contenido intactos; (b) frontera `Period.Start == today` → éxito (mismo criterio que `Create`); (c) `Period.Start == today - 1` (draft creado con fecha válida y sometido con un `today` posterior) → `VF-REQ-002`, estado sigue `Draft`, `SubmittedAtUtc` sigue null; (d) `Submit` sobre un request ya `Submitted` → `VF-REQ-005` con el **mensaje interpolado exacto** `"This request cannot move from Submitted to Submitted."` (`TE-005` criterio 3 — la aserción pinta la interpolación, no solo el código). **Además, saldar `US-016-plan` `D7` = `AC5`**: (e) `Create` → `Submit` → `UpdateDetails` con datos válidos → `VF-REQ-003` y ningún campo mutado — el unitario de `RULE-03` sobre estado no-`Draft` que quedó pendiente por inalcanzabilidad, ahora por la vía pública legítima |
| 8 | Test | Crear | `tests/BigSolutions.VacaFlow.Application.UnitTests/Requests/SubmitRequestHandlerTests.cs` | Con los fakes existentes (`FakeRequestRepository`, `FakeCurrentUser`, `FakeUnitOfWork`, `FixedTimeProvider`) — el fake del repositorio ya tiene `GetByIdAsync`, no se toca: (a) dueño somete su draft → éxito, `State == Submitted`, `SaveChanges` invocado; (b) id inexistente → `VF-REQ-006`, nada guardado; (c) `FakeCurrentUser` con otro `EmployeeId` → `VF-REQ-004`, estado intacto — y el orden: dueño **antes** que estado, un no-dueño de un request ya sometido recibe `403`, no `409` (ver `S1`); (d) draft creado con `start == today` y reloj avanzado un día antes de someter → `VF-REQ-002`, nada guardado; (e) request ya `Submitted` (sometido en el arrange vía `Submit` real) → `VF-REQ-005`; (f) frontera `start == today` → éxito |
| 9 | Test | Modificar | `tests/BigSolutions.VacaFlow.Api.FunctionalTests/Endpoints/RequestEndpointTests.cs` | Contra `VacaFlowApiFactory` (pipeline real, cookie real; draft previo por `POST /api/requests` real): (a) **`AC1`** — `POST /api/requests/{id}/submit` del propio draft → `204`; `LoadRequestAsync` (helper DI existente, probado fiable) muestra `State == Submitted` y `SubmittedAtUtc` no nulo; (b) **`AC5`** — sobre ese mismo request, `PUT /api/requests/{id}` → `409` `{ code: "VF-REQ-003" }`: **el test HTTP que el comentario final de esta clase difirió textualmente a `US-018`**; reescribir ese comentario (la vía (b) ya existe); (c) **`AC3`** — segundo `POST …/submit` → `409` `{ code: "VF-REQ-005", message: "This request cannot move from Submitted to Submitted." }` (aserción del mensaje interpolado también en el borde HTTP); (d) **`AC4`** — segunda cuenta registrada somete el draft de la primera → `403` `VF-REQ-004`; (e) Guid aleatorio → `404` `VF-REQ-006`; (f) sin cookie → `401` `VF-AUT-004`; (g) **`AC2`** — retrasar `StartDate`/`EndDate` de un draft real a ayer vía `ExecuteSqlRawAsync` **sobre el `DbContext` del contenedor DI de la factory** (la vía in-host que `LoadRequestAsync` prueba fiable — no la `SqliteConnection` out-of-band que está documentadamente rota) y someter → `400` `VF-REQ-002`. Si (g) resultara flaky pese a ser in-host, degradar a cobertura unitaria (#7c, #8d) y documentarlo en el comentario, mismo tratamiento que tuvo `RULE-03` — ver `D6` |
| 10 | Test | Modificar | `tests/BigSolutions.VacaFlow.ArchitectureTests/SourceRuleTests.cs` | Añadir `[InlineData("VF-REQ-005", "StatusCodes.Status409Conflict")]` a `Known_Error_Codes_Should_Map_To_Their_Documented_Status` (el pin de estados crece "as new codes with a settled HTTP mapping arrive", según su propio remark) |
| 11 | Test | Verificar | Suites completas: `dotnet build` + `dotnet test VacaFlow.slnx` · `npm run lint` + `npm run depcruise` + `npm run build` en `src/web` (sin cambios web, deben seguir verdes) | Arquitectura sin modificar en verde: handler `sealed` terminado en `Handler`, endpoint con autorización explícita, `VF-REQ-005` mapeado, sin lectura directa del reloj, sin tokens de identidad (no hay contrato nuevo que barrer), anillos intactos |

**Dependencias:** 1 → 2 → {3, 7} · 3 → 4 · 1 → 5 · {3, 4} → 6 · {5, 6} → 9 · 3 → 8 · 5 → 10 · todo → 11. **Paralelizable:** #5/#10 (tras #1) y #7 (tras #2) con la rama del handler (#3–#4). **Ruta crítica:** 1 → 2 → 3 → 6 → 9.

---

## 4. Casos de uso y tabla de trazabilidad

Caso de uso único de Application: **someter el propio borrador a aprobación** (`SubmitRequestHandler`), consumido por `POST /api/requests/{id}/submit`. Actor: el dueño autenticado (`RULE-04`); cualquier otro usuario recibe `VF-REQ-004`, cualquier estado no-`Draft` recibe `VF-REQ-005`, y un borrador con la fecha de inicio ya vencida recibe `VF-REQ-002` (`OQ-04`). `AC5` no introduce caso de uso nuevo: re-verifica `UpdateRequestHandler`/`UpdateDetails` existentes contra el estado `Submitted` por fin alcanzable.

| Historia | Criterio de aceptación (verbatim) | Ítems del plan que lo cubren | Cómo se verifica |
|---|---|---|---|
| `US-018` | "Given my own `Draft`, when I press `Submit`, then it becomes `Submitted`, the list reloads and the banner reads `Request submitted for approval.`" | #2 (`Submit` transiciona y sella `SubmittedAtUtc`), #3, #4, #6. **Parte visual diferida** (botón `Submit` en la fila, reload de la lista, banner): la fila y sus acciones son de `US-024`, que depende de esta historia — ver `D2` | Dominio #7a/#7b · handler #8a/#8f · funcional #9a (`204` real con cookie real, `Submitted` + `SubmittedAtUtc` verificados) · §6 pasos 4–5 |
| `US-018` | "Given a `Draft` whose start date has since passed, when I submit, then `VF-REQ-002` is returned in an error banner. *(`OQ-04`, confirmed by the prototype.)*" | #2 (re-validación `FR-LFC-003` con el **mismo** `StartDateInPast` del catálogo), #3, #6. El "error banner" es render de `US-024` (`D2`); el backend entrega código, mensaje y `field` idénticos a los de create/edit | Dominio #7c · handler #8d (reloj avanzado con `FixedTimeProvider`) · funcional #9g (fechas retrasadas in-host → `400 VF-REQ-002`) · §6 paso 7 |
| `US-018` | "Given a request that is not a `Draft`, when a submit is attempted, then `VF-REQ-005` is returned." | #1 (`InvalidTransition` interpolado), #2 (guarda de estado primero), #5 (`409`), #6 | Dominio #7d (mensaje interpolado exacto) · handler #8e · funcional #9c (doble submit → `409` con mensaje interpolado en el wire) · pin #10 · §6 paso 6 |
| `US-018` | "Given another employee's request, when a submit is attempted, then `VF-REQ-004` is returned." | #3 (**`RULE-04`** — comparación contra `ICurrentUser.EmployeeId`, `FR-LFC-002`); error y mapeo `403` ya existentes, se reutilizan | Handler #8c (incluida la precedencia dueño-antes-que-estado) · funcional #9d (dos cuentas reales → `403`) · §6 paso 8 |
| `US-018` | "Given a submitted request, when an edit is attempted, then it is rejected." | **Ya implementado** por `US-016` (`UpdateDetails` guarda `RULE-03` + `UpdateRequestHandler` corto-circuito → `VF-REQ-003` `409`) — esta historia **no añade código de producción** para este criterio; añade los dos tests que quedaron documentadamente pendientes: #7e (unitario de dominio, deuda `US-016` `D7`) y #9b (HTTP, deuda del comentario de `RequestEndpointTests`) | Dominio #7e (`Create` → `Submit` → `UpdateDetails` → `VF-REQ-003`, nada mutado) · funcional #9b (`PUT` tras submit real → `409 VF-REQ-003`) · §6 paso 9 |

**Conteo: 5 criterios de entrada · 5 cubiertos** (los fragmentos de UI de `AC1`/`AC2` diferidos con destino explícito `US-024`, decisión `D2` — mismo tratamiento aprobado en `US-015` `D9` y `US-016` `D8`).

Deuda de UI que esta historia **añade** a `US-024` (reanotada para su plan): botón `Submit` (estilo primario) en la fila `Draft`, llamada `submitRequest(id)` nueva en `lib/api.ts`, reload de la lista tras someter (`FR-UIX-005`), banner de éxito `Request submitted for approval.`, y errores de la operación (`VF-REQ-002/004/005`) pintados en **banner de error** (no bajo un campo — en `S-04` no hay formulario), según la matriz que `US-031` completa.

---

## 5. Supuestos y decisiones

Sesión de planificación sin interlocutor humano (Fase 3 no interactiva): las ambigüedades se resolvieron con criterio de arquitecto y quedan documentadas con su reversibilidad. **`D2` es la decisión de alcance mayor; revisarla primero.**

| # | Decisión | Justificación | Impacto si es incorrecta |
|---|---|---|---|
| `D1` | **La ruta es `POST /api/requests/{id}/submit`** | Es la forma literal de `FRD.md` §6.3 (fila 9: "no generic state-change operation" — un verbo por transición), bajo la convención `/api` ya ratificada tres veces (`US-014` `D1`, `US-015` `D1`, `US-016` `D1`). El grupo `/api/requests` existe | Renombrar es una línea más tests |
| `D2` | **Toda la superficie web (`botón Submit`, reload de lista, banner de éxito, banner de error) se difiere a `US-024`; esta historia no toca `src/web/`** | El backlog asigna la pantalla `S-04` real — filas, matriz de acciones por estado (`Draft` → `Edit · Submit · Cancel`), estilos de botón — a `US-024`, que **depende de `US-018`** (el orden historia-backend → historia-pantalla es el diseñado, no un accidente). Hoy `/requests` es un placeholder sin lista (verificado): no hay fila donde poner el botón ni lista que recargar; construir una "mini-lista" provisional aquí duplicaría `US-024` y `US-020`. Precedente triple aprobado: `US-014` `D5`, `US-015` `D9`, `US-016` `D8` difirieron su UI a `US-017` exactamente así, con la deuda reanotada (§4). La historia queda "backend done", demostrable end-to-end por API (§6) | Si el usuario prefiere una afordancia mínima ya (p. ej. botón `Submit` provisional en la página `[id]` de edición cuando el estado es `Draft`), el añadido es local: `submitRequest()` en `lib/api.ts` + un botón en `RequestForm`/página — aditivo, sin rehacer nada de este plan. **Pregunta abierta al usuario, ver §7** |
| `D3` | **`204 No Content` en el éxito; el handler devuelve `Result` sin valor** — no "the updated request" pese al literal `200` de `FRD.md` §6.3 | `ADR-012`/`SAD.md` §18 corrigen expresamente al FRD: command endpoints devuelven `204` y la UI refetchea (`FR-UIX-005`); `ToHttpResult()` ya lo implementa. Tercera aplicación del mismo delta (`US-015` `D2`, `US-016` `D2`) | Si la UI necesitara el objeto, `GET /api/requests/{id}` (US-017) ya lo sirve; cambiar a un cuerpo sería local al endpoint |
| `D4` | **`SubmitRequestHandler.Handle(Guid requestId, ct)` — sin command record ni `Validate()`** | `FRD.md` §6.3: "Request: empty" — no hay payload, no hay campos que validar; un `SubmitRequestCommand(Guid RequestId)` con `Validate()` vacío sería ceremonia (`TC-06` en espíritu). `ADR-011` gobierna commands **con** datos; el precedente de handler-sin-command con el id de la ruta ya existe y está aprobado (`GetRequestByIdHandler`, `US-017` ítem #2). El constraint `{id:guid}` de la ruta hace imposible un id malformado | Si se prefiriera el record por uniformidad de commands mutadores, el cambio es cosmético y local (`US-016` `D5` describe esa forma) |
| `D5` | **La guarda de estado usa `State is not RequestState.Draft` directamente** (como el sketch de `SAD.md` §5.3), no `IsEditable` | `IsEditable` documenta `RULE-03` ("editable"); reutilizarlo para la elegibilidad de submit acoplaría dos reglas distintas (`RULE-03` edición vs `T1` transición) que hoy coinciden por casualidad — si `US-019` matiza qué es "editable", submit no debe moverse con ello. La semántica correcta del fallo también difiere: submit inválido es `VF-REQ-005` (transición), no `VF-REQ-003` (edición) | Cero costo: son expresiones equivalentes hoy; el desacople es gratis ahora y caro después |
| `D6` | **`AC2` a nivel funcional se produce retrasando las fechas del draft por `ExecuteSqlRawAsync` sobre el `DbContext` del contenedor DI de la factory** (in-host), no esperando tiempo real ni sustituyendo `TimeProvider` en la factory | `VacaFlowApiFactory` monta el composition root real con el reloj real (verificado); inyectar un reloj falso al host funcional alteraría el pipeline que estos tests existen para probar tal cual. La vía out-of-band (SqliteConnection nueva contra el archivo) está **documentadamente rota** en este harness; la vía in-host es la que `LoadRequestAsync` ya usa y declara fiable. El dato mutado (fechas) es contenido, no estado de máquina — el mismo tipo de mutación que el `UPDATE` de `AbsenceTypeRepositoryTests` sobre `IsActive` | Si aun así resultara flaky, la cobertura de `AC2` queda íntegra en dominio (#7c) y handler (#8d) con reloj fijo, y el funcional lo documenta — mismo tratamiento que `RULE-03` tuvo en `US-016` hasta hoy |
| `D7` | **Submit no re-valida el catálogo (`ExistsActiveAsync`) ni el contenido del draft más allá de `RULE-02`** | `FR-LFC-003` re-evalúa **solo** `FR-REQ-003` (fecha de inicio); ni el FRD ni el backlog piden re-comprobar el tipo de ausencia al someter (el contrato de errores de `POST …/submit` no lista `VF-CAT-001`). El draft ya nació/se editó validado (`FR-REQ-007`); un tipo desactivado después de crear el draft es un escenario que ninguna regla del MVP cubre — inventar la guarda sería scope creep (`TC-06`), y el simétrico exacto de la decisión inversa `US-016` `D3` (allí la edición **cambia** el tipo, por eso valida; aquí no cambia nada) | Si producto decidiera que un draft con tipo desactivado no puede someterse, es un check + un test en el handler — aditivo |
| `S1` | El orden del handler (cargar → `NotFound` → dueño → dominio) devuelve **un** error por petición: un no-dueño de un request no-`Draft` recibe `403`, no `409` | Patrón vigente (`FR-ERR-002`, `US-016` `S1`): la autorización se responde antes que el estado del recurso — no se revela el estado de una solicitud ajena a quien no puede actuar sobre ella. Es el mismo orden de `UpdateRequestHandler` | Cambiar el orden es local al handler; el contrato de error no cambia |
| `S2` | El doble submit del mismo draft (dos pestañas) se resuelve por la guarda de estado: el segundo recibe `VF-REQ-005`, sin token de concurrencia | Único actor posible: el dueño. El resultado es exactamente el correcto (`Draft`→`Submitted` una vez; el segundo intento es una transición inválida `Submitted`→`Submitted`). `TC-06` prohíbe maquinaria extra | Ninguno: el comportamiento emergente ya es el especificado |
| `S3` | "Today" para `FR-LFC-003` es la fecha UTC del `TimeProvider`, calculada en el handler y pasada como parámetro — mismo `D4`/`D9` de `US-015`/`US-016`, sin re-litigar | Coherencia estricta: si create, edit y submit usaran relojes distintos, un mismo draft sería válido en una operación e inválido en otra. `AS-04` (sin zonas horarias) vigente | Cambiaría en los tres handlers a la vez; dominio y tests intactos |
| `S4` | El mensaje interpolado usa los `ToString()` del enum (`Draft`, `Submitted`, …), que coinciden carácter a carácter con los labels de §3.4 | `TE-005` criterio 3 exige interpolar "the state names of §3.4"; el enum ya los tiene como identificadores. Sin diccionario de labels duplicado | Si §3.4 y el enum divergieran algún día, la factory #1 es el único punto a tocar |

---

## 6. Verificación end-to-end

| # | Paso | Resultado esperado |
|---|---|---|
| 1 | `dotnet build VacaFlow.slnx` | Compila con **0 warnings** (`TreatWarningsAsErrors`) |
| 2 | `dotnet test VacaFlow.slnx` | Suite completa verde, incluidos #7–#10 y los tests de arquitectura sin modificar |
| 3 | `dotnet run --project src/BigSolutions.VacaFlow.Api` (puerto 5217) | Arranca; sin migración nueva que aplicar |
| 4 | Login `employee@vacaflow.test` / `Employee123!` · crear un draft (`POST /api/requests`, fechas futuras) y capturar el `id` | `201` |
| 5 | `POST /api/requests/{id}/submit` (cuerpo vacío) | `204`; en la base, `State = 1` (`Submitted`), `SubmittedAtUtc` y `UpdatedAtUtc` sellados, `CreatedAtUtc` intacto |
| 6 | Repetir el `POST …/submit` sobre el mismo id | `409` `{ "code": "VF-REQ-005", "message": "This request cannot move from Submitted to Submitted." }` — interpolación visible en el wire |
| 7 | Crear otro draft, retrasar sus `StartDate`/`EndDate` a ayer por SQL directo sobre la base de desarrollo, y someterlo | `400` `{ "code": "VF-REQ-002", "message": "The start date cannot be in the past.", "field": "startDate" }` |
| 8 | Login `manager@vacaflow.test` e intentar el submit de un draft de Carlos | `403` `{ "code": "VF-REQ-004", "message": "You can only act on your own requests." }` |
| 9 | Como dueño, `PUT /api/requests/{id}` del request sometido en el paso 5 (payload válido) | `409` `{ "code": "VF-REQ-003", "message": "Only Draft requests can be edited." }` — `AC5` end-to-end |
| 10 | `POST /api/requests/{guid aleatorio}/submit` · submit sin cookie | `404` `VF-REQ-006` · `401` `VF-AUT-004` |
| 11 | En el web (`npm run dev`): abrir `/requests/{id}` del request sometido | `GET` devuelve `state: "Submitted"` → título `Request detail`, controles deshabilitados, sin botón primario (`US-017` `AC8` sigue funcionando con el estado nuevo — regresión visual, sin cambios de código) |
| 12 | `cd src/web && npm run lint && npm run depcruise && npm run build` | Verdes sin cambios (esta historia no toca el web) |

---

## 7. Riesgos y preguntas abiertas

> ⚠️ **Pregunta abierta para el usuario (no bloquea el backend — bloquea solo si la respuesta amplía el alcance):**
> `AC1`/`AC2` mencionan superficie visible ("the list reloads", "error banner") que este plan difiere íntegra a `US-024` (`D2`), siguiendo el patrón triple ya aprobado. **¿Se acepta el diferimiento, o se quiere una afordancia provisional de `Submit` ya en esta historia** (p. ej. un botón `Submit` en la página de edición `/requests/[id]` cuando el estado es `Draft`)? Si se quiere la afordancia, se añaden tres ítems web (función `submitRequest` en `lib/api.ts`, botón + estados de carga/error en `RequestForm` o la página `[id]`, banner de éxito reutilizando `setPendingNotification`) — aditivos sobre este plan.

| Riesgo | Mitigación |
|---|---|
| `D2` deja la historia sin demo visual en la aplicación (solo API + regresión de `US-017` en §6.11) | Deuda reanotada nominalmente en §4 para `US-024` (mismo mecanismo que llevó la deuda de `US-014`–`US-016` a `US-017` sin pérdidas); pregunta abierta arriba por si se prefiere la afordancia provisional |
| El caso funcional #9g depende de mutar fechas por SQL in-host — la variante out-of-band está rota en este harness | La vía elegida es la misma que `LoadRequestAsync` prueba fiable; fallback documentado en `D6` (cobertura queda en #7c/#8d con reloj fijo, como `RULE-03` estuvo hasta hoy) |
| La factory `InvalidTransition` introduce el primer error paramétrico — el escáner de `Every_Domain_Error_Code_Should_Have_A_Status_Mapping` debe seguir viéndolo | Verificado en el código del test: escanea literales `"VF-[A-Z]+-\d+"` en cualquier `*Errors.cs`, no solo campos `static readonly`; el literal está en la factory. #10 añade además el pin del `409` |
| `US-019` (Cancel) reutilizará `InvalidTransition` con otro destino (`Cancelled`) | La factory ya es genérica (`from`, `to`) — cero retoque previsto; anotado como entrada para el plan de `US-019` |
| El doc-comment de `IRequestRepository` menciona "Listing operations arrive with their first consumer (US-018/US-020…)" — podría leerse como que esta historia debía crecer el puerto | No: submit opera sobre un único agregado por id (`GetByIdAsync` existente); el listado es de `US-020`. Actualizar ese comentario al tocar cerca **no** entra en esta historia (el archivo no se modifica) |

---

Siguiente paso — implementación:

```
/user-story-implement ".claude/implementation plans/US-018-plan.md"
```
