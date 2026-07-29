# Plan de implementación — `US-007` · Create an account

| Campo | Valor |
|---|---|
| Historia | `US-007` — Create an account |
| Épica | `EP-02` — Authentication and identity |
| Prioridad · Talla | **Must** · `M` (más el corte de habilitadores, ver §1.4) |
| Pantalla | `S-02` — Create account |
| Fuentes | `Backlog.md` v2.0 · `FRD.md` · `NFR.md` · `SAD.md` v2.0 · `Intent.md` |
| Repositorio | `C:\Users\avald\OneDrive\Work\VacaFlow\repositories\vacaflow` |
| Rama sugerida | `feat/us-007-create-account` |
| Estado | Aprobado el 2026-07-28 |

> **Este documento no implementa nada.** La implementación la ejecuta `/user-story-implement`.

---

## 1. Entendimiento

### 1.1 Contexto — por qué ahora

`TE-001` está completo: la solución compila, los cuatro proyectos de `src/` tienen sus referencias apuntando hacia adentro, el API responde `/health` y los 14 tests de arquitectura pasan. No existe todavía ninguna entidad de negocio, ninguna tabla y ningún caso de uso.

`US-007` es la primera historia con valor de usuario del MVP y la puerta de entrada de todo lo demás: sin cuentas no hay identidad, y sin identidad no hay dueño de solicitud ni manager responsable. Es también la primera vez que el esqueleto se somete a carga real — la primera entidad, el primer value object, el primer puerto, el primer repositorio, la primera migración y el primer endpoint con autenticación.

El resultado esperado: un usuario nuevo se registra, queda con sesión abierta, y su contraseña queda almacenada como hash irreversible. Eso cubre `AC-01` del `Intent.md` en su parte de servidor.

### 1.2 Narrativa

> Como usuario nuevo, quiero registrarme con mi nombre, correo, contraseña y rol, para poder acceder a VacaFlow con mi propia cuenta.

### 1.3 Criterios de aceptación — verbatim

**Comportamiento**

| # | Criterio |
|---|---|
| `AC1` | "Given valid data, when I `POST /auth/register`, then an `Employee` and a `UserAccount` are created and I am signed in directly, landing on `S-04` with the banner `Account created. Welcome to VacaFlow!`" |
| `AC2` | "Given an already-registered email, when I register, then `VF-AUT-001` is returned beneath the email field and no second account is created. Comparison is case-insensitive." |
| `AC3` | "Given any registration, when the database is inspected, then the password is stored hashed." |
| `AC4` | "Given a name over 120 characters, a malformed email or a password under 8 characters, when I submit, then the corresponding validation message from §3.5 appears beneath that field." |

**Visual — `S-02`**

| # | Criterio |
|---|---|
| `AC5` | "Auth card 420px per §3.3, subtitle `Create an account`." |
| `AC6` | "Four groups in order: `Full name` (maxlength 120), `Email` (`type=email`), `Password` (`type=password`, helper `Minimum 8 characters.`), and a `fieldset` with legend `Role (for demo purposes)`." |
| `AC7` | "The role control is two radio options side by side, each in a bordered 8px-radius box, equal width, labels `Employee` and `Manager`. `Employee` is preselected." |
| `AC8` | "Primary full-width button `Create account`, disabled while saving." |
| `AC9` | "Below the form: `Already have an account? Sign in`." |

### 1.4 Alcance

**Entra**

- El caso de uso completo de registro, de dominio a endpoint.
- El corte mínimo de los habilitadores que `US-007` necesita para existir:
  - de `TE-002`: `DbContext`, configuraciones de `Employee` y `UserAccount`, migración inicial de esas **dos** tablas;
  - de `TE-004`: `TimeProvider` ya está registrado en `Program.cs`; se consume aquí;
  - de `TE-005`: el mapeo `Result` → HTTP y el manejador global de excepciones, poblado con los códigos `VF-AUT-*` y `VF-VAL-001`.
- El esquema de autenticación por cookie y la emisión de la cookie tras un registro exitoso — lo exige `AC1` ("signed in directly").
- Tests de dominio, de aplicación con dobles, y de integración contra un archivo SQLite temporal.

**No entra**

| Excluido | Por qué |
|---|---|
| `AC5`–`AC9` (pantalla `S-02`) | `src/web` no existe. El scaffolding de Next.js es el paquete `4.5` del WBS y quedó aplazado hasta decidir la exclusión de OneDrive. Estos criterios se trazan a `US-012` — ver §4 |
| Validación de credenciales al iniciar sesión | Es `US-008`. Aquí solo se emite la cookie; `IPasswordHasher.Verify` se implementa pero su consumidor llega con `US-008` |
| `GET /auth/me` | Es `US-010` |
| Seed de los tres empleados y del catálogo de tipos de ausencia | Es `TE-003`, y `US-007` no lo necesita |
| Mapeo de `Request`, `Approval` y `AbsenceType` | Sus entidades no existen; llegan con `US-014` y `US-015`. La migración inicial cubre solo lo que existe |
| Asignación de `ManagerId` | `OQ-01` sigue abierto. Ver §5 |

---

## 2. Cambios estructurales / de base

### 2.1 Esquema de base de datos

Migración inicial `AddEmployeesAndUserAccounts`, en `src/BigSolutions.VacaFlow.Infrastructure/Persistence/Migrations/`.

| Tabla | Columnas | Restricciones |
|---|---|---|
| `Employees` | `Id` (TEXT, PK) · `FullName` (TEXT, 120, NOT NULL) · `Email` (TEXT, 200, NOT NULL) · `Role` (INTEGER, NOT NULL) · `IsActive` (INTEGER, NOT NULL) · `ManagerId` (TEXT, NULL) | `UNIQUE(Email)` · `FK ManagerId → Employees(Id)` |
| `UserAccounts` | `Id` (TEXT, PK) · `EmployeeId` (TEXT, NOT NULL) · `PasswordHash` (TEXT, NOT NULL) · `CreatedAtUtc` (TEXT, NOT NULL) | `UNIQUE(EmployeeId)` · `FK EmployeeId → Employees(Id)` ON DELETE CASCADE |

Las tres tablas restantes del `SAD.md` §7.2 — `AbsenceTypes`, `Requests`, `Approvals` — llegan en migraciones posteriores, cuando sus entidades existan.

`UNIQUE(Email)` no es una regla de negocio en la base: es la red de seguridad de `FR-AUT-002`, cuya comprobación vive en el handler. Coherente con `CA-INF-003`.

### 2.2 Herramientas

`dotnet-ef` **no está instalado** en la máquina. Se añade como **herramienta local** en `.config/dotnet-tools.json` en la raíz del repositorio, con la versión fijada, de modo que `dotnet tool restore` la instale de forma reproducible. Esto satisface `NFR-POR-002` (versiones fijadas y documentadas) mejor que una instalación global.

### 2.3 Dependencias NuGet

**Ninguna nueva.** Merece decirse explícitamente:

- PBKDF2 está en la BCL — `System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2`. No hace falta `Microsoft.AspNetCore.Cryptography.KeyDerivation`.
- La autenticación por cookie viene en el *shared framework* de ASP.NET Core. No hace falta paquete.
- `Microsoft.EntityFrameworkCore.Design` ya está en el proyecto `Api`, que es el proyecto de arranque para `dotnet ef`.

### 2.4 Configuración

- `appsettings.json` ya tiene `ConnectionStrings:VacaFlow`. No se añaden claves.
- La vida de la cookie (8 horas) y su nombre se fijan en código en `Program.cs`. No son secretos, así que `CA-INF-007` no aplica; `NFR-SEC-005` exige que la vida sea acotada, y lo es.
- `.gitignore` ya excluye `*.db`, `*.db-shm` y `*.db-wal` (`LC-03`).

---

## 3. Plan ordenado por dependencia

De adentro hacia afuera. Los ítems del mismo bloque sin dependencia entre sí pueden paralelizarse.

### Domain

| # | Capa | Acción | Artefacto | Notas |
|---|---|---|---|---|
| 1 | Domain | Modificar | `src/BigSolutions.VacaFlow.Domain/Primitives/Error.cs` | Añadir tercer componente opcional `Field`: `Error(string Code, string Message, string? Field = null)`. Lo exige la forma `{ code, message, field? }` de `FR-ERR-002`. Los usos actuales siguen compilando |
| 2 | Domain | Crear | `.../Domain/Employees/EmployeeId.cs` | `readonly record struct EmployeeId(Guid Value)`. **Sin** factory `New()`: `Guid.NewGuid()` está prohibido en Domain (`CA-DOM-009`) y el test `Domain_And_Application_Should_Not_Read_The_Clock_Directly` lo detecta. El identificador entra como parámetro |
| 3 | Domain | Crear | `.../Domain/Employees/EmployeeRole.cs` | `enum EmployeeRole { Employee = 1, Manager = 2 }`. Valores explícitos: se persisten como entero |
| 4 | Domain | Crear | `.../Domain/Employees/Email.cs` | `ValueObject`. `Create(string) → Result<Email>`: no vacío, formato válido, ≤ 200 caracteres, **normalizado a minúsculas**. La normalización es lo que hace que `UNIQUE(Email)` implemente la comparación *case-insensitive* de `AC2` sin necesidad de `COLLATE` |
| 5 | Domain | Crear | `.../Domain/Employees/Errors/EmployeeErrors.cs` | `EmailAlreadyRegistered` (`VF-AUT-001`, field `email`), `FullNameRequired`, `EmailInvalid`, `RoleInvalid` — los tres últimos con código `VF-VAL-001` y su `Field`. Mensajes verbatim de `Backlog.md` §3.5 |
| 6 | Domain | Crear | `.../Domain/Employees/Employee.cs` | `AggregateRoot<EmployeeId>`. Constructor privado + `static Result<Employee> Create(EmployeeId id, string fullName, Email email, EmployeeRole role)`. Propiedades con setter privado. `IsActive = true` y `ManagerId = null` al crear. **No tiene ninguna noción de contraseña** (`LC-02`) |

> `Employee` no expone método para asignar manager en esta historia. Cuando `OQ-01` se resuelva, se añade `AssignManager(EmployeeId)` como método del agregado.

### Application

| # | Capa | Acción | Artefacto | Notas |
|---|---|---|---|---|
| 7 | Application | Crear | `.../Application/Abstractions/IIdGenerator.cs` | `Guid NewId()`. **Puerto nuevo, no previsto en el `SAD.md` §6.3.** Es la consecuencia directa de `CA-DOM-009`/`CA-CRS-002`: si el dominio no puede llamar a `Guid.NewGuid()`, alguien tiene que inyectar el identificador. Además hace deterministas los tests |
| 8 | Application | Crear | `.../Application/Abstractions/IUnitOfWork.cs` | `Task SaveChangesAsync(CancellationToken)` devolviendo `Result` — para poder traducir la violación de unicidad sin excepciones de control de flujo |
| 9 | Application | Crear | `.../Application/Abstractions/IEmployeeRepository.cs` | Para esta historia: `Task<bool> EmailExistsAsync(Email, CancellationToken)` y `void Add(Employee)`. Sin `IQueryable`, sin genéricos (`CA-APP-005`, `CA-INF-004`) |
| 10 | Application | Crear | `.../Application/Abstractions/ICredentialStore.cs` | `void Add(EmployeeId, string passwordHash)` y `Task<string?> FindHashAsync(EmployeeId, CancellationToken)`. **Puerto nuevo.** Permite que `UserAccount` siga siendo `internal` de Infrastructure y que Application nunca lo vea |
| 11 | Application | Crear | `.../Application/Abstractions/IPasswordHasher.cs` | `string Hash(string password)` · `bool Verify(string password, string hash)` |
| 12 | Application | Crear | `.../Application/Abstractions/IDatabaseInitializer.cs` | `Task InitializeAsync(CancellationToken)`. **Puerto nuevo.** Necesario porque `VacaFlowDbContext` es `internal` y el composition root no puede tocarlo; y porque resolver servicios con `GetRequiredService` está prohibido dentro de Infrastructure (`CA-CFG-003`, verificado por `No_Layer_Should_Resolve_Services_From_The_Container`) |
| 13 | Application | Crear | `.../Application/Authentication/RegisterEmployeeCommand.cs` | `record RegisterEmployeeCommand(string FullName, string Email, string Password, string Role)` con `Result Validate()`: nombre 1–120, contraseña ≥ 8, rol parseable. Validación **estructural** aquí (`CA-APP-007`, `ADR-011`); las reglas de negocio quedan en el dominio |
| 14 | Application | Crear | `.../Application/Authentication/RegisterEmployeeHandler.cs` y `.../Authentication/RegisteredAccountDto.cs` | `sealed class` con constructor primario: `IEmployeeRepository`, `ICredentialStore`, `IPasswordHasher`, `IUnitOfWork`, `IIdGenerator`, `TimeProvider`. Devuelve `Task<Result<RegisteredAccountDto>>`, donde `RegisteredAccountDto(Guid Id, string FullName, string Email, string Role)` es el DTO de salida del caso de uso (`CA-APP-006`). Secuencia en §3.1 |
| 15 | Application | Modificar | `.../Application/DependencyInjection.cs` | Registrar `RegisterEmployeeHandler` como *scoped* |

#### 3.1 Secuencia del handler

1. `command.Validate()` — si falla, devolver.
2. `Email.Create(command.Email)` — si falla, devolver `VF-VAL-001` con `Field = "email"`.
3. `await repository.EmailExistsAsync(email, ct)` — si existe, devolver `EmployeeErrors.EmailAlreadyRegistered` (`VF-AUT-001`).
4. `Employee.Create(new EmployeeId(idGenerator.NewId()), command.FullName, email, role)` — si falla, devolver.
5. `var hash = passwordHasher.Hash(command.Password)`.
6. `repository.Add(employee)` · `credentialStore.Add(employee.Id, hash)`.
7. `await unitOfWork.SaveChangesAsync(ct)` — si devuelve fallo por unicidad, se propaga como `VF-AUT-001`.
8. `Result.Success(new RegisteredAccountDto(employee.Id.Value, employee.FullName, employee.Email.Value, employee.Role.ToString()))`.

El paso 3 y el paso 7 comprueban lo mismo a propósito. El paso 3 da el mensaje correcto en el caso normal; el paso 7 cierra la ventana de carrera entre la comprobación y el `INSERT`. Sin el paso 7, dos registros simultáneos con el mismo correo producirían una excepción sin traducir y un `500`.

### Infrastructure

| # | Capa | Acción | Artefacto | Notas |
|---|---|---|---|---|
| 16 | Infrastructure | Crear | `.../Infrastructure/Persistence/UserAccount.cs` | `internal sealed class`. Registro técnico, sin entidad de dominio detrás (`Intent.md` §7.1). Propiedades: `Id`, `EmployeeId`, `PasswordHash`, `CreatedAtUtc` |
| 17 | Infrastructure | Crear | `.../Infrastructure/Persistence/VacaFlowDbContext.cs` | `internal sealed`. `DbSet<Employee>`, `DbSet<UserAccount>`. `ApplyConfigurationsFromAssembly` |
| 18 | Infrastructure | Crear | `.../Persistence/Configurations/EmployeeConfiguration.cs` | Fluent API. Conversor de valor para `EmployeeId`; conversión de `Email` a `string`; `FullName` 120; `Role` como entero; `UNIQUE(Email)`; FK auto-referencial `ManagerId`. **Sin atributos en el dominio** (`CA-DOM-001`) |
| 19 | Infrastructure | Crear | `.../Persistence/Configurations/UserAccountConfiguration.cs` | `UNIQUE(EmployeeId)`, FK con borrado en cascada |
| 20 | Infrastructure | Crear | `.../Persistence/Repositories/EmployeeRepository.cs` | `internal sealed`. Implementa `IEmployeeRepository` |
| 21 | Infrastructure | Crear | `.../Persistence/Repositories/CredentialStore.cs` | `internal sealed`. Implementa `ICredentialStore`; es el único sitio que conoce `UserAccount` |
| 22 | Infrastructure | Crear | `.../Persistence/UnitOfWork.cs` | `internal sealed`. Envuelve `SaveChangesAsync` y **traduce** `DbUpdateException` por violación de índice único (SQLite `19`/`2067`) a `VF-AUT-001` (`CA-INF-005`). Ninguna excepción de proveedor cruza el anillo |
| 23 | Infrastructure | Crear | `.../Persistence/DatabaseInitializer.cs` | `internal sealed`. Implementa `IDatabaseInitializer` aplicando `Database.MigrateAsync()` |
| 24 | Infrastructure | Crear | `.../Infrastructure/Security/Pbkdf2PasswordHasher.cs` | `internal sealed`. PBKDF2-HMAC-SHA256, 210 000 iteraciones, sal aleatoria de 128 bits por contraseña, clave derivada de 256 bits, comparación en tiempo constante (`CryptographicOperations.FixedTimeEquals`). Formato almacenado `pbkdf2-sha256$<iter>$<salt-b64>$<hash-b64>`, para poder subir las iteraciones sin invalidar cuentas (`ADR-010`) |
| 25 | Infrastructure | Crear | `.../Infrastructure/Identifiers/GuidIdGenerator.cs` | `internal sealed`. Implementa `IIdGenerator` con `Guid.CreateVersion7()` — ordenable temporalmente, mejor localidad de índice que `NewGuid()`. **Carpeta nueva**, no prevista en el `SAD.md` §7.1 |
| 26 | Infrastructure | Modificar | `.../Infrastructure/DependencyInjection.cs` | Registrar `DbContext` con SQLite usando la cadena ya validada; repositorios, `ICredentialStore`, `IUnitOfWork`, `IDatabaseInitializer` como *scoped*; `IPasswordHasher` e `IIdGenerator` como *singleton*. Todo `internal`; solo `AddInfrastructure()` sigue siendo público |
| 27 | Infrastructure | Generar | `.../Persistence/Migrations/*_AddEmployeesAndUserAccounts.cs` | `dotnet ef migrations add AddEmployeesAndUserAccounts -p src/BigSolutions.VacaFlow.Infrastructure -s src/BigSolutions.VacaFlow.Api -o Persistence/Migrations` |

### API

| # | Capa | Acción | Artefacto | Notas |
|---|---|---|---|---|
| 28 | API | Crear | `.../Api/ErrorHandling/ErrorStatusMap.cs` | Diccionario `código → StatusCode` con el catálogo de `FRD.md` §7. Para esta historia: `VF-VAL-001`→400, `VF-AUT-001`→409, `VF-AUT-004`→401. El resto se añade con sus historias |
| 29 | API | Crear | `.../Api/ErrorHandling/ResultExtensions.cs` | `ToHttpResult()` y `ToCreatedResult<TDto, TBody>(Func<TDto,string> location, Func<TDto,TBody> body)`. **Punto único** de traducción (`CA-PRE-004`). En fallo emite `{ code, message, field? }`; en éxito emite `201` + `Location` + cuerpo |
| 30 | API | Crear | `.../Api/ErrorHandling/GlobalExceptionHandler.cs` | `IExceptionHandler`: `500` genérico que no filtra internos (`NFR-USA-003`) |
| 31 | API | Crear | `.../Api/Contracts/RegisterAccountContract.cs` y `.../Contracts/RegisterAccountResponse.cs` | Entrada: `record RegisterAccountContract(string FullName, string Email, string Password, string Role)` — **no lleva ningún identificador**, `TC-08` se cumple por la forma del contrato. Salida: `record RegisterAccountResponse(Guid Id, string FullName, string Email, string Role)`, propiedad del anillo de presentación (`CA-PRE-003`), mapeada desde `RegisteredAccountDto` |
| 32 | API | Crear | `.../Api/Endpoints/AuthEndpoints.cs` | `POST /api/auth/register`. Delega en el handler; si tiene éxito, emite la cookie con `HttpContext.SignInAsync` (claims `NameIdentifier` = id, `Role` = rol) y devuelve **`201` con `Location: /api/auth/me` y cuerpo `{ id, fullName, email, role }`**, tal como especifica `FRD.md` §6.1. Nunca incluye el hash (`NFR-SEC-002`). Bajo 15 líneas (`CA-PRE-001`) |
| 33 | API | Modificar | `.../Api/Program.cs` | Añadir `AddAuthentication().AddCookie()` con `HttpOnly` + `SameSite=Lax` + vida de 8 h; `AddAuthorization()`; `AddExceptionHandler`; `UseAuthentication`/`UseAuthorization`; mapear `AuthEndpoints`; y aplicar migraciones al arrancar resolviendo `IDatabaseInitializer` dentro de un *scope* — permitido aquí porque el composition root está exento de `CA-CFG-003` |

### Tests

| # | Capa | Acción | Artefacto | Notas |
|---|---|---|---|---|
| 34 | Test | Crear | `tests/...Domain.UnitTests/Employees/EmailTests.cs` | Formato válido e inválido, longitud máxima, vacío, y **normalización a minúsculas**. Sin infraestructura (`CA-TST-002`) |
| 35 | Test | Crear | `tests/...Domain.UnitTests/Employees/EmployeeTests.cs` | `Create` con datos válidos; nombre vacío y > 120; `IsActive` verdadero y `ManagerId` nulo al crear |
| 36 | Test | Crear | `tests/...Application.UnitTests/Authentication/RegisterEmployeeHandlerTests.cs` | Dobles de los cinco puertos (`CA-TST-003`). Casos: camino feliz persiste empleado **y** credencial y llama a `SaveChangesAsync`; el `RegisteredAccountDto` devuelto lleva el identificador generado y el correo normalizado, y **no** expone el hash; correo duplicado devuelve `VF-AUT-001` y no añade nada; contraseña corta y correo inválido devuelven `VF-VAL-001` con el `Field` correcto; el hash almacenado **no** es la contraseña en claro; el identificador proviene de `IIdGenerator` |
| 37 | Test | Crear | `tests/...Infrastructure.IntegrationTests/Persistence/EmployeeRepositoryTests.cs` | Contra archivo SQLite temporal por clase (`CA-TST-004`): `Add` + `SaveChangesAsync` persiste; `EmailExistsAsync` verdadero y falso; el segundo `INSERT` con el mismo correo se traduce a `VF-AUT-001` y no lanza excepción de proveedor |
| 38 | Test | Crear | `tests/...Infrastructure.IntegrationTests/Security/Pbkdf2PasswordHasherTests.cs` | El hash no contiene la contraseña; dos hashes de la misma contraseña difieren (sal por contraseña); `Verify` acepta la correcta y rechaza la incorrecta |
| 39 | Test | Verificar | `tests/...ArchitectureTests/` | **Sin cambios.** Tres aserciones que hoy pasan en vacío empiezan a morder: `Handlers_Should_Be_Sealed_And_Suffixed`, `Infrastructure_Implementations_Should_Not_Be_Public` y `Employees_Should_Not_Depend_On_Requests` |

**Paralelizable:** los ítems 2–5 entre sí; los puertos 7–12 entre sí; los tests 34–38 una vez existe su sujeto. **Ruta crítica:** 1 → 6 → 14 → 17 → 26 → 27 → 33.

---

## 4. Casos de uso y tabla de trazabilidad

| Historia | Criterio (verbatim, abreviado en la cita larga) | Ítems que lo cubren | Cómo se verifica |
|---|---|---|---|
| `US-007` | `AC1` — "…an `Employee` and a `UserAccount` are created and I am signed in directly…" | 6, 14, 16, 20, 21, 22, 32, 33 | Test de integración (#37) confirma las dos filas; `curl` sobre el endpoint devuelve `201` + `Set-Cookie` (§6, paso 4) |
| `US-007` | `AC2` — "…`VF-AUT-001` is returned beneath the email field and no second account is created. Comparison is case-insensitive." | 4, 5, 9, 14, 22, 28, 29 | Test de aplicación (#36) para el camino normal; test de integración (#37) para la carrera y para `Bob@X.com` vs `bob@x.com` |
| `US-007` | `AC3` — "…the password is stored hashed." | 11, 21, 24 | Test de infraestructura (#38); e inspección directa de la tabla `UserAccounts` (§6, paso 6) |
| `US-007` | `AC4` — "…the corresponding validation message from §3.5 appears beneath that field." | 1, 5, 13, 29 | Test de aplicación (#36) verifica código, mensaje y `Field`; `curl` (§6, paso 5) verifica el JSON `{ code, message, field }` |
| `US-012` | `AC5` — "Auth card 420px per §3.3, subtitle `Create an account`." | ⏸️ ninguno | Ver el aviso de abajo |
| `US-012` | `AC6` — "Four groups in order: `Full name`… `Role (for demo purposes)`." | ⏸️ ninguno | Ver el aviso de abajo |
| `US-012` | `AC7` — "…two radio options side by side… `Employee` is preselected." | ⏸️ ninguno | Ver el aviso de abajo |
| `US-012` | `AC8` — "Primary full-width button `Create account`, disabled while saving." | ⏸️ ninguno | Ver el aviso de abajo |
| `US-012` | `AC9` — "Below the form: `Already have an account? Sign in`." | ⏸️ ninguno | Ver el aviso de abajo |

**Conteo: 9 criterios de entrada · 4 cubiertos por este plan · 5 reasignados a `US-012`.**

> ⏸️ **`AC5`–`AC9` no se planifican aquí, y no es una omisión.**
>
> Son criterios de la pantalla `S-02`, y `src/web` no existe: el scaffolding de Next.js es el paquete `4.5` del WBS y quedó aplazado a propósito hasta decidir si el árbol de código se excluye de la sincronización de OneDrive.
>
> El propio `Backlog.md` ya los asigna dos veces: `US-012` — "Create-account screen" dice literalmente *"Covered by the visual criteria of `US-007`"*. Son los mismos cinco criterios vistos desde la historia de pantalla.
>
> **Consecuencia que hay que aceptar:** al terminar `US-007`, `AC-01` del `Intent.md` será demostrable **por API** (`curl` o cliente HTTP), no por interfaz. La demo de aceptación completa necesita `US-012`, y `US-012` necesita antes el paquete `4.5`.

---

## 5. Supuestos y decisiones

| # | Supuesto o decisión | Justificación | Impacto si es incorrecto |
|---|---|---|---|
| `S1` | **`ManagerId` queda `null` al registrarse.** No se implementa el fallback del prototipo ("el primer manager de la tabla") | `OQ-01` sigue abierto y ese fallback no es una regla de negocio. `ApprovalPolicy` ya falla cerrado | Un empleado auto-registrado no podrá recibir aprobación hasta que `OQ-01` se resuelva. Es el statu quo ya documentado, no una regresión |
| `S2` | **`POST /api/auth/register` devuelve `201` + `Location` + cuerpo `{ id, fullName, email, role }`**, tal como especifica hoy el `FRD.md` §6.1. Decisión del arquitecto, 2026-07-28, en contra de la lectura literal del `ADR-012` | El cuerpo no contiene ningún dato que el cliente no tuviera ya, salvo el identificador — que `CA-APP-002` permite explícitamente ("más allá de identificadores/resultado"). Es un eco de la entrada, no una consulta. Y ahorra al frontend un `GET /auth/me` inmediato que necesita para el nombre del header y para decidir si pinta la pestaña `Approval Queue` | Un auditor estricto podría leer `fullName`/`email`/`role` como datos de lectura. Por eso el `ADR-012` debe recoger el razonamiento por escrito: es lo que protege la decisión en la auditoría. Si finalmente se considera desviación, es 🟡 y cuesta un punto en el bloque `CA-APP` de la rúbrica |
| `S3` | **La selección de rol en el registro se implementa**, etiquetada "for demo purposes" | Está en los criterios visuales de `US-007` y en el microcopy de `Backlog.md` §3.5. `OQ-02` sigue sin confirmación formal del sponsor | Si el sponsor lo rechaza, se elimina el campo del contrato y `RegisterEmployeeHandler` crea siempre `EmployeeRole.Employee`. Es un cambio contenido |
| `S4` | **El correo se normaliza a minúsculas en el value object** | Hace que `UNIQUE(Email)` implemente la comparación *case-insensitive* de `AC2` sin `COLLATE NOCASE` ni índice funcional | Si algún día hiciera falta preservar la capitalización original, habría que añadir una columna `EmailDisplay` y migrar |
| `S5` | **Se introduce `IIdGenerator`** en vez de generar el identificador en el dominio | `CA-DOM-009` prohíbe `Guid.NewGuid()` en el dominio, y el test `SourceRuleTests` lo verifica también en Application | Sin este puerto no hay forma legal de crear un identificador. La alternativa sería relajar el test, que es peor |
| `S6` | **Se introduce `IDatabaseInitializer`** para aplicar migraciones al arrancar | `VacaFlowDbContext` es `internal`, así que el composition root no puede resolverlo directamente; y `GetRequiredService` está prohibido dentro de Infrastructure | Sin el puerto habría que hacer público el `DbContext`, rompiendo `CA-DEP-007` |
| `S7` | **`Guid.CreateVersion7()`** para los identificadores | Ordenable temporalmente; mejor localidad en el índice de clave primaria que `NewGuid()`. Disponible en .NET 9+ | Ninguno funcional. Si molestara, se cambia por `Guid.NewGuid()` en una sola línea de `GuidIdGenerator` |
| `S8` | **`Verify` se implementa junto a `Hash`** aunque su consumidor sea `US-008` | Es el mismo archivo, y probar un hasher sin verificación es probar la mitad | Ninguno. Queda código sin consumidor durante una historia |

---

## 6. Verificación end-to-end

| # | Paso | Resultado esperado |
|---|---|---|
| 1 | `dotnet tool restore` | `dotnet-ef` disponible en la versión fijada |
| 2 | `dotnet build VacaFlow.slnx` | Compila con **0 warnings** — `TreatWarningsAsErrors` está activo |
| 3 | `dotnet test VacaFlow.slnx` | Los 23 tests actuales siguen verdes, más los nuevos de #34–#38. Los tests de arquitectura pasan con las tres aserciones ya armadas |
| 4 | `dotnet run --project src/BigSolutions.VacaFlow.Api --urls http://localhost:5080`, luego `POST /api/auth/register` con datos válidos | `201`, header `Location`, header `Set-Cookie` con `HttpOnly`, y cuerpo `{ id, fullName, email, role }` con el correo ya normalizado a minúsculas. **El cuerpo no contiene `passwordHash` ni ningún otro campo** (`NFR-SEC-002`). Se crea `vacaflow.db` con las tablas `Employees` y `UserAccounts` |
| 5 | Repetir el mismo correo con distinta capitalización · contraseña de 5 caracteres · correo malformado | `409 VF-AUT-001` · `400 VF-VAL-001` con `field: "password"` · `400 VF-VAL-001` con `field: "email"`. Mensajes idénticos a `Backlog.md` §3.5 |
| 6 | Inspeccionar `SELECT * FROM UserAccounts` | `PasswordHash` empieza por `pbkdf2-sha256$210000$`. La contraseña en claro no aparece en ninguna columna de ninguna tabla |
| 7 | Revisar los logs del arranque y del registro | Ninguna contraseña ni hash en la salida (`NFR-SEC-002`, `NFR-PRV-005`) |
| 8 | `git status` | Limpio: `vacaflow.db` y sus ficheros `-shm`/`-wal` no aparecen (`LC-03`) |

---

## 7. Enmiendas a la documentación que este plan produce

Ninguna es contradictoria; todas son consecuencia de aplicar las reglas al código real. Conviene aplicarlas al cerrar la historia, no dejarlas para el final del proyecto.

| Documento | Cambio |
|---|---|
| `FRD.md` §6.1 | **Ninguno.** El contrato queda tal como está escrito: `201` con `{ id, fullName, email, role }` (`S2`) |
| `SAD.md` `ADR-012` | **Debe matizarse.** Hoy afirma que todo endpoint de comando devuelve `204`. Añadir la excepción de `POST /auth/register` con el razonamiento de `S2`: el cuerpo solo devuelve el identificador más un eco de la entrada, lo que `CA-APP-002` permite; y evita un `GET /auth/me` inmediato que el frontend necesita para el header y para la pestaña `Approval Queue`. Sin este párrafo, el código y el ADR se contradicen |
| `SAD.md` §8.3 | Misma matización: el título "Command responses" pasa de una regla absoluta a una regla con una excepción nombrada |
| `SAD.md` §6.3 | La tabla de puertos gana tres filas: `ICredentialStore`, `IIdGenerator`, `IDatabaseInitializer` |
| `SAD.md` §7.1 | La estructura de Infrastructure gana `Identifiers/` y `Persistence/UserAccount.cs` |
| `SAD.md` §5.5 | `Error` lleva un tercer componente opcional `Field` |
| `SAD.md` §14.1 | **Hueco detectado:** no existe proyecto `Api.FunctionalTests`, que la estructura de referencia del documento normativo §15.1 sí contempla y que `CA-TST-005` pide para los flujos críticos de presentación. Hoy los endpoints solo se verifican a mano. Proponer como habilitador propio, probablemente junto a `US-008` |

---

## 8. Riesgos

| Riesgo | Mitigación |
|---|---|
| El handler crece con lógica de negocio disfrazada de orquestación (`CA-APP-010`) | La secuencia de §3.1 tiene un solo condicional propio — la comprobación de unicidad — que es autorización de datos, no regla del agregado. Revisar en el code review |
| La comprobación de unicidad en el paso 3 se considera suficiente y se omite la traducción del paso 7 | El test #37 exige explícitamente que el segundo `INSERT` se traduzca y no lance excepción de proveedor |
| `Validate()` se olvida en el handler (coste aceptado de `ADR-011`) | El test #36 cubre contraseña corta y correo inválido; si falta la llamada, esos casos fallan |
| La migración inicial se genera con las cinco tablas por copiar el `SAD.md` §7.2 | `Request`, `Approval` y `AbsenceType` no existen como tipos; EF no puede mapearlas. El error sería del compilador, no silencioso |
| `src/web` se scaffoldea "de paso" para cerrar `AC5`–`AC9` | Está fuera de alcance por decisión explícita (§1.4). Es el paquete `4.5` y depende de resolver la exclusión de OneDrive |
| **El cuerpo de la respuesta filtra más de lo debido.** Ahora que `201` devuelve datos, un descuido — serializar la entidad `Employee` en vez del DTO, o añadir campos al DTO sin pensar — expondría información que no toca. Este riesgo no existía con la respuesta vacía | El endpoint mapea explícitamente `RegisteredAccountDto` → `RegisterAccountResponse`, cuatro campos nombrados uno a uno; nunca serializa la entidad (`CA-APP-006`). El test #36 y el paso 4 de §6 comprueban que el hash no aparece |
