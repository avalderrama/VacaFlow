# US-027 — Unit tests for rules and transitions

**Should** · `M` · Depende de: `US-021` · Traza: `SC-16`, `TC-16`, `RK-05`

## 1. Entendimiento

**Criterios de aceptación (verbatim, Backlog.md §EP-09):**
- *"`RULE-01` and `RULE-02` covered including boundaries: start equal to today (valid); start equal to end (valid); end one day before start (invalid)."*
- *"Every valid transition passes and every invalid transition is rejected."*
- *"Domain tests require no database, no network and no IO mocks."*

`NFR-MNT-007` (traza a esta historia) añade el método de verificación: *"map tests to rules; the mapping is complete"* — cada `RULE-01`–`RULE-09` debe tener al menos un test, y cada transición del state machine (`T1`–`T5`, FRD.md §4.2) debe tener test de camino válido e inválido.

**Hallazgo de la investigación — la cobertura ya existe casi por completo:**
- `DateRangeTests.cs` ya cubre `RULE-01` con exactamente los tres límites pedidos: `end == start` (válido), `end` un día antes de `start` (inválido). ✅
- `RequestTests.cs` ya cubre `RULE-02` con `start == today` válido en `Create`, `UpdateDetails` y `Submit`, y `start < today` inválido en los tres. ✅
- Las 5 transiciones válidas (`T1`–`T5`) tienen test: `Draft→Submitted`, `Draft→Cancelled`, `Submitted→Approved`, `Submitted→Rejected`, `Submitted→Cancelled`. ✅
- Casi toda la matriz de transiciones inválidas tiene test: `Cancel` inválido desde `Cancelled`, `Approved`, `Rejected` ✅; `Decide` inválido desde `Draft`, `Cancelled` (no-`Submitted`) y desde `Approved`/`Rejected` (ya decidido) ✅.

**El hueco real:** `Request.Submit` usa un único guard (`State is not Draft` → `VF-REQ-005`) que colapsa los cuatro estados no-`Draft` en la misma rama. Los tests actuales solo ejercitan ese guard invocando `Submit` desde `Submitted` y desde `Cancelled` — **nunca desde `Approved` ni desde `Rejected`**. El comportamiento ya es correcto (mismo guard), pero la matriz de "cada transición inválida rechazada" queda con dos casillas sin verificación explícita, lo que además significa que `NFR-MNT-007` ("la mapping está completa") no se puede demostrar hoy con un test señalado.

**Alcance — entra:**
- Dos tests nuevos en `RequestTests.cs`: `Submit` inválido desde `Approved` y desde `Rejected`, simétricos a los ya existentes para `Submitted`/`Cancelled`.
- Un comentario XML de trazabilidad `RULE-01`–`RULE-09` → test(s), siguiendo la convención ya establecida en el repo de documentar trazabilidad en comentarios (visto en `Request.cs`, `ApprovalErrors.cs`, etc.), para satisfacer el método de verificación de `NFR-MNT-007` ("map tests to rules; the mapping is complete") sin inventar un artefacto nuevo.

**Alcance — no entra:**
- No se toca `UpdateDetails` (edición): su guard es `RULE-03`, no una transición de `FRD.md §4.2`; ya tiene un caso representativo (`Submitted`) y ampliar a `Approved`/`Rejected`/`Cancelled` no lo pide ningún criterio de esta historia.
- No se crean tests de integración ni funcionales — el tercer criterio ("no database, no network, no IO mocks") ya se cumple porque el proyecto es `Domain.UnitTests`, sin dependencias de infraestructura; se verifica por inspección, no requiere trabajo.
- No se modifica ningún código de producción — el comportamiento de `Request.Submit` ya es correcto; solo faltan las aserciones.
- No se tocan `DateRangeTests.cs` ni `ApprovalPolicyTests.cs` — ya cumplen sus criterios sin huecos.

## 2. Cambios estructurales / de base

No se requieren cambios de esquema, configuración ni dependencias.

## 3. Plan ordenado por dependencia

| # | Capa | Acción | Artefacto | Notas |
|---|------|--------|-----------|-------|
| 1 | Domain (tests) | Modificar | `tests/BigSolutions.VacaFlow.Domain.UnitTests/Requests/RequestTests.cs` | Agregar `Submit_Should_Fail_When_The_Request_Is_Already_Approved` y `Submit_Should_Fail_When_The_Request_Is_Already_Rejected`; agregar comentario XML de trazabilidad `RULE-01`–`RULE-09` → tests en la clase |

Ítem único, sin dependencias internas.

## 4. Casos de uso y tabla de trazabilidad

| Historia | Criterio (verbatim) | Ítems del plan que lo cubren | Cómo se verifica |
|---|---|---|---|
| US-027 | "`RULE-01` and `RULE-02` covered including boundaries: start equal to today (valid); start equal to end (valid); end one day before start (invalid)." | Ya cubierto por `DateRangeTests.cs` y `RequestTests.cs` existentes; #1 añade la trazabilidad explícita | `dotnet test` — inspección de los tests ya verdes |
| US-027 | "Every valid transition passes and every invalid transition is rejected." | Las 5 válidas ya cubiertas; #1 cierra las 2 casillas inválidas faltantes (`Submit` desde `Approved`/`Rejected`) | `dotnet test` — los 2 tests nuevos deben pasar |
| US-027 | "Domain tests require no database, no network and no IO mocks." | Ya cumplido — `Domain.UnitTests` no referencia `Infrastructure` ni frameworks de IO | Inspección: `dotnet test tests/BigSolutions.VacaFlow.Domain.UnitTests` corre sin fixtures de base de datos |

Conteo: 3 criterios de entrada → 3 cubiertos.

## 5. Supuestos y decisiones

- **No se refactoriza el par de tests ya existentes (`Submit` inválido desde `Submitted`/`Cancelled`) en una `Theory` combinada.** Se agregan los dos casos faltantes como `Fact` individuales, simétricos al estilo ya usado en ese mismo archivo para `Cancel` (`Cancel_Should_Fail_When_The_Request_Has_Already_Been_Decided` sí usa `Theory` porque nace ahí; los de `Submit` nacieron como `Fact` separados). Mantener el patrón existente minimiza el diff y el riesgo sobre tests que ya pasan. Impacto si esto fuera incorrecto: bajo — es una preferencia de estilo, no de cobertura.
- **La trazabilidad `RULE-01`–`RULE-09` se documenta como comentario XML en la clase de test, no como un archivo Markdown nuevo.** Sigue la convención ya usada en el código de producción (`Request.cs`, `ApprovalErrors.cs`) de citar la regla junto al código que la aplica. Impacto si fuera incorrecto: bajo — es fácilmente movible a otro formato si se pide.

No hay ambigüedades duales — el hueco es concreto, verificable por lectura del código (`Request.Submit`), y no requiere preguntar nada.
