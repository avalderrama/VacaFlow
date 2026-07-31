# Plan de implementación — `US-019` · Cancel a request

| Campo | Valor |
|---|---|
| Historia | `US-019` — Cancel a request |
| Épica | `EP-06` — Request lifecycle |
| Prioridad · Talla | **Must** · `S` |
| Pantallas | `S-04` (My Requests — fila, de `US-024`) · `S-06` (Request detail — de `US-025`) · `S-08` (Cancel confirmation modal — de `US-033`). **Las tres superficies se difieren a sus historias dueñas** (ver `D2`/`D3`) |
| Depende de | `US-018` (**implementada en `feat/us-018-submit-request`, commit `3fb6e3d` — aún NO mergeada a `main` al escribir este plan**) · `US-033` (modal de confirmación — dependencia **solo web**, no bloquea el backend, ver `D3`) |
| Trazas | `SC-06` · `RULE-04` · `FR-LFC-005` (Cancel, transiciones `T2`/`T5`) · `FR-LFC-006` (ownership) · `FRD.md` §4.1 (estados finales), §4.2, §6.3 (fila 10), §7 · `SAD.md` §5.3 (sketch de `Cancel`), §6.1 (`CancelRequestHandler`), §18 (`ADR-012`) · `Backlog.md` §EP-06 `US-019`, §3.5 (`Request cancelled.`) |
| Fuentes | `Backlog.md` v2.0 · `FRD.md` · `SAD.md` v2.0 · código real verificado en `src/` y `tests/` (rama `feat/us-018-submit-request`, commit `3fb6e3d`) |
| Repositorio | `C:\Users\avald\OneDrive\Work\VacaFlow\repositories\vacaflow` |
| Rama sugerida | `feat/us-019-cancel-request`, creada **desde `main` una vez mergeado el PR de `US-018`** (hoy `main` está en `4069a94`, sin `Submit`; crear la rama antes del merge dejaría la dependencia rota) |
| Estado | Borrador presentado para aprobación (sesión de planificación delegada; decisiones de arquitecto en §5 — revisar la pregunta abierta de §7 antes de implementar) |

> **Este documento no implementa nada.** La implementación la ejecuta `/user-story-implement`.

---

## 1. Entendimiento

### 1.1 Contexto — por qué ahora y qué hay ya

`US-019` es la **segunda y tercera transición** del agregado `Request`: `T2` (`Draft` → `Cancelled`) y `T5` (`Submitted` → `Cancelled`), ambas del dueño (`FRD.md` §4.2). Cierra el ciclo que el empleado controla por sí mismo; las transiciones de manager (`T3`/`T4`, `Decide`) llegan con `US-021`/`US-022`.

Hallazgos de grounding, todos verificados contra el código real (commit `3fb6e3d`):

- **`Request` está listo para recibir `Cancel`**: su doc-comment ya anota *"Cancel/Decide arrive with their own stories (US-019/US-021)"*. `ClosedAtUtc` está **declarado pero jamás asignado** (nullable; columna `ClosedAtUtc` ya creada por la migración `AddRequests` `20260731004548` y mapeada en `RequestConfiguration.cs:61`) — esta historia es su **primer uso** (`FRD.md` §3.4: "Set when the request reaches a final state"). Cero cambios de esquema.
- **`RequestState`** (verificado): `Draft, Submitted, Approved, Rejected, Cancelled` — los `ToString()` coinciden con los labels de §3.4, que es lo que el mensaje interpolado necesita.
- **`RequestErrors` ya lo tiene todo**: la factory `InvalidTransition(RequestState from, RequestState to)` (`VF-REQ-005`, creada por `US-018`) fue diseñada genérica precisamente para esto — el propio plan de `US-018` (§7, riesgos) lo dejó anotado: *"US-019 (Cancel) reutilizará InvalidTransition con otro destino (Cancelled) — la factory ya es genérica, cero retoque previsto"*. `NotOwner` (`VF-REQ-004`) y `NotFound` (`VF-REQ-006`) se reutilizan tal cual. **Esta historia no añade ni un error nuevo** (ver `D5`).
- **`ErrorStatusMap` no cambia**: `VF-REQ-005 → 409`, `VF-REQ-004 → 403` y `VF-REQ-006 → 404` ya están mapeados (verificado en `ErrorStatusMap.cs`). El pin `[InlineData("VF-REQ-005", "StatusCodes.Status409Conflict")]` de `SourceRuleTests.cs:211` ya existe. **Cero ítems de API de mapeo.**
- **`IRequestRepository` no crece**: `GetByIdAsync` (US-016) es todo lo que Cancel necesita — cero cambios en el puerto, cero en `RequestRepository`, cero en Infrastructure (`CA-INF-004`, cuarta historia consecutiva).
- **El patrón está fijado por `SubmitRequestHandler`** (US-018, mismo caso: operación por id sin cuerpo): handler `sealed` sin command record, secuencia cargar → `NotFound` → dueño vs `ICurrentUser.EmployeeId` → método de dominio → `SaveChangesAsync`. `SAD.md` §6.1 ya lista el nombre `CancelRequestHandler.cs`.
- **El endpoint está decidido por el FRD**: `POST /requests/{id}/cancel` (§6.3 fila 10, "Request: empty"). Con la convención `/api`: `POST /api/requests/{id}/cancel`, en el grupo existente de `RequestEndpoints`. Éxito `204` vía `ToHttpResult()` (`ADR-012`).
- **`SAD.md` §5.3 trae el sketch exacto** de `Cancel(DateTime nowUtc)`: guarda `State is not (Draft or Submitted)` → `InvalidTransition(State, Cancelled)`; éxito → `State = Cancelled`, `ClosedAtUtc = nowUtc`, `UpdatedAtUtc = nowUtc`. **Sin parámetro `today`**: el contrato de errores de `POST …/cancel` (§6.3) **no lista `VF-REQ-002`** — cancelar no re-valida fechas ni contenido (ver `D4`).
- **"Final state" tiene definición formal** (`FRD.md` §4.1): `Approved`, `Rejected` y `Cancelled` son finales; `Draft` y `Submitted` no. La guarda del sketch (`not (Draft or Submitted)`) y "estado final" son exactamente el mismo conjunto — no hay que inventar semántica.
- **Web**: `src/web` sigue sin lista en `/requests` (placeholder de `US-017`) y **sin `submitRequest()` en `lib/api.ts`** — el diferimiento `D2` de `US-018` se mantuvo en la implementación. No hay fila donde poner `Cancel`, no hay detalle `S-06` real (la página `[id]` es el formulario de edición de `US-017`), no hay modal. Ver `D2`.
- **Precedente de deuda de test**: `Approved`/`Rejected` son **inalcanzables por la API pública del agregado** hasta que `US-021` traiga `Decide` — misma situación que `US-016` `D7` tuvo con `Submitted`. La cobertura de "cancel desde estado final" se hace con `Cancelled` (alcanzable vía el propio `Cancel`) y la deuda de `Approved`/`Rejected` se anota para `US-021` (ver `D8`).

### 1.2 Narrativa

El backlog formula `US-019` por criterios. La intención la fijan `EP-06`, `FR-LFC-005` y `FR-LFC-006`: el dueño retira su propia solicitud mientras aún está en su mano (`Draft` o `Submitted`), llevándola a `Cancelled` y sellando `ClosedAtUtc`; una solicitud ya decidida (o ya cancelada) es intocable (`VF-REQ-005`); nadie cancela solicitudes ajenas (`RULE-04`, `VF-REQ-004`). La confirmación previa ("when I confirm cancellation") es la responsabilidad del modal `S-08` (`US-033`).

### 1.3 Criterios de aceptación — verbatim (`Backlog.md` §EP-06 · `US-019`)

| # | Criterio |
|---|---|
| `AC1` | "Given my own request in `Draft` or `Submitted`, when I confirm cancellation, then it becomes `Cancelled` and the banner reads `Request cancelled.`" |
| `AC2` | "Given a request in a final state, when cancellation is attempted, then `VF-REQ-005` is returned." |
| `AC3` | "Given another employee's request, when cancellation is attempted, then `VF-REQ-004` is returned." |
| `AC4` | "Given a `Submitted` request opened as detail, when `S-06` renders, then a `Cancel request` button appears pushed to the right of the action row." |

Reglas y errores implicados, verbatim del catálogo (`FRD.md` §7 = `Backlog.md` §3.5) — **todos ya existentes, ninguno nuevo**:

| Código | HTTP | Mensaje | Regla | Estado en el código |
|---|---|---|---|---|
| `VF-REQ-005` | 409 | `This request cannot move from {current} to {target}.` | `FR-LFC-005` / §4.2 | Factory `InvalidTransition` (US-018) — se reutiliza con destino `Cancelled` |
| `VF-REQ-004` | 403 | `You can only act on your own requests.` | `RULE-04` (`FR-LFC-006`) | Declarado y mapeado — se reutiliza |
| `VF-REQ-006` | 404 | `The request was not found.` | contrato del endpoint (§6.3) | Declarado y mapeado — se reutiliza |
| `VF-AUT-004` | 401 | `You must be signed in to perform this action.` | `FR-AUT-011` | Resuelto por `TE-011` |

Contrato del endpoint, verbatim de `FRD.md` §6.3 fila 10 (con el delta `ADR-012` sobre el cuerpo de éxito — ver `D1`):

> **`POST /requests/{id}/cancel`** · Request: empty · Success `200`: the updated request · Errors: `VF-REQ-004` `403` · `VF-REQ-006` `404` · `VF-REQ-005` `409`

Nótese la diferencia con submit: **no hay `VF-REQ-002`** — cancelar no re-valida la fecha de inicio (`D4`).

Banner de éxito (§3.5, verbatim): `Request cancelled.` — su render pertenece a la superficie diferida (`D2`).

### 1.4 Alcance

**Entra**

- El comportamiento `Cancel(DateTime nowUtc)` en el agregado `Request` (transiciones `T2`/`T5`: guarda de estado → `State = Cancelled`, **primer sellado de `ClosedAtUtc`**, `UpdatedAtUtc`), con la forma exacta de `SAD.md` §5.3.
- El caso de uso `CancelRequestHandler` (sin command record — sin cuerpo, precedente `SubmitRequestHandler`), reutilizando `IRequestRepository.GetByIdAsync` **sin crecer el puerto**.
- API: `POST /api/requests/{id:guid}/cancel` → `204` (`ADR-012`). Sin cambios en `ErrorStatusMap` ni en el pin de `SourceRuleTests` (todo ya mapeado).
- Tests: unitarios de dominio (`Cancel` desde `Draft`, desde `Submitted`, desde `Cancelled` con mensaje interpolado exacto; `ClosedAtUtc` sellado), unitarios del handler, funcionales del endpoint (ambas transiciones, doble cancel, no-dueño, 404, 401).

**No entra**

| Excluido | Por qué / destino |
|---|---|
| Botón `Cancel` en la fila de `S-04`, reload de la lista y banner `Request cancelled.` | `US-024` (My Requests screen) es la dueña de la fila y su matriz de acciones (`Draft` → `Edit · Submit · Cancel`; `Submitted` → `View · Cancel`) — y **depende de `US-019`**. Mismo patrón de `US-018` `D2`. Ver `D2` |
| Botón `Cancel request` en el detalle `S-06` (**`AC4`**) | `S-06` como pantalla real es de **`US-025`**, cuyo criterio dice verbatim: *"Given a `Submitted` request, when `S-06` renders, then no decision block appears and `Cancel request` is available."* — el mismo botón que `AC4` pide, con dueño nominal posterior. Hoy no existe `S-06`: la página `[id]` es el formulario de `US-017`. Ver `D2` — diferimiento limpio, no huérfano |
| Modal de confirmación `S-08` | Es **`US-033` entera** (su historia propia, `Depends on: US-030` — el shell). El "when I confirm cancellation" de `AC1` es esa UI. Ver `D3` |
| `cancelRequest()` en `src/web/lib/api.ts` | Código muerto hasta `US-024`/`US-033` (verificado: ni `submitRequest()` se escribió por adelantado en `US-018`) |
| `Decide`/`Approval`/`ApprovalPolicy`, errores `VF-DEC-*` | `US-021`/`US-022` |
| Re-validación de fechas o contenido al cancelar | El contrato §6.3 no lista `VF-REQ-002` para cancel; `FR-LFC-005` solo pide la transición (`D4`) |
| Concurrencia optimista (doble cancel simultáneo) | Sin requisito (`TC-06`); el segundo recibe `VF-REQ-005` por la guarda (`S2`) |

---

## 2. Cambios estructurales / de base

**No se requieren cambios de esquema, migraciones, configuración, variables de entorno, permisos, feature flags ni dependencias nuevas.** `Cancel` escribe únicamente columnas que la migración `AddRequests` (`20260731004548`) ya creó — `State`, `ClosedAtUtc` (nullable, verificada en `RequestConfiguration.cs:61` y en el snapshot) y `UpdatedAtUtc`. `ErrorStatusMap` tampoco cambia (a diferencia de `US-018`): todos los códigos del contrato ya están mapeados.

---

## 3. Plan ordenado por dependencia

De adentro hacia afuera (Domain → Application → API → tests). Sin ítems de Infrastructure ni de Web (§1.4, `D2`). Sin ítems en `RequestErrors` ni `ErrorStatusMap` ni `SourceRuleTests` (todo reutilizado — primera historia de `EP-06` que no toca el catálogo de errores).

| # | Capa | Acción | Artefacto | Notas |
|---|---|---|---|---|
| 1 | Domain | Modificar | `src/BigSolutions.VacaFlow.Domain/Requests/Request.cs` | Añadir `public Result Cancel(DateTime nowUtc)` (forma de `SAD.md` §5.3, transiciones `T2`/`T5`): (1) `State is not (RequestState.Draft or RequestState.Submitted)` → `RequestErrors.InvalidTransition(State, RequestState.Cancelled)` (`FR-LFC-005` — la guarda es literalmente el complemento del conjunto "final" de §4.1); éxito → `State = RequestState.Cancelled`, `ClosedAtUtc = nowUtc` (**primera asignación de la propiedad en el codebase**), `UpdatedAtUtc = nowUtc`. `SubmittedAtUtc` queda como esté (un `Submitted` cancelado conserva su sello de envío — historial honesto). **Sin parámetro `today`** — cancel no re-valida fechas (`D4`). Doc-comment: ownership (`RULE-04`) es responsabilidad del handler, como en `Submit`; actualizar el doc-comment de la clase (solo `Decide` queda pendiente, `US-021`) |
| 2 | Application | Crear | `src/BigSolutions.VacaFlow.Application/Requests/CancelRequestHandler.cs` | `public sealed class CancelRequestHandler(ICurrentUser currentUser, IRequestRepository requests, IUnitOfWork unitOfWork, TimeProvider timeProvider)` → `public async Task<Result> Handle(Guid requestId, CancellationToken cancellationToken)`. Secuencia **idéntica a `SubmitRequestHandler`**: (1) `GetByIdAsync` → null → `NotFound` (`VF-REQ-006`); (2) `request.OwnerId != currentUser.EmployeeId` → `NotOwner` (`RULE-04`/`FR-LFC-006` — un manager tampoco cancela lo ajeno: no hay excepción por rol, §2 del FRD lo confirma con ❌ en Manager); (3) `request.Cancel(timeProvider.GetUtcNow().UtcDateTime)` → `VF-REQ-005`; (4) `SaveChangesAsync`. Sin command record ni `Validate()` (`D6`). El nombre es el que `SAD.md` §6.1 ya lista |
| 3 | Application | Modificar | `src/BigSolutions.VacaFlow.Application/DependencyInjection.cs` | `services.AddScoped<CancelRequestHandler>();` |
| 4 | API | Modificar | `src/BigSolutions.VacaFlow.Api/Endpoints/RequestEndpoints.cs` | En el grupo existente `/api/requests`, añadir `group.MapPost("/{id:guid}/cancel", …)`: sin contrato de entrada (cuerpo vacío, §6.3), invoca `CancelRequestHandler.Handle(id, ct)` → `result.ToHttpResult()` → `204` (`ADR-012`, `D1`). **`.RequireAuthorization()` explícito** (test `Every_Endpoint_Should_State_Its_Authorization_Explicitly`). Recibe, delega, mapea — cero condicionales (`CA-PRE-001`). Forma calcada del endpoint `submit` inmediatamente encima |
| 5 | Test | Modificar | `tests/BigSolutions.VacaFlow.Domain.UnitTests/Requests/RequestTests.cs` | Bloque `Cancel` (fechas por parámetro, `TE-004`): (a) `Draft` → éxito (`T2`): `State == Cancelled`, `ClosedAtUtc == nowUtc`, `UpdatedAtUtc == nowUtc`, `SubmittedAtUtc` sigue null, `CreatedAtUtc`/contenido intactos; (b) `Create` → `Submit` → `Cancel` → éxito (`T5`): `State == Cancelled`, `ClosedAtUtc` sellado, **`SubmittedAtUtc` conservado**; (c) doble `Cancel` → `VF-REQ-005` con mensaje interpolado exacto `"This request cannot move from Cancelled to Cancelled."` y `ClosedAtUtc` no re-sellado (`TE-005` criterio 3 — la aserción pinta la interpolación); (d) `Submit` sobre un request `Cancelled` → `VF-REQ-005` `"This request cannot move from Cancelled to Submitted."` — el destino distingue los mensajes aunque el código sea el mismo, exactamente el diseño de la factory. **Deuda anotada, no test**: cancel desde `Approved`/`Rejected` es inalcanzable por API pública hasta `US-021` (`Decide`) — mismo mecanismo que `US-016` `D7` usó con `Submitted`; dejar comentario apuntando a `US-021` (ver `D8`) |
| 6 | Test | Crear | `tests/BigSolutions.VacaFlow.Application.UnitTests/Requests/CancelRequestHandlerTests.cs` | Con los fakes existentes (`FakeRequestRepository`, `FakeCurrentUser`, `FakeUnitOfWork`, `FixedTimeProvider`) — ninguno se toca: (a) dueño cancela su `Draft` → éxito, `State == Cancelled`, `ClosedAtUtc` sellado con la hora del fake, `SaveChanges` invocado; (b) dueño cancela su `Submitted` (sometido en el arrange vía `Submit` real) → éxito; (c) id inexistente → `VF-REQ-006`, nada guardado; (d) otro `EmployeeId` → `VF-REQ-004`, estado intacto — y la precedencia: un no-dueño de un request ya `Cancelled` recibe `403`, no `409` (`S1`); (e) request ya `Cancelled` → `VF-REQ-005`, `ClosedAtUtc` sin re-sellar, nada guardado |
| 7 | Test | Modificar | `tests/BigSolutions.VacaFlow.Api.FunctionalTests/Endpoints/RequestEndpointTests.cs` | Contra `VacaFlowApiFactory` (pipeline real, cookie real; drafts por `POST /api/requests` real): (a) **`AC1`/`T2`** — cancelar el propio `Draft` → `204`; `LoadRequestAsync` muestra `State == Cancelled` y `ClosedAtUtc` no nulo; (b) **`AC1`/`T5`** — crear, `POST …/submit`, `POST …/cancel` → `204`; `Cancelled` con `SubmittedAtUtc` **y** `ClosedAtUtc` sellados; (c) **`AC2`** — segundo `POST …/cancel` → `409` `{ code: "VF-REQ-005", message: "This request cannot move from Cancelled to Cancelled." }` (interpolación verificada en el wire); (d) **`AC2` cruzado** — `POST …/submit` de un request cancelado → `409` `"This request cannot move from Cancelled to Submitted."`; (e) **`AC3`** — segunda cuenta cancela el draft de la primera → `403` `VF-REQ-004`; (f) Guid aleatorio → `404` `VF-REQ-006`; (g) sin cookie → `401` `VF-AUT-004`. Estados `Approved`/`Rejected` inalcanzables por HTTP hasta `US-021` — deuda anotada en comentario (`D8`) |
| 8 | Test | Verificar | Suites completas: `dotnet build` + `dotnet test VacaFlow.slnx` · `npm run lint` + `npm run depcruise` + `npm run build` en `src/web` (sin cambios web, deben seguir verdes) | Arquitectura sin modificar en verde: handler `sealed` terminado en `Handler`, endpoint con autorización explícita, sin lectura directa del reloj, sin tokens de identidad en contratos (no hay contrato nuevo), anillos intactos. `Every_Domain_Error_Code_Should_Have_A_Status_Mapping` verde sin tocar nada (ningún código nuevo) |

**Dependencias:** 1 → {2, 5} · 2 → 3 · {2, 3} → 4 · 4 → 7 · 2 → 6 · todo → 8. **Paralelizable:** #5 (tras #1) con la rama del handler (#2–#4); #6 (tras #2) con #4/#7. **Ruta crítica:** 1 → 2 → 4 → 7.

---

## 4. Casos de uso y tabla de trazabilidad

Caso de uso único de Application: **cancelar la propia solicitud** (`CancelRequestHandler`), consumido por `POST /api/requests/{id}/cancel`. Actor: el dueño autenticado (`RULE-04`); cualquier otro usuario recibe `VF-REQ-004`; cualquier estado final (`Approved`/`Rejected`/`Cancelled`, §4.1) recibe `VF-REQ-005` con el mensaje interpolado del intento concreto. `AC4` no introduce caso de uso: es superficie de `S-06`, diferida con dueño nominal (`US-025`).

| Historia | Criterio de aceptación (verbatim) | Ítems del plan que lo cubren | Cómo se verifica |
|---|---|---|---|
| `US-019` | "Given my own request in `Draft` or `Submitted`, when I confirm cancellation, then it becomes `Cancelled` and the banner reads `Request cancelled.`" | #1 (`Cancel` transiciona desde ambos estados y sella `ClosedAtUtc`), #2, #3, #4. **Partes visuales diferidas**: la confirmación ("when I confirm") es el modal `S-08` = `US-033` (`D3`); el banner `Request cancelled.` y el botón de fila son de `US-024` (`D2`) | Dominio #5a/#5b (ambas transiciones `T2`/`T5`) · handler #6a/#6b · funcional #7a/#7b (`204` real, `Cancelled` + `ClosedAtUtc` verificados en base) · §6 pasos 4–6 |
| `US-019` | "Given a request in a final state, when cancellation is attempted, then `VF-REQ-005` is returned." | #1 (guarda `not (Draft or Submitted)` = exactamente el conjunto "final" de `FRD.md` §4.1; reutiliza la factory `InvalidTransition` existente con destino `Cancelled` — `D5`), #4 (el `409` ya estaba mapeado) | Dominio #5c/#5d (mensajes interpolados exactos) · handler #6e · funcional #7c/#7d (doble cancel y submit-tras-cancel → `409` en el wire) · §6 paso 7. Cobertura desde `Approved`/`Rejected` diferida a `US-021` con deuda anotada (`D8`) |
| `US-019` | "Given another employee's request, when cancellation is attempted, then `VF-REQ-004` is returned." | #2 (`RULE-04`/`FR-LFC-006` — comparación única contra `ICurrentUser.EmployeeId`; error y mapeo `403` existentes, se reutilizan) | Handler #6d (incluida la precedencia dueño-antes-que-estado, `S1`) · funcional #7e (dos cuentas reales → `403`) · §6 paso 8 |
| `US-019` | "Given a `Submitted` request opened as detail, when `S-06` renders, then a `Cancel request` button appears pushed to the right of the action row." | **Íntegramente diferido a `US-025`** (dueña nominal de `S-06`, `Backlog.md` §pantallas), cuyo criterio lo pide verbatim: *"Given a `Submitted` request, when `S-06` renders, then no decision block appears and `Cancel request` is available."* Cadena de secuencia: `US-025` depende de `US-024`, que depende de `US-019` — mismo orden backend-historia → pantalla-historia de `US-018` `D2`. **Ningún ítem de este plan produce código para este criterio** — ver `D2` y la pregunta abierta de §7 | Se verificará en el plan de `US-025` (deuda reanotada en §4, párrafo siguiente). En esta historia: §6 paso 10 confirma que la página `[id]` actual (US-017) sigue funcionando con el estado `Cancelled` sin cambios |

**Conteo: 4 criterios de entrada · 4 cubiertos** — `AC4` cubierto por diferimiento con dueño nominal explícito (`US-025`), y los fragmentos de UI de `AC1` diferidos a `US-024` (banner, fila) y `US-033` (confirmación), mismo mecanismo aprobado en `US-018` `D2`.

Deuda de UI que esta historia **añade** a sus historias dueñas (reanotada para sus planes):
- **`US-024` (`S-04`)**: botón `Cancel` (outlined, danger) en filas `Draft` y `Submitted`, función `cancelRequest(id)` en `lib/api.ts`, reload tras cancelar (`FR-UIX-005`), banner `Request cancelled.`, errores `VF-REQ-004/005` en banner de error.
- **`US-033` (`S-08`)**: el modal invoca `cancelRequest(id)` al pulsar `Yes, cancel`; el flujo de fila/detalle abre el modal, no llama a la API directo.
- **`US-025` (`S-06`)**: botón `Cancel request` a la derecha de la fila de acciones para `Submitted` (= `AC4` de esta historia), sin acción de cambio de estado para requests decididos.
- **`US-021`**: los tests de `Cancel` desde `Approved`/`Rejected` (dominio y HTTP), imposibles hasta que `Decide` exista (`D8`).

---

## 5. Supuestos y decisiones

Sesión de planificación delegada (Fase 3 no interactiva): las ambigüedades se resolvieron con criterio de arquitecto y quedan documentadas con su reversibilidad. **`D2`/`D3` son las decisiones de alcance; revisarlas primero junto a la pregunta de §7.**

| # | Decisión | Justificación | Impacto si es incorrecta |
|---|---|---|---|
| `D1` | **La ruta es `POST /api/requests/{id}/cancel` y el éxito es `204 No Content`** (no el `200` + cuerpo del literal FRD) | Forma literal de `FRD.md` §6.3 fila 10 bajo la convención `/api` (cuarta ratificación) y el delta `ADR-012` (cuarta aplicación: `US-015` `D2`, `US-016` `D2`, `US-018` `D3`); `ToHttpResult()` ya lo hace | Cambio local al endpoint |
| `D2` | **Toda la superficie web se difiere a sus historias dueñas; esta historia no toca `src/web/`.** En particular `AC4` (botón `Cancel request` en `S-06`) se difiere **entero** a `US-025` | A diferencia del caso genérico, aquí el diferimiento de `AC4` es **limpio y verificable**: `Backlog.md` asigna `S-06` a `US-025`, y `US-025` repite el requisito verbatim ("`Cancel request` is available" para `Submitted`) — hay dueño nominal posterior pidiéndolo, igual que `US-024` pide el botón `Submit` que `US-018` difirió. La cadena `US-019` → `US-024` → `US-025` garantiza la secuencia. Hoy además no existe `S-06`: la página `[id]` es el formulario de `US-017`, y montarle una fila de acciones provisional duplicaría `US-025`. Precedente cuádruple (`US-014` `D5`, `US-015` `D9`, `US-016` `D8`, `US-018` `D2` — este último **mantenido en la implementación**, verificado: `src/web` no tiene `submitRequest`) | Si el usuario prefiere una afordancia provisional (p. ej. botón `Cancel request` en la página `[id]` cuando el estado es `Draft`/`Submitted`), el añadido es local y aditivo: `cancelRequest()` en `lib/api.ts` + botón + confirmación nativa provisional (`window.confirm`) hasta `US-033`. **Pregunta abierta, ver §7** |
| `D3` | **La dependencia declarada `US-019` → `US-033` no bloquea esta implementación backend-first** | `US-033` es una historia 100% web (`Screen: S-08`, `Depends on: US-030` — el shell de aplicación, tampoco construido): es el modal que *invoca* la cancelación. La dependencia expresa que la **experiencia completa** de cancelar pasa por el modal, no que el endpoint lo necesite. El "when I confirm cancellation" de `AC1` queda satisfecho en backend por la semántica del endpoint (la confirmación ocurre antes de llamar); `US-033` `AC3` ("when I press `Yes, cancel`, then the cancellation executes") consumirá este endpoint. Es exactamente la relación `US-018` ↔ `US-024` ya aprobada, con la flecha de dependencia al revés en el papel pero idéntica en la práctica | Si se interpretara que `US-033` debe ir primero, `US-033` no puede ejecutar su `AC3` sin este endpoint — la dependencia sería circular; la lectura backend-first es la única orden topológica válida |
| `D4` | **`Cancel(nowUtc)` no lleva `today` ni re-valida fechas o contenido** | El contrato de `POST …/cancel` (§6.3) no lista `VF-REQ-002` (submit sí lo lista — el contraste es deliberado); `FR-LFC-005` pide solo la transición + `ClosedAtUtc`; el sketch de `SAD.md` §5.3 firma `Cancel(DateTime nowUtc)` sin fecha. Cancelar una solicitud cuyo inicio ya pasó es legítimo (retirar algo obsoleto es el caso de uso natural) | Añadir una guarda sería una línea + tests, pero contradiría el FRD — improbable |
| `D5` | **`AC2` se cumple reutilizando la factory `InvalidTransition(State, RequestState.Cancelled)` — ningún error nuevo** | La factory (US-018) toma `from`/`to` precisamente para esto: mismo código `VF-REQ-005`, mensajes distintos por interpolación (`"…from Approved to Cancelled."` vs `"…from Submitted to Submitted."`). El catálogo §7 tiene **un** código para toda transición inválida — inventar `VF-REQ-00X` nuevo rompería el catálogo. El plan de `US-018` dejó esta reutilización anotada como entrada de este plan. Arquitectónicamente sano: el código identifica la *clase* de fallo (transición ilegal), el mensaje identifica la *instancia* (`FR-ERR-002`: "message is a specific, human-readable statement") | Ninguno previsible: es el diseño documentado dos veces (SAD §5.5 y US-018) |
| `D6` | **`CancelRequestHandler.Handle(Guid requestId, ct)` — sin command record ni `Validate()`** | "Request: empty" (§6.3); precedente doble ya aprobado e implementado (`GetRequestByIdHandler`, `SubmitRequestHandler`). `ADR-011` gobierna commands con payload | Cambio cosmético local |
| `D7` | **Primera asignación de `ClosedAtUtc`; sin migración** | La columna existe desde `AddRequests` (verificado en configuración, migración y snapshot); el FRD §3.4 define su semántica ("set when the request reaches a final state") y `T2`/`T5` la sellan. `Decide` (US-021) la sellará también — el patrón queda establecido aquí | Ninguno: es la semántica documentada |
| `D8` | **La cobertura de `AC2` desde `Approved`/`Rejected` se difiere a `US-021` con deuda anotada en comentario; aquí `AC2` se prueba desde `Cancelled`** | `Approved`/`Rejected` son inalcanzables por la API pública del agregado hasta que `Decide` exista (no hay setter, no hay atajo — `CA-DOM-002` prohíbe fabricarlos). Forzar el estado por reflexión o SQL contradiría el patrón del proyecto. Es el mismo mecanismo `US-016` `D7` → `US-018` #7e, que funcionó: la deuda venció y se pagó. `Cancelled` sí es alcanzable (por el propio `Cancel`) y ejercita la guarda real con dos mensajes distintos (#5c/#5d, #7c/#7d) | `US-021` añade dos casos triviales (`Decide` → `Cancel` → `VF-REQ-005`); la guarda es un solo pattern match — el riesgo de que falle solo para `Approved`/`Rejected` es estructuralmente nulo |
| `S1` | El orden del handler (cargar → `NotFound` → dueño → dominio) devuelve **un** error: un no-dueño de un request final recibe `403`, no `409` | Patrón vigente (`US-016` `S1`, `US-018` `S1`): la autorización responde antes que el estado — no se revela el estado de una solicitud ajena | Cambio local al handler |
| `S2` | Doble cancel (dos pestañas) se resuelve por la guarda: el segundo recibe `VF-REQ-005` `Cancelled → Cancelled`, sin token de concurrencia | Único actor posible: el dueño; el resultado emergente es el especificado (`TC-06` prohíbe maquinaria extra). Idéntico a `US-018` `S2` | Ninguno |
| `S3` | Un request cancelado **conserva** `SubmittedAtUtc` si lo tenía | El FRD define `SubmittedAtUtc` como "set when the request transitions to Submitted" — es un hecho histórico, no un flag de estado vigente; nada ordena borrarlo. El sketch de `SAD.md` §5.3 tampoco lo toca | Si producto quisiera limpiarlo, es una línea; improbable — destruiría trazabilidad |
| `S4` | La rama se crea desde `main` **después** de que el PR de `US-018` mergee | Verificado: `main` está en `4069a94` (sin `Submit`); `US-018` vive solo en `feat/us-018-submit-request` (`3fb6e3d`). Crear `feat/us-019-cancel-request` desde `main` hoy no compilaría contra las precondiciones de este plan | Alternativa si el merge se demora: ramificar desde `feat/us-018-submit-request` y rebasar tras el merge |

---

## 6. Verificación end-to-end

| # | Paso | Resultado esperado |
|---|---|---|
| 1 | `dotnet build VacaFlow.slnx` | Compila con **0 warnings** (`TreatWarningsAsErrors`) |
| 2 | `dotnet test VacaFlow.slnx` | Suite completa verde, incluidos #5–#7 y los tests de arquitectura sin modificar |
| 3 | `dotnet run --project src/BigSolutions.VacaFlow.Api` (puerto 5217) | Arranca; sin migración nueva que aplicar |
| 4 | Login `employee@vacaflow.test` / `Employee123!` · crear un draft (`POST /api/requests`, fechas futuras) y capturar el `id` | `201` |
| 5 | `POST /api/requests/{id}/cancel` (cuerpo vacío) — **`T2`** | `204`; en la base, `State = 4` (`Cancelled`), `ClosedAtUtc` y `UpdatedAtUtc` sellados, `SubmittedAtUtc` null, `CreatedAtUtc` intacto |
| 6 | Crear otro draft, `POST …/submit` (`204`), luego `POST …/cancel` — **`T5`** | `204`; `State = 4`, con `SubmittedAtUtc` **y** `ClosedAtUtc` ambos sellados |
| 7 | Repetir el `POST …/cancel` sobre el request del paso 6 · y también `POST …/submit` sobre él | `409` `{ "code": "VF-REQ-005", "message": "This request cannot move from Cancelled to Cancelled." }` · `409` `"…from Cancelled to Submitted."` — interpolación visible en el wire |
| 8 | Login `manager@vacaflow.test` e intentar el cancel de un draft de Carlos | `403` `{ "code": "VF-REQ-004", "message": "You can only act on your own requests." }` — un manager tampoco cancela lo ajeno |
| 9 | `POST /api/requests/{guid aleatorio}/cancel` · cancel sin cookie | `404` `VF-REQ-006` · `401` `VF-AUT-004` |
| 10 | En el web (`npm run dev`): abrir `/requests/{id}` del request cancelado en el paso 5 | `GET` devuelve `state: "Cancelled"` → título `Request detail`, controles deshabilitados, sin botón primario (`US-017` `AC8` sigue funcionando con el estado nuevo — regresión visual, sin cambios de código) |
| 11 | `cd src/web && npm run lint && npm run depcruise && npm run build` | Verdes sin cambios (esta historia no toca el web) |

---

## 7. Riesgos y preguntas abiertas

> ⚠️ **Pregunta abierta para el usuario (no bloquea el backend — bloquea solo si la respuesta amplía el alcance):**
> `AC1` y `AC4` mencionan superficie visible (confirmación, banner, botón `Cancel request` en `S-06`) que este plan difiere íntegra a `US-033`, `US-024` y `US-025` (`D2`/`D3`), siguiendo el patrón cuádruple ya aprobado — y con la particularidad de que **`AC4` tiene dueño nominal posterior verificado** (`US-025` lo repite verbatim). **¿Se acepta el diferimiento, o se quiere una afordancia provisional de cancelación ya en esta historia** (p. ej. botón `Cancel request` en la página `/requests/[id]` cuando el estado es `Draft`/`Submitted`, con `window.confirm` provisional hasta que `US-033` traiga el modal real)? Si se quiere, se añaden tres ítems web aditivos (función `cancelRequest` en `lib/api.ts`, botón + estados de carga/error en la página `[id]`, banner de éxito con `setPendingNotification`).

| Riesgo | Mitigación |
|---|---|
| **`US-018` aún no está en `main`** (`3fb6e3d` solo en su rama feature) — este plan asume `Submit`, la factory `InvalidTransition` y el mapeo `VF-REQ-005` presentes | `S4`: ramificar desde `main` tras el merge del PR de `US-018` (o desde la rama feature con rebase). El plan queda válido en ambos casos; solo cambia el punto de partida |
| `D2` deja la historia sin demo visual (solo API + regresión de `US-017` en §6.10) | Deuda reanotada nominalmente en §4 para `US-024`/`US-025`/`US-033` (mismo mecanismo que llevó la deuda de `US-018` a `US-024`); pregunta abierta arriba por si se prefiere la afordancia provisional |
| La cobertura de `AC2` desde `Approved`/`Rejected` queda diferida a `US-021` | `D8`: la guarda es un único pattern match ejercitado con `Cancelled` en dominio y HTTP; deuda anotada en comentario en #5 y #7, mismo mecanismo `US-016` `D7` que ya venció y se pagó en `US-018` |
| `US-033` (modal) llegará después y podría descubrir que necesita algo más del endpoint (p. ej. el estado actual para el texto del modal) | El texto del modal (§3.5) es estático y `GET /api/requests/{id}` ya sirve el estado; el endpoint `cancel` no necesita devolver cuerpo (`ADR-012`: la UI refetchea) |
| `US-021` (`Decide`) reutilizará `ClosedAtUtc` y la factory con destinos `Approved`/`Rejected` | Ambos quedan establecidos aquí con la semántica del FRD; anotado como entrada para el plan de `US-021` |

---

Siguiente paso — implementación:

```
/user-story-implement ".claude/implementation plans/US-019-plan.md"
```
