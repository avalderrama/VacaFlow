# Plan de implementación — `TE-011` · Server-side identity derivation

| Campo | Valor |
|---|---|
| Historia | `TE-011` — Server-side identity derivation |
| Épica | `EP-02` — Authentication and identity |
| Prioridad · Talla | **Must** · `M` (WBS 4.4: 0.50 d) |
| Pantalla | Ninguna — historia técnica de servidor |
| Depende de | `US-008` (completada y mergeada en `main`) |
| Trazas | `SC-09`, `TC-08`, `OBJ-02`, `RK-02`, `AC-14`, `FR-AUT-010`, `NFR-SEC-003` |
| Fuentes | `Backlog.md` §EP-02 · `FRD.md` §5.1 · `SAD.md` §6, §8.5, §10 · `WBS.md` 4.4 |
| Repositorio | `C:\Users\avald\OneDrive\Work\VacaFlow\repositories\vacaflow` |
| Rama sugerida | `feat/te-011-server-side-identity` |
| Estado | Aprobado el 2026-07-29 (decisiones de arquitecto documentadas en §5; sesión sin interlocutor humano) |

> **Este documento no implementa nada.** La implementación la ejecuta `/user-story-implement`.

---

## 1. Entendimiento

### 1.1 Contexto — por qué ahora

`US-007`, `US-008` y `US-009` están mergeadas en `main`: existe registro, inicio y cierre de sesión con cookie `HttpOnly`, y una `FallbackPolicy` global que exige usuario autenticado en todo endpoint que no opte por `AllowAnonymous()`. La cookie ya transporta los dos claims que esta historia necesita — `AuthEndpoints.BuildPrincipal` emite `ClaimTypes.NameIdentifier` (el id del empleado) y `ClaimTypes.Role` (el rol como texto).

Lo que **no** existe todavía es el puente entre esa identidad autenticada y la capa de aplicación: el puerto `ICurrentUser` del `SAD.md` §6.3 no está declarado, y `Program.cs` lo dice en voz alta con la línea comentada:

```csharp
// builder.Services.AddScoped<ICurrentUser, CurrentUserAccessor>();   // WP 4.4
```

`TE-011` construye ese puente. El propio backlog la califica como *"The single most important technical story in the MVP"*: `RK-02` (suplantación editando un payload) y `AC-14` cuelgan de ella, y el `WBS.md` advierte que el paquete 4.4 es *"the risk-carrying package of the whole project"* — una elusión de identidad demostrada es rechazo de la entrega, no un defecto. El `FRD.md` `FR-AUT-010` lo formula como requisito: ninguna decisión de negocio acepta un identificador de empleado o manager como entrada; la identidad se deriva del contexto autenticado a través de un puerto.

Punto clave del alcance: **hoy no existe ningún caso de uso que necesite al usuario actuante.** `RegisterEmployeeHandler` y `SignInHandler` son, por definición, anónimos; los consumidores reales de `ICurrentUser` llegan con `US-010` (`GET /auth/me`), `US-015` (crear borrador) y siguientes. Esta historia entrega el puerto, su implementación, su registro y las barreras verificables — no inventa un consumidor.

### 1.2 Narrativa

> As the sponsor, I need every business decision to use the identity from the authenticated context, so that nobody can act on behalf of another person by editing a request payload.

### 1.3 Criterios de aceptación — verbatim

| # | Criterio |
|---|---|
| `AC1` | "Given the API contracts, when reviewed, then no endpoint accepts `employeeId` or `responsibleManagerId`." |
| `AC2` | "Given a payload containing such a field, when processed, then the value is ignored entirely." |
| `AC3` | "Given a use case needing the acting user, when it runs, then it obtains it through the `ICurrentUser` port." |

### 1.4 Alcance

**Entra**

- El puerto `ICurrentUser` en `Application/Abstractions`, con la forma que el `SAD.md` §6.2 ya consume (`currentUser.EmployeeId`).
- Su implementación `CurrentUserAccessor` en el proyecto **Api** — no en Infrastructure — leyendo los claims de `HttpContext`, tal como fija el `SAD.md` §6.3 y el comentario de `Program.cs`.
- El registro *scoped* en el composition root (descomentar y completar la línea de `Program.cs`), más `AddHttpContextAccessor()`.
- La barrera **estructural y verificable** de `AC1`/`AC2`: un test de arquitectura que impide que cualquier contrato o comando presente o futuro transporte un campo de identidad, y un test funcional que demuestra que un payload con `employeeId`/`responsibleManagerId` se ignora.
- El proyecto mínimo `tests/BigSolutions.VacaFlow.Api.FunctionalTests` — el hueco que el plan de `US-007` §7 ya detectó contra `SAD.md` §14.1 y `CA-TST-005`, y que esta historia es la primera en necesitar de verdad: sin pipeline HTTP real no hay forma de demostrar `AC2`.

**No entra**

| Excluido | Por qué |
|---|---|
| Cualquier consumidor de `ICurrentUser` (`GetCurrentUserHandler`, `CreateRequestHandler`, …) | Son `US-010`, `US-015` y siguientes. `TE-011` entrega el puerto; los casos de uso llegan con sus historias |
| `GET /auth/me` | Es `US-010`. Será el primer consumidor real del puerto |
| Autorización por rol (`RequireRole`, manager-only) | Empieza con `US-021`; el comentario de `Program.cs` sobre `OnRedirectToAccessDenied` lo deja anotado |
| Cambios en `AuthEndpoints`, handlers de autenticación o cookie | Ya cumplen: sus contratos no llevan identidad y sus casos de uso son anónimos por naturaleza |
| Frontend | No hay superficie web todavía (paquete 4.5 del WBS); esta historia es exclusivamente de servidor y no tiene paridad de superficies que mantener |

---

## 2. Cambios estructurales / de base

**No se requieren cambios de esquema de base de datos, migraciones, configuración, variables de entorno ni dependencias NuGet en `src/`.**

Merece decirse explícitamente:

- `IHttpContextAccessor` viene en el *shared framework* de ASP.NET Core; `AddHttpContextAccessor()` es una línea en `Program.cs`, no un paquete.
- El único artefacto estructural nuevo es el **proyecto de tests** `BigSolutions.VacaFlow.Api.FunctionalTests`, que se añade a `VacaFlow.slnx` y referencia `Microsoft.AspNetCore.Mvc.Testing` (versión alineada con el framework, fijada en el `.csproj` conforme a `NFR-POR-002`). `Program.cs` ya declara `public partial class Program;` precisamente para habilitarlo — el comentario del propio archivo menciona "the architecture and functional test projects".
- La base de datos de los tests funcionales es un archivo SQLite temporal por clase de test, el mismo patrón que `SqliteDatabaseFixture` ya usa en `Infrastructure.IntegrationTests` (`CA-TST-004`), sobrescribiendo la cadena de conexión vía `WebApplicationFactory`.

---

## 3. Plan ordenado por dependencia

De adentro hacia afuera. **Domain e Infrastructure no se tocan.**

### Application

| # | Capa | Acción | Artefacto | Notas |
|---|---|---|---|---|
| 1 | Application | Crear | `src/BigSolutions.VacaFlow.Application/Abstractions/ICurrentUser.cs` | Puerto previsto en `SAD.md` §6.3. Forma: `EmployeeId EmployeeId { get; }` y `EmployeeRole Role { get; }` — tipos del dominio, que Application ya referencia. Sin métodos, sin `Task`: leer un claim no es I/O. Ver decisión `D1` |

### API

| # | Capa | Acción | Artefacto | Notas |
|---|---|---|---|---|
| 2 | API | Crear | `src/BigSolutions.VacaFlow.Api/Security/CurrentUserAccessor.cs` | `internal sealed class`, carpeta `Security/` nueva prevista en `SAD.md` §8.1. Constructor primario sobre `IHttpContextAccessor`. Lee `ClaimTypes.NameIdentifier` → `Guid` → `EmployeeId`, y `ClaimTypes.Role` → `Enum.Parse<EmployeeRole>`. Si falta el `HttpContext`, el claim, o no parsea: `InvalidOperationException` con mensaje que nombra el claim ausente — es un error de programación (endpoint sin `RequireAuthorization` consumiendo identidad), nunca un caso de negocio. Ver decisión `D2`. `HttpContext` no cruza hacia Application: el accessor extrae y entrega valores planos (`CA-PRE-006`) |
| 3 | API | Modificar | `src/BigSolutions.VacaFlow.Api/Program.cs` | Dos líneas: `builder.Services.AddHttpContextAccessor();` y **descomentar/completar** `builder.Services.AddScoped<ICurrentUser, CurrentUserAccessor>();` (la línea 28 actual, con su comentario explicativo conservado y el sufijo `// WP 4.4` retirado por cumplido). *Scoped* conforme a `SAD.md` §10 (`CA-CFG-005`). Añadir el `using` de `BigSolutions.VacaFlow.Api.Security` |

### Tests

| # | Capa | Acción | Artefacto | Notas |
|---|---|---|---|---|
| 4 | Test | Modificar | `tests/BigSolutions.VacaFlow.ArchitectureTests/SourceRuleTests.cs` | Nuevo test `No_Contract_Or_Command_Should_Carry_An_Identity_Field` (`FR-AUT-010`, `NFR-SEC-003`, `SAD.md` §16 — "No command record carries an identity field"). Escanea los `*Contract.cs` de `Api/Contracts` y los `*Command.cs` de `Application` buscando los tokens `EmployeeId`, `ManagerId`, `ResponsibleManagerId` en líneas no comentadas, reutilizando el helper `Scan` existente. Hoy pasa (ningún contrato los lleva); su valor es **impedir la regresión** cuando lleguen `US-015`–`US-021`. Es la garantía "por la forma del contrato" del `SAD.md` §8.2 convertida en guardarraíl |
| 5 | Test | Crear | `tests/BigSolutions.VacaFlow.Api.FunctionalTests/BigSolutions.VacaFlow.Api.FunctionalTests.csproj` + `VacaFlowApiFactory.cs` | Proyecto xunit + `Microsoft.AspNetCore.Mvc.Testing`. `VacaFlowApiFactory : WebApplicationFactory<Program>` apuntando la cadena `ConnectionStrings:VacaFlow` a un archivo SQLite temporal, borrado al hacer `Dispose` — espejo del patrón de `SqliteDatabaseFixture`. Añadir el proyecto a `VacaFlow.slnx` |
| 6 | Test | Crear | `tests/BigSolutions.VacaFlow.Api.FunctionalTests/Security/CurrentUserAccessorTests.cs` | Unit tests directos del accessor con `DefaultHttpContext` y un doble de `IHttpContextAccessor` (sin pipeline): principal con ambos claims → `EmployeeId` y `Role` correctos; sin `HttpContext` → `InvalidOperationException`; sin claim `NameIdentifier` → excepción; claim no-Guid → excepción; rol no parseable → excepción. Verifica `AC3` en su mitad implementable hoy |
| 7 | Test | Crear | `tests/BigSolutions.VacaFlow.Api.FunctionalTests/Endpoints/IdentityIgnoredTests.cs` | Contra el pipeline real: `POST /api/auth/register` con un payload que **añade** `"employeeId"` y `"responsibleManagerId"` con GUIDs ajenos → `201`, y el `id` devuelto **no** es ninguno de los inyectados; el empleado persistido tiene `ManagerId` nulo. Mismo ataque contra `/api/auth/login` → la sesión emitida corresponde al dueño de las credenciales, no al `employeeId` del payload. Demuestra `AC2` de extremo a extremo |
| 8 | Test | Verificar | `tests/BigSolutions.VacaFlow.ArchitectureTests/` | **Sin cambios necesarios**, comprobar que siguen verdes: `Application_Should_Not_Depend_On_Web_Or_Data_Access` (el puerto no arrastra ASP.NET), `Every_Endpoint_Should_State_Its_Authorization_Explicitly` (no se mapea endpoint nuevo) y `No_Layer_Should_Resolve_Services_From_The_Container` |

**Dependencias:** 1 → 2 → 3; 5 → 6 y 5 → 7. El ítem 4 es independiente y paralelizable con todo. **Ruta crítica:** 1 → 2 → 3 → 7.

---

## 4. Casos de uso y tabla de trazabilidad

Esta historia no introduce casos de uso de negocio; introduce el puerto que los futuros casos de uso consumirán y las barreras que los mantienen honestos.

| Historia | Criterio de aceptación (verbatim) | Ítems del plan que lo cubren | Cómo se verifica |
|---|---|---|---|
| `TE-011` | "Given the API contracts, when reviewed, then no endpoint accepts `employeeId` or `responsibleManagerId`." | #4 (y revisión de los 3 contratos existentes: `RegisterAccountContract`, `SignInContract`, `AuthenticatedUserResponse` — ya cumplen) | Test de arquitectura #4 en verde; falla en cuanto un contrato o comando futuro declare un campo de identidad |
| `TE-011` | "Given a payload containing such a field, when processed, then the value is ignored entirely." | #5, #7 | Test funcional #7: registro y login con campos de identidad inyectados; la identidad resultante proviene de las credenciales, nunca del payload |
| `TE-011` | "Given a use case needing the acting user, when it runs, then it obtains it through the `ICurrentUser` port." | #1, #2, #3, #6 | Tests #6 del accessor (claims → valores planos); registro DI verificado por arranque de la factory en #7. La otra mitad — que cada handler futuro lo consuma — la custodian el test #4 (ningún comando puede transportar identidad, así que la única vía es el puerto) y el patrón obligatorio del `SAD.md` §6.2; se ejercita por primera vez en `US-010` |

**Conteo: 3 criterios de entrada · 3 cubiertos.**

---

## 5. Supuestos y decisiones

Decisiones tomadas con criterio de arquitecto y documentadas aquí; la sesión de planificación no contó con interlocutor humano para la Fase 3, así que cada una lleva su reversibilidad anotada.

| # | Decisión | Justificación | Impacto si es incorrecta |
|---|---|---|---|
| `D1` | **`ICurrentUser` expone `EmployeeId` y `Role`, nada más.** Ni `Email`, ni `FullName`, ni un `IsAuthenticated` | Son exactamente los dos claims que `BuildPrincipal` emite hoy. El `SAD.md` §6.2 solo consume `EmployeeId`; `Role` se incluye porque la cookie ya lo transporta y las reglas de negocio de `US-021` (manager-only, self-decision) lo necesitarán — no es especulación, está en el backlog Must. Nombre y correo no están en la cookie y añadirlos obligaría a engordar el claim set o a golpear la base en cada request | Si un consumidor futuro necesita más datos, se amplía el puerto y `BuildPrincipal` en la historia que lo necesite; cambio aditivo |
| `D2` | **El accessor lanza `InvalidOperationException` ante identidad ausente o malformada**, en lugar de devolver un nullable o un `Result` | Detrás de la `FallbackPolicy`, todo endpoint que consuma `ICurrentUser` tiene garantizado un principal autenticado; llegar al accessor sin claims válidos solo puede ser un endpoint mal declarado (`AllowAnonymous` consumiendo identidad) o una cookie corrupta — errores de programación, no casos de negocio. Un `ICurrentUser` nullable obligaría a cada handler a repetir un `if` que la política global ya resolvió, y `GlobalExceptionHandler` convierte la excepción en un `500` genérico sin filtrar internos | Si apareciera un endpoint legítimamente anónimo con identidad opcional (hoy no existe ninguno en el backlog), se añadiría en ese momento un miembro explícito para ese caso; el diseño actual falla ruidosamente, que es el comportamiento seguro para `RK-02` |
| `D3` | **`CurrentUserAccessor` vive en `Api/Security/` y es `internal sealed`** | Ubicación fijada por `SAD.md` §6.3/§8.1 y por el comentario de `Program.cs`: lee `HttpContext`, y ponerlo en Infrastructure filtraría el framework web hacia adentro. `internal` sigue la convención del proyecto Api (`AuthEndpoints` es `internal`); el composition root vive en el mismo ensamblado, así que no necesita visibilidad pública | Ninguno funcional. Si un test externo necesitara el tipo, el proyecto de tests funcionales puede recibir `InternalsVisibleTo` — no hace falta hoy porque #6 vive dentro del nuevo proyecto y accede vía DI… ver `D4` |
| `D4` | **Se crea `Api.FunctionalTests` en esta historia**, con `InternalsVisibleTo` desde el Api si los tests del accessor lo requieren | `AC2` es indemostrable sin pipeline HTTP real: el "ignorado por completo" es comportamiento del binding JSON más el contrato, y solo un `POST` de verdad lo prueba. El hueco ya estaba señalado (`US-007` plan §7, `SAD.md` §14.1, `CA-TST-005`) y esta es la primera historia que lo paga. Se mantiene mínimo: una factory y dos clases de tests | Coste de ~0.25 d sobre la talla del WBS. La alternativa — declarar `AC2` cubierto por el comportamiento por defecto de `System.Text.Json` sin test — dejaría el criterio central de `RK-02` sin red de verificación, inaceptable para la historia de la que "cuelga la aceptación de la entrega" |
| `D5` | **El test de arquitectura #4 escanea por nombre de campo (`EmployeeId`, `ManagerId`, `ResponsibleManagerId`) en contratos y comandos**, no por tipo | Los contratos usan primitivos (`Guid`, `string`), así que un escaneo por tipo no distinguiría un id de identidad de cualquier otro Guid. El escaneo por token sobre `*Contract.cs` y `*Command.cs` reutiliza el helper `Scan` existente y produce el guardarraíl que `SAD.md` §16 promete (`RK-02`: "No command record carries an identity field") | Falsos positivos posibles si una futura **query** legítima necesitara filtrar por manager — pero el `SAD.md` §6.3 ya resuelve eso con métodos de repositorio (`ListPendingForManagerAsync(EmployeeId manager, …)`) alimentados desde `ICurrentUser`, nunca desde el contrato; si surgiera un caso legítimo real, se excluye por nombre exacto con deviation anotada, como ya hace `DV-03` |
| `S1` | **`Enum.Parse<EmployeeRole>` sobre el claim `Role` es seguro** porque el único emisor del claim es `BuildPrincipal`, que serializa `user.Role` desde el DTO cuya fuente es el enum del dominio | Cookie firmada por Data Protection: un tercero no puede fabricar claims. El caso "rol desconocido" solo ocurre si el enum pierde un valor entre despliegues con cookies vivas — y entonces la excepción de `D2` es el comportamiento correcto (sesión inválida → `500` hoy; si molestara, invalidar la cookie sería una mejora de otra historia) | Escenario remoto y auto-limitado a las 8 h de vida de la cookie |

---

## 6. Verificación end-to-end

| # | Paso | Resultado esperado |
|---|---|---|
| 1 | `dotnet build VacaFlow.slnx` | Compila con **0 warnings** (`TreatWarningsAsErrors` activo), con el proyecto de tests funcionales incorporado a la solución |
| 2 | `dotnet test VacaFlow.slnx` | Toda la suite existente sigue verde; se suman los tests de #4, #6 y #7. El test #4 pasa contra los contratos actuales |
| 3 | `dotnet run --project src/BigSolutions.VacaFlow.Api --urls http://localhost:5080`, luego `POST /api/auth/register` con un payload válido **más** `"employeeId": "<guid ajeno>"` y `"responsibleManagerId": "<guid ajeno>"` | `201`; el `id` del cuerpo no coincide con ninguno de los GUIDs inyectados; `SELECT ManagerId FROM Employees` para la fila nueva devuelve `NULL` |
| 4 | Con la cookie de un usuario A, repetir `POST /api/auth/login` de un usuario B añadiendo `"employeeId"` del usuario A | La sesión resultante es la de B: los campos inyectados no alteran la identidad |
| 5 | Revisar `git grep -n "employeeId\|responsibleManagerId" src/` | Cero apariciones en contratos y comandos; las únicas menciones legítimas son `EmployeeId` como value object del dominio y sus usos internos |
| 6 | Revisar `Program.cs` | La línea `// WP 4.4` comentada ya no existe; en su lugar, el registro real de `ICurrentUser` con su comentario de ubicación conservado |

---

## 7. Enmiendas a la documentación que este plan produce

| Documento | Cambio |
|---|---|
| `SAD.md` §14.1 | El hueco "no existe `Api.FunctionalTests`" queda cerrado por esta historia; actualizar la sección para reflejar el proyecto nuevo y su alcance mínimo |
| `SAD.md` §10 / §16 | Ninguno de fondo — el registro de `ICurrentUser` y el guardarraíl `NFR-SEC-003` pasan de previstos a reales; verificar que la redacción no quede en futuro |

---

## 8. Riesgos

| Riesgo | Mitigación |
|---|---|
| **El puerto queda sin consumidor y se "adelanta" un caso de uso para justificarlo** | Prohibido por alcance (§1.4). El primer consumidor es `US-010` y llega con su propia historia. El valor de `TE-011` está en las barreras (#4, #7), no en un consumo ficticio |
| El test #4 se escribe demasiado laxo (solo `Api/Contracts`) y un comando de Application cuela un `EmployeeId` | El escaneo cubre explícitamente los `*Command.cs` de Application — es donde `SAD.md` §16 sitúa el riesgo (`ICurrentUser` bypassed by a handler taking an id parameter") |
| `WebApplicationFactory` arranca el `Program.cs` real, que aplica migraciones al inicio; dos clases de tests en paralelo sobre el mismo archivo SQLite chocarían | Un archivo temporal **por factory** con nombre único (mismo patrón por el que `SqliteDatabaseFixture` existe), borrado en `Dispose` |
| El accessor se registra pero `AddHttpContextAccessor()` se olvida; el fallo solo aparece al resolver | El arranque de la factory en #7 resuelve el grafo completo (`CA-CFG-006`: fail fast); además el test #6 no depende del contenedor |
| Deriva futura: un endpoint nuevo consume `ICurrentUser` bajo `AllowAnonymous()` y revienta en runtime | Comportamiento deseado (`D2`): explota ruidosamente en el primer request, nunca decide con identidad vacía. El test `Every_Endpoint_Should_State_Its_Authorization_Explicitly` obliga además a que el opt-out sea una decisión visible |

---

Siguiente paso — implementación:

```
/user-story-implement ".claude/implementation plans/TE-011-plan.md"
```
