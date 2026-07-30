# Plan de implementación — `TE-003` · Seed data

| Campo | Valor |
|---|---|
| Historia | `TE-003` — Seed data |
| Épica | `EP-01` — Foundations |
| Prioridad · Talla | **Must** · `S` declarada — **crece hacia `M`** por el prerequisito de `AbsenceType` (ver §1.4 y decisión `D1`) |
| Pantalla | Ninguna — comportamiento de arranque de la API; la superficie visible es el `TEST ACCOUNTS` de `S-01` (historia `US-013`, fuera de alcance aquí) |
| Depende de | `TE-002` (mergeada en `main` vía `US-007`, PR #4) — **no** depende de `TE-011` ni `US-010` |
| Trazas | `SC-14`, `BC-03`, `LC-04` · `FR-DAT-002`–`FR-DAT-005` · `SAD.md` §7.5 |
| Fuentes | `Backlog.md` §EP-01 y §3.6 · `FRD.md` §9 · `SAD.md` §5, §7 · `WBS.md` paquete 3.4 |
| Repositorio | `C:\Users\avald\OneDrive\Work\VacaFlow\repositories\vacaflow` |
| Rama sugerida | `feat/te-003-seed-data`, creada desde `main` (todo lo que este plan toca existe ya en `main`; no requiere las ramas de `TE-011`/`US-010`) |
| Estado | Aprobado el 2026-07-30 (decisiones de arquitecto documentadas en §5; sesión sin interlocutor humano) |

> **Este documento no implementa nada.** La implementación la ejecuta `/user-story-implement`.

---

## 1. Entendimiento

### 1.1 Contexto — por qué ahora

`TE-002` dejó el mecanismo de arranque en su sitio: `Program.cs` resuelve `IDatabaseInitializer` en un scope propio y `DatabaseInitializer` (`src/BigSolutions.VacaFlow.Infrastructure/Persistence/DatabaseInitializer.cs`) aplica migraciones con `Database.MigrateAsync()`. Pero la base recién creada queda **vacía**: no hay tipos de ausencia que listar (`US-014` depende de esto) ni cuentas con las que un revisor pueda iniciar sesión sin registrarse primero (`FR-DAT-003`, `BC-03`). `TE-003` cierra ese hueco con un seeder idempotente que corre en el mismo punto de arranque, exactamente donde el comentario de `AddInfrastructure` lo anuncia desde `US-007`: *"The seeder joins this list with TE-003"*.

**Hallazgo estructural verificado:** el agregado `AbsenceType` **no existe en ninguna capa**. `src/BigSolutions.VacaFlow.Domain/AbsenceTypes/` y `src/BigSolutions.VacaFlow.Application/AbsenceTypes/` contienen únicamente `.gitkeep`; no hay configuración EF, ni tabla, ni migración (`VacaFlowDbContext` solo expone `Employees` y `UserAccounts`; la única migración es `20260729173612_AddEmployeesAndUserAccounts`). El `WBS.md` (detalle del paquete 3.2) asignaba `AbsenceType` al modelado de dominio de `TE-001`/`TE-002`, pero esas historias se implementaron solo con lo que `US-007`/`US-008` necesitaban. No se puede sembrar lo que no existe: este plan incluye la creación **mínima** del agregado como prerequisito explícito (Bloque A del §3), con la justificación en la decisión `D1`.

Segundo hueco verificado: `Employee` (`src/BigSolutions.VacaFlow.Domain/Employees/Employee.cs`) no tiene ninguna vía para asignar `ManagerId` — el constructor lo fija en `null` y su doc-comment lo declara *"Null until OQ-01 is resolved"*. El mapeo y la FK autorreferencial **sí** existen (`EmployeeConfiguration`, `FK ManagerId → Employees(Id)` con `Restrict`), así que la columna está lista: falta el comportamiento de dominio. Ver decisión `D2`.

### 1.2 Narrativa

El backlog formula `TE-003` sin narrativa "Como… quiero…" (es una historia técnica); su intención la fijan `FR-DAT-002`–`FR-DAT-005`:

> `FR-DAT-002` — "The catalog is seeded with Vacation, Personal Leave and Sick Leave."
> `FR-DAT-003` — "At least one account with the `Manager` role is seeded so approvals are testable without an administration screen."
> `FR-DAT-004` — "Restarting against an existing database creates no duplicates."
> `FR-DAT-005` — "Seeded credentials are clearly non-production and are documented in the README."

### 1.3 Criterios de aceptación — verbatim (`Backlog.md` §EP-01 · `TE-003`)

| # | Criterio |
|---|---|
| `AC1` | "Given a new database, when the API starts, then the three absence types of §3.6 exist with their English display names and matching codes." |
| `AC2` | "Given a new database, when the API starts, then the three employees of §3.6 exist, with Carlos Ruiz and Ana Torres assigned to Laura Méndez." |
| `AC3` | "Given a restart on an existing database, when seeding runs, then no duplicates are created." |
| `AC4` | "Given the seeded credentials, when inspected, then they are clearly non-production and documented in the README." |

### 1.3.1 Datos fijados por `Backlog.md` §3.6 (verificados contra el documento del repo)

| Empleado | Email | Rol | Manager |
|---|---|---|---|
| Laura Méndez | `manager@vacaflow.test` | Manager | — |
| Carlos Ruiz | `employee@vacaflow.test` | Employee | Laura Méndez |
| Ana Torres | `ana@vacaflow.test` | Employee | Laura Méndez |

Contraseñas: `Manager123!` (Laura) y `Employee123!` (Carlos y Ana — §3.6 publica dos pares de credenciales; ver supuesto `S2`). Tipos de ausencia: **Vacation** (`VACATION`), **Personal Leave** (`PERSONAL_LEAVE`), **Sick Leave** (`SICK_LEAVE`) — nombres en inglés, nunca los del prototipo en español.

### 1.4 Alcance

**Entra**

- **Bloque A (prerequisito):** el agregado `AbsenceType` mínimo en Domain (`AbsenceType`, `AbsenceTypeId`, `AbsenceTypeCode`), su configuración Fluent API, el `DbSet` y la migración `AddAbsenceTypes` — la forma que el `SAD.md` §5 y §7.2 ya dibujan, sin ningún comportamiento de negocio (eso llega con `US-014` y `EP-05`).
- `Employee.AssignManager(...)` — el método de dominio aditivo que hoy falta para cumplir `AC2`.
- `DatabaseSeeder` idempotente en `Infrastructure/Persistence/` (nombre y ubicación fijados por `SAD.md` §7.1), invocado por `DatabaseInitializer` inmediatamente después de `MigrateAsync()` — mismo punto de arranque, ningún mecanismo paralelo.
- Contraseñas sembradas hasheadas con el `IPasswordHasher` real (PBKDF2) y persistidas vía `ICredentialStore` — el mismo camino que un registro real; ningún hash alternativo.
- README: sección de cuentas sembradas con credenciales explícitas y marca clara de no-producción; retirar los avisos "*Applies once work package 3.3/3.4 lands*".
- Tests: unitarios de dominio (`AbsenceType`, `AbsenceTypeCode`, `AssignManager`), integración del seeder sobre SQLite real (doble ejecución = idempotencia), y funcional de arranque (sign-in con una cuenta sembrada contra el pipeline real).

**No entra**

| Excluido | Por qué |
|---|---|
| `IAbsenceTypeRepository`, `ListAbsenceTypesHandler`, `GET /absence-types` | Son `US-014` (paquete 5.1 del WBS). El seeder es interno a Infrastructure y no necesita el puerto; crearlo hoy sería adelantar trabajo sin consumidor |
| Comportamiento de negocio en `AbsenceType` (activar/desactivar, renombrar) | Ninguna historia del MVP lo pide; el catálogo es *"Seeded catalog, read-only at runtime"* (`SAD.md` §5.1) |
| Resolver `OQ-01` (cómo se asigna manager en el registro) | `AssignManager` es mecanismo, no política: el flujo de registro (`US-007`) sigue sin asignar manager. La pregunta abierta sigue abierta |
| Bloque `TEST ACCOUNTS (NON-PRODUCTION)` en la pantalla `S-01` | Es criterio visual de `US-013` (web, paquete 4.x); no hay superficie web todavía, así que no hay paridad que mantener |
| Reparar o actualizar filas ya existentes (p. ej. un empleado sembrado al que le borraron el manager) | `FR-DAT-004` exige no duplicar, no reconciliar. El reset documentado (`FR-DAT-006`) es la vía de reparación |
| Sembrar `Requests`/`Approvals` de ejemplo | §3.6 no los fija; el flujo se demuestra creándolos en vivo |

---

## 2. Cambios estructurales / de base

- **Nueva tabla `AbsenceTypes`** — `Id` (PK, Guid), `Code` (`UNIQUE`), `Name`, `IsActive` — exactamente el esquema del `SAD.md` §7.2. Llega vía una migración nueva `AddAbsenceTypes` en `src/BigSolutions.VacaFlow.Infrastructure/Persistence/Migrations/`, generada con `dotnet ef migrations add` (versionada en el repo, `CA-INF-008`). Cambio **aditivo**: no toca `Employees`, `UserAccounts` ni datos existentes, y una base ya creada la recibe por `MigrateAsync()` sin reset.
- **Sin cambios** de configuración, variables de entorno, permisos, feature flags ni dependencias NuGet. La cadena de conexión y el pipeline de arranque quedan como están.
- **Cambio documental obligatorio** (es un `AC`): README, sección de cuentas sembradas y reset.

---

## 3. Plan ordenado por dependencia

De adentro hacia afuera. **API no se toca**: `Program.cs` ya resuelve `IDatabaseInitializer`, y el seeder se cuelga detrás de ese mismo puerto (decisión `D3`).

### Bloque A — prerequisito: agregado `AbsenceType` mínimo (Domain → Infrastructure)

| # | Capa | Acción | Artefacto | Notas |
|---|---|---|---|---|
| 1 | Domain | Crear | `src/BigSolutions.VacaFlow.Domain/AbsenceTypes/AbsenceTypeId.cs` | `readonly record struct` sobre `Guid`, espejo exacto de `EmployeeId` (mismo patrón, mismo guard de Guid no vacío si `EmployeeId` lo tiene) |
| 2 | Domain | Crear | `src/BigSolutions.VacaFlow.Domain/AbsenceTypes/AbsenceTypeCode.cs` | Value object (`SAD.md` §5.2): invariante "uno de `VACATION`, `PERSONAL_LEAVE`, `SICK_LEAVE`", inmutable, igualdad estructural, `Create` → `Result<AbsenceTypeCode>` como `Email.Create`. Los tres valores conocidos expuestos como estáticos (`AbsenceTypeCode.Vacation`, etc.) para que el seeder no construya strings mágicos |
| 3 | Domain | Crear | `src/BigSolutions.VacaFlow.Domain/AbsenceTypes/Errors/AbsenceTypeErrors.cs` | Solo el error del VO (código inválido). No hay entrada de catálogo FRD §7 para esto — es un error de programación/datos, no de usuario; basta un `Error` interno al dominio siguiendo el patrón de `EmployeeErrors` |
| 4 | Domain | Crear | `src/BigSolutions.VacaFlow.Domain/AbsenceTypes/AbsenceType.cs` | `sealed class AbsenceType : AggregateRoot<AbsenceTypeId>` con `Code` (`AbsenceTypeCode`), `Name` (string, requerido), `IsActive` (bool, `true` al crear). Factoría `Create` → `Result<AbsenceType>`; constructor privado sin parámetros para EF (patrón `Employee`). **Ningún otro método**: catálogo de solo lectura (`SAD.md` §5.1) |
| 5 | Infrastructure | Crear | `src/BigSolutions.VacaFlow.Infrastructure/Persistence/Configurations/AbsenceTypeConfiguration.cs` | Tabla `AbsenceTypes`; `Id` con converter `ValueGeneratedNever` (patrón `EmployeeConfiguration`); `Code` con converter a string, `HasMaxLength` acotado, **`UNIQUE(Code)`** — el índice es la red de seguridad de la idempotencia (`SAD.md` §7.2/§7.5); `Name` requerido con longitud acotada; `IsActive` requerido |
| 6 | Infrastructure | Modificar | `src/BigSolutions.VacaFlow.Infrastructure/Persistence/VacaFlowDbContext.cs` | Añadir `DbSet<AbsenceType> AbsenceTypes` |
| 7 | Infrastructure | Crear | `src/BigSolutions.VacaFlow.Infrastructure/Persistence/Migrations/<timestamp>_AddAbsenceTypes.cs` | Generada por `dotnet ef migrations add AddAbsenceTypes` (actualiza también el `ModelSnapshot`). Revisar que solo crea la tabla nueva |

### Bloque B — comportamiento de dominio para `AC2`

| # | Capa | Acción | Artefacto | Notas |
|---|---|---|---|---|
| 8 | Domain | Modificar | `src/BigSolutions.VacaFlow.Domain/Employees/Employee.cs` | Añadir `public Result AssignManager(EmployeeId managerId)` — único invariante: `managerId != Id` (nadie es su propio manager). Actualizar el doc-comment de `ManagerId` (deja de ser inalcanzable; `OQ-01` sigue abierta para el flujo de registro). Ver decisión `D2` |
| 9 | Domain | Modificar | `src/BigSolutions.VacaFlow.Domain/Employees/Errors/EmployeeErrors.cs` | Añadir el error del auto-manager (patrón de los existentes; sin código FRD §7 — no es un error de cara al usuario en el MVP) |

### Bloque C — el seeder

| # | Capa | Acción | Artefacto | Notas |
|---|---|---|---|---|
| 10 | Infrastructure | Crear | `src/BigSolutions.VacaFlow.Infrastructure/Persistence/DatabaseSeeder.cs` | `internal sealed` (CA-DEP-007), nombre y ruta del `SAD.md` §7.1. Dependencias: `VacaFlowDbContext` (consultas de existencia y `SaveChangesAsync`), `IPasswordHasher` (hashear `Manager123!`/`Employee123!` por el camino real), `ICredentialStore` (persistir el hash igual que `RegisterEmployeeHandler` — reutilización, no mecanismo paralelo), `IIdGenerator`. Algoritmo: (1) por cada tipo de §3.6, si no existe fila con ese `Code` → `AbsenceType.Create` e insertar; (2) si no existe empleado con email `manager@vacaflow.test` → `Employee.Create` (rol `Manager`) + `credentialStore.Add`; (3) ídem Carlos y Ana (rol `Employee`) con `AssignManager(lauraId)` — resolviendo el id de Laura de la base si ya existía; (4) un único `SaveChangesAsync` final. Idempotencia por `AbsenceType.Code` y `Employee.Email` (`SAD.md` §7.5, `FR-DAT-004`). Construye **siempre** vía factorías de dominio — el seeder no elude invariantes |
| 11 | Infrastructure | Modificar | `src/BigSolutions.VacaFlow.Infrastructure/Persistence/DatabaseInitializer.cs` | Inyectar `DatabaseSeeder` y llamar `SeedAsync` tras `MigrateAsync()`. El puerto `IDatabaseInitializer` **no cambia** y `Program.cs` **no se toca** (decisión `D3`); actualizar el doc-comment ("applies migrations **and seeds**") |
| 12 | Infrastructure | Modificar | `src/BigSolutions.VacaFlow.Infrastructure/DependencyInjection.cs` | `services.AddScoped<DatabaseSeeder>();` y retirar el comentario "*The seeder joins this list with TE-003*" que esta línea salda |

### Bloque D — documentación (`AC4`)

| # | Capa | Acción | Artefacto | Notas |
|---|---|---|---|---|
| 13 | Docs | Modificar | `README.md` | (a) Sección "Seeded accounts": tabla con los tres empleados, emails, roles, manager y las dos contraseñas **en texto**, encabezada con una advertencia inequívoca de no-producción (dominio `.test`, "must never be reused anywhere real" — la frase ya existe, ahora con los datos delante); (b) sección "Resetting the database": retirar "*Applies once work package `3.3` lands.*" (el procedimiento ya es real); (c) retirar "*Applies once work package `3.4` lands.*"; (d) ajustar el párrafo de estado ("no business behaviour yet") que esta historia deja obsoleto en lo relativo a seed |

### Bloque E — tests

| # | Capa | Acción | Artefacto | Notas |
|---|---|---|---|---|
| 14 | Test | Crear | `tests/BigSolutions.VacaFlow.Domain.UnitTests/AbsenceTypes/AbsenceTypeCodeTests.cs` | Los tres códigos válidos crean; cualquier otro string, vacío o casing distinto falla; igualdad estructural |
| 15 | Test | Crear | `tests/BigSolutions.VacaFlow.Domain.UnitTests/AbsenceTypes/AbsenceTypeTests.cs` | `Create` válido → `IsActive` true y campos correctos; nombre vacío falla |
| 16 | Test | Modificar | `tests/BigSolutions.VacaFlow.Domain.UnitTests/Employees/EmployeeTests.cs` | `AssignManager` asigna; auto-asignación falla con el error nuevo |
| 17 | Test | Crear | `tests/BigSolutions.VacaFlow.Infrastructure.IntegrationTests/Persistence/DatabaseSeederTests.cs` | Sobre `SqliteDatabaseFixture` (que ya ejecuta `IDatabaseInitializer`, por lo que tras el ítem 11 **ya siembra**): (a) **`AC1`** — existen exactamente 3 tipos con los pares nombre/código de §3.6; (b) **`AC2`** — existen los 3 empleados; Carlos y Ana con `ManagerId` = id de Laura; Laura sin manager y con rol `Manager`; (c) **`AC3`** — ejecutar `InitializeAsync` una segunda vez sobre la misma base → mismos conteos (3/3/3 cuentas), sin filas nuevas; (d) el hash sembrado de `manager@vacaflow.test` verifica contra `Manager123!` vía `IPasswordHasher.Verify` (resuelto del contenedor real) y **no es** la contraseña en claro |
| 18 | Test | Crear | `tests/BigSolutions.VacaFlow.Api.FunctionalTests/Endpoints/SeededAccountsTests.cs` | Contra `VacaFlowApiFactory` (arranque real, base nueva): (a) `POST /api/auth/login` con `manager@vacaflow.test`/`Manager123!` → `200` con `fullName` "Laura Méndez" y `role` "Manager" — demuestra `AC1`+`AC2` extremo a extremo *"when the API starts"* y que el hash es compatible con el flujo real de sign-in; (b) ídem `employee@vacaflow.test`/`Employee123!` → `200`, rol `Employee`; (c) `POST /api/auth/register` con email `manager@vacaflow.test` → `VF-AUT-001` (la fila sembrada participa de la unicidad real) |
| 19 | Test | Verificar | `tests/BigSolutions.VacaFlow.ArchitectureTests/` + suites existentes | **Sin cambios, comprobar en verde**: los tipos nuevos de Domain no arrastran EF/ASP.NET (`DependencyRuleTests`); `DatabaseSeeder` es `internal sealed`; las suites funcionales/integración existentes siguen pasando ahora que **toda** base de test arranca sembrada (riesgo §7) |

**Dependencias:** {1,2,3} → 4 → 5 → 6 → 7; {8,9} independiente del Bloque A; {4,8} → 10 → 11 → 12; 13 independiente; {1–4} → {14,15}; {8,9} → 16; {10–12} → {17,18}; todo → 19. Bloques A y B paralelizables entre sí; 13 en cualquier momento. **Ruta crítica:** 1→4→5→7→10→11→17/18.

---

## 4. Casos de uso y tabla de trazabilidad

No hay caso de uso de Application (ningún actor invoca esto; es comportamiento de arranque — por eso el plan no crea handlers ni endpoints). El "caso de uso" único es **arranque de la API sobre base nueva o existente → catálogo y cuentas de §3.6 presentes, sin duplicados**.

| Historia | Criterio de aceptación (verbatim) | Ítems del plan que lo cubren | Cómo se verifica |
|---|---|---|---|
| `TE-003` | "Given a new database, when the API starts, then the three absence types of §3.6 exist with their English display names and matching codes." | #1–#7 (entidad y tabla) · #10, #11, #12 (siembra en el arranque) | Test de integración #17a (conteo y pares nombre/código exactos) · funcional #18 (arranque real del pipeline) |
| `TE-003` | "Given a new database, when the API starts, then the three employees of §3.6 exist, with Carlos Ruiz and Ana Torres assigned to Laura Méndez." | #8, #9 (`AssignManager`) · #10, #11, #12 (siembra) | Test de dominio #16 · integración #17b (`ManagerId` de Carlos y Ana = id de Laura) · funcional #18a/#18b (sign-in real con ambas cuentas) |
| `TE-003` | "Given a restart on an existing database, when seeding runs, then no duplicates are created." | #10 (chequeos por `Code`/`Email`) · #5 (`UNIQUE(Code)` como red de seguridad, junto al `UNIQUE(Email)` existente) | Test de integración #17c: segunda ejecución del initializer sobre la misma base → conteos idénticos |
| `TE-003` | "Given the seeded credentials, when inspected, then they are clearly non-production and documented in the README." | #13 (README) · #10 (emails de dominio `.test`, hash real — nunca texto claro en la base) | Revisión del README (tabla + advertencia) · integración #17d (lo almacenado es un hash PBKDF2 verificable, no la contraseña) |

**Conteo: 4 criterios de entrada · 4 cubiertos.**

---

## 5. Supuestos y decisiones

Decisiones tomadas con criterio de arquitecto y documentadas aquí; la sesión de planificación no contó con interlocutor humano para la Fase 3, así que cada una lleva su reversibilidad anotada.

| # | Decisión | Justificación | Impacto si es incorrecta |
|---|---|---|---|
| `D1` | **El agregado `AbsenceType` mínimo se crea dentro de esta historia** (Bloque A), en lugar de declararlo bloqueante o abrir una historia nueva | Es un prerequisito directo e inseparable: sin entidad no hay nada que sembrar, y ninguna otra historia pendiente lo entrega antes (`US-014` depende de `TE-003`, no al revés — declararlo bloqueante crearía un ciclo). El WBS lo asignaba al paquete 3.2 (`TE-001`/`TE-002`) pero quedó sin hacer; recuperarlo aquí es aditivo y de forma fijada por el `SAD.md` (§5.1, §5.2, §7.2): tres tipos de dominio sin comportamiento, una configuración, una migración. **Coste honesto:** la talla real deja de ser `S` y se acerca a `M` — señalado al usuario en la presentación del plan, no escondido | Si el equipo prefiriera imputar el Bloque A a `TE-002` (reabrirla) o a una historia técnica nueva, el bloque se extrae tal cual — está delimitado precisamente para eso |
| `D2` | **`AssignManager(EmployeeId)` como método del agregado**, con el único invariante "no a sí mismo"; sin parámetro `managerId` en `Employee.Create` | `SAD.md` §5.1: *"Identity and manager assignment change independently"* — es un cambio de estado del agregado, no un dato de nacimiento; el propio doc-comment de `Employee` lo dice. Meterlo en `Create` obligaría a tocar la firma y a `RegisterEmployeeHandler` (que no debe asignar manager: `OQ-01` sigue abierta). No se valida "el manager tiene rol Manager" en el dominio: exigiría cargar otro agregado dentro del agregado (rompe referencias por identidad, `CA-DOM-007`); el único llamador del MVP es el seeder, cuyos datos fija §3.6 | Cuando `OQ-01` se resuelva, la política de asignación (y su validación de rol, en el handler que corresponda) se construye sobre este mismo método |
| `D3` | **El seeder se invoca desde `DatabaseInitializer`, detrás del puerto existente** — `IDatabaseInitializer` no cambia y `Program.cs` no se toca | "Migrar y sembrar" son un solo hecho de arranque (`WBS` criterio de salida: *"dotnet run creates the database, seeds it"*; `SAD.md` §13: *"applies migrations, seeds, listens"*). Un segundo puerto `ISeeder` resuelto en `Program.cs` sería un mecanismo paralelo sin llamador adicional. Beneficio colateral deliberado: `SqliteDatabaseFixture` y `VacaFlowApiFactory` ejecutan el initializer real, así que **toda base de test queda sembrada automáticamente**, igual que producción | Si una historia futura necesitara arrancar sin seed (p. ej. un entorno demo limpio), se introduciría entonces la separación — el `DatabaseSeeder` ya es una clase aparte, extraerla al puerto es mecánico |
| `D4` | **El seeder reutiliza `ICredentialStore` + `IPasswordHasher` + `IIdGenerator` y las factorías de dominio**; solo las consultas de existencia y el `SaveChangesAsync` van directos al `DbContext` | Las contraseñas sembradas deben ser indistinguibles de un registro real (mismo formato de hash PBKDF2 autodescriptivo, mismo `CreatedAtUtc` por reloj inyectado — todo lo da `CredentialStore.Add`). No se reutiliza `IEmployeeRepository`: sus métodos por email existen, pero no hay consulta por `Code` de tipo de ausencia y el seeder es interno a Infrastructure — consultar el `DbContext` directamente es exactamente lo que la capa permite, sin engordar puertos de Application para un consumidor técnico (`CA-INF-004`) | Si `US-014` crea `IAbsenceTypeRepository` con una consulta equivalente, el seeder **no** migra a él: seguiría siendo tráfico interno de la capa |
| `D5` | **Un solo `SaveChangesAsync` al final del seed**, directo al `DbContext` (no `IUnitOfWork`) | El seed es una operación técnica atómica: o queda el estado completo de §3.6 o nada (media siembra deja una base engañosa). `IUnitOfWork` es el límite transaccional que decide *un caso de uso* y traduce violaciones a `Result` para el usuario; aquí no hay usuario — un fallo de arranque debe tumbar el arranque, ruidosamente (fail-fast, coherente con `AddInfrastructure` ante configuración ausente) | Ninguno plausible: el comportamiento externo (base sembrada o proceso caído) es el mismo por ambas vías |
| `D6` | **La idempotencia es fila a fila** (existe ese `Code` / ese `Email` → se salta esa fila), no "si hay algún dato, no sembrar" | Es literalmente lo que prescribe `SAD.md` §7.5: *"only when absent, matching on `AbsenceType.Code` and `Employee.Email`"*. Además es correcta ante el caso real: un usuario registró cuentas propias (`US-007` ya funciona) y luego reinicia — el seed debe completar lo que falte sin tocar lo suyo. Caso borde: si `employee@vacaflow.test` existiera por registro manual previo (sin manager), el seeder no lo repara (fuera de alcance §1.4); el reset documentado es la vía | Si se quisiera reconciliación, sería una historia nueva con semántica propia — no se adelanta |
| `S1` | Los `Id` sembrados son Guids generados en cada siembra (vía `IIdGenerator`), **no** constantes fijas | Ningún requisito pide ids estables entre entornos; la identidad externa de los datos sembrados son `Code` y `Email`, que es justo por donde se ancla la idempotencia. Ids fijos con `HasData` atarían los datos a la migración, y `HasData` no puede hashear contraseñas ni pasar por las factorías de dominio — por eso el seeder es imperativo | Si algún test futuro quisiera ids conocidos, los resuelve por email/código, como hace este plan |
| `S2` | La contraseña de **Ana** es `Employee123!` (la misma de Carlos) | §3.6 dice "Passwords: `Manager123!` and `Employee123!`" — dos contraseñas para tres cuentas; el bloque `TEST ACCOUNTS` de `S-01` publica exactamente dos pares de credenciales. La lectura natural: una para el rol Manager, otra para las cuentas Employee. El hash es por-cuenta (salt aleatorio), así que compartir contraseña no comparte hash | Si producto quisiera una tercera contraseña, es un literal en el seeder y una línea del README |
| `S3` | El `Name` sembrado coincide con el display name de §3.6 (`Vacation`, `Personal Leave`, `Sick Leave`) y `IsActive = true` en los tres | `AC1` exige "English display names and matching codes"; `US-014` listará "the active types", así que sembrar alguno inactivo rompería la pantalla `S-05` sin que ninguna historia lo pida | Ninguno plausible dentro del MVP |

---

## 6. Verificación end-to-end

| # | Paso | Resultado esperado |
|---|---|---|
| 1 | `dotnet build VacaFlow.slnx` | Compila con **0 warnings** (`TreatWarningsAsErrors`) |
| 2 | `dotnet test VacaFlow.slnx` | Suite completa verde, incluidos #14–#18 y los tests de arquitectura sin modificar |
| 3 | Borrar `vacaflow.db*` del proyecto Api; `dotnet run --project src/BigSolutions.VacaFlow.Api --urls http://localhost:5080` | Arranca; la base se crea con la tabla `AbsenceTypes` y queda sembrada |
| 4 | Inspeccionar la base (o vía tests): `AbsenceTypes` y `Employees` | 3 tipos con códigos/nombres de §3.6 · 3 empleados; Carlos y Ana → `ManagerId` de Laura · 3 filas en `UserAccounts` con hash PBKDF2, nunca texto claro |
| 5 | `POST /api/auth/login` con `manager@vacaflow.test` / `Manager123!` y con `employee@vacaflow.test` / `Employee123!` | `200` con nombre y rol correctos en ambos casos |
| 6 | Detener la API y volver a arrancar sobre la **misma** base; repetir el paso 4 | Conteos idénticos — cero duplicados (`AC3`) |
| 7 | Revisar `README.md` | Credenciales listadas, marcadas inequívocamente como no-productivas; avisos "*Applies once…*" retirados (`AC4`) |

---

## 7. Riesgos

| Riesgo | Mitigación |
|---|---|
| **Inflación de talla**: la historia declarada `S` carga con el Bloque A (entidad + migración) que el WBS imputaba al paquete 3.2 | Señalado explícitamente (D1) y delimitado en un bloque extraíble. El bloque es de forma fijada por el SAD — no hay diseño abierto, solo transcripción disciplinada |
| Las suites de test existentes asumían base vacía y ahora **toda** base de test arranca sembrada (D3) | Ítem #19: pasar las suites completas. Revisados los tests actuales: registran emails propios (no colisionan con `*.vacaflow.test`) y ninguno afirma conteos globales de tablas; si alguno rompiera, se ajusta el test, no el seeder |
| El seeder elude las factorías de dominio o inventa un hash propio "por simplicidad" | El plan lo prohíbe por diseño (#10: factorías + `ICredentialStore` + `IPasswordHasher`); el test #17d verifica el hash contra el verificador real y el #18 contra el sign-in real |
| Sembrar en el arranque encarece cada test funcional (3 hashes PBKDF2 de 210k iteraciones por factory) | Coste medido en centenas de ms por instancia de factory — aceptable; las factories ya se comparten por clase de test. Si doliera, la palanca sería una config de iteraciones para tests, decisión que no se toma hoy |
| Migración generada toca más de lo esperado (snapshot desincronizado) | Revisión del ítem #7: la migración debe contener únicamente `CreateTable AbsenceTypes` + índice único; cualquier otra cosa es una señal de parar |
| Concurrencia de arranque (dos procesos API sembrando a la vez) | Fuera de amenaza real en el MVP (proceso único, SQLite local); los índices únicos (`Email`, `Code`) convierten la carrera hipotética en fallo ruidoso de un proceso, nunca en duplicados |

---

Siguiente paso — implementación:

```
/user-story-implement ".claude/implementation plans/TE-003-plan.md"
```
