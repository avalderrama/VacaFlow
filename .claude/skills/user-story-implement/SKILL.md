---
name: user-story-implement
description: >-
  Motor de ejecución end-to-end de historias de usuario. Implementa una historia
  completa sobre Clean Architecture (Onion): dominio, aplicación, infraestructura,
  API y web, con revisión de calidad, revisión de seguridad, verificación
  end-to-end y reporte de sesión. MANEJA LOS DOS CASOS: si ya existe un plan
  aprobado lo consume como fuente de verdad; si no existe, lo construye inline
  con el mismo protocolo de planificación y pide aprobación antes de tocar código.
  ACTÍVALO SIEMPRE que el usuario diga cosas como "implementa esta historia",
  "ejecuta el plan", "implementa el US-1234", "desarrolla esta funcionalidad",
  "arranca con la historia", "codifica esto", "aplica el plan de implementación",
  "hazlo end to end", "termina esta historia", o cuando pase una ruta a un
  documento de plan, un ID de work item de Azure DevOps o la descripción de una
  tarea de desarrollo — aunque no mencione la palabra "skill".
  Fases: 0 intent y preflight de rama · 1 implementación caso por caso ·
  2 migraciones y datos · 3 revisión de calidad (2 rondas) ·
  4 revisión de seguridad (2 rondas) · 5 verificación y pruebas ·
  6 chequeo de cumplimiento arquitectónico · 7 reporte de sesión.
argument-hint: "[ruta al plan | ID de work item de ADO | descripción de la tarea]"
model: opus
---

**MANDATORY: TODA LA COMUNICACIÓN, DOCUMENTACIÓN Y EL REPORTE DE SESIÓN SE ESCRIBEN EN ESPAÑOL. TODO ARTEFACTO DE CÓDIGO SE ESCRIBE EN INGLÉS: CLASES, MÉTODOS, PROPIEDADES, VARIABLES, ENDPOINTS, DTOs, TABLAS, COLUMNAS, MIGRACIONES, NOMBRES DE ARCHIVO, RAMAS Y MENSAJES DE COMMIT. LOS COMENTARIOS EN CÓDIGO, CUANDO SEAN NECESARIOS, VAN EN INGLÉS. NUNCA MEZCLAR IDIOMAS DENTRO DE UN MISMO ARTEFACTO. ESTA REGLA NO TIENE EXCEPCIONES.**

# User Story Implement

Motor de **ejecución end-to-end**. Toma una historia de usuario —con plan previo o sin él— y la lleva hasta código implementado, revisado, verificado y reportado.

Esta es la fase costosa del ciclo. Se ejecuta con el modelo de mayor capacidad disponible y sin atajos: cada fase se completa antes de pasar a la siguiente, y los resultados que se reportan son los reales, no los esperados.

**Límite del skill:** no hace `git commit`, `git push`, ni despliegues, salvo que el usuario lo pida explícitamente. Deja el árbol de trabajo listo y lo declara en el reporte final.

---

## Fase 0 — intent

### 0.1 Memoria y reglas del proyecto (tarea #1)

Leer, en este orden:

1. `CLAUDE.md` de la raíz — reglas vinculantes del proyecto.
2. Cualquier `CLAUDE.md` path-scoped de los directorios que se van a tocar (`src/**/CLAUDE.md`).
3. `.claude/` — convenciones, decisiones y planes previos.
4. Documentos de reglas vinculantes del repositorio:
   - `docs/reglas-clean-architecture-onion.md` — reglas `CA-*` (arquitectura).
   - `docs/reglas-diseno-ui-ux-web.md` — reglas `UX-*` (frontend).
   - `docs/adr/` — decisiones de arquitectura vigentes.

Estos documentos son **ley** durante toda la ejecución (ver Fase 1) y son la base del chequeo de cumplimiento de la Fase 6.

### 0.2 Caso A — Existe un plan

Buscar el plan en este orden de prioridad:

| # | Fuente |
|---|---|
| 1 | Ruta pasada como argumento al skill |
| 2 | `.claude/implementation plans/<ID>-plan.md` |
| 3 | Texto del plan pegado en la conversación |

Si se encuentra:

- **Leerlo completo**, no en diagonal.
- Extraer la tabla de casos de uso y la tabla de trazabilidad contra criterios de aceptación.
- Extraer el plan ordenado por dependencia y los cambios estructurales declarados.
- Tratarlo como **fuente de verdad**: no volver a preguntar nada que el plan ya responda. Re-preguntar lo ya decidido es un defecto de este skill.
- Si el plan tiene secciones marcadas como `⏸️ PENDIENTE DE DEFINICIÓN`, esas sí se preguntan antes de empezar.
- Si el código real contradice al plan (algo ya existe, una firma cambió, un archivo se movió), **no improvisar en silencio**: señalar la discrepancia al usuario, proponer el ajuste y continuar con su confirmación.

### 0.3 Caso B — No existe plan

Construir el plan **inline**, con el mismo protocolo del skill de planificación, antes de escribir una sola línea de código:

1. **Entendimiento** — qué, por qué y para qué, atado a la historia y a sus criterios de aceptación extraídos **verbatim** (desde Azure DevOps vía su MCP, desde `docs/user-stories/<ID>.md`, o desde el texto pegado).
2. **Cambios estructurales** — esquema, migraciones, configuración, dependencias. Si no aplica, declararlo explícitamente.
3. **Plan ordenado por dependencia** — de adentro hacia afuera: Domain → Application → Infrastructure → API → Web, con ruta de archivo y tag de capa por ítem.
4. **Casos de uso trazables** — cada criterio de aceptación cubierto por al menos un ítem. Conteo verificado.
5. **Ambigüedades** — toda duda dual o incompleta se pregunta con `AskUserQuestion`, agrupando las relacionadas. **Nunca asumir.**
6. **Aprobación** — presentar el plan y obtener aprobación explícita del usuario **antes de tocar código**.

Sin aprobación no se implementa. No hay excepción por urgencia.

### 0.4 Preflight de rama y entorno (obligatorio, antes de cualquier escritura)

Validar **siempre** el estado actual antes de escribir nada:

```bash
git status --short --branch
git log --oneline -3
```

**Reglas de decisión:**

| Rama actual | Acción |
|---|---|
| `main`, `master`, `develop`, `release/*`, o cualquier rama compartida o protegida | **NO EJECUTAR AQUÍ.** Informar al usuario, pedirle que cambie o cree la rama correcta, y **detenerse hasta que lo haga.** No crear la rama por cuenta propia salvo que el usuario lo autorice. |
| Rama de trabajo que **correlaciona claramente** con la historia (ej. `feature/US-1234-apply-order-discount` para `US-1234`) | Continuar. |
| Rama de trabajo que **no correlaciona** o correlaciona de forma dudosa | **Preguntar antes de asumir.** ¿Se trabaja aquí, se cambia de rama, se crea una nueva? Nunca continuar a ciegas. |

Verificar además:

- **Árbol de trabajo limpio.** Si hay cambios sin commitear que no son de esta historia, señalarlos y preguntar qué hacer antes de mezclar trabajo.
- **Entorno de destino.** Si hay variables de entorno o cadenas de conexión apuntando a un entorno distinto de desarrollo local, detenerse y confirmar. Ninguna migración ni script se ejecuta contra un entorno compartido o productivo desde este skill.

### 0.5 Checklist viva

Cargar todos los casos de uso e ítems de trabajo en `TodoWrite`, uno por ítem del plan.

- Se marca `in_progress` **un solo ítem a la vez**.
- Se marca `completed` inmediatamente al terminarlo, nunca por lotes al final.
- Si aparece trabajo no previsto, se agrega a la lista en el momento, no se hace de contrabando.

La checklist es el estado visible de la sesión: debe reflejar la realidad en todo momento.

---

## Fase 1 — Implementación, caso de uso por caso de uso

### 1.1 Las reglas del repositorio son ley

Las reglas de `CLAUDE.md` (raíz y path-scoped), de `docs/reglas-clean-architecture-onion.md` (`CA-*`) y de `docs/reglas-diseno-ui-ux-web.md` (`UX-*`) son **vinculantes**.

Si un ítem del plan parece exigir romper una regla —por ejemplo, referenciar infraestructura desde el dominio (`CA-DEP-003`), o exponer una entidad de dominio en un contrato de API (`UX/CA`)— **DETENERSE**. No implementar la violación y no implementar un rodeo silencioso. Plantear al usuario:

- Qué regla se estaría rompiendo y por qué el requerimiento parece exigirlo.
- Dos o tres alternativas que sí cumplen, con su costo.
- La recomendación.

Y esperar la decisión.

### 1.2 Principios de diseño no negociables

- **SOLID**, con énfasis en responsabilidad única e inversión de dependencias.
- **Inyección de dependencias por constructor.** Prohibido el service locator (`CA-CFG-003`) y el estado estático mutable (`CA-CFG-004`).
- **Sin lógica de negocio en la capa de entrada.** Controllers y endpoints delegan; no deciden (`CA-PRE-001`).
- **Las reglas de negocio viven en el dominio**, no en handlers ni en repositorios.
- **Los puertos se declaran en el anillo interno**, las implementaciones en el externo (`CA-DEP-004`).
- **Código autoexplicativo antes que comentarios.** Nombres que expliquen la intención; comentarios solo para el *por qué* no obvio, nunca para el *qué*.
- **Manejo explícito de errores.** `Result` para errores esperados; excepciones solo para lo excepcional.
- **Sin código muerto, sin TODOs huérfanos, sin `Console.WriteLine` de depuración.**

### 1.3 Paridad entre superficies

Si la funcionalidad afecta a más de un cliente o consumidor —API pública, aplicación web, aplicación móvil, jobs en background, integraciones—, **todo cambio de comportamiento o de interfaz debe reflejarse en todas las superficies relevantes**.

Reglas:

- Antes de cerrar un caso de uso, verificar explícitamente qué superficies lo consumen.
- Cuando un cambio de backend **ya cubre a todas las superficies** (por ejemplo, una validación en el caso de uso que aplica a cualquier cliente), **decirlo explícitamente** en el momento y en el reporte final: *"El cambio en `ApplyDiscountCommandHandler` cubre API y web; no requiere cambio adicional en cliente."*
- Cuando **no** las cubre, cada superficie es un ítem propio en la checklist, no una nota al pie.
- Una paridad que no se pudo completar en esta sesión **se declara como pendiente en la Fase 7**. Nunca se omite.

### 1.4 Ritmo de trabajo

Un ítem a la vez:

1. **Anunciar el inicio:** qué ítem, qué capa, qué criterio de aceptación cubre.
2. **Implementar**, reutilizando lo que ya existe antes de crear algo nuevo.
3. **Confirmar el cierre** mapeado a su criterio: *"Ítem #3 completo — cubre el criterio 'el sistema exige aprobación del supervisor'."*
4. Actualizar `TodoWrite`.

No se abren tres frentes a la vez. No se deja un ítem a medias para empezar otro.

### 1.5 Gate de build incremental

Compilar a medida que se avanza, no solo al final: al terminar cada capa o cada ítem que introduzca tipos nuevos.

```bash
dotnet build --nologo -warnaserror
# Frontend
npm run typecheck
```

**Gotchas conocidos del entorno de build:**

- **Bloqueo de archivos en Windows** (`MSB3027` / `MSB3021`, "el proceso no puede acceder al archivo"): ocurre cuando la aplicación, `dotnet watch`, IIS Express o el depurador están corriendo. Detener el proceso antes de recompilar. No hay que "reintentar hasta que pase".
- **Caché de `obj/` y `bin/` desincronizada** tras cambios de framework o de referencias: `dotnet clean` antes de volver a compilar.
- **Herramientas de EF Core** con múltiples proyectos de inicio: especificar siempre `--project` y `--startup-project`.
- **Node**: si `npm run` falla tras cambiar de rama, `npm ci` antes de investigar más.

Un build roto **detiene el avance**. No se acumulan errores de compilación "para arreglarlos al final".

---
## Fase 2 — Cambios estructurales y de datos

Aplica cuando la historia toca esquema de base de datos, configuración persistida o datos existentes. Si no aplica, declararlo y pasar a la Fase 3.

### 2.1 Orden de dependencia que nunca rompe el estado existente

1. **Aditivo primero.** Agregar columnas, tablas e índices nuevos. Las columnas nuevas nacen **nullable** o con valor por defecto; nunca `NOT NULL` sin default sobre una tabla con datos.
2. **Backfill.** Poblar los datos nuevos a partir de los existentes, en lotes si el volumen lo exige, con script re-ejecutable.
3. **Endurecer.** Solo después del backfill verificado se aplican restricciones (`NOT NULL`, `UNIQUE`, claves foráneas).
4. **Deprecar, no destruir.** Lo que deja de usarse se marca como obsoleto y se elimina en una migración posterior, cuando ningún despliegue activo lo consume. **Nunca `DROP COLUMN` en la misma migración que introduce el reemplazo.**

### 2.2 Guards de idempotencia

Toda migración y todo script de datos debe poder ejecutarse dos veces sin causar daño:

- `IF NOT EXISTS` / `IF EXISTS` en operaciones de esquema manuales.
- Comprobación de estado previa en los backfills (`WHERE new_column IS NULL`).
- Sin `INSERT` ciegos de datos semilla: `MERGE` o comprobación previa por clave natural.

### 2.3 Reglas de ejecución

- Las migraciones se **generan y se revisan siempre** antes de aplicarse. Leer el SQL producido; no confiar en el scaffolding a ciegas.
- Se aplican **únicamente contra la base de datos de desarrollo local**.
- **Nunca** se ejecutan contra un entorno compartido, de pruebas de cliente o productivo desde este skill. Eso queda como pendiente explícito en la Fase 7.
- Toda migración tiene su camino de reversión identificado, aunque no se ejecute.

---

## Fase 3 — Revisión de calidad — EXACTAMENTE 2 rondas

### 3.1 Herramienta de revisión

Invocar la revisión de código sobre el **diff de trabajo** de la sesión (`git diff` contra el punto de partida de la rama), no sobre el repositorio completo.

Orden de preferencia:

1. **Subagente revisor** vía la tool `Task` con un agente de tipo *code-reviewer*, pasándole el diff y los documentos de reglas (`CA-*`, `UX-*`) como criterio.
2. **Skill o comando de review del repositorio**, si existe.
3. **Wrapper model-invocable.** Si la revisión oficial del entorno es *manual-only* —un slash command que solo puede lanzar la persona—, no bloquear el flujo: crear o usar un wrapper invocable por el modelo (por ejemplo un subagente `code-review-runner` que reciba el diff y aplique la misma rúbrica). Si tampoco es posible, ejecutar la revisión de forma estructurada dentro de la sesión contra la rúbrica de abajo y **declarar en el reporte final que la revisión fue interna, no con la herramienta oficial**.

### 3.2 Foco de la revisión

- Cumplimiento de las reglas `CA-*` de Clean Architecture: dirección de dependencias, puertos en el anillo correcto, ausencia de lógica de negocio fuera del dominio.
- Cumplimiento de las reglas `UX-*` en los cambios de frontend: estados de la vista, accesibilidad, tokens, formularios.
- SOLID, cohesión, acoplamiento, duplicación con código ya existente.
- Nombres, legibilidad y código autoexplicativo.
- Manejo de errores, casos límite, valores nulos, concurrencia.
- Cobertura de pruebas de lo implementado.
- Código muerto, restos de depuración, TODOs sin dueño.

### 3.3 El loop

1. **Ronda 1** — invocar la revisión sobre el diff.
2. **Aplicar** las correcciones reportadas. Las que se decida no aplicar se justifican por escrito.
3. **Ronda 2** — invocar la revisión de nuevo, para verificar los fixes de la ronda 1.
4. **Aplicar** lo que quede accionable.

**CORTE EN 2 RONDAS.** Lo que siga abierto después de la ronda 2 **no se sigue iterando**: se registra en el reporte final de la Fase 7 como pendiente, con su severidad y el motivo. Un tercer ciclo de revisión no es diligencia, es bucle.

---

## Fase 4 — Revisión de seguridad y riesgo — EXACTAMENTE 2 rondas

Mismo patrón y mismas reglas de invocación que la Fase 3 (incluida la nota del wrapper model-invocable), con foco distinto.

### 4.1 Foco de la revisión

- **Autenticación y autorización**: cada endpoint nuevo o modificado exige el rol/política correcta. Verificar acceso directo a objetos por identificador (IDOR) y filtrado por tenant u organización.
- **Validación de entrada**: en el borde de aplicación, no solo en el cliente. Sobre-publicación de propiedades (*mass assignment*) en los DTOs de entrada.
- **Inyección**: consultas parametrizadas siempre; sin SQL concatenado, sin interpolación en LINQ dinámico, sin comandos de shell construidos con entrada de usuario.
- **Secretos**: ninguna credencial, token, cadena de conexión ni clave en el código ni en archivos versionados (`CA-INF-007`). Verificar que no se hayan filtrado en logs, mensajes de error o respuestas de API.
- **Datos personales y sensibles**: minimización, enmascaramiento en pantalla y en logs, y cumplimiento del régimen de protección de datos aplicable al cliente.
- **Exposición de información**: los errores hacia el cliente no revelan stack traces, nombres de tablas ni detalles de infraestructura (`UX-EST-004`).
- **Frontend**: sin `dangerouslySetInnerHTML` ni `innerHTML` con contenido de usuario, protección CSRF donde aplique, sin datos sensibles en `localStorage`.
- **Dependencias nuevas**: procedencia, mantenimiento y vulnerabilidades conocidas.

### 4.2 El loop

1. **Ronda 1** — revisión de seguridad sobre el diff.
2. **Aplicar** las correcciones.
3. **Ronda 2** — verificar los fixes.
4. **Aplicar** lo que quede.

**CORTE EN 2 RONDAS.** Los hallazgos abiertos van al reporte final con su severidad. **Un hallazgo de severidad crítica abierto se escala explícitamente al usuario en el cierre**, no se entierra en una tabla.

---

## Fase 5 — Verificación

### 5.1 Pruebas, typecheck y build por componente

Ejecutar, en cada componente que se haya modificado:

```bash
# Backend
dotnet build --nologo -warnaserror
dotnet test --nologo

# Frontend
npm run typecheck
npm run lint
npm test
```

**Reportar resultados reales, incluidos los fallos.** Si algo falla:

- Se distingue si el fallo lo introdujo esta sesión o si ya venía roto en la rama base (comprobarlo, no suponerlo).
- Los fallos introducidos por esta sesión **se corrigen**.
- Los fallos preexistentes se reportan como tales, sin arreglarlos por iniciativa propia salvo que sean triviales y se declare.

Nunca se afirma que la suite pasa sin haberla ejecutado.

### 5.2 Verificación end-to-end

1. **Identificar qué casos de uso son verificables** en las superficies disponibles (aplicación web levantada localmente, API vía HTTP, worker ejecutable). Declarar cuáles **no** lo son y por qué.
2. **Ejecutar cada caso verificable** de extremo a extremo, con las herramientas disponibles: navegador automatizado o MCP de navegador para la web, cliente HTTP para la API, ejecución directa para jobs.
3. **Capturar evidencia**: captura de pantalla, cuerpo de respuesta, código de estado, log relevante o registro creado en base de datos.
4. Mapear cada verificación a su criterio de aceptación.

**Credenciales y secretos:** si se necesita un usuario, una clave, un token o una cadena de conexión que no se conoce, **preguntar al usuario**. Nunca adivinar, nunca reutilizar credenciales encontradas en otros archivos, nunca forzar el acceso ni saltarse la autenticación para "poder probar". Si no se obtienen, el caso queda como no verificado en el reporte.

---

## Fase 6 — Chequeo de cumplimiento arquitectónico

Se ejecuta **al final**, cuando todo lo demás ya está verde. Es **informativo, no bloqueante**.

### 6.1 Qué se corre

Validación de los cambios contra las reglas vinculantes del proyecto:

- Tests de arquitectura del repositorio, si existen (`dotnet test` sobre el proyecto `*.ArchitectureTests`).
- Revisión de los archivos tocados contra las reglas `CA-*` de `docs/reglas-clean-architecture-onion.md`.
- Revisión de los cambios de frontend contra las reglas `UX-*` de `docs/reglas-diseno-ui-ux-web.md`, incluida una pasada de accesibilidad automatizada si la herramienta está disponible.

### 6.2 Cómo se reporta

Capturar el resultado como **porcentaje de cumplimiento por capa**, sobre los archivos tocados en esta sesión:

```markdown
| Capa / Componente | Reglas evaluadas | Cumplidas | % | Violaciones abiertas |
|---|---:|---:|---:|---|
| Domain | 11 | 11 | 100 % | — |
| Application | 11 | 10 | 91 % | `CA-APP-006` (1) |
| Infrastructure | 8 | 8 | 100 % | — |
| API | 6 | 6 | 100 % | — |
| Web | 14 | 12 | 86 % | `UX-EST-003` (1), `UX-ACC-004` (1) |
```

### 6.3 Reglas del chequeo

- **No se loopea sobre esto.** No hay rondas de corrección automática en esta fase.
- **No se auto-corrige.** Aunque la violación parezca trivial.
- Cualquier violación de severidad 🔴 se anota como **pendiente para que el usuario decida** si se corrige ahora, se difiere o se documenta como excepción.

---

## Fase 7 — Reporte de sesión

Entregar un resumen final estructurado, en español, con **todas** estas secciones:

### 1. Ítems de trabajo y trazabilidad

```markdown
| # | Ítem de trabajo | Criterio de aceptación que cubre | Estado |
|---|-----------------|----------------------------------|--------|
| 1 | `Order.ApplyDiscount` + evento de dominio | "…exige aprobación del supervisor" | ✅ Completo |
| 2 | `ApplyDiscountCommandHandler` | "…exige aprobación del supervisor" | ✅ Completo |
| 5 | Pantalla de detalle de pedido | "…el usuario ve el descuento aplicado" | ⚠️ Parcial — falta estado de error |
```

### 2. Archivos creados y modificados

Agrupados por capa, componente y superficie (Domain / Application / Infrastructure / API / Web / Tests), indicando creado o modificado.

### 3. Cambios estructurales y de datos

Migraciones y scripts creados, en qué orden se aplican, **si se aplicaron o no al entorno de desarrollo**, y su camino de reversión.

### 4. Loops de revisión

Para **cada uno** de los dos loops (calidad y seguridad):

- Qué se encontró en la ronda 1 y en la ronda 2.
- Qué se corrigió.
- **Qué quedó pendiente**, con severidad y motivo.
- Si la revisión se hizo con la herramienta oficial o con un sustituto interno.

### 5. Verificación

- Resultados reales de build, typecheck, lint y suite de pruebas, por componente.
- Casos de uso verificados end-to-end, con la evidencia capturada.
- **Casos que quedaron sin verificar y por qué** (falta de credenciales, superficie no disponible, dependencia externa).

### 6. Cumplimiento arquitectónico

La tabla de porcentajes de la Fase 6 y la lista de violaciones críticas abiertas, si las hay.

### 7. Pendientes explícitos

Todo lo que queda fuera de esta sesión, sin eufemismos:

- Verificaciones de paridad entre superficies que no se completaron.
- Migraciones no aplicadas a entornos distintos del local.
- Despliegue a pruebas o producción.
- Hallazgos de revisión diferidos.
- Trabajo del plan que no se ejecutó y por qué.
- Decisiones que quedaron esperando al usuario.

---

## Reglas absolutas (resumen)

| # | Regla |
|---|---|
| 1 | Comunicación en español; artefactos de código en inglés. Sin excepciones. |
| 2 | Memoria y reglas del proyecto se leen primero; son ley durante toda la sesión. |
| 3 | Sin plan aprobado no se toca código — ni siquiera cuando el plan se construye inline. |
| 4 | Preflight de rama obligatorio: en rama compartida o protegida, no se ejecuta y se detiene. |
| 5 | Un ítem a la vez, con checklist viva y actualizada en tiempo real. |
| 6 | Si el requerimiento exige romper una regla vinculante, se detiene y se plantea. |
| 7 | Migraciones: aditivo → backfill → endurecer → deprecar. Solo contra desarrollo local. |
| 8 | Revisión de calidad y revisión de seguridad: exactamente 2 rondas cada una. Sin tercera. |
| 9 | Se reportan resultados reales de pruebas, incluidos los fallos. |
| 10 | Credenciales desconocidas se preguntan; nunca se adivinan ni se evaden. |
| 11 | El chequeo de cumplimiento es informativo: no bloquea, no se loopea, no se auto-corrige. |
| 12 | Sin commit, push ni despliegue salvo petición explícita del usuario. |

---

## Anti-patrones

| Anti-patrón | Por qué está prohibido |
|---|---|
| Empezar a codificar antes del preflight de rama | Riesgo de escribir sobre `develop` o sobre trabajo ajeno. |
| Re-preguntar lo que el plan aprobado ya define | El plan es fuente de verdad; re-preguntar es desperdicio y ruido. |
| Improvisar cuando el código contradice al plan | La discrepancia se señala y se confirma, no se resuelve en silencio. |
| Tercera ronda de revisión "porque quedó algo" | El corte en 2 es la regla; lo abierto va al reporte. |
| Reportar "todas las pruebas pasan" sin ejecutarlas | Falsedad sobre el estado del sistema. |
| Adivinar credenciales o saltarse la autenticación para probar | Riesgo de seguridad y de datos. Se pregunta. |
| Aplicar migraciones a un entorno compartido | Fuera del alcance del skill. Va a pendientes. |
| Auto-corregir violaciones detectadas en la Fase 6 | La fase es informativa; corregir ahí reabre el ciclo sin control. |
| Marcar todos los todos como completos al final | La checklist debe reflejar la realidad en tiempo real. |
| Omitir del reporte lo que quedó a medias | El valor del reporte está en lo que declara pendiente. |

---

## Checklist de cierre de sesión

- [ ] Memoria y reglas del proyecto leídas al inicio.
- [ ] Plan consumido o construido inline y aprobado antes de codificar.
- [ ] Preflight de rama y entorno ejecutado y aprobado.
- [ ] Todos los ítems del plan implementados o declarados como pendientes.
- [ ] Paridad entre superficies verificada o declarada.
- [ ] Build y typecheck en verde en todos los componentes tocados.
- [ ] 2 rondas de revisión de calidad completadas.
- [ ] 2 rondas de revisión de seguridad completadas.
- [ ] Suite de pruebas ejecutada, con resultados reales reportados.
- [ ] Verificación end-to-end ejecutada con evidencia; no verificados declarados.
- [ ] Chequeo de cumplimiento ejecutado y capturado, sin auto-corrección.
- [ ] Reporte de sesión entregado con sus 7 secciones completas.
