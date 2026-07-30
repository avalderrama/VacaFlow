# Plan de implementación — `US-010` · Retrieve the current user

| Campo | Valor |
|---|---|
| Historia | `US-010` — Retrieve the current user |
| Épica | `EP-02` — Authentication and identity |
| Prioridad · Talla | **Must** · `S` |
| Pantalla | Ninguna — endpoint de API (`GET /auth/me`); las pantallas que lo consumen son `US-013` y `US-030` |
| Depende de | `US-008` (mergeada en `main`) · `TE-011` (implementada en `feat/te-011-server-side-identity`, PR #7 abierto; este plan asume su código presente) |
| Trazas | `SC-02`, `SC-09`, `FR-AUT-009` |
| Fuentes | `Backlog.md` §EP-02 · `FRD.md` §5.1 (`FR-AUT-009`) · `SAD.md` §6.4, §8.5 |
| Repositorio | `C:\Users\avald\OneDrive\Work\VacaFlow\repositories\vacaflow` |
| Rama sugerida | `feat/us-010-current-user` (a crear desde el resultado del merge de `TE-011`) |
| Estado | Aprobado el 2026-07-29 (decisiones de arquitecto documentadas en §5; sesión sin interlocutor humano) |

> **Este documento no implementa nada.** La implementación la ejecuta `/user-story-implement`.

---

## 1. Entendimiento

### 1.1 Contexto — por qué ahora

`TE-011` entregó el puerto `ICurrentUser` (`src/BigSolutions.VacaFlow.Application/Abstractions/ICurrentUser.cs`), su implementación `CurrentUserAccessor` (`src/BigSolutions.VacaFlow.Api/Security/CurrentUserAccessor.cs`) y su registro *scoped* en `Program.cs` — pero, por alcance deliberado de aquel plan (§1.4: *"El primer consumidor es `US-010` y llega con su propia historia"*), el puerto quedó **sin consumidor**. `US-010` es ese primer consumidor real: un endpoint `GET /auth/me` que devuelve quién es el usuario de la sesión actual, y del que dependen directamente `US-013` (la pantalla de sign-in muestra nombre y rol tras autenticarse) y `US-030` (el shell de la aplicación pinta nombre, rol y navegación por rol en cada pantalla).

La cookie solo transporta dos claims (`ClaimTypes.NameIdentifier` y `ClaimTypes.Role`, ver `AuthEndpoints.BuildPrincipal`), pero `FR-AUT-009` exige devolver **cuatro** datos — identifier, full name, email, role — y prohíbe devolver el hash de contraseña. Nombre y correo no están en la cookie (decisión `D1` del plan de `TE-011`: no engordar el claim set), así que el caso de uso debe cargar el agregado `Employee` desde la base por el id que entrega `ICurrentUser`. Y ahí está el hueco que esta historia paga: `IEmployeeRepository` hoy solo expone `EmailExistsAsync`, `GetByEmailAsync` y `Add` — **no existe `GetByIdAsync`**. Añadirlo es un cambio aditivo previsto por el propio diseño del puerto (`CA-INF-004`: cada repositorio expone solo lo que su agregado necesita, y este es el primer caso de uso que necesita buscar por id).

La mitad negativa de la historia — "sin sesión, `VF-AUT-004`" — **ya está resuelta estructuralmente**: la `FallbackPolicy` global de `Program.cs` exige usuario autenticado en todo endpoint que no declare `AllowAnonymous()`, y el evento `OnRedirectToLogin` escribe `EmployeeErrors.NotAuthenticated` (`VF-AUT-004`, mapeado a `401` en `ErrorStatusMap`). El handler no tiene que hacer nada para ese criterio; el trabajo del plan es **demostrarlo** con un test funcional, no reimplementarlo.

### 1.2 Narrativa

El backlog formula `US-010` sin narrativa propia (hereda el marco de `EP-02`); su intención funcional la fija `FR-AUT-009`:

> "The system returns the authenticated user's identifier, full name, email and role. It **MUST NOT** return the password hash."

### 1.3 Criterios de aceptación — verbatim

| # | Criterio |
|---|---|
| `AC1` | "Given a signed-in user, when `GET /auth/me` is called, then it returns identifier, name, email and role — never the password hash." |
| `AC2` | "Given no session, when it is called, then it returns `VF-AUT-004`." |

### 1.4 Alcance

**Entra**

- `GetByIdAsync` en el puerto `IEmployeeRepository`, su implementación EF Core y su cobertura de integración — el único cambio de puerto de la historia, aditivo.
- El caso de uso `GetCurrentUserHandler` en `Application/Authentication/`, exactamente donde el `SAD.md` §6.4 ya lo dibuja, consumiendo `ICurrentUser` + `IEmployeeRepository` y devolviendo el `AuthenticatedUserDto` **existente** (reutilización: su propio doc-comment dice *"and by GET /auth/me when US-010 lands"*).
- El endpoint `GET /me` dentro del grupo `/api/auth` de `AuthEndpoints`, devolviendo el `AuthenticatedUserResponse` **existente** (ídem: su doc-comment ya lo reserva para esta historia) vía el helper `ToResponse` ya presente en la clase.
- `.RequireAuthorization()` explícito en el endpoint — obligatorio para el test de arquitectura `Every_Endpoint_Should_State_Its_Authorization_Explicitly`, y coherente con cómo `/logout` ya lo declara.
- Registro del handler en `Application/DependencyInjection.cs` (una línea, como anticipa su comentario).
- Tests: unitarios del handler, integración del método nuevo de repositorio, y funcionales del endpoint (ambos criterios, extremo a extremo, sobre la infraestructura `VacaFlowApiFactory` creada por `TE-011`).

**No entra**

| Excluido | Por qué |
|---|---|
| DTO o contrato de respuesta nuevos | `AuthenticatedUserDto` y `AuthenticatedUserResponse` existen, tienen exactamente los cuatro campos de `FR-AUT-009` y fueron diseñados para ser compartidos por esta historia. Crear duplicados violaría la regla de reutilización |
| Cambios en `ICurrentUser`, `CurrentUserAccessor` o `BuildPrincipal` | La decisión `D1` de `TE-011` (solo `EmployeeId`+`Role` en la cookie) se mantiene: los otros dos campos se cargan de la base, no del claim set |
| Manejo de "no session" en el handler o el endpoint | Ya lo resuelve la `FallbackPolicy` + `OnRedirectToLogin`; duplicarlo sería el `if` que la política global existe para eliminar |
| Frontend (`US-013`, `US-030`) | Historias propias; no hay superficie web todavía (paquete 4.5 del WBS), así que no hay paridad de superficies que mantener |
| Autorización por rol | Empieza con `US-021`; `/auth/me` es para cualquier usuario autenticado |

---

## 2. Cambios estructurales / de base

**No se requieren cambios de esquema de base de datos, migraciones, configuración, variables de entorno ni dependencias nuevas.**

- `GetByIdAsync` consulta la tabla `Employees` existente por su clave primaria; ningún cambio de mapeo en `EmployeeConfiguration`.
- Toda la infraestructura de tests (incluido `Api.FunctionalTests` con `VacaFlowApiFactory`) existe desde `TE-011`; esta historia solo añade clases de test a proyectos ya presentes en `VacaFlow.slnx`.
- `ErrorStatusMap` ya mapea `VF-AUT-004` → `401`; no crece.

---

## 3. Plan ordenado por dependencia

De adentro hacia afuera. **Domain no se toca** — no hay regla de negocio nueva; leer quién soy no es un invariante.

### Application

| # | Capa | Acción | Artefacto | Notas |
|---|---|---|---|---|
| 1 | Application | Modificar | `src/BigSolutions.VacaFlow.Application/Abstractions/IEmployeeRepository.cs` | Añadir `Task<Employee?> GetByIdAsync(EmployeeId id, CancellationToken cancellationToken);` — cambio aditivo, mismo estilo nullable de `GetByEmailAsync`. **Es el método que hoy falta**; ver decisión `D2` |
| 2 | Application | Crear | `src/BigSolutions.VacaFlow.Application/Authentication/GetCurrentUserHandler.cs` | Nombre y ubicación fijados por `SAD.md` §6.4. Constructor primario sobre `ICurrentUser` + `IEmployeeRepository`. **Sin record de command/query**: no hay entrada del cliente — la única entrada es la identidad, y esa viene del puerto (`FR-AUT-010`); firma `Task<Result<AuthenticatedUserDto>> Handle(CancellationToken cancellationToken)`, como el `SubmitRequestHandler` del `SAD.md` §6.5 que tampoco recibe payload de identidad. Flujo: `employees.GetByIdAsync(currentUser.EmployeeId, ct)`; si es `null` → `Result.Failure<AuthenticatedUserDto>(EmployeeErrors.NotAuthenticated)` (ver decisión `D3`); si existe → `Result.Success(new AuthenticatedUserDto(employee.Id.Value, employee.FullName, employee.Email.Value, employee.Role.ToString()))`, el mismo mapeo literal de `SignInHandler`. Solo lectura: **no** inyecta `IUnitOfWork` |
| 3 | Application | Modificar | `src/BigSolutions.VacaFlow.Application/DependencyInjection.cs` | `services.AddScoped<GetCurrentUserHandler>();` — la línea que su propio comentario ("one line each") anticipa |

### Infrastructure

| # | Capa | Acción | Artefacto | Notas |
|---|---|---|---|---|
| 4 | Infrastructure | Modificar | `src/BigSolutions.VacaFlow.Infrastructure/Persistence/Repositories/EmployeeRepository.cs` | Implementar `GetByIdAsync` con `dbContext.Employees.FirstOrDefaultAsync(employee => employee.Id == id, cancellationToken)`. `EmployeeId` es la PK mapeada con converter; comparación por PK, sin la fragilidad del converter de `Email` que los remarks de `GetByEmailAsync` documentan |

### API

| # | Capa | Acción | Artefacto | Notas |
|---|---|---|---|---|
| 5 | API | Modificar | `src/BigSolutions.VacaFlow.Api/Endpoints/AuthEndpoints.cs` | Nuevo `group.MapGet("/me", …)` → ruta efectiva `/api/auth/me`, exactamente la URI que el `Location` del `201` de `/register` ya promete (línea `result.ToCreatedResult(_ => "/api/auth/me", ToResponse)`). Recibe `GetCurrentUserHandler` y `CancellationToken`, delega y cierra con `result.ToOkResult(ToResponse)` — reutiliza el `ToResponse` privado existente, cero lógica en el endpoint (`CA-PRE-001`). Termina en `.RequireAuthorization()` con un comentario espejo del de `/logout`: la `FallbackPolicy` ya lo cubre, pero el contrato se declara en el sitio (exigido por el test de arquitectura) |

### Tests

| # | Capa | Acción | Artefacto | Notas |
|---|---|---|---|---|
| 6 | Test | Modificar | `tests/BigSolutions.VacaFlow.Application.UnitTests/Authentication/Fakes/FakeEmployeeRepository.cs` | Implementar el nuevo `GetByIdAsync` sobre la colección en memoria existente (buscar por `Id`). Necesario para que el proyecto compile tras el ítem 1 |
| 7 | Test | Crear | `tests/BigSolutions.VacaFlow.Application.UnitTests/Authentication/Fakes/FakeCurrentUser.cs` | Fake trivial de `ICurrentUser` con `EmployeeId` y `Role` asignables — el primer doble del puerto; no existía porque no había consumidor |
| 8 | Test | Crear | `tests/BigSolutions.VacaFlow.Application.UnitTests/Authentication/GetCurrentUserHandlerTests.cs` | Casos: (a) empleado existente → `Success` con los cuatro campos correctos, tomados del empleado persistido y no de los claims; (b) id sin empleado en el repositorio → `Failure` con `EmployeeErrors.NotAuthenticated`; (c) el DTO no expone ningún miembro relacionado con contraseña (afirmable por la forma del record — ver ítem 10 para la verificación de cable) |
| 9 | Test | Modificar | `tests/BigSolutions.VacaFlow.Infrastructure.IntegrationTests/Persistence/EmployeeRepositoryTests.cs` | Dos casos sobre `SqliteDatabaseFixture`: `GetByIdAsync` devuelve el empleado sembrado (campos completos) y devuelve `null` para un id inexistente |
| 10 | Test | Crear | `tests/BigSolutions.VacaFlow.Api.FunctionalTests/Endpoints/CurrentUserEndpointTests.cs` | Contra el pipeline real (`VacaFlowApiFactory`, cliente con cookies): (a) **`AC1`** — `POST /api/auth/register`, luego `GET /api/auth/me` con la cookie → `200` con `id`, `fullName`, `email`, `role` iguales a los registrados; (b) **`AC1`, mitad negativa** — el JSON crudo de la respuesta contiene **exactamente** las cuatro propiedades esperadas y ninguna otra (esto es lo que hace verificable el "never the password hash": afirmar el conjunto cerrado de claves, no la ausencia de una clave concreta); (c) **`AC2`** — `GET /api/auth/me` sin cookie → `401` con cuerpo `{ code: "VF-AUT-004", message: "You must be signed in to perform this action." }`; (d) tras `POST /api/auth/logout`, repetir el `GET` → `401` `VF-AUT-004` (enlaza con el segundo criterio de `US-009`) |
| 11 | Test | Verificar | `tests/BigSolutions.VacaFlow.ArchitectureTests/` | **Sin cambios**, comprobar que siguen verdes: `Every_Endpoint_Should_State_Its_Authorization_Explicitly` (el `/me` nuevo declara `.RequireAuthorization()`), `No_Contract_Or_Command_Should_Carry_An_Identity_Field` (esta historia no crea ningún `*Contract.cs`/`*Command.cs`/`*Query.cs` — precisamente porque la identidad viene del puerto) y las reglas de dependencia (el handler no arrastra ASP.NET) |

**Dependencias:** 1 → {2, 4, 6}; 2 → 3 → 5; {2, 6, 7} → 8; 4 → 9; {3, 5} → 10. Los ítems 4/9 y 6/7/8 son paralelizables entre sí una vez cerrado el 1. **Ruta crítica:** 1 → 2 → 3 → 5 → 10.

---

## 4. Casos de uso y tabla de trazabilidad

Un único caso de uso nuevo: **obtener el usuario actual** (`GetCurrentUserHandler`) — el primer consumidor real de `ICurrentUser`, cerrando la mitad diferida del `AC3` de `TE-011` ("Given a use case needing the acting user, when it runs, then it obtains it through the `ICurrentUser` port").

| Historia | Criterio de aceptación (verbatim) | Ítems del plan que lo cubren | Cómo se verifica |
|---|---|---|---|
| `US-010` | "Given a signed-in user, when `GET /auth/me` is called, then it returns identifier, name, email and role — never the password hash." | #1, #2, #3, #4, #5 (implementación) · #8, #9, #10a, #10b (verificación) | Unit test del handler (cuatro campos desde el agregado); test funcional #10a con sesión real; #10b afirma el conjunto cerrado de propiedades del JSON — el hash no puede viajar porque ni el DTO ni el contrato tienen dónde llevarlo (`NFR-SEC-002`) y el cuerpo se verifica clave a clave |
| `US-010` | "Given no session, when it is called, then it returns `VF-AUT-004`." | Ningún ítem de implementación — cubierto estructuralmente por la `FallbackPolicy` + `OnRedirectToLogin` de `Program.cs` (ya existentes) y por el `.RequireAuthorization()` del ítem #5 · #10c, #10d (verificación) | Test funcional: sin cookie → `401` con código `VF-AUT-004` verbatim; tras logout → ídem. Que el criterio se cumpla "gratis" es el resultado diseñado de `US-009`/`FR-AUT-011`, y el test lo convierte en garantía en lugar de coincidencia |

**Conteo: 2 criterios de entrada · 2 cubiertos.**

---

## 5. Supuestos y decisiones

Decisiones tomadas con criterio de arquitecto y documentadas aquí; la sesión de planificación no contó con interlocutor humano para la Fase 3, así que cada una lleva su reversibilidad anotada.

| # | Decisión | Justificación | Impacto si es incorrecta |
|---|---|---|---|
| `D1` | **La ruta es `/api/auth/me`** aunque el backlog escriba `GET /auth/me` | Mismo trato que `US-008` dio a `POST /auth/login` → `/api/auth/login`: el grupo `MapGroup("/api/auth")` es el precedente, y el header `Location` del `201` de `/register` ya publica `/api/auth/me` desde `US-007`. Cambiar el prefijo rompería los tres endpoints hermanos | Ninguno plausible: el consumidor web (US-013/US-030) aún no existe y consumirá lo que se publique aquí |
| `D2` | **`GetByIdAsync` se añade a `IEmployeeRepository`** en lugar de crear un puerto de lectura aparte o reutilizar `GetByEmailAsync` | `CA-INF-004`: un repositorio por agregado que expone solo lo que sus casos de uso necesitan — y este es el primer caso de uso que busca por id, de modo que el método llega exactamente cuando se justifica (el plan de `TE-011` prohibía adelantarlo). Buscar por email requeriría poner el email en la cookie, contradiciendo la `D1` de `TE-011`. Cambio aditivo: los dos consumidores existentes del puerto no se tocan; el único coste colateral es actualizar `FakeEmployeeRepository` (ítem #6) | Si un futuro caso de uso necesitara una proyección más ligera (solo nombre y rol), se añadiría entonces; devolver el agregado completo es hoy lo más simple y lo que `SignInHandler` ya hace |
| `D3` | **Sesión válida cuyo `EmployeeId` no existe en la base → `Result.Failure(EmployeeErrors.NotAuthenticated)`** (`VF-AUT-004`, `401`), no una excepción | Distinto del caso `D2` de `TE-011` (claims ausentes o malformados = error de programación → excepción): aquí los claims son válidos pero el dato de respaldo desapareció — un desajuste de ciclo de vida de datos (base recreada con cookies vivas, empleado purgado), no un bug. Fallar cerrado con "you must be signed in" es literalmente cierto — esa sesión ya no identifica a nadie — y reutiliza un error ya catalogado y mapeado, sin filtrar que el id existió. En el MVP no hay flujo de borrado de empleados, así que el caso es remoto; aun así el handler no debe poder devolver `500` por un estado alcanzable sin bug | Si producto quisiera distinguir "cuenta eliminada" con su propio código, se añadiría al catálogo FRD §7 en esa historia; el cambio sería local al handler |
| `D4` | **Un empleado inactivo con sesión viva recibe `200` con sus datos** — `GET /me` no revalida `IsActive` | `AC1` no condiciona la respuesta a la actividad y `FR-AUT-009` tampoco; `VF-AUT-003` pertenece al sign-in (`US-008`), que ya bloquea la puerta de entrada. `GET /me` es una consulta de identidad, no una decisión de negocio; la revocación de sesiones en caliente no existe en el MVP (no hay flujo que desactive empleados) y las sesiones expiran a las 8 h. Colar un chequeo de negocio aquí convertiría una query en un policía que ninguna historia pidió | Si una historia futura introduce desactivación de empleados, la revocación de sesión será su requisito explícito y se decidirá globalmente (middleware o validación de cookie), no endpoint a endpoint |
| `D5` | **Sin record `GetCurrentUserQuery`**: el handler expone `Handle(CancellationToken)` a secas | No hay ningún dato de entrada del cliente que modelar — la identidad viene de `ICurrentUser` y un record vacío sería ceremonia (driver `D4` del SAD: "no unnecessary patterns"). Además mantiene inmaculado el guardarraíl `No_Contract_Or_Command_Should_Carry_An_Identity_Field`: no existe artefacto donde un `EmployeeId` pudiera colarse | Si la historia creciera hacia "ver el perfil de otro usuario" (no está en el backlog), esa sería otra query con su propio record y su regla de autorización |
| `S1` | **`AuthenticatedUserDto` y `AuthenticatedUserResponse` se reutilizan sin cambios** | Ambos declaran en su doc-comment que `GET /auth/me` es su tercer consumidor previsto; tienen exactamente los cuatro campos de `FR-AUT-009` y ningún campo capaz de transportar un hash | Si `/me` necesitara algún día campos extra (p. ej. `managerId` para el shell), se evaluaría entonces si el contrato compartido se bifurca; hoy los tres consumidores responden la misma pregunta |

---

## 6. Verificación end-to-end

| # | Paso | Resultado esperado |
|---|---|---|
| 1 | `dotnet build VacaFlow.slnx` | Compila con **0 warnings** (`TreatWarningsAsErrors` activo) |
| 2 | `dotnet test VacaFlow.slnx` | Suite completa verde: se suman #8, #9 y #10; los tests de arquitectura de #11 pasan sin modificarse |
| 3 | `dotnet run --project src/BigSolutions.VacaFlow.Api --urls http://localhost:5080`; `POST /api/auth/register` válido y, con la cookie emitida, `GET /api/auth/me` | `200` con `{ id, fullName, email, role }` idénticos a los registrados; el cuerpo no contiene ninguna otra propiedad |
| 4 | `GET /api/auth/me` sin cookie (o tras `POST /api/auth/logout`) | `401` con `{ "code": "VF-AUT-004", "message": "You must be signed in to perform this action." }` |
| 5 | Revisar que el `Location: /api/auth/me` del `201` de `/register` ahora apunta a un endpoint real | `GET` a esa URI con la cookie de la respuesta → `200` |

---

## 7. Riesgos

| Riesgo | Mitigación |
|---|---|
| El handler devuelve datos de los claims en vez de la base (tentación: evitar el round-trip) y el nombre/email quedan desactualizados o ausentes | El diseño lo impide: los claims no llevan `FullName` ni `Email`, así que compilar exige ir al repositorio. El unit test #8a siembra un empleado cuyo nombre difiere de cualquier dato en claims y afirma que gana la base |
| `GetByIdAsync` hereda sin querer la fragilidad del converter documentada en `GetByEmailAsync` | La comparación es por PK (`EmployeeId`, converter de valor único); el test de integración #9 sobre SQLite real la cubre igual que `EmployeeRepositoryTests` cubre las búsquedas por email (`CA-TST-004`) |
| El "never the password hash" se declara cumplido por diseño y nadie lo verifica en el cable | Test funcional #10b: afirma el conjunto **cerrado** de claves del JSON, de modo que cualquier campo añadido al contrato en el futuro rompe el test y obliga a una decisión consciente |
| El endpoint olvida `.RequireAuthorization()` confiando en la `FallbackPolicy` | El test de arquitectura `Every_Endpoint_Should_State_Its_Authorization_Explicitly` falla en build — es exactamente el guardarraíl para este descuido |
| Este plan se implementa sobre `main` sin `TE-011` mergeada y `ICurrentUser` no existe | Preflight de rama de `/user-story-implement`: partir de una base que contenga `TE-011` (el merge del PR #7). Los ítems #1–#11 asumen presentes `ICurrentUser`, `CurrentUserAccessor`, su registro y `Api.FunctionalTests` |

---

Siguiente paso — implementación:

```
/user-story-implement ".claude/implementation plans/US-010-plan.md"
```
