# Plan de implementación — `US-013` · Sign-in screen

| Campo | Valor |
|---|---|
| Historia | `US-013` — Sign-in screen |
| Épica | `EP-02` — Authentication and identity |
| Prioridad · Talla | **Must** · `M` |
| Pantalla | `S-01` (Sign in) |
| Depende de | `US-008` (Sign in, backend — mergeado hace tiempo), `US-010` (Retrieve the current user — mergeado) — **sin precondiciones pendientes** |
| Traza | `Backlog.md` líneas 339–351 (`US-008`) y 381–384 (`US-013`) |
| Fuentes | `Backlog.md` v2.0 · código real verificado en `main` (commit `cf7676e` — `US-012` mergeada, PR #29), archivo por archivo en `src/web/` |
| Repositorio | `C:\Users\avald\OneDrive\Work\VacaFlow\repositories\vacaflow` |
| Rama sugerida | `feat/us-013-sign-in-screen`, creada desde `main` (`cf7676e`) |
| Estado | Borrador presentado para aprobación — sin preguntas abiertas |

> **Este documento no implementa nada.** La implementación la ejecuta `/user-story-implement`.

---

## 1. Entendimiento

### 1.1 Contexto

`US-008` (Sign in, backend) ya implementó de punta a punta el inicio de sesión: `POST /api/auth/login` (`AuthEndpoints.cs`), `SignInHandler` (verificación de credenciales *timing-safe*, `VF-AUT-002` sin distinguir email inexistente de password incorrecto, `VF-AUT-003` para cuenta inactiva), cookie de sesión. **Nada de esto cambia.**

La pantalla actual (`src/web/app/(auth)/sign-in/page.tsx`) es, por su propio comentario, *"Provisional scaffolding, not the S-01 screen (destination: US-013)"* — existía únicamente para poder establecer una sesión durante `US-017`. Comparada contra el spec de `S-01` (`US-008` líneas 346–351), le faltan: la card visual completa (400px, wordmark, subtítulo), `autocomplete` en los campos, el banner `Signed in as {name}.` al aterrizar, el vaciado del campo de password tras un error, el enlace `Don't have an account? Sign up` y el bloque `TEST ACCOUNTS (NON-PRODUCTION)`.

`US-012` (Create-account screen, mergeada esta sesión) ya construyó `/sign-up` con la card visual completa (420px) y ya apunta de vuelta a `/sign-in` con `Already have an account? Sign in` — este plan construye el enlace recíproco y reutiliza exactamente el mismo patrón de card (mismo `--shadow-auth-card`, `--radius-card`, wordmark mono, estructura general) para no reinventar estilo entre las dos pantallas hermanas.

La parte "plus" de `US-013` — *"after signing in, the header of `S-03` shows the current user's name and role on every screen"* — **ya está satisfecha** por `AppHeader.tsx` (`US-030`, mergeada), que muestra nombre y rol del usuario autenticado en cada pantalla del shell. Se declara explícitamente, sin ítem de plan nuevo.

**Historia solo Web.** El backend de `US-008`/`US-010` está completo y probado; ningún archivo de `Domain`, `Application`, `Infrastructure` ni `API` cambia.

### 1.2 Narrativa

`US-013` no trae narrativa propia — es *"Covered by the visual criteria of `US-008`"*. La narrativa de referencia (`US-008`, implícita en su comportamiento) es: un usuario con cuenta existente inicia sesión con email y password para acceder a VacaFlow.

### 1.3 Criterios de aceptación — verbatim

**De `US-008`** (`Backlog.md` líneas 342–351):

| # | Criterio |
|---|---|
| `AC1` | "Given correct credentials, when I `POST /auth/login`, then a session is established and I land on `S-04` with the banner `Signed in as {name}.`" |
| `AC2` | "Given a wrong password or unknown email, when I sign in, then `VF-AUT-002` appears in an alert block above the form, the email is preserved and the password field is cleared." |
| `AC3` | "Given an inactive employee, when I sign in, then `VF-AUT-003` is returned." *(ya satisfecho por el backend de `US-008`; no verificable vía UI sin un empleado sembrado inactivo — ver §7)* |

**Visual `S-01`** (`Backlog.md` líneas 346–351):

| # | Criterio |
|---|---|
| `V1` | "Auth card 400px, subtitle `Absence request management`." |
| `V2` | "Two fields with `autocomplete` set to `email` and `current-password`; primary full-width `Sign in`." |
| `V3` | "Below: `Don't have an account? Sign up`." |
| `V4` | "Bottom block separated by a top border: an 11px uppercase letter-spaced heading `TEST ACCOUNTS (NON-PRODUCTION)` and the two credential pairs in mono 12px." |
| `V5` | "The error block, when present, sits between the subtitle and the form with `role=\"alert\"`." |

**De `US-013`** (`Backlog.md` línea 384):

| # | Criterio |
|---|---|
| `AC4` | "…after signing in, the header of `S-03` shows the current user's name and role on every screen." *(ya satisfecho por `AppHeader.tsx`/`US-030` — sin ítem de plan nuevo, ver 1.4)* |

### 1.4 Alcance

**Entra**: reconstrucción completa de `(auth)/sign-in/page.tsx` a su spec `S-01` — card 400px, `autocomplete`, banner de éxito, vaciado de password en error, enlace a `/sign-up`, bloque de cuentas de prueba.

**No entra**

| Excluido | Por qué / destino |
|---|---|
| Backend de sign-in (`POST /auth/login`, `SignInHandler`, cookie) | Ya existe y está probado (`US-008`) — esta historia es solo la pantalla que lo consume |
| Header del shell mostrando nombre/rol (`AC4`) | Ya construido por `US-030`/`AppHeader.tsx` — se verifica, no se construye |
| Verificación de `AC3` (cuenta inactiva) vía navegador | Ningún empleado sembrado está inactivo (`DatabaseSeeder.cs`); ya cubierto por tests backend existentes de `US-008`. Sembrar una cuenta inactiva de prueba está fuera del alcance de esta historia (tocaría `TE-003`) |
| Tests automatizados de frontend | Sin runner en `src/web/package.json` — ratificación de la decisión ya tomada en `US-012 D6` y anteriores |

---

## 2. Cambios estructurales / de base

**No se requieren cambios de esquema, migraciones, configuración, variables de entorno, feature flags, dependencias nuevas (npm ni NuGet) ni cambios de contrato de API.** El endpoint `POST /api/auth/login` y `signIn()` en `lib/api.ts` ya existen y no cambian.

---

## 3. Plan ordenado por dependencia

**Sin ítems de Domain, Application, Infrastructure ni API.** Todo es Web.

| # | Capa | Acción | Artefacto | Notas |
|---|---|---|---|---|
| 1 | Web | Reescribir | `src/web/app/(auth)/sign-in/page.tsx` | Card 400px (mismo patrón visual que `sign-up/page.tsx`: `--color-surface`, `--radius-card`, `--shadow-auth-card`, wordmark mono 22px/600), subtítulo `Absence request management` (`V1`); campos `Email`/`Password` con `autocomplete="email"`/`autocomplete="current-password"`, botón primario full-width `Sign in` (`V2`); alerta de error entre el subtítulo y el form, `role="alert"` (`V5`); en catch de `ApplicationError`: mensaje en el alert, `setPassword('')` (vacía password, preserva email — `AC2`); en éxito: `setPendingNotification(\`Signed in as ${user.fullName}.\`)` + `router.push('/requests')` (`AC1`, usa el valor ya devuelto por `signIn()`, hoy descartado); enlace `Don't have an account? Sign up` → `/sign-up` vía `next/link` (`V3`); bloque inferior separado por borde superior, heading 11px mayúsculas con letter-spacing `TEST ACCOUNTS (NON-PRODUCTION)`, 2 pares de credenciales en mono 12px — `manager@vacaflow.test` / `Manager123!` y `employee@vacaflow.test` / `Employee123!` (`V4`, valores de `README.md`/`DatabaseSeeder.cs`) |
| 2 | Web | Verificar | `cd src/web && npm run lint && npm run typecheck && npm run depcruise && npm run build` | Regresión estática |
| 3 | Test | Verificar | `dotnet build VacaFlow.slnx` + `dotnet test VacaFlow.slnx` | Regresión pura — cero cambios backend |
| 4 | E2E | Verificar | Verificación manual §6 con la app corriendo (API + `npm run dev`) | Único punto donde `AC1`, `AC2`, `V1`–`V5` son demostrables juntos; `AC3` y `AC4` se verifican por lectura de código (ver §6) |

**Dependencias:** 1 → {2, 3, 4}. **Ruta crítica:** 1 → 4.

---

## 4. Casos de uso y tabla de trazabilidad

**Cero casos de uso nuevos en Application.** El sign-in en sí (`SignInHandler`) es de `US-008` y no cambia.

| Historia | Criterio de aceptación (verbatim) | Ítems del plan que lo cubren | Cómo se verifica |
|---|---|---|---|
| `US-008` | "Given correct credentials, when I `POST /auth/login`, then a session is established and I land on `S-04` with the banner `Signed in as {name}.`" (`AC1`) | #1 | §6 paso 2: sign-in con una cuenta sembrada → aterriza en `/requests` con el banner exacto, nombre real interpolado |
| `US-008` | "Given a wrong password or unknown email, when I sign in, then `VF-AUT-002` appears in an alert block above the form, the email is preserved and the password field is cleared." (`AC2`) | #1 | §6 paso 3: password incorrecto → mensaje `The email or password is incorrect.`, email intacto, password vacío |
| `US-008` | "Given an inactive employee, when I sign in, then `VF-AUT-003` is returned." (`AC3`) | — (backend ya probado, `US-008`) | Verificado por lectura: `SignInHandler` ya retorna `EmployeeErrors.AccountInactive`; sin cuenta inactiva sembrada para probar vía UI — declarado en §7 |
| `US-008` (visual) | "Auth card 400px, subtitle `Absence request management`… autocomplete… `Don't have an account? Sign up`… `TEST ACCOUNTS (NON-PRODUCTION)`… error block `role=\"alert\"`." (`V1`–`V5`) | #1 | §6 paso 1: inspección visual/estructural de la pantalla contra cada punto |
| `US-013` | "…the header of `S-03` shows the current user's name and role on every screen." (`AC4`) | — (ya construido, `US-030`) | §6 paso 2 (parte final): tras el sign-in, el header ya muestra nombre/rol — reconfirmado, no reconstruido |

**Conteo: 4 criterios funcionales (`AC1`–`AC4`) + 5 visuales (`V1`–`V5`) = 9 · 9 cubiertos** (2 de ellos, `AC3` y `AC4`, cubiertos por trabajo ya existente, declarado explícitamente en 1.4).

---

## 5. Supuestos y decisiones

| # | Decisión | Justificación | Impacto si es incorrecta |
|---|---|---|---|
| `D1` | **Las "dos parejas de credenciales" del bloque de prueba son manager + employee** (`manager@vacaflow.test`/`Manager123!` y `employee@vacaflow.test`/`Employee123!`), no las 3 cuentas sembradas | El spec dice literalmente "the two credential pairs" (singular por rol); `README.md` lista 3 cuentas pero la tercera (`ana@vacaflow.test`) es un segundo *Employee* bajo el mismo manager, no un rol distinto — no aporta una pareja nueva que mostrar | Cosmético — agregar la tercera es un cambio de una línea si se pidiera |
| `D2` | **Reutilizar el mismo patrón visual de card que `sign-up/page.tsx`** (mismos tokens, misma estructura de wordmark/subtítulo/form) en vez de definir un layout nuevo | Ambas son pantallas hermanas del mismo componente de patrón (`S-01`/`S-02`, `Backlog.md` §3.3 "Auth card") — reutilizar evita divergencia visual entre ellas | N/A — consistente por diseño |
| `D3` | **`AC3` (cuenta inactiva) no se verifica vía navegador** | Ningún empleado sembrado (`TE-003`/`DatabaseSeeder.cs`) está inactivo; sembrar uno de prueba es cambio de alcance de `TE-003`, no de esta historia. El comportamiento del backend ya está probado por los tests existentes de `US-008` (`SignInHandlerTests`) | Ninguno — declarado explícitamente como no verificado en el reporte, no omitido en silencio |
| `D4` | **`AC4` no requiere ítem de plan** — ya construido por `AppHeader.tsx`/`US-030` | Verificado leyendo el código actual: el header ya interpola nombre y rol del usuario autenticado en cada pantalla del shell | N/A — hecho verificado en el código actual |
| `D5` | **El `catch` no distingue `VF-AUT-002` de `VF-AUT-003`** para el manejo de UI — ambos son errores sin `field` y van al mismo bloque de alerta general | El propio spec exige que `VF-AUT-002` sea indistinguible entre email desconocido y password incorrecto (`FR-AUT-006`); `VF-AUT-003` tampoco tiene `field`. Mismo patrón que `applyFieldError`/`pickFieldError` ya usado en el resto de la app: sin `field`, va al alert general | N/A — ambos mensajes ya vienen distintos y correctos desde el backend |
| `D6` | **Sin tests automatizados de frontend** | Ratificación de `US-012 D6`…`US-033 D6`: sigue sin existir runner en `src/web/package.json` | Si se estrena runner, se añade como ítem previo |
| `S1` | **La rama se crea desde `main` (`cf7676e`) directamente** | `US-012` mergeada (PR #29); `origin/main` al día | Ninguno |

---

## 6. Verificación end-to-end

Con la API corriendo y `npm run dev` en `src/web/`, usando las cuentas sembradas (`manager@vacaflow.test`/`Manager123!`, `employee@vacaflow.test`/`Employee123!`).

| # | Paso | Resultado esperado |
|---|---|---|
| 1 | Navegar a `/sign-in` (con sesión cerrada) — **`V1`–`V5`** | Card blanca 400px centrada, wordmark `VacaFlow`, subtítulo `Absence request management`; campos `Email`/`Password` (inspeccionar `autocomplete` en el árbol de accesibilidad/DOM); botón `Sign in` full-width; enlace `Don't have an account? Sign up`; bloque inferior con borde superior, heading `TEST ACCOUNTS (NON-PRODUCTION)` y las 2 parejas de credenciales |
| 2 | Sign in con `employee@vacaflow.test`/`Employee123!` — **`AC1`, `AC4`** | Aterriza en `/requests` con el banner `Signed in as Carlos Ruiz.`; el header del shell muestra `Carlos Ruiz` / `Employee` |
| 3 | Sign out; sign in con email correcto y password incorrecto — **`AC2`** | Alerta `The email or password is incorrect.` entre el subtítulo y el form, con `role="alert"`; el campo `Email` conserva el valor tipeado; el campo `Password` queda vacío |
| 4 | Click en `Don't have an account? Sign up` — **`V3`** | Navega a `/sign-up` sin recarga completa de página (confirmar vía `next/link`, mismo patrón que el enlace recíproco de `US-012`) |
| 5 | Regresión: `dotnet build` + `dotnet test`; `npm run lint`/`typecheck`/`depcruise`/`build` | Todo verde, 0 warnings |

**No verificado vía navegador:** `AC3` (cuenta inactiva) — sin empleado sembrado inactivo; comportamiento ya cubierto por los tests unitarios/funcionales existentes de `US-008` (`SignInHandlerTests`, `AuthEndpointTests`), que no se re-ejecutan como ítem nuevo porque no cambian.

---

## 7. Riesgos y preguntas abiertas

**Sin preguntas abiertas.** `D1` y `D2` son lecturas directas del spec y del patrón ya construido en `US-012`, no ambigüedades genuinas.

| Riesgo | Mitigación |
|---|---|
| Confundir esta historia con un cambio al backend de `US-008` | El reporte final declara explícitamente que `SignInHandler`/`AuthEndpoints.cs` no se tocaron |
| El bloque `TEST ACCOUNTS` sobrevive a un build de producción real (`FUT-30`) | Fuera del alcance de esta historia — ya señalado en el backlog como ítem futuro explícito, no una omisión de esta sesión |

---

Siguiente paso — implementación:

```
/user-story-implement ".claude/implementation plans/US-013-plan.md"
```
