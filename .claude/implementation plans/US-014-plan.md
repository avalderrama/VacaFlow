# Plan de implementación — `US-014` · List absence types

| Campo | Valor |
|---|---|
| Historia | `US-014` — List absence types |
| Épica | `EP-04` — Absence catalog |
| Prioridad · Talla | **Must** · `S` |
| Pantalla | `S-05` (selector `Absence type` del formulario de solicitud) — **sin superficie web todavía**; esta historia entrega solo el endpoint (ver §1.4) |
| Depende de | `TE-003` (mergeada — el catálogo `AbsenceTypes` ya existe y se siembra al arrancar) |
| Trazas | `SC-14` · `FRD.md` §6.2 (`GET /absence-types` → `200 [{ id, code, name }]` · `VF-AUT-004` `401`) · `SAD.md` §6.1, §6.3, §8.1 · `WBS.md` paquete 5.1 |
| Fuentes | `Backlog.md` §EP-04 · `FRD.md` §3.3, §6.2 · `SAD.md` §5–§8 |
| Repositorio | `C:\Users\avald\OneDrive\Work\VacaFlow\repositories\vacaflow` |
| Rama sugerida | `feat/us-014-list-absence-types`, creada desde `main` |
| Estado | Aprobado el 2026-07-30 (decisiones de arquitecto documentadas en §5; sesión sin interlocutor humano) |

> **Este documento no implementa nada.** La implementación la ejecuta `/user-story-implement`.

---

## 1. Entendimiento

### 1.1 Contexto — por qué ahora

`TE-003` dejó el catálogo listo pero mudo: el agregado `AbsenceType` existe completo en `src/BigSolutions.VacaFlow.Domain/AbsenceTypes/` (`AbsenceType.cs`, `AbsenceTypeCode.cs`, `AbsenceTypeId.cs`, `Errors/AbsenceTypeErrors.cs`), su configuración EF (`AbsenceTypeConfiguration.cs`, tabla `AbsenceTypes` con `UNIQUE(Code)`), la migración `20260730185524_AddAbsenceTypes` y el `DatabaseSeeder` siembran los tres tipos de `Backlog.md` §3.6 con `IsActive = true` — **verificado en el código, no asumido**. Lo que **no** existe es ninguna vía de lectura: no hay `IAbsenceTypeRepository`, ni handler, ni endpoint. El plan de `TE-003` lo excluyó a propósito (*"Son `US-014`… crearlo hoy sería adelantar trabajo sin consumidor"*). El consumidor llega ahora: `US-015` (crear solicitud) depende de `US-014`, y la pantalla `S-05` poblará su selector desde este endpoint.

Hallazgos de grounding adicionales, verificados:

- **No hay mediator** (`ADR-002`): los handlers son clases planas registradas en `Application/DependencyInjection.cs` e inyectadas directas en el endpoint (`RegisterEmployeeHandler`, `SignInHandler` son el patrón).
- **`FallbackPolicy` global** (`Program.cs`, `TE-011`): todo endpoint exige sesión salvo `.AllowAnonymous()`. Además, el test de arquitectura `Every_Endpoint_Should_State_Its_Authorization_Explicitly` (`tests/BigSolutions.VacaFlow.ArchitectureTests/SourceRuleTests.cs`) obliga a que **cada** `Map{Verb}("...")` declare `.AllowAnonymous()` o `.RequireAuthorization()` en el sitio.
- **`VF-AUT-004` ya está resuelto de extremo a extremo**: `EmployeeErrors.NotAuthenticated` (Domain) + `ErrorStatusMap["VF-AUT-004"] = 401` (Api) + el evento `OnRedirectToLogin` del cookie auth escribe ese cuerpo. El criterio "no session → `VF-AUT-004`" **no requiere código nuevo**, solo no romperlo.
- **`tests/BigSolutions.VacaFlow.Api.FunctionalTests` con `VacaFlowApiFactory` existe** (de `TE-011`): arranca el pipeline real sobre SQLite temporal, que además queda **sembrado** por el initializer real — un test funcional puede afirmar los tres tipos de §3.6 sin preparar datos.
- Nota de estado: `GET /api/auth/me` (`US-010`) **no está implementado** aún pese a comentarios que lo anticipan (`AuthEndpoints.cs`, `AuthenticatedUserDto.cs`). No afecta a esta historia — `US-014` depende solo de `TE-003` — pero el plan no debe asumir su existencia.

### 1.2 Narrativa

El backlog formula `US-014` directamente por criterios (sin "Como… quiero…"); su intención la fija `EP-04`: exponer el catálogo sembrado para que el formulario de solicitud (`S-05`) nunca tenga tipos de ausencia codificados en duro, y `FRD.md` §6.2 fija el contrato: `GET /absence-types` → `200` con `[{ id, code, name }]`.

### 1.3 Criterios de aceptación — verbatim (`Backlog.md` §EP-04 · `US-014`)

| # | Criterio |
|---|---|
| `AC1` | "Given a signed-in user, when `GET /absence-types` is called, then the active types are returned with identifier, code and display name." |
| `AC2` | "Given the request form, when it loads, then the `Absence type` select is populated from this endpoint and never hardcoded." |
| `AC3` | "Given the select, when it renders, then the first option is the disabled-value placeholder `Select…`." |
| `AC4` | "Given no session, when the endpoint is called, then it returns `VF-AUT-004`." |

### 1.4 Alcance

**Entra**

- El puerto de lectura `IAbsenceTypeRepository` en `Application/Abstractions` (una operación: listar activos), su implementación `internal sealed` en Infrastructure, el caso de uso `ListAbsenceTypesHandler` con su DTO, el contrato de respuesta del Api y el endpoint `GET /api/absence-types` con `.RequireAuthorization()` explícito.
- Registros de DI: el handler en `Application/DependencyInjection.cs`, el repositorio en `Infrastructure/DependencyInjection.cs`.
- Tests: unitario del handler, integración del repositorio sobre SQLite real (filtro de activos y orden), y funcionales contra el pipeline HTTP real (`200` con los tres tipos sembrados; `401` con cuerpo `VF-AUT-004` sin sesión).

**No entra**

| Excluido | Por qué |
|---|---|
| `AC2` y `AC3` en su superficie visual (el `<select>` de `S-05`) | **No existe todavía ninguna aplicación web en el repositorio** (verificado: no hay `web/` ni `src/web/`). El formulario de solicitud llega con `US-015`/`US-017` (pantalla `S-05`, `EP-05`), que ya dependen de `US-014`. Esta historia entrega la parte de `AC2` que le corresponde al backend: el endpoint que hace posible "populated from this endpoint and never hardcoded". Ver decisión `D5` y la tabla de trazabilidad §4 |
| Comportamiento de escritura sobre el catálogo (crear/activar/desactivar tipos) | *"Seeded catalog, read-only at runtime"* (`SAD.md` §5.1); ninguna historia del MVP lo pide |
| Filtros, paginación o parámetros de consulta en el endpoint | El catálogo tiene tres filas fijas; `FRD.md` §6.2 no define parámetros |
| Códigos de error nuevos en Domain | `VF-AUT-004` ya existe y ya está mapeado; no hay ninguna otra ruta de fallo (ver `D3`) |
| Caché de la respuesta | Tres filas en SQLite local; optimizar sería el over-engineering que `TC-06` prohíbe |

---

## 2. Cambios estructurales / de base

**No se requieren cambios de esquema, migraciones, configuración, variables de entorno, permisos, feature flags ni dependencias nuevas.** La tabla `AbsenceTypes` y su seed llegaron con `TE-003`; esta historia es lectura pura sobre lo que ya existe. `ErrorStatusMap` tampoco cambia: no se introduce ningún código de error nuevo.

---

## 3. Plan ordenado por dependencia

De adentro hacia afuera. **Domain no se toca**: el agregado ya tiene todo lo que la lectura necesita (`Code`, `Name`, `IsActive`).

| # | Capa | Acción | Artefacto | Notas |
|---|---|---|---|---|
| 1 | Application | Crear | `src/BigSolutions.VacaFlow.Application/Abstractions/IAbsenceTypeRepository.cs` | Un repositorio por agregado raíz, solo lo que este agregado necesita (`CA-INF-004`, patrón `IEmployeeRepository`): `Task<IReadOnlyList<AbsenceType>> ListActiveAsync(CancellationToken cancellationToken)`. Sin `IQueryable`, sin `IRepository<T>` (`CA-APP-005`). El filtro "activos" es parte del contrato del puerto — ver `D2` |
| 2 | Application | Crear | `src/BigSolutions.VacaFlow.Application/AbsenceTypes/AbsenceTypeDto.cs` | `public sealed record AbsenceTypeDto(Guid Id, string Code, string Name)` — espejo del patrón `AuthenticatedUserDto`: el handler nunca devuelve la entidad de dominio hacia el Api (`CA-APP-006`). Nombre `*Dto`, no `*Response`/`*Query`, para no entrar en el barrido de `No_Contract_Or_Command_Should_Carry_An_Identity_Field` (no lleva ningún campo prohibido de todos modos) |
| 3 | Application | Crear | `src/BigSolutions.VacaFlow.Application/AbsenceTypes/ListAbsenceTypesHandler.cs` | Clase plana `sealed`, ctor primario con `IAbsenceTypeRepository` (patrón `SignInHandler`, `SAD.md` §6.2). `Task<IReadOnlyList<AbsenceTypeDto>> Handle(CancellationToken)` — **sin** `Result<T>` ni record de query: no hay parámetros ni ruta de fallo (ver `D3`, `D4`). Mapea entidad → DTO campo a campo (`Id.Value`, `Code.Value`, `Name`) preservando el orden que entrega el repositorio |
| 4 | Application | Modificar | `src/BigSolutions.VacaFlow.Application/DependencyInjection.cs` | `services.AddScoped<ListAbsenceTypesHandler>();` — la línea que el comentario *"Further handlers are registered here…"* anuncia |
| 5 | Infrastructure | Crear | `src/BigSolutions.VacaFlow.Infrastructure/Persistence/Repositories/AbsenceTypeRepository.cs` | `internal sealed` (`CA-DEP-007`), ctor primario con `VacaFlowDbContext` (patrón `EmployeeRepository`). `ListActiveAsync`: `dbContext.AbsenceTypes.AsNoTracking().Where(type => type.IsActive).OrderBy(type => type.Name).ToListAsync(...)`. `AsNoTracking` porque es lectura pura; orden por `Name` para una respuesta determinista (ver `D6`) |
| 6 | Infrastructure | Modificar | `src/BigSolutions.VacaFlow.Infrastructure/DependencyInjection.cs` | `services.AddScoped<IAbsenceTypeRepository, AbsenceTypeRepository>();` junto a los repositorios existentes |
| 7 | API | Crear | `src/BigSolutions.VacaFlow.Api/Contracts/AbsenceTypeResponse.cs` | `public sealed record AbsenceTypeResponse(Guid Id, string Code, string Name)` — contrato propiedad del Api (`CA-PRE-003`), mapeado campo a campo desde `AbsenceTypeDto` (patrón `AuthenticatedUserResponse`). Serializa como `{ id, code, name }`, exactamente `FRD.md` §6.2 |
| 8 | API | Crear | `src/BigSolutions.VacaFlow.Api/Endpoints/AbsenceTypeEndpoints.cs` | Nombre y ubicación fijados por `SAD.md` §8.1. `MapAbsenceTypeEndpoints(this IEndpointRouteBuilder)`: `MapGroup("/api/absence-types")` + `MapGet("", ...)` que inyecta `ListAbsenceTypesHandler`, mapea DTO → `AbsenceTypeResponse` y devuelve `Results.Ok(...)` — recibe, delega, mapea, sin condicional de negocio (`CA-PRE-001`). **`.RequireAuthorization()` explícito en el `MapGet`** — obligatorio para `Every_Endpoint_Should_State_Its_Authorization_Explicitly` además de la `FallbackPolicy` (ver `D1` para el prefijo `/api`) |
| 9 | API | Modificar | `src/BigSolutions.VacaFlow.Api/Program.cs` | `app.MapAbsenceTypeEndpoints();` junto a `app.MapAuthEndpoints();`. Nada más cambia en la composición |
| 10 | Test | Crear | `tests/BigSolutions.VacaFlow.Application.UnitTests/AbsenceTypes/ListAbsenceTypesHandlerTests.cs` (+ `AbsenceTypes/Fakes/FakeAbsenceTypeRepository.cs`) | Fake in-memory del puerto (patrón `FakeEmployeeRepository`). Casos: (a) mapea cada entidad a su DTO con `Id`/`Code`/`Name` correctos; (b) preserva el orden del repositorio; (c) catálogo vacío → lista vacía, no null |
| 11 | Test | Crear | `tests/BigSolutions.VacaFlow.Infrastructure.IntegrationTests/Persistence/AbsenceTypeRepositoryTests.cs` | Sobre `SqliteDatabaseFixture` (base real, **ya sembrada** por el initializer): (a) `ListActiveAsync` devuelve exactamente los 3 tipos de §3.6 con sus pares código/nombre; (b) orden alfabético por `Name` (`Personal Leave`, `Sick Leave`, `Vacation`); (c) **filtro de activos**: desactivar una fila vía SQL directo (`UPDATE AbsenceTypes SET IsActive = 0 …` con `ExecuteSqlAsync` — el agregado no expone `Deactivate` a propósito, ver `D7`) → esa fila deja de aparecer |
| 12 | Test | Crear | `tests/BigSolutions.VacaFlow.Api.FunctionalTests/Endpoints/AbsenceTypeEndpointTests.cs` | Contra `VacaFlowApiFactory` (pipeline HTTP real, base sembrada): (a) **`AC4`** — `GET /api/absence-types` sin sesión → `401` y cuerpo `{ code: "VF-AUT-004", … }`; (b) **`AC1`** — registrar o iniciar sesión (el `HttpClient` de la factory conserva la cookie) y llamar de nuevo → `200` con 3 elementos `{ id, code, name }`, códigos `VACATION`/`PERSONAL_LEAVE`/`SICK_LEAVE`, nombres de §3.6, `id` Guid no vacío |
| 13 | Test | Verificar | `tests/BigSolutions.VacaFlow.ArchitectureTests/` + suites existentes | **Sin cambios, comprobar en verde**: el `MapGet` nuevo declara autorización explícita; ni `AbsenceTypeDto` ni `AbsenceTypeResponse` disparan el barrido de identidad; no hay código `VF-*` nuevo que exigir en `ErrorStatusMap`; Domain sigue sin dependencias hacia afuera |

**Dependencias:** 1 → {3, 5} · 2 → 3 · 3 → 4 · {1, 5} → 6 · {3, 7} → 8 → 9 · {1, 2, 3} → 10 · {5, 6} → 11 · {8, 9} → 12 · todo → 13. Paralelizable: {1, 2, 7} entre sí; la rama Application (3–4) y la rama Infrastructure (5–6) tras el ítem 1. **Ruta crítica:** 1 → 3 → 8 → 9 → 12.

---

## 4. Casos de uso y tabla de trazabilidad

Caso de uso único de Application: **listar los tipos de ausencia activos** (`ListAbsenceTypesHandler`), consumido por `GET /api/absence-types`. Sin comando ni parámetros; autorización = "cualquier usuario autenticado" (empleado o manager por igual — el catálogo es el mismo para ambos).

| Historia | Criterio de aceptación (verbatim) | Ítems del plan que lo cubren | Cómo se verifica |
|---|---|---|---|
| `US-014` | "Given a signed-in user, when `GET /absence-types` is called, then the active types are returned with identifier, code and display name." | #1–#9 (puerto, handler, repositorio, contrato, endpoint) | Unitario #10 (mapeo id/código/nombre) · integración #11a/#11c (solo activos, datos de §3.6) · funcional #12b (`200` real con sesión real) |
| `US-014` | "Given the request form, when it loads, then the `Absence type` select is populated from this endpoint and never hardcoded." | #7, #8 (el contrato `{ id, code, name }` y el endpoint que el formulario consumirá) — **parte visual diferida**: no existe superficie web en el repo; el `<select>` llega con `US-015`/`US-017` (`S-05`), que dependen de `US-014` (ver `D5`) | Funcional #12b demuestra que el contrato entrega todo lo que el select necesita (`id` para el `value`, `name` para la etiqueta); la verificación del select en sí pertenece a la historia que construya `S-05` |
| `US-014` | "Given the select, when it renders, then the first option is the disabled-value placeholder `Select…`." | **Diferido íntegro a la historia de `S-05`** (`US-015`/`US-017`) — es un criterio 100 % de presentación sobre una superficie que no existe todavía (ver `D5`) | En esta historia: nada que verificar en backend. Trazado aquí para que la historia de `S-05` lo recoja explícitamente |
| `US-014` | "Given no session, when the endpoint is called, then it returns `VF-AUT-004`." | #8 (`.RequireAuthorization()`) + infraestructura ya existente de `TE-011` (`FallbackPolicy` + `OnRedirectToLogin` → `WriteErrorAsync(EmployeeErrors.NotAuthenticated)` + `ErrorStatusMap["VF-AUT-004"] = 401`) — **cero código nuevo** | Funcional #12a: `401` con `code == "VF-AUT-004"` en el cuerpo, contra el pipeline real |

**Conteo: 4 criterios de entrada · 4 cubiertos** (2 completos en backend, `AC2` parcial-por-diseño y `AC3` diferido, ambos con destino explícito — decisión `D5`).

---

## 5. Supuestos y decisiones

Sesión de planificación sin interlocutor humano (Fase 3 no interactiva): las ambigüedades se resolvieron con criterio de arquitecto y quedan documentadas con su reversibilidad.

| # | Decisión | Justificación | Impacto si es incorrecta |
|---|---|---|---|
| `D1` | **La ruta es `GET /api/absence-types`**, aunque `FRD.md` §6.2 y el backlog escriben `/absence-types` | El FRD también escribe `/auth/register` y el código real lo montó como `/api/auth/register` (verificado en `AuthEndpoints.cs`): la convención vigente del repo prefija todo con `/api`, y romperla para una historia dejaría dos familias de rutas incoherentes. El criterio se lee como ruta lógica, no literal | Renombrar el `MapGroup` es un cambio de una línea más el ajuste de los tests funcionales |
| `D2` | **El filtro `IsActive` vive en el puerto** (`ListActiveAsync`), no en el handler | `AC1` pide "the active types": es el contrato de la consulta, no una regla que el handler decida — un handler sin condicionales de negocio (`CA-APP-010`) no debe filtrar colecciones que el puerto puede entregar ya correctas; además evita traer filas muertas de la base para descartarlas en memoria | Si otra historia necesitara también los inactivos, se añade otra operación al puerto (`CA-INF-004`: el repositorio expone lo que el agregado necesita, operación a operación) |
| `D3` | **El handler devuelve `IReadOnlyList<AbsenceTypeDto>` directamente, sin `Result<T>`** | La consulta no tiene ninguna ruta de fallo de negocio: sin parámetros que validar, sin autorización fina (la gruesa la da el endpoint), sin estados ilegales — envolver en `Result` fabricaría una rama de error muerta que ningún test podría ejercitar. `Result` es para decisiones que pueden fallar (`SignInHandler`); aquí no hay decisión. El endpoint responde `Results.Ok(...)` directo, como `/health` responde el suyo | Si una evolución introdujera un fallo posible, el cambio de firma a `Result<...>` es local al handler y al endpoint; `ResultExtensions.ToOkResult` ya existe para ese día |
| `D4` | **No hay record `ListAbsenceTypesQuery`** | Cero parámetros: un record vacío sería ceremonia sin información (el patrón del repo ya pasa argumentos sueltos cuando son pocos — `SAD.md` §6.2 muestra `Handle(RequestId id, ...)`). Beneficio lateral: un archivo `*Query.cs` menos en el barrido del test de identidad, aunque no habría llevado campos prohibidos | Si `US-015`+ necesitara parámetros de consulta aquí (nada lo anuncia), se introduce entonces |
| `D5` | **`AC2` (parcial) y `AC3` se difieren a la historia que construya `S-05`** (`US-015`/`US-017`), dejándolo trazado explícitamente en §4 | No existe aplicación web en el repositorio (verificado). El backlog asigna `S-05` a `EP-05`, cuyas historias dependen de `US-014`: estos dos criterios describen el *consumo* del endpoint, escritos en `US-014` para fijar el contrato desde el lado del productor. Inventar aquí un esqueleto de frontend para "cumplirlos" sería adelantar `EP-05` sin historia. La paridad de superficies (Fase 1.3) queda satisfecha declarándolo: hoy la única superficie exigible es la API | Si el equipo prefiriera esperar al frontend para cerrar `US-014`, la historia queda "backend done" y se cierra junto a `US-017`; el plan no cambia |
| `D6` | **Orden estable por `Name` ascendente** (`Personal Leave`, `Sick Leave`, `Vacation`) | Ningún documento fija un orden; un `SELECT` sin `ORDER BY` es no determinista y haría frágiles los tests y la UI. Orden alfabético por el nombre visible es lo que un selector espera por defecto | Si producto pidiera un orden curado (p. ej. `Vacation` primero), es un `OrderBy` distinto o una columna `SortOrder` — historia nueva, cambio local |
| `D7` | **El test de integración #11c desactiva una fila vía SQL directo**, no añadiendo `Deactivate()` al agregado | `SAD.md` §5.1: el catálogo es *"read-only at runtime"* — añadir comportamiento de dominio solo para un test violaría el diseño deliberado de `TE-003`. El test necesita una fila inactiva en la base, no un método de negocio; manipular la base con SQL es exactamente lo que un test de integración de Infrastructure tiene permitido | Si una historia futura introduce la desactivación real, el test migra al método de dominio y el SQL desaparece |
| `S1` | El endpoint es accesible por **cualquier** usuario autenticado, sin distinción de rol | `AC1` dice "a signed-in user"; `FRD.md` §6.2 no restringe por actor (a diferencia de §6.3, que sí distingue Employee/Manager); el catálogo es el mismo para todos | Si se restringiera por rol (nada lo sugiere), sería un `RequireAuthorization` con policy — cambio local al endpoint |

---

## 6. Verificación end-to-end

| # | Paso | Resultado esperado |
|---|---|---|
| 1 | `dotnet build VacaFlow.slnx` | Compila con **0 warnings** (`TreatWarningsAsErrors`) |
| 2 | `dotnet test VacaFlow.slnx` | Suite completa verde, incluidos #10–#12 y los tests de arquitectura sin modificar |
| 3 | `dotnet run --project src/BigSolutions.VacaFlow.Api --urls http://localhost:5080` | Arranca; base creada/sembrada por el initializer existente |
| 4 | `GET http://localhost:5080/api/absence-types` sin cookie | `401` con `{ "code": "VF-AUT-004", … }` |
| 5 | `POST /api/auth/login` con `employee@vacaflow.test` / `Employee123!`; repetir el paso 4 con la cookie | `200` con exactamente 3 elementos `{ id, code, name }`: `PERSONAL_LEAVE`/`Personal Leave`, `SICK_LEAVE`/`Sick Leave`, `VACATION`/`Vacation` (orden por nombre) |
| 6 | Repetir el paso 5 con `manager@vacaflow.test` / `Manager123!` | Misma respuesta — el catálogo no distingue rol (`S1`) |

---

## 7. Riesgos

| Riesgo | Mitigación |
|---|---|
| Divergencia ruta FRD (`/absence-types`) vs implementada (`/api/absence-types`) confunde a la historia de frontend | Decisión `D1` documentada aquí y verificable en el código; el cliente web (`lib/api`) se escribirá contra el código real, igual que hizo con `/api/auth/*` |
| El barrido regex de `Every_Endpoint_Should_State_Its_Authorization_Explicitly` corta el "bloque" en el siguiente `Map{Verb}` — un endpoint nuevo mal ordenado podría acreditarse la autorización del vecino | Un solo `MapGet` en `AbsenceTypeEndpoints.cs` con su `.RequireAuthorization()` inmediato; el ítem #13 pasa la suite para confirmarlo |
| `AC2`/`AC3` diferidos podrían perderse al cerrar `EP-05` | Trazados nominalmente en §4 con destino (`US-015`/`US-017`); el plan de esa historia debe recogerlos — anotado como entrada obligatoria para su planificación |
| El mapeo `Code`/`Name` del test funcional se acopla a los literales del seed | Es deliberado: §3.6 los fija como contrato de datos del MVP (mismo acoplamiento que `SeededAccountsTests` acepta para las cuentas) |
| `OrderBy(Name)` en SQLite ordena por collation binaria (BINARY por defecto) | Irrelevante con los tres nombres actuales (ASCII, iniciales distintas); si el catálogo creciera con acentos, se decidiría collation entonces |

---

Siguiente paso — implementación:

```
/user-story-implement ".claude/implementation plans/US-014-plan.md"
```
