# Plan de implementación — `US-012` · Create-account screen

| Campo | Valor |
|---|---|
| Historia | `US-012` — Create-account screen |
| Épica | `EP-02` — Authentication and identity |
| Prioridad · Talla | **Must** · `M` |
| Pantalla | `S-02` (Create account) |
| Depende de | `US-007` (Create an account, backend — mergeado, PR #4/#1) — **sin precondiciones pendientes** |
| Traza | `Backlog.md` líneas 321–337 (`US-007`) y 376–379 (`US-012`) |
| Fuentes | `Backlog.md` v2.0 · código real verificado en `main` (commit `62c6b2e` — `US-033` mergeada, PR #28), archivo por archivo en `src/web/` y `src/BigSolutions.VacaFlow.Api/` |
| Repositorio | `C:\Users\avald\OneDrive\Work\VacaFlow\repositories\vacaflow` |
| Rama sugerida | `feat/us-012-create-account-screen`, creada desde `main` (`62c6b2e`) |
| Estado | Borrador presentado para aprobación — sin preguntas abiertas |

> **Este documento no implementa nada.** La implementación la ejecuta `/user-story-implement`.

---

## 1. Entendimiento

### 1.1 Contexto

`US-007` (Create an account, mergeada) ya implementó el registro de punta a punta en el backend: `POST /api/auth/register` (`AuthEndpoints.cs`), `RegisterEmployeeCommand` con validación estructural (`FullNameRequired`, `PasswordTooShort`, `PasswordTooLong`, `RoleInvalid`, todos `VF-VAL-001` con `Field` distinto), `RegisterEmployeeHandler` (unicidad de email case-insensitive → `VF-AUT-001` en el campo `email`, hash de contraseña, alta atómica de `Employee` + `UserAccount`, sign-in automático vía cookie) y el mapeo a `AuthenticatedUserResponse`. **Nada de esto cambia.**

Lo que falta es exclusivamente la pantalla `S-02`: **no existe ningún componente ni ruta de registro en `src/web`** (`src/web/app` solo tiene `(auth)/sign-in`, no `(auth)/sign-up` ni equivalente) y `lib/api.ts` no tiene una función `registerAccount`. Hoy la única forma de crear una cuenta es un `POST` manual a la API.

`US-013` (Sign-in screen) es la historia hermana que reconstruye `S-01` con su spec visual completo — **no entra en este plan**. La pantalla de sign-in actual (`src/web/app/(auth)/sign-in/page.tsx`) es, por su propio comentario, "provisional scaffolding, not the S-01 screen (destination: US-013)"; no se toca aquí, y por lo tanto el enlace `Don't have an account? Sign up` en `S-01` no se agrega en este plan — queda para cuando `US-013` reconstruya esa pantalla con su spec completo. `S-02` sí incluye su propio enlace de vuelta (`Already have an account? Sign in`), que si apunta a `/sign-in`.

**Historia solo Web.** El backend de `US-007` está completo y probado; ningún archivo de `Domain`, `Application`, `Infrastructure` ni `API` cambia.

### 1.2 Narrativa (verbatim, `US-007`)

> "As a new user, I want to register with my name, email, password and role, so that I can access VacaFlow with my own account."

### 1.3 Criterios de aceptación — verbatim

**De `US-007`** (`Backlog.md` líneas 327–337):

| # | Criterio |
|---|---|
| `AC1` | "Given valid data, when I `POST /auth/register`, then an `Employee` and a `UserAccount` are created and I am signed in directly, landing on `S-04` with the banner `Account created. Welcome to VacaFlow!`" |
| `AC2` | "Given an already-registered email, when I register, then `VF-AUT-001` is returned beneath the email field and no second account is created. Comparison is case-insensitive." |
| `AC3` | "Given any registration, when the database is inspected, then the password is stored hashed." *(ya satisfecho por el backend de `US-007`; sin ítem de este plan)* |
| `AC4` | "Given a name over 120 characters, a malformed email or a password under 8 characters, when I submit, then the corresponding validation message from §3.5 appears beneath that field." |

**Visual `S-02`** (`Backlog.md` líneas 332–337):

| # | Criterio |
|---|---|
| `V1` | "Auth card 420px per §3.3, subtitle `Create an account`." |
| `V2` | "Four groups in order: `Full name` (maxlength 120), `Email` (`type=email`), `Password` (`type=password`, helper `Minimum 8 characters.`), and a `fieldset` with legend `Role (for demo purposes)`." |
| `V3` | "The role control is two radio options side by side, each in a bordered 8px-radius box, equal width, labels `Employee` and `Manager`. `Employee` is preselected." |
| `V4` | "Primary full-width button `Create account`, disabled while saving." |
| `V5` | "Below the form: `Already have an account? Sign in`." |

**De `US-012`** (`Backlog.md` línea 379):

| # | Criterio |
|---|---|
| `AC5` | "Covered by the visual criteria of `US-007`, plus: every field has a visible `<label>` bound by `for`/`id`; errors render with `role=\"alert\"`; the entered values survive a rejected submission." |

### 1.4 Alcance

**Entra**: la pantalla `S-02` completa (`(auth)/sign-up`) con los 4 grupos de campos, el `fieldset` de rol, el botón primario y el enlace de vuelta; `registerAccount` en `lib/api.ts`; mapeo de errores de campo (`email`, `fullName`, `password`, `role`) siguiendo el mismo patrón ya establecido en `RequestForm.tsx`; banner `Account created. Welcome to VacaFlow!` al aterrizar en `/requests` tras el registro.

**No entra**

| Excluido | Por qué / destino |
|---|---|
| Backend de registro (`POST /auth/register`, `RegisterEmployeeHandler`, hashing, unicidad de email) | Ya existe y está probado (`US-007`) — esta historia es solo la pantalla que lo consume |
| Reconstrucción de `S-01` (sign-in) a su spec completo, incluido el enlace `Don't have an account? Sign up` | `US-013` — historia hermana, no planificada aquí |
| Bloque `TEST ACCOUNTS (NON-PRODUCTION)` | Pertenece a `S-01` (`US-008`/`US-013`), no a `S-02` |
| Tests automatizados de frontend | Sin runner en `src/web/package.json` — ratificación de la decisión ya tomada en `US-023 D7`…`US-033 D7` |

---

## 2. Cambios estructurales / de base

**No se requieren cambios de esquema, migraciones, configuración, variables de entorno, feature flags, dependencias nuevas (npm ni NuGet) ni cambios de contrato de API.** El endpoint `POST /api/auth/register` y el contrato `RegisterAccountContract` ya existen y no cambian.

---

## 3. Plan ordenado por dependencia

**Sin ítems de Domain, Application, Infrastructure ni API.** Todo es Web.

| # | Capa | Acción | Artefacto | Notas |
|---|---|---|---|---|
| 1 | Web | Modificar | `src/web/lib/api.ts` | Agregar `registerAccount(fullName, email, password, role)`, `POST /auth/register`, mismo patrón que `signIn` (usa el `request<T>` compartido; el `ApplicationError` con `field` ya viaja en `ApiError`, sin cambios en `lib/types.ts`) |
| 2 | Web | Crear | `src/web/app/(auth)/sign-up/page.tsx` | Pantalla `S-02` completa: auth card 420px (blanco, `--radius-card`, 40px padding, `--shadow-auth-card`), wordmark mono 22px/600, subtítulo 14px `Create an account`, 28px de gap antes del form; 4 grupos con gap 16px (`Full name` maxlength 120, `Email` type=email, `Password` type=password + helper `Minimum 8 characters.`, `fieldset`/`legend` `Role (for demo purposes)` con 2 radios `Employee`/`Manager` en cajas bordeadas 8px, `Employee` preseleccionado); botón primario full-width `Create account` deshabilitado mientras `saving`; enlace `Already have an account? Sign in` → `/sign-in`. Cada campo con `<label htmlFor>`/`id`, error de campo en `<p role="alert" id="...-error">` (mismo patrón de `RequestForm.tsx`'s `applyFieldError`), estado controlado que no se limpia en el `catch` (`AC5`, valores sobreviven una entrega rechazada). Al éxito: `setPendingNotification('Account created. Welcome to VacaFlow!')` + `router.push('/requests')` (`AC1`) |
| 3 | Web | Verificar | `cd src/web && npm run lint && npm run typecheck && npm run depcruise && npm run build` | Regresión estática |
| 4 | Test | Verificar | `dotnet build VacaFlow.slnx` + `dotnet test VacaFlow.slnx` | Regresión pura — cero cambios backend |
| 5 | E2E | Verificar | Verificación manual §6 con la app corriendo (API + `npm run dev`) | Único punto donde `AC1`, `AC2`, `AC4`, `AC5` y `V1`–`V5` son demostrables juntos |

**Dependencias:** 1 → 2 → {3, 4, 5}. **Ruta crítica:** 1 → 2 → 5.

---

## 4. Casos de uso y tabla de trazabilidad

**Cero casos de uso nuevos en Application.** El registro en sí (`RegisterEmployeeHandler`) es de `US-007` y no cambia.

| Historia | Criterio de aceptación (verbatim) | Ítems del plan que lo cubren | Cómo se verifica |
|---|---|---|---|
| `US-007` | "Given valid data, when I `POST /auth/register`, then an `Employee` and a `UserAccount` are created and I am signed in directly, landing on `S-04` with the banner `Account created. Welcome to VacaFlow!`" (`AC1`) | #1, #2 | §6 paso 2: registro con datos válidos → aterriza en `/requests` con el banner exacto |
| `US-007` | "Given an already-registered email, when I register, then `VF-AUT-001` is returned beneath the email field and no second account is created. Comparison is case-insensitive." (`AC2`) | #1, #2 | §6 paso 3: registrar con un email ya usado (y con capitalización distinta) → mensaje `An account with this email already exists.` bajo `Email` |
| `US-007` | "Given a name over 120 characters, a malformed email or a password under 8 characters, when I submit, then the corresponding validation message from §3.5 appears beneath that field." (`AC4`) | #1, #2 | §6 paso 4: 3 sub-casos (nombre >120, email inválido, password <8) → mensaje correcto bajo el campo correcto |
| `US-012` | "Covered by the visual criteria of `US-007`, plus: every field has a visible `<label>` bound by `for`/`id`; errors render with `role=\"alert\"`; the entered values survive a rejected submission." (`AC5`) | #2 | §6 paso 4 (inspección del árbol de accesibilidad) + paso 5: tras un error, los campos conservan lo tipeado |
| `US-012` (visual, `US-007` líneas 332–337) | "Auth card 420px… Four groups… role control… Primary full-width button… Below the form: `Already have an account? Sign in`." (`V1`–`V5`) | #2 | §6 paso 1: inspección visual/estructural de la pantalla contra cada punto |

**Conteo: 5 criterios funcionales (`AC1`, `AC2`, `AC4`, `AC5`) + 5 criterios visuales (`V1`–`V5`) = 10 · 10 cubiertos.** (`AC3`, ya satisfecho por el backend existente, no requiere ítem nuevo — declarado explícitamente en 1.3.)

---

## 5. Supuestos y decisiones

| # | Decisión | Justificación | Impacto si es incorrecta |
|---|---|---|---|
| `D1` | **Ruta `/sign-up`**, no `/register` | Espeja la convención ya usada por `/sign-in` (nombre de acción visible al usuario, no el nombre del endpoint) y coincide textualmente con el copy del enlace (`Sign up`) | Cosmético — renombrar la carpeta de ruta es un cambio local |
| `D2` | **No se toca `(auth)/sign-in/page.tsx`** para agregar el enlace `Don't have an account? Sign up` | Esa pantalla es un scaffold provisional explícitamente reservado para `US-013`; agregarle un enlace ahora sería tocar una pantalla fuera del alcance de esta historia por una sola línea, cuando `US-013` la reconstruye entera de todos modos | Hasta que `US-013` se implemente, `/sign-up` solo es alcanzable por URL directa — aceptable para una demo/MVP, y ya es el patrón de esta historia (US-012 no depende de US-013) |
| `D3` | **Reutilizar el patrón de mapeo de errores de campo de `RequestForm.tsx`** (`FieldErrors` como diccionario, `role="alert"` por campo, sin limpiar el estado del formulario en el `catch`) en vez de crear un patrón nuevo | Ya es el patrón establecido en el único otro formulario complejo de la app; reutilizar evita divergencia de estilo | N/A — consistente con el resto del código |
| `D4` | **El `role` viaja como `"Employee"`/`"Manager"` literal** (values de los radios), no como número | `RegisterEmployeeCommand.TryParseRole` usa `Enum.TryParse(role, ignoreCase: true, ...)` sobre `EmployeeRole { Employee = 1, Manager = 2 }` — el string es lo que el backend espera | Si fuera incorrecto, el registro fallaría con `VF-VAL-001`/`role` inmediatamente, visible en la primera verificación E2E |
| `D5` | **Tras el registro exitoso, la respuesta del `AuthenticatedUser` se descarta** (igual que `signIn` en el sign-in actual) — la navegación a `/requests` y el refetch de `getMe()` en el layout del shell ya resuelven quién es el usuario | Mismo patrón ya usado en `(auth)/sign-in/page.tsx`; no hay necesidad de un estado global de usuario en esta capa | N/A — patrón ya validado en producción por `US-008` |
| `D6` | **Sin tests automatizados de frontend** | Ratificación de `US-023 D7`…`US-033 D7`: sigue sin existir runner en `src/web/package.json` | Si se estrena runner, se añade como ítem previo |
| `S1` | **La rama se crea desde `main` (`62c6b2e`) directamente** | `US-033` mergeada (PR #28); `origin/main` al día | Ninguno |

---

## 6. Verificación end-to-end

Con la API corriendo y `npm run dev` en `src/web/`, usando cuentas nuevas (no las sembradas por `TE-003`, para no chocar con `VF-AUT-001` de forma no intencional).

| # | Paso | Resultado esperado |
|---|---|---|
| 1 | Navegar a `/sign-up` — **`V1`–`V5`** | Card blanca 420px centrada, wordmark `VacaFlow`, subtítulo `Create an account`; 4 grupos en el orden `Full name`/`Email`/`Password` (con helper `Minimum 8 characters.`)/`fieldset` `Role (for demo purposes)` con 2 radios `Employee` (preseleccionado) y `Manager` en cajas bordeadas; botón `Create account` full-width; enlace `Already have an account? Sign in` |
| 2 | Completar con datos válidos (nombre, email nuevo, password ≥8 caracteres, rol `Employee` o `Manager`) y enviar — **`AC1`** | Redirige a `/requests` con el banner `Account created. Welcome to VacaFlow!`; el header del shell muestra el nombre y rol recién registrados (confirmando el sign-in automático) |
| 3 | Repetir el registro con el mismo email (probar también con mayúsculas distintas) — **`AC2`** | Mensaje `An account with this email already exists.` bajo `Email`, formulario permanece en `/sign-up`, ninguna cuenta nueva creada |
| 4 | Tres sub-casos por separado — **`AC4`**: (a) nombre >120 caracteres, (b) email con formato inválido, (c) password <8 caracteres | Cada uno muestra su mensaje exacto de §3.5 bajo el campo correspondiente, con `role="alert"` (confirmar en el árbol de accesibilidad) |
| 5 | Tras cualquiera de los errores del paso 3/4, inspeccionar el formulario — **`AC5`** | Los valores tipeados en los demás campos siguen presentes (no se limpiaron); cada `<label>` sigue asociado a su control por `for`/`id` |
| 6 | Regresión: `dotnet build` + `dotnet test`; `npm run lint`/`typecheck`/`depcruise`/`build` | Todo verde, 0 warnings |

---

## 7. Riesgos y preguntas abiertas

**Sin preguntas abiertas.** `D1` y `D2` son decisiones de nomenclatura/alcance directamente justificadas por el código y el spec existentes, no lecturas ambiguas del requerimiento.

| Riesgo | Mitigación |
|---|---|
| Confundir esta historia con una reconstrucción de `S-01` | El reporte final declara explícitamente que `(auth)/sign-in/page.tsx` no se tocó — eso es `US-013` |
| El mensaje de `VF-AUT-001` podría no distinguirse visualmente de una validación estructural si no se prueba explícitamente la insensibilidad a mayúsculas | Paso 3 de §6 prueba ambos casos (mismo email exacto y con capitalización distinta) |

---

Siguiente paso — implementación:

```
/user-story-implement ".claude/implementation plans/US-012-plan.md"
```
