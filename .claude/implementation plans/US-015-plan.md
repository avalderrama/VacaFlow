# Plan de implementación — `US-015` · Create a Draft request

| Campo | Valor |
|---|---|
| Historia | `US-015` — Create a Draft request |
| Épica | `EP-05` — Request authoring |
| Prioridad · Talla | **Must** · `M` |
| Pantalla | `S-05` (formulario de solicitud) — **sin superficie web todavía**; esta historia entrega el agregado y el endpoint (ver §1.4 y `D9`) |
| Depende de | `TE-011` (mergeada — `ICurrentUser` + `CurrentUserAccessor`) · `US-014` (**en PR #11, sin mergear** — `IAbsenceTypeRepository` y `GET /api/absence-types` viven en `feat/us-014-list-absence-types`) · `TE-004` (`TimeProvider.System` registrado en `Program.cs`) |
| Trazas | `RULE-01` · `RULE-02` · `AC-03`–`AC-05` · `FR-REQ-001`–`FR-REQ-004` · `FR-CAT-003` · `FR-AUT-010` · `FRD.md` §3.4, §4.1, §6.3, §7 · `SAD.md` §5, §6, §7.2, §8 · `WBS.md` paquete 5.2 |
| Fuentes | `Backlog.md` §EP-05, §3.5 · `FRD.md` · `SAD.md` · código real verificado en `src/` y `tests/` |
| Repositorio | `C:\Users\avald\OneDrive\Work\VacaFlow\repositories\vacaflow` |
| Rama sugerida | `feat/us-015-create-draft-request`, creada **desde `feat/us-014-list-absence-types`** (apilada — ver `D10`) |
| Estado | Aprobado el 2026-07-30 (decisiones de arquitecto documentadas en §5; sesión sin interlocutor humano) |

> **Este documento no implementa nada.** La implementación la ejecuta `/user-story-implement`.

---

## 1. Entendimiento

### 1.1 Contexto — por qué ahora

Esta es la primera historia que crea el corazón del producto: el agregado `Request`. Hasta hoy el sistema autentica (`EP-02`), se conoce a sí mismo (`GET /api/auth/me`) y expone el catálogo (`GET /api/absence-types`), pero **no existe ninguna solicitud de ausencia en ninguna capa** — verificado: `src/BigSolutions.VacaFlow.Domain/Requests/` contiene solo las subcarpetas vacías `Errors/` y `Services/`; no hay `Request.cs`, ni `IRequestRepository`, ni tabla `Requests`, ni endpoint. A diferencia de `TE-003`/`US-014` (estructurales), esta historia diseña reglas de negocio reales: `RULE-01` y `RULE-02` viven en el dominio, no en el handler.

Hallazgos de grounding, todos verificados contra el código:

- **`TE-011` entregó `ICurrentUser`** (`Application/Abstractions/ICurrentUser.cs`: `EmployeeId EmployeeId`, `EmployeeRole Role`), implementado por `Api/Security/CurrentUserAccessor.cs` desde claims. El dueño de la solicitud sale de ahí, **nunca** del payload (`FR-AUT-010`). El test de arquitectura `No_Contract_Or_Command_Should_Carry_An_Identity_Field` barre `*Contract.cs`/`*Command.cs`/`*Query.cs`/`*Request.cs` en Api y Application y prohíbe los tokens `EmployeeId`, `ManagerId`, `ResponsibleManagerId` — el comando y el contrato de esta historia no pueden nombrarlos.
- **`US-014` (rama actual, PR #11)** entregó `IAbsenceTypeRepository` con una sola operación (`ListActiveAsync`). Validar "tipo existente y activo" (`FR-CAT-003` → `VF-CAT-001`) necesita una operación nueva en ese puerto (ver ítem #8 y `D6`).
- **`TE-004`**: `TimeProvider.System` está registrado como singleton en `Program.cs`; `CredentialStore` ya lo consume por constructor, y `CredentialStoreTests` fija el reloj con un `FixedTimeProvider` local — patrón reutilizable. El test `Domain_And_Application_Should_Not_Read_The_Clock_Directly` prohíbe `DateTime.Now/UtcNow/Today`, `DateTimeOffset.Now/UtcNow`, `DateOnly.FromDateTime(DateTime…` y `Guid.NewGuid(` en Domain y Application: la fecha entra al dominio **como parámetro**.
- **Primitivas listas**: `AggregateRoot<TId>`, `ValueObject`, `Result`/`Result<T>`, `Error(Code, Message, Field?)`. Los ids son `readonly record struct` (`EmployeeId`, `AbsenceTypeId`). `IIdGenerator`, `IUnitOfWork` (devuelve `Result`) y sus implementaciones existen desde `US-007` — **se reutilizan, no se crean**.
- **Patrón de handler**: clase plana `sealed` con ctor primario, `Validate()` estructural en el command como primera línea, errores de dominio en `*Errors.cs` con códigos `VF-*` verbatim del catálogo. `ErrorStatusMap` debe recibir todo código nuevo o el test `Every_Domain_Error_Code_Should_Have_A_Status_Mapping` falla (y sin entrada, el código cae a `500`).
- **Endpoints**: `MapGroup("/api/…")` + `.RequireAuthorization()` explícito en cada `Map{Verb}` (obligado por `Every_Endpoint_Should_State_Its_Authorization_Explicitly`); `ResultExtensions.ToCreatedResult(location, body)` ya existe para el `201`.
- **`SAD.md` §5 ya diseñó este agregado** (`Request`, `RequestId`, `RequestState`, `DateRange`, `RequestErrors`) con firmas ilustrativas; este plan lo aterriza al código real (ids por parámetro desde `IIdGenerator`, `Error` con `Field`, etc.).
- **`ADR-012` (SAD §8.3, §18)**: los command endpoints devuelven `204`; **create devuelve `201` + `Location` con el identificador**, no la solicitud creada — el `FRD.md` §6.3 ("Success `201`: the created request") está expresamente corregido por el delta de SAD §18 (ver `D2`).
- **No existe aplicación web** (`src/web/` no existe). Los fragmentos de UI de los criterios (banner, retorno a `S-04`, mensajes bajo el campo) se difieren a la historia que construya `S-05`/`S-04` (`US-017`+), igual que hizo `US-014` con su select (ver `D9`).

### 1.2 Narrativa

El backlog formula `US-015` por criterios. La intención la fijan `EP-05` y `FR-REQ-001`: *"An authenticated employee creates a request with absence type, start date, end date and reason. The request is created in state `Draft`, owned by the authenticated user."* — el primer paso del ciclo de vida de `FRD.md` §4.

### 1.3 Criterios de aceptación — verbatim (`Backlog.md` §EP-05 · `US-015`)

| # | Criterio |
|---|---|
| `AC1` | "Given valid data, when I `POST /requests`, then a request is created in `Draft` owned by the authenticated user, I return to `S-04` and the banner reads `Draft created.`" |
| `AC2` | "Given an end date before the start date, when I save, then `VF-REQ-001` appears beneath `End date`." |
| `AC3` | "Given a start date before today, when I save, then `VF-REQ-002` appears beneath `Start date`." |
| `AC4` | "Given a payload carrying an `employeeId`, when processed, then the owner is still the authenticated user." |
| `AC5` | "Given a missing type, date or reason, when I save, then the corresponding validation message appears beneath that field." |

Reglas y errores implicados, verbatim del catálogo (`FRD.md` §7 = `Backlog.md` §3.5):

| Código | HTTP | Mensaje | Regla |
|---|---|---|---|
| `VF-REQ-001` | 400 | `The end date cannot be earlier than the start date.` | `RULE-01` (`FR-REQ-002`: "A single-day absence where both dates are equal is valid") |
| `VF-REQ-002` | 400 | `The start date cannot be in the past.` | `RULE-02` (`FR-REQ-003`: "A start date equal to today is valid") |
| `VF-CAT-001` | 400 | `The selected absence type does not exist or is not available.` | `FR-CAT-003` |
| `VF-VAL-001` | 400 | `The submitted data is not valid. (field-specific detail)` — mensajes de campo de §3.5: `The start date is required.` · `The end date is required.` · `The reason is required (1 to 500 characters).` | `FR-REQ-004` |
| `VF-AUT-004` | 401 | `You must be signed in to perform this action.` | `FR-AUT-011` (ya resuelto de extremo a extremo por `TE-011`) |

### 1.4 Alcance

**Entra**

- El agregado `Request` completo para esta historia: `RequestId`, `RequestState` (los **cinco** estados de `FRD.md` §4.1 — la enumeración es cerrada y las historias siguientes no deben tocarla), el value object `DateRange` (`RULE-01` inquebrantable por construcción, `SAD.md` §5.2), `RequestErrors`, y `Request` con **solo** el comportamiento que esta historia ejercita: `Create` (ver `D5`).
- El caso de uso `CreateRequestHandler` con `CreateRequestCommand.Validate()`, el puerto `IRequestRepository` (solo `Add`), la operación nueva `ExistsActiveAsync` en `IAbsenceTypeRepository`.
- Persistencia: `RequestConfiguration`, `DbSet<Request>`, `RequestRepository`, migración `AddRequests` con el índice `(EmployeeId, State)` de `SAD.md` §7.2.
- API: contrato `CreateRequestContract`, endpoint `POST /api/requests` (`201` + `Location`), tres entradas nuevas en `ErrorStatusMap`.
- Tests: unitarios de dominio (`DateRange`, `Request`), unitarios del handler, integración del repositorio, funcionales del endpoint (incluido el ataque de `AC4`).

**No entra**

| Excluido | Por qué |
|---|---|
| `UpdateDetails`, `Submit`, `Cancel`, `Decide`, `Approval`, `ApprovalPolicy`, `ApprovalErrors` | Son `US-016`, `US-018`, `US-019`, `US-021`+. Crear hoy métodos sin historia ni consumidor sería adelantar trabajo y declarar códigos de error (`VF-REQ-003/004/005`) que obligarían a entradas muertas en `ErrorStatusMap` (ver `D5`) |
| `GET /requests` (listado) y `VF-REQ-006` | `US-020`/`FR-VIS-*`; el puerto crecerá operación a operación (`CA-INF-004`) |
| Toda la superficie web de los criterios (formulario `S-05`, banner `Draft created.`, retorno a `S-04`, mensajes bajo los campos) | No existe `src/web/` (verificado). Igual que `US-014-plan` `D5`: esta historia entrega la parte backend de cada criterio y deja el consumo trazado a `US-017`+ (ver `D9`). Los mensajes con `Field` en el cuerpo `{ code, message, field? }` son exactamente lo que la UI necesitará para pintarlos "beneath that field" |
| `Reason` con contador `N/500`, `min` en los date pickers | Afordances de UI (`Backlog.md` §EP-05 US-017); la API valida siempre |
| Caché, paginación, concurrencia optimista | Nada del MVP lo pide; `TC-06` prohíbe la maquinaria sin requisito |

---

## 2. Cambios estructurales / de base

**Sí aplica** — esta historia crea una tabla:

- **Tabla `Requests`** (`SAD.md` §7.2, `FRD.md` §3.4): `Id` (PK, Guid), `EmployeeId` (FK → `Employees(Id)`, requerido), `AbsenceTypeId` (FK → `AbsenceTypes(Id)`, requerido), `StartDate` y `EndDate` (`DateOnly` — EF Core sobre SQLite lo persiste como `TEXT` ISO-8601 de forma nativa, ver `D3`), `Reason` (requerido, máx. 500), `State` (int, conversión de enum como `Employee.Role`), `CreatedAtUtc`, `UpdatedAtUtc` (requeridos), `SubmittedAtUtc`, `ClosedAtUtc` (nullables — columnas creadas ya para no migrar dos veces; las escriben `US-018`/`US-019`). Índice **`(EmployeeId, State)`**; `DeleteBehavior.Restrict` en ambas FKs (patrón `ManagerId`).
- **Migración EF Core** `AddRequests`, aplicada al arrancar por el `DatabaseInitializer` existente (`ADR-008`) — sin cambio en el initializer.
- **Sin** dependencias nuevas, variables de entorno, configuración, feature flags ni cambios de seed. `ErrorStatusMap` (Api, no base de datos) gana tres entradas — ítem #18.

---

## 3. Plan ordenado por dependencia

De adentro hacia afuera (Domain → Application → Infrastructure → API → tests). Prosa en español, identificadores en inglés.

| # | Capa | Acción | Artefacto | Notas |
|---|---|---|---|---|
| 1 | Domain | Crear | `src/BigSolutions.VacaFlow.Domain/Requests/RequestId.cs` | `public readonly record struct RequestId(Guid Value)` con `ToString()` — espejo exacto de `EmployeeId` (ADR-007). Sin factoría: el Guid llega de `IIdGenerator` (CA-DOM-009) |
| 2 | Domain | Crear | `src/BigSolutions.VacaFlow.Domain/Requests/RequestState.cs` | `public enum RequestState { Draft, Submitted, Approved, Rejected, Cancelled }` — los cinco estados de `FRD.md` §4.1, cerrados desde el día uno (ver `D5`) |
| 3 | Domain | Crear | `src/BigSolutions.VacaFlow.Domain/Requests/Errors/RequestErrors.cs` | Solo los errores que esta historia ejercita, mensajes verbatim de §3.5 y con `Field` para que la UI los pinte bajo su campo: `EndDateBeforeStartDate` (`VF-REQ-001`, `Field: "endDate"`) · `StartDateInPast` (`VF-REQ-002`, `Field: "startDate"`) · `AbsenceTypeRequired` (`VF-VAL-001`, `Field: "absenceTypeId"` — mensaje no catalogado en §3.5, ver `D7`) · `StartDateRequired` (`VF-VAL-001`, `Field: "startDate"`, "The start date is required.") · `EndDateRequired` (`VF-VAL-001`, `Field: "endDate"`, "The end date is required.") · `ReasonRequired` (`VF-VAL-001`, `Field: "reason"`, "The reason is required (1 to 500 characters).") |
| 4 | Domain | Crear | `src/BigSolutions.VacaFlow.Domain/Requests/DateRange.cs` | Value object (`ValueObject` base o record — seguir `Email`, que usa clase con `Create`): `public static Result<DateRange> Create(DateOnly start, DateOnly end)` → `EndDateBeforeStartDate` si `end < start` (igualdad válida: absencia de un día, `FR-REQ-002`). Propiedades `Start`/`End`. **`RULE-01` inquebrantable por construcción** (`SAD.md` §5.2); `RULE-02` deliberadamente fuera — depende del reloj, y un VO no lo lee |
| 5 | Domain | Modificar | `src/BigSolutions.VacaFlow.Domain/AbsenceTypes/Errors/AbsenceTypeErrors.cs` | Añadir `NotAvailable` (`VF-CAT-001`, "The selected absence type does not exist or is not available.", `Field: "absenceTypeId"`) — es un error del catálogo, vive con su agregado (ver `D6`) |
| 6 | Domain | Crear | `src/BigSolutions.VacaFlow.Domain/Requests/Request.cs` | `public sealed class Request : AggregateRoot<RequestId>`. Propiedades con setter privado: `OwnerId` (`EmployeeId`), `AbsenceTypeId`, `Period` (`DateRange`), `Reason`, `State`, `CreatedAtUtc`, `UpdatedAtUtc`, `SubmittedAtUtc?`, `ClosedAtUtc?`. Ctor privado EF (patrón `Employee`). `public static Result<Request> Create(RequestId id, EmployeeId ownerId, AbsenceTypeId absenceTypeId, DateRange period, string? reason, DateOnly today, DateTime nowUtc)`: `period.Start < today` → `StartDateInPast` (**`RULE-02`**; igualdad con hoy válida, `FR-REQ-003`); reason nulo/blanco o `Trim().Length > 500` → `ReasonRequired` (backstop de dominio de `FR-REQ-004`, patrón `Employee.Create`/full name); éxito → estado `Draft`, `Reason` trimmed, `CreatedAtUtc = UpdatedAtUtc = nowUtc`. **La fecha entra como parámetro** — el dominio jamás lee el reloj (`TE-004`, test de arquitectura). Sin `Submit`/`UpdateDetails`/`Cancel` (ver `D5`) |
| 7 | Application | Crear | `src/BigSolutions.VacaFlow.Application/Abstractions/IRequestRepository.cs` | Solo lo que esta historia necesita (`CA-INF-004`, precedente `IAbsenceTypeRepository`): `void Add(Request request);`. `GetByIdAsync`/listados llegan con `US-016`/`US-018`/`US-020` (ver `D8`) |
| 8 | Application | Modificar | `src/BigSolutions.VacaFlow.Application/Abstractions/IAbsenceTypeRepository.cs` | Añadir `Task<bool> ExistsActiveAsync(AbsenceTypeId id, CancellationToken cancellationToken);` — el handler solo necesita saber si el tipo existe y está activo, no cargar el agregado (ver `D6`) |
| 9 | Application | Crear | `src/BigSolutions.VacaFlow.Application/Requests/CreateRequestCommand.cs` | `public sealed record CreateRequestCommand(Guid? AbsenceTypeId, DateOnly? StartDate, DateOnly? EndDate, string? Reason)` con `Validate()` estructural (CA-APP-007, ADR-011, patrón `RegisterEmployeeCommand`): `AbsenceTypeId` nulo o `Guid.Empty` → `AbsenceTypeRequired`; `StartDate` nulo → `StartDateRequired`; `EndDate` nulo → `EndDateRequired`; `Reason` nulo/blanco o `Trim().Length > 500` → `ReasonRequired`. **Sin campo `employeeId`** — no puede llevarlo: `No_Contract_Or_Command_Should_Carry_An_Identity_Field` barre `*Command.cs` (`FR-AUT-010`, `AC4`) |
| 10 | Application | Crear | `src/BigSolutions.VacaFlow.Application/Requests/CreateRequestHandler.cs` | `public sealed class CreateRequestHandler(ICurrentUser currentUser, IAbsenceTypeRepository absenceTypes, IRequestRepository requests, IUnitOfWork unitOfWork, IIdGenerator idGenerator, TimeProvider timeProvider)` → `Task<Result<Guid>> Handle(CreateRequestCommand command, CancellationToken cancellationToken)`. Secuencia: (1) `command.Validate()`; (2) `DateRange.Create(...)` → `VF-REQ-001`; (3) `await absenceTypes.ExistsActiveAsync(new AbsenceTypeId(command.AbsenceTypeId!.Value), ...)` → si no, `AbsenceTypeErrors.NotAvailable` (`VF-CAT-001`); (4) `var now = timeProvider.GetUtcNow();` y `Request.Create(new RequestId(idGenerator.NewId()), currentUser.EmployeeId, ..., DateOnly.FromDateTime(now.UtcDateTime), now.UtcDateTime)` → `VF-REQ-002`/`VF-VAL-001` (ver `D4` para "today" en UTC); (5) `requests.Add(...)`; (6) `await unitOfWork.SaveChangesAsync(...)`; (7) `Result.Success(request.Id.Value)`. **El dueño sale de `ICurrentUser.EmployeeId`, único origen posible** (`FR-AUT-010`). El orden fechas→catálogo es deliberado: falla barato antes de tocar la base. Sin condicional de negocio propio (CA-APP-010): validación estructural, invariantes en el dominio, existencia del tipo es autorización de datos del caso de uso |
| 11 | Application | Modificar | `src/BigSolutions.VacaFlow.Application/DependencyInjection.cs` | `services.AddScoped<CreateRequestHandler>();` |
| 12 | Infrastructure | Crear | `src/BigSolutions.VacaFlow.Infrastructure/Persistence/Configurations/RequestConfiguration.cs` | `internal sealed`, tabla `Requests`. `Id` con converter (`RequestId`), `ValueGeneratedNever()`. `OwnerId` con converter a columna **`EmployeeId`** (`HasColumnName`, para casar con el esquema de `SAD.md` §7.2) + `HasOne<Employee>().WithMany().HasForeignKey(...)` `Restrict`. `AbsenceTypeId` ídem contra `AbsenceTypes`. `Period` como owned type (`OwnsOne`) con columnas `StartDate`/`EndDate` (`DateOnly`, requeridas). `Reason` `HasMaxLength(500)` requerido. `State` `HasConversion<int>()` (patrón `Employee.Role`). Timestamps requeridos/nullables según §2. `HasIndex` compuesto `(EmployeeId, State)` — sobre la propiedad sombra/columna del owner y `State` |
| 13 | Infrastructure | Modificar | `src/BigSolutions.VacaFlow.Infrastructure/Persistence/VacaFlowDbContext.cs` | `public DbSet<Request> Requests => Set<Request>();` |
| 14 | Infrastructure | Crear | `src/BigSolutions.VacaFlow.Infrastructure/Persistence/Repositories/RequestRepository.cs` | `internal sealed class RequestRepository(VacaFlowDbContext dbContext) : IRequestRepository` — `Add` delega en `dbContext.Requests.Add(...)` (patrón `EmployeeRepository.Add`) |
| 15 | Infrastructure | Modificar | `src/BigSolutions.VacaFlow.Infrastructure/Persistence/Repositories/AbsenceTypeRepository.cs` | Implementar `ExistsActiveAsync`: `dbContext.AbsenceTypes.AnyAsync(type => type.Id == id && type.IsActive, ...)` |
| 16 | Infrastructure | Modificar | `src/BigSolutions.VacaFlow.Infrastructure/DependencyInjection.cs` | `services.AddScoped<IRequestRepository, RequestRepository>();` |
| 17 | Infrastructure | Crear | `src/BigSolutions.VacaFlow.Infrastructure/Persistence/Migrations/…_AddRequests.cs` | `dotnet ef migrations add AddRequests` (generada, no escrita a mano — patrón `AddAbsenceTypes`). Revisar que el snapshot no arrastre cambios ajenos |
| 18 | API | Modificar | `src/BigSolutions.VacaFlow.Api/ErrorHandling/ErrorStatusMap.cs` | `["VF-REQ-001"] = 400` · `["VF-REQ-002"] = 400` · `["VF-CAT-001"] = 400` — exactamente los de `FRD.md` §6.3 para `POST /requests`; obligatorio para `Every_Domain_Error_Code_Should_Have_A_Status_Mapping` |
| 19 | API | Crear | `src/BigSolutions.VacaFlow.Api/Contracts/CreateRequestContract.cs` | `public sealed record CreateRequestContract(Guid? AbsenceTypeId, DateOnly? StartDate, DateOnly? EndDate, string? Reason)` — espejo de `FRD.md` §6.3: `{ absenceTypeId, startDate, endDate, reason }`, **sin `employeeId`** (`FR-ERR-001`: la garantía es la forma del contrato; un `employeeId` inyectado lo descarta el binding JSON, como demuestra `IdentityIgnoredTests`) |
| 20 | API | Crear | `src/BigSolutions.VacaFlow.Api/Endpoints/RequestEndpoints.cs` | `MapRequestEndpoints(this IEndpointRouteBuilder)`: `MapGroup("/api/requests")` + `MapPost("", ...)` que construye `CreateRequestCommand` desde el contrato, invoca `CreateRequestHandler` y devuelve `result.ToCreatedResult(id => $"/api/requests/{id}", id => new { id })` — `201` + `Location` con el identificador, sin cuerpo de lectura (`ADR-012`, ver `D2`). **`.RequireAuthorization()` en el `MapPost`** (test de arquitectura). Recibe, delega, mapea — cero condicionales (CA-PRE-001) |
| 21 | API | Modificar | `src/BigSolutions.VacaFlow.Api/Program.cs` | `app.MapRequestEndpoints();` junto a los mapeos existentes |
| 22 | Test | Crear | `tests/BigSolutions.VacaFlow.Domain.UnitTests/Requests/DateRangeTests.cs` | (a) `end < start` → `VF-REQ-001`; (b) `end == start` → éxito (día único, `FR-REQ-002` verbatim); (c) `end > start` → éxito; (d) igualdad estructural |
| 23 | Test | Crear | `tests/BigSolutions.VacaFlow.Domain.UnitTests/Requests/RequestTests.cs` | Fechas fijas pasadas por parámetro (determinismo `TE-004`): (a) datos válidos → `Draft`, dueño, timestamps `= nowUtc`, reason trimmed; (b) `start == today` → éxito (frontera `FR-REQ-003`); (c) `start == today - 1 día` → `VF-REQ-002`; (d) reason nulo/blanco/501 chars → `VF-VAL-001` reason; (e) reason de 500 chars → éxito (frontera) |
| 24 | Test | Crear | `tests/BigSolutions.VacaFlow.Application.UnitTests/Requests/CreateRequestHandlerTests.cs` (+ `Requests/Fakes/FakeRequestRepository.cs`, `Requests/Fakes/FixedTimeProvider.cs`; ampliar `FakeAbsenceTypeRepository` con `ExistsActiveAsync`) | Con `FakeCurrentUser`, `FakeIdGenerator`, `FakeUnitOfWork` existentes y reloj fijo (patrón `CredentialStoreTests.FixedTimeProvider`): (a) éxito → solicitud añadida, `Draft`, **`OwnerId == currentUser.EmployeeId`**, `SaveChanges` invocado, id devuelto; (b) cada campo ausente → su `VF-VAL-001` con el `Field` correcto y nada persistido; (c) `end < start` → `VF-REQ-001`; (d) `start` ayer → `VF-REQ-002`; (e) tipo inexistente o inactivo → `VF-CAT-001`; (f) `start == today` → éxito |
| 25 | Test | Crear | `tests/BigSolutions.VacaFlow.Infrastructure.IntegrationTests/Persistence/RequestRepositoryTests.cs` | Sobre `SqliteDatabaseFixture` (base real sembrada): (a) `Add` + `SaveChangesAsync` → roundtrip completo releyendo con un contexto limpio (ids tipados, `DateOnly`, `State`, timestamps); (b) FK violada (owner inexistente) → el `SaveChangesAsync` del `UnitOfWork` devuelve `Result` de fallo, no excepción (`CA-INF-005`); (c) `ExistsActiveAsync` verdadero para un tipo sembrado, falso para Guid aleatorio y falso para una fila desactivada vía SQL directo (patrón `US-014` #11c) |
| 26 | Test | Crear | `tests/BigSolutions.VacaFlow.Api.FunctionalTests/Endpoints/RequestEndpointTests.cs` | Contra `VacaFlowApiFactory` (pipeline real, cookie real): (a) **`AC1`** — registrar/iniciar sesión, `POST /api/requests` válido (tipo tomado de `GET /api/absence-types`) → `201`, `Location` `/api/requests/{id}`; (b) **`AC2`** — `endDate < startDate` → `400` `{ code: "VF-REQ-001", field: "endDate" }`; (c) **`AC3`** — `startDate` ayer → `400` `{ code: "VF-REQ-002", field: "startDate" }`; (d) **`AC5`** — sin tipo / sin fecha / sin reason → `400` `VF-VAL-001` con el `field` del campo ausente; (e) tipo Guid aleatorio → `400` `VF-CAT-001`; (f) sin sesión → `401` `VF-AUT-004`; (g) **`AC4`** — payload con `employeeId` (y `responsibleManagerId`) de otra cuenta → `201` y el dueño persistido es el usuario de la sesión (verificable vía un segundo intento de la otra cuenta cuando exista `GET`, o afirmando sobre la base de la factory; patrón `IdentityIgnoredTests`) |
| 27 | Test | Verificar | `tests/BigSolutions.VacaFlow.ArchitectureTests/` + suites completas | **Sin cambios, comprobar en verde**: sin lectura directa del reloj ni `Guid.NewGuid` en Domain/Application; `CreateRequestCommand`/`CreateRequestContract` sin tokens de identidad; `MapPost` con autorización explícita; `VF-REQ-001/002` y `VF-CAT-001` mapeados; dependencias de anillos intactas |

**Dependencias:** {1, 2, 3} → 4 → 6 · 5 → 10 · 6 → {7, 10} · {7, 8} → 10 → 11 · 6 → 12 → 13 → 17 · {7} → 14 → 16 · 8 → 15 · {10} → 19 → 20 → 21 · 18 ∥ (tras 3 y 5) · {1–6} → 22, 23 · {8–11} → 24 · {12–17} → 25 · {18–21} → 26 · todo → 27. Paralelizable: la rama Infrastructure (12–17) y la rama API (18–21) tras Application; los tests de dominio (22–23) desde el ítem 6. **Ruta crítica:** 1→4→6→10→20→21→26.

---

## 4. Casos de uso y tabla de trazabilidad

Caso de uso único de Application: **crear una solicitud en borrador** (`CreateRequestHandler`), consumido por `POST /api/requests`. Actor: cualquier usuario autenticado (`FR-REQ-001` dice "employee"; no se restringe por rol — un manager también puede pedir ausencias, ver `S2`). El dueño es siempre `ICurrentUser.EmployeeId`.

| Historia | Criterio de aceptación (verbatim) | Ítems del plan que lo cubren | Cómo se verifica |
|---|---|---|---|
| `US-015` | "Given valid data, when I `POST /requests`, then a request is created in `Draft` owned by the authenticated user, I return to `S-04` and the banner reads `Draft created.`" | #1–#17, #19–#21 (agregado, handler, persistencia, endpoint). **Parte visual diferida** ("I return to `S-04`", banner `Draft created.`): no existe `src/web/`; trazado a `US-017`+ (ver `D9`) | Dominio #23a · handler #24a (dueño = sesión) · integración #25a · funcional #26a (`201` real con cookie real) |
| `US-015` | "Given an end date before the start date, when I save, then `VF-REQ-001` appears beneath `End date`." | #3, #4 (`DateRange` — `RULE-01` por construcción), #10, #18. El "beneath `End date`" backend es `Field: "endDate"` en `{ code, message, field? }`; el pintado es de `US-017`+ | `DateRangeTests` #22a · handler #24c · funcional #26b (`400`, `code` y `field` exactos) |
| `US-015` | "Given a start date before today, when I save, then `VF-REQ-002` appears beneath `Start date`." | #3, #6 (`Request.Create`, `RULE-02` con `today` inyectado), #10 (reloj de `TimeProvider`), #18 | `RequestTests` #23b/#23c (fronteras hoy/ayer, deterministas) · handler #24d/#24f · funcional #26c |
| `US-015` | "Given a payload carrying an `employeeId`, when processed, then the owner is still the authenticated user." | #9, #19 (ni el command ni el contrato **pueden** llevar `employeeId` — forma del contrato + test de arquitectura), #10 (dueño desde `ICurrentUser`) | Arquitectura #27 (barrido de identidad) · handler #24a (`OwnerId == currentUser.EmployeeId`) · funcional #26g (inyección real ignorada, patrón `IdentityIgnoredTests`) |
| `US-015` | "Given a missing type, date or reason, when I save, then the corresponding validation message appears beneath that field." | #3 (errores `VF-VAL-001` con `Field` y mensajes verbatim de §3.5), #9 (`Validate()`), #6 (backstop de reason en dominio) | Handler #24b (campo a campo) · dominio #23d/#23e (fronteras de reason) · funcional #26d |

**Conteo: 5 criterios de entrada · 5 cubiertos** (los fragmentos de UI de `AC1`/`AC2`/`AC3`/`AC5` diferidos con destino explícito `US-017`+, decisión `D9` — mismo tratamiento aprobado en `US-014-plan` `D5`).

Deuda recogida de `US-014-plan` §7: sus criterios visuales (`AC2` parcial y `AC3` — el `<select>` y el placeholder `Select…`) siguen diferidos a la historia que construya `S-05`; esta historia no los cierra y los reanota para `US-017`.

---

## 5. Supuestos y decisiones

Sesión de planificación sin interlocutor humano (Fase 3 no interactiva): las ambigüedades se resolvieron con criterio de arquitecto y quedan documentadas con su reversibilidad.

| # | Decisión | Justificación | Impacto si es incorrecta |
|---|---|---|---|
| `D1` | **La ruta es `POST /api/requests`**, aunque el backlog y `FRD.md` §6.3 escriben `/requests` | Mismo razonamiento ya aprobado en `US-014-plan` `D1`: la convención vigente del repo prefija todo con `/api` (`/api/auth/*`, `/api/absence-types` — verificado). El criterio se lee como ruta lógica | Renombrar el `MapGroup` es una línea más tests |
| `D2` | **`201` + `Location: /api/requests/{id}` con cuerpo `{ id }`; el handler devuelve `Result<Guid>`** — no se devuelve la solicitud creada, pese al literal de `FRD.md` §6.3 | `ADR-012` y el delta registrado en `SAD.md` §18 corrigen expresamente al FRD: los commands no devuelven datos de lectura (`CA-APP-002`); create devuelve el identificador, que la regla permite. La UI refresca la lista al volver a `S-04` (`FR-UIX-005`), así que un cuerpo completo sería peso muerto. `ToCreatedResult` ya existe (lo usa `/register`) | Si el frontend necesitara el objeto completo, `US-020` (`GET /requests`) lo entrega; cambiar el cuerpo del `201` sería local al endpoint |
| `D3` | **`StartDate`/`EndDate` son `DateOnly`**, envueltas en el value object `DateRange` | `FRD.md` §3.4 ("date. Calendar date, no time component, no time zone", `AS-04`) y `SAD.md` §5.2 lo fijan; son fechas de calendario sin hora. No hay precedente `DateOnly` en el código (verificado) — este es el primero; EF Core sobre SQLite lo mapea nativo a `TEXT` ISO-8601, ordenable y comparable. `System.Text.Json` lo (de)serializa como `"yyyy-MM-dd"`, exactamente lo que emite `<input type="date">` | Ninguno previsible: cualquier alternativa (`DateTime` truncado) es estrictamente peor y violaría `AS-04` |
| `D4` | **"Today" para `RULE-02` es la fecha UTC del `TimeProvider` inyectado**: `DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime)` en el handler, pasada al dominio como parámetro | `AS-04` (sin zonas horarias en el MVP) y el patrón ilustrado en `SAD.md` §6.2. El dominio recibe `today` y `nowUtc` como argumentos y nunca lee el reloj — obligatorio por el test `Domain_And_Application_Should_Not_Read_The_Clock_Directly` (el token prohibido es `DateOnly.FromDateTime(DateTime…` inline; la forma con variable desde `TimeProvider` es la permitida) | Si el sponsor pidiera "hoy" en hora local del servidor, cambia una línea del handler; el dominio y sus tests no se tocan |
| `D5` | **El agregado nace con `RequestState` completo (5 estados) pero solo con `Create`**; `UpdateDetails`/`Submit`/`Cancel`/`Decide` llegan con sus historias | La enumeración de estados es un hecho cerrado del modelo (`FRD.md` §4.1) que las configuraciones EF y los badges necesitan estable — crearla parcial obligaría a una migración conceptual después. Los métodos, en cambio, son comportamiento con historia propia (`US-016`/`US-018`/`US-019`/`US-021`); escribirlos hoy declararía `VF-REQ-003/004/005` sin consumidor y sin tests de historia, y `US-014-plan` sentó el precedente de no adelantar trabajo sin consumidor | Cero: las historias siguientes añaden métodos al agregado existente sin tocar lo creado aquí. Las columnas `SubmittedAtUtc`/`ClosedAtUtc` ya existen (nullables) para no migrar dos veces |
| `D6` | **La validez del tipo de ausencia se comprueba en el handler vía `IAbsenceTypeRepository.ExistsActiveAsync` (operación nueva), y el error `VF-CAT-001` vive en `AbsenceTypeErrors.NotAvailable`** | `FR-CAT-003` es **Application** en el propio FRD ("A request referencing an unknown or inactive absence type is rejected") — validar contra otro agregado exige cargar datos, cosa que un aggregate no hace (`CA-DOM-007`). Un `bool` basta: el handler no necesita el agregado, solo su existencia activa — traerlo entero sería I/O sin uso. El error es del catálogo, no de la solicitud, así que vive con `AbsenceType` (su carpeta `Errors/` ya existe). Sin FK-como-regla: la FK de la tabla es red de seguridad (`CA-INF-003`), la decisión la toma el handler | Si `US-016` (edit) necesita la misma comprobación, reutiliza la operación tal cual. Si alguna historia necesitara el agregado completo, se añade `GetByIdAsync` entonces (`CA-INF-004`) |
| `D7` | **Mensaje para tipo ausente: `"The absence type is required."` (`VF-VAL-001`, `Field: "absenceTypeId"`)** — no está en el catálogo §3.5 | `FR-REQ-004` exige rechazar el tipo ausente con `VF-VAL-001` identificando el campo, pero §3.5 solo cataloga los mensajes de fechas y reason. Se acuña siguiendo exactamente el estilo de los catalogados ("The start date is required."), con el precedente documentado de `EmployeeErrors.PasswordTooLong` (mensaje no catalogado, anotado para añadir a §3.5) | Cambiar el literal es una constante; anotado como pendiente de incorporar a `Backlog.md` §3.5 |
| `D8` | **`IRequestRepository` nace solo con `Add`** | Es la única operación que este caso de uso necesita (`CA-INF-004`: el repositorio expone lo que el agregado necesita, operación a operación — precedente exacto de `IAbsenceTypeRepository` en `US-014`). El `GetByIdAsync` del `SAD.md` §6.3 llega con su primer consumidor (`US-016`/`US-018`) | Añadir operaciones después es aditivo y seguro |
| `D9` | **Toda la superficie web de los criterios se difiere a `US-017`+ y queda trazada en §4** | No existe `src/web/` (verificado). `US-017` ("Request form screen") es la historia que construye `S-05` y depende de `US-015` — el backlog ya reparte así el trabajo. El backend entrega todo lo que la UI necesitará: códigos, mensajes verbatim y `field` en el cuerpo de error para pintar "beneath that field", y el `201` para disparar el banner | Igual que `US-014` `D5`: si el equipo prefiriera cerrar la historia con la UI, queda "backend done" hasta `US-017`; el plan no cambia |
| `D10` | **Rama `feat/us-015-create-draft-request` creada desde `feat/us-014-list-absence-types`** (apilada), PR contra `main` tras el merge de PR #11 — o con base `feat/us-014-list-absence-types` si se abre antes | `US-015` depende de `US-014`, que está commiteada y pusheada pero **sin mergear** (PR #11 abierto, verificado con `git log`): el ítem #8/#15/#24/#26a consumen `IAbsenceTypeRepository` y el endpoint del catálogo, que solo existen en esa rama. Partir de `main` no compilaría | Si PR #11 se mergea antes de empezar, basta rebase sobre `main`; si PR #11 recibiera cambios, rebase de la pila — coste conocido del stacking |
| `S1` | El orden de validación del handler (estructural → rango → catálogo → dominio) devuelve **un** error por petición, el primero encontrado | Es el patrón vigente (`Validate()` de `RegisterEmployeeCommand` corta al primer fallo; `FR-ERR-002` define un cuerpo con un solo `{ code, message, field? }`). La UI pinta el error de su campo y el usuario itera | Si producto pidiera todos los errores a la vez, sería un cambio de contrato (`FR-ERR-002`) transversal, no de esta historia |
| `S2` | **Un manager también puede crear solicitudes** (sin restricción de rol en el endpoint) | `FR-REQ-001` dice "an authenticated employee" en el sentido de persona del proceso, no del enum `Role`: la maqueta muestra a la manager con vista `My Requests` (`S-09`/`09-my-requests-manager.png`) y `FR-VIS-001` le lista "sus" solicitudes. Nada en el FRD excluye al rol `Manager` de crear | Si se restringiera, es una policy en el `MapPost` — cambio local |
| `S3` | `CreatedAtUtc`/`UpdatedAtUtc` los fija `Request.Create` desde el `nowUtc` recibido; no hay interceptor de auditoría | Patrón mínimo coherente con `CredentialStore` (usa `TimeProvider` directamente); un interceptor EF sería maquinaria sin requisito (`TC-06`) | Introducir un interceptor después es transparente para el dominio |

---

## 6. Verificación end-to-end

| # | Paso | Resultado esperado |
|---|---|---|
| 1 | `dotnet build VacaFlow.slnx` | Compila con **0 warnings** (`TreatWarningsAsErrors`) |
| 2 | `dotnet test VacaFlow.slnx` | Suite completa verde, incluidos #22–#26 y los tests de arquitectura sin modificar |
| 3 | `dotnet run --project src/BigSolutions.VacaFlow.Api --urls http://localhost:5080` | Arranca; la migración `AddRequests` se aplica al inicio |
| 4 | `POST /api/requests` sin cookie | `401` `{ "code": "VF-AUT-004", … }` |
| 5 | Login `employee@vacaflow.test` / `Employee123!` · `GET /api/absence-types` para un `id` · `POST /api/requests` con `{ absenceTypeId, startDate: hoy, endDate: hoy+2, reason: "Family trip" }` | `201`, `Location: /api/requests/{id}`, cuerpo `{ id }` |
| 6 | Igual con `endDate` < `startDate` | `400` `{ "code": "VF-REQ-001", "message": "The end date cannot be earlier than the start date.", "field": "endDate" }` |
| 7 | Igual con `startDate` = ayer | `400` `{ "code": "VF-REQ-002", "message": "The start date cannot be in the past.", "field": "startDate" }` |
| 8 | Igual con `absenceTypeId` aleatorio | `400` `{ "code": "VF-CAT-001", … }` |
| 9 | Igual sin `reason` (y variantes sin tipo / sin fecha) | `400` `VF-VAL-001` con el `field` del campo ausente |
| 10 | Igual añadiendo `"employeeId": "<guid ajeno>"` al payload del paso 5 | `201`; en la base, `Requests.EmployeeId` es el id de la sesión, no el inyectado |
| 11 | Inspección de la base (`Requests`) | Fila con `State = 0` (`Draft`), `StartDate`/`EndDate` como texto ISO `yyyy-MM-dd`, `CreatedAtUtc = UpdatedAtUtc`, `SubmittedAtUtc`/`ClosedAtUtc` nulos |

---

## 7. Riesgos

| Riesgo | Mitigación |
|---|---|
| **Rama apilada**: PR #11 (`US-014`) podría recibir cambios o mergearse con squash, obligando a rebase de la pila | `D10` lo deja explícito; el preflight de `/user-story-implement` debe partir de `feat/us-014-list-absence-types` actualizada y verificar `git log` antes de crear la rama |
| Primer uso de `DateOnly` en el stack (modelo, EF/SQLite, JSON) — un desajuste de formato aparecería tarde | Cubierto en tres niveles a propósito: roundtrip EF real (#25a), serialización por el pipeline HTTP real (#26), y el paso 11 de §6 inspecciona el texto persistido |
| El owned type `Period` (`OwnsOne`) más el índice `(EmployeeId, State)` sobre columnas de converter puede requerir ajuste fino en la configuración EF | El ítem #12 lo anota; la migración generada (#17) se revisa a mano antes de commitear, y #25a falla si el mapeo no cuadra |
| `VF-REQ-002` frontera de medianoche UTC: cerca de medianoche local, "hoy" UTC puede diferir del "hoy" del usuario | Decisión `D4` documentada (`AS-04`: sin zonas horarias en el MVP); los tests fijan el reloj y no dependen de la hora de ejecución |
| El mensaje acuñado en `D7` podría contradecir un literal futuro de producto | Anotado como pendiente de incorporar a `Backlog.md` §3.5; cambiarlo es una constante |
| Los fragmentos de UI diferidos (`D9`) podrían perderse al cerrar `EP-05` | Trazados nominalmente en §4 con destino `US-017`+, junto con la deuda heredada de `US-014` (`AC2` parcial y `AC3` del select) — entrada obligatoria para la planificación de `US-017` |
| Declarar en `RequestErrors` solo los errores de esta historia obliga a `US-016`+ a ampliarlo | Deliberado (`D5`): cada código nuevo llega con su historia, su mapeo en `ErrorStatusMap` y sus tests — el test de arquitectura lo fuerza |

---

Siguiente paso — implementación:

```
/user-story-implement ".claude/implementation plans/US-015-plan.md"
```
