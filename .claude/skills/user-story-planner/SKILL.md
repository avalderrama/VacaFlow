---
name: user-story-planner
description: >-
  Planificador de historias de usuario. Convierte una historia de usuario y sus
  criterios de aceptación en un documento de plan de implementación detallado,
  trazable y aterrizado en el código real del repositorio. ACTÍVALO SIEMPRE que
  el usuario diga cosas como "planifica esta historia", "planear la HU",
  "arma el plan de implementación", "analiza esta historia de usuario",
  "prepara el plan para el work item", "vamos a planear el US-1234",
  "qué hay que hacer para esta historia", "desglosa esta historia",
  "estimar/desglosar la tarea", "plan antes de implementar", o cuando pegue el
  texto de una historia de usuario o pase un ID de Azure DevOps y pida
  analizarla — aunque no use la palabra "skill" ni la palabra "plan".
  Este skill NO IMPLEMENTA NADA: no escribe código, ni configuración, ni
  migraciones, ni tests. Su única salida es un documento Markdown de plan.
  La implementación la ejecuta después el skill `/user-story-implement`,
  que consume el documento generado aquí.
argument-hint: "<ID de work item de Azure DevOps | ruta a archivo de historia (.md) | texto de la historia pegado>"
model: opus
allowed-tools:
  - Read
  - Glob
  - Grep
  - TodoWrite
  - AskUserQuestion
  - ExitPlanMode
  - Write
---

**MANDATORY: EL DOCUMENTO DE PLAN SE ESCRIBE COMPLETAMENTE EN ESPAÑOL. TODO IDENTIFICADOR DE CÓDIGO (CLASES, MÉTODOS, PROPIEDADES, VARIABLES, ENDPOINTS, TABLAS, COLUMNAS, RAMAS, NOMBRES DE ARCHIVO) SE ESCRIBE EN INGLÉS. NUNCA MEZCLAR: PROSA EN ESPAÑOL, CÓDIGO EN INGLÉS. ESTA REGLA NO TIENE EXCEPCIONES.**

# User Story Planner

Flujo de **planificación pura**. Traduce una historia de usuario en un plan de implementación ejecutable, ordenado por dependencias y trazable contra cada criterio de aceptación.

**Este skill nunca implementa.** No crea ni modifica código fuente, configuración, esquemas, migraciones ni tests. El único archivo que llega a escribir es el documento de plan, y solo después de la aprobación explícita del usuario.

---

## Modo de operación

1. **Entrar en plan mode al inicio del run**, antes de leer nada. Si la herramienta de plan mode no está disponible en la sesión, operar igualmente bajo disciplina de plan mode: solo lectura.
2. Durante todo el run están **prohibidas** todas las escrituras: `Write`, `Edit`, `NotebookEdit`, comandos de shell que modifiquen el árbol de trabajo, `git commit`, `git checkout -b`, instalación de paquetes, ejecución de migraciones o de generadores de código.
3. **Solo se sale de plan mode en la Fase 4**, para presentar el plan y pedir aprobación.
4. Tras la aprobación, se permite **una única escritura**: guardar el documento de plan en Markdown (Fase 4, paso 2).

Si en cualquier punto surge la tentación de "ya que estoy, dejo creada la interfaz", **no hacerlo**. Ese impulso es la señal de que hay que anotarlo en el plan y seguir.

---

## Fase 0 — Intake

### 0.1 Leer la memoria del proyecto (tarea #1, obligatoria)

Antes de cualquier otra acción, leer el contexto persistente del proyecto:

- `CLAUDE.md` en la raíz del repositorio y cualquier `CLAUDE.md` de subdirectorio relevante.
- `.claude/` — reglas, convenciones, decisiones y planes previos.
- `docs/adr/` o equivalente — decisiones de arquitectura vigentes.
- Planes anteriores en `.claude/implementation plans/` — para no contradecir trabajo ya planificado ni duplicarlo.

Esto define convenciones, arquitectura y restricciones. Un plan que ignora la memoria del proyecto es un plan inválido.

### 0.2 Localizar el requerimiento

Buscar en **este orden estricto de prioridad**, deteniéndose en la primera fuente que dé resultado:

| # | Fuente | Cómo |
|---|---|---|
| 1 | **Argumento explícito pasado al skill** | ID de work item, ruta de archivo o texto directo |
| 2 | **Azure DevOps** | Tool/MCP de ADO: obtener el work item, su descripción, criterios de aceptación, adjuntos, work items relacionados (padre, hijos, links) y comentarios relevantes |
| 3 | **Archivo convencional del repositorio** | `docs/user-stories/<ID>.md`, y en su defecto `docs/user-stories/` completo |
| 4 | **Texto pegado en la conversación** | La historia tal como la escribió el usuario |

Si ninguna fuente arroja resultado, **detenerse y preguntar** al usuario dónde está la historia. No inventar una.

### 0.3 Extraer verbatim

Leer el requerimiento **completo**, no en diagonal. Extraer y conservar **textualmente**:

- La narrativa de la historia (Como… quiero… para…).
- **Cada criterio de aceptación, palabra por palabra.**
- Reglas de negocio, validaciones, mensajes y datos mencionados.
- Referencias a pantallas, endpoints, reportes o integraciones.

Los criterios de aceptación son la **columna vertebral del plan**: la tabla de trazabilidad de la Fase 2 se construye sobre ellos. No parafrasearlos, no resumirlos, no fusionarlos.

### 0.4 Alcance múltiple

Si hay más de una historia en alcance (un épico, un conjunto de work items, varias historias pegadas):

- Planificarlas **juntas**, para detectar trabajo compartido y ordenar dependencias entre ellas.
- Mantener **trazabilidad individual**: cada ítem del plan y cada fila de la tabla de trazabilidad indica a qué historia pertenece (`US-1234`, `US-1235`).
- Señalar explícitamente el trabajo transversal que sirve a varias historias, para que se construya una sola vez.

---

## Fase 1 — Grounding en el sistema real

Antes de escribir una sola línea del plan, investigar el repositorio. **Investigación suficiente para responder, no una auditoría completa**: se busca contexto para decidir bien, no un inventario del sistema.

Determinar:

1. **Capas de arquitectura afectadas.** Bajo Clean Architecture / Onion, identificar qué toca en cada anillo:
   - **Domain** — entidades, value objects, agregados, eventos, reglas de negocio.
   - **Application** — casos de uso (commands/queries), puertos, DTOs, validadores.
   - **Infrastructure** — repositorios, configuraciones de ORM, migraciones, clientes externos, mensajería.
   - **API / Presentation** — endpoints, contratos, mapeos, autorización.
   - **Web / Cliente** — vistas, componentes, estado, rutas.

2. **Qué ya existe y se puede reutilizar.** Buscar entidades, casos de uso, repositorios, validadores, componentes y utilidades equivalentes antes de proponer crear algo nuevo. **Reutilizar por defecto; crear solo con justificación.** Si el plan propone un componente nuevo que se parece a uno existente, la justificación debe estar escrita.

3. **Paridad entre superficies.** Si la funcionalidad debe existir en más de un cliente o canal (API pública, aplicación web, aplicación móvil, job en background, integración), verificar cuáles deben mantener paridad funcional y dejarlo explícito en el plan. Una historia entregada solo en una superficie cuando el producto exige dos es un plan incompleto.

4. **Estado actual de lo que se va a extender.** Leer el código concreto que se modificará: firmas actuales, contratos publicados, consumidores existentes, cobertura de tests. El objetivo es que los cambios propuestos sean **aditivos y seguros**, y que cualquier cambio que rompa compatibilidad esté identificado como tal, con su impacto y su estrategia de migración.

Registrar los hallazgos como notas internas: alimentan las secciones 1, 3 y 5 del documento.

---

## Fase 2 — Construcción del documento de plan

Redactar el plan **en español**, con estas secciones **en este orden exacto**:

### 1. Entendimiento

Qué se pide, por qué se pide y para qué sirve, atado a los requisitos de entrada. Incluir la narrativa de la historia y la lista literal de criterios de aceptación. Cerrar con el alcance: qué entra y, explícitamente, **qué queda fuera**.

### 2. Cambios estructurales / de base

Cambios de esquema de base de datos, migraciones, configuración, variables de entorno, permisos, feature flags, contratos de integración o dependencias nuevas.

**Solo si aplica.** Si no aplica, escribirlo explícitamente: *"No se requieren cambios de esquema, configuración ni dependencias."* Nunca omitir la sección en silencio.

### 3. Plan ordenado por dependencia

Los ítems de trabajo en el orden en que deben ejecutarse, de adentro hacia afuera (Domain → Application → Infrastructure → API → Web), respetando la regla de dependencias de Clean Architecture.

Cada ítem indica: qué se crea o modifica, ruta del archivo, y **tag de capa/superficie**.

```markdown
| # | Capa | Acción | Artefacto | Notas |
|---|------|--------|-----------|-------|
| 1 | Domain | Modificar | `src/Company.Product.Domain/Orders/Order.cs` | Agregar método `ApplyDiscount` y evento `OrderDiscountAppliedDomainEvent` |
| 2 | Application | Crear | `src/Company.Product.Application/Orders/ApplyDiscount/ApplyDiscountCommand.cs` | Command + Handler + Validator |
| 3 | Infrastructure | Modificar | `src/Company.Product.Infrastructure/Persistence/Configurations/OrderConfiguration.cs` | Mapeo de la nueva columna |
| 4 | API | Crear | `src/Company.Product.Api/Endpoints/Orders/ApplyDiscountEndpoint.cs` | `POST /orders/{id}/discount` |
| 5 | Web | Modificar | `src/web/src/features/orders/OrderDetail.tsx` | Acción y estado de carga/error |
```

Marcar dependencias entre ítems y qué se puede paralelizar.

### 4. Casos de uso y tabla de trazabilidad

Una fila por criterio de aceptación. **Cada criterio de entrada debe estar cubierto por al menos un ítem del plan.** Un criterio sin cobertura es un defecto del plan, no una omisión aceptable.

```markdown
| Historia | Criterio de aceptación (verbatim) | Ítems del plan que lo cubren | Cómo se verifica |
|----------|-----------------------------------|------------------------------|------------------|
| US-1234 | "Dado un pedido confirmado, cuando el usuario aplica un descuento mayor al 20 %, entonces el sistema exige aprobación del supervisor." | #1, #2, #4 | Test unitario de dominio + test de integración del endpoint |
```

Antes de continuar, verificar el conteo: **número de criterios de entrada = número de criterios cubiertos**.

### 5. Supuestos y decisiones

Mínimos y explícitos. Cada uno con su justificación y su impacto si resulta incorrecto.

**La ambigüedad real no se resuelve aquí.** Si algo es dual, incompleto o admite dos lecturas razonables, no es un supuesto: es una pregunta para la Fase 3.

---

## Fase 3 — Resolver ambigüedades (no inventar)

**Regla dura: todo lo dual, incompleto o ambiguo se pregunta al usuario. Nunca se asume.**

- Usar la tool de preguntas (`AskUserQuestion`), **agrupando las preguntas relacionadas** en una sola interacción en lugar de interrogar de a una.
- Ofrecer opciones concretas cuando existan, con la implicación de cada una. Preguntar "¿cuál de estos dos comportamientos?" es más útil que "¿qué hacemos aquí?".
- Preguntar solo lo que cambia el plan. Si dos respuestas producen el mismo trabajo, no es una pregunta.

Si una respuesta pendiente **bloquea una sección completa** del plan, marcar esa sección como pendiente de forma visible, en lugar de adivinar:

```markdown
> ⏸️ **PENDIENTE DE DEFINICIÓN — bloquea esta sección.**
> Pregunta: ¿el descuento se aplica sobre el subtotal o sobre el total con impuestos?
> Esta sección se completa al recibir la respuesta. No se planifica sobre una suposición.
```

Volver a la Fase 2 con las respuestas y completar las secciones pendientes.

---

## Fase 4 — Presentar, aprobar, guardar

### 4.1 Presentar

Salir de plan mode y presentar el **plan completo** para aprobación del usuario.

**Regla absoluta: no escribir código, configuración ni ningún artefacto de implementación.** Salir de plan mode habilita la escritura del documento, no la del sistema. Los fragmentos de código dentro del plan son ilustrativos y viven dentro del Markdown; no se crean archivos fuente.

Al presentar, señalar de forma destacada:
- Los supuestos tomados.
- Las preguntas que quedaron pendientes, si las hay.
- Los cambios que rompen compatibilidad, si los hay.

### 4.2 Guardar

Al recibir aprobación explícita, ejecutar **la única escritura permitida del skill**:

```
.claude/implementation plans/<ID>-plan.md
```

- `<ID>` es el identificador del work item de Azure DevOps (`US-1234`) o, si no existe, un slug corto derivado del título en inglés (`apply-order-discount`).
- **Crear la carpeta si no existe.**
- Si el archivo ya existe, no sobrescribir en silencio: preguntar si se reemplaza o se versiona (`<ID>-plan-v2.md`).
- Guardar el plan completo, con las respuestas de la Fase 3 ya incorporadas.

### 4.3 Cerrar

Informar que el plan está listo, indicar la ruta exacta del documento y **cuál es el siguiente paso**:

```
Plan guardado en: .claude/implementation plans/US-1234-plan.md

Siguiente paso — implementación:
/user-story-implement ".claude/implementation plans/US-1234-plan.md"
```

**El skill se detiene aquí. No implementa.** Si el usuario pide continuar con la implementación en el mismo turno, responder que ese trabajo corresponde a `/user-story-implement` y esperar a que lo invoque.

---

## Reglas absolutas (resumen)

| # | Regla |
|---|---|
| 1 | Prosa del plan en español; identificadores de código en inglés. Sin excepciones. |
| 2 | Leer la memoria del proyecto es la tarea #1, antes que cualquier otra cosa. |
| 3 | Los criterios de aceptación se extraen verbatim y se trazan uno a uno. |
| 4 | Cero escrituras hasta la aprobación; después, exactamente una: el documento de plan. |
| 5 | Reutilizar antes que crear; toda creación nueva se justifica por escrito. |
| 6 | Lo ambiguo se pregunta agrupado, nunca se asume. |
| 7 | La sección de cambios estructurales nunca se omite: si no aplica, se dice. |
| 8 | Cada criterio de aceptación cubierto por al menos un ítem del plan. |
| 9 | El plan respeta la dirección de dependencias de Clean Architecture (Domain → afuera). |
| 10 | El skill termina entregando a `/user-story-implement`. Nunca implementa. |

---

## Anti-patrones

| Anti-patrón | Por qué está prohibido |
|---|---|
| Crear "solo la interfaz" o "solo el esqueleto" durante la planificación | Es implementación. Rompe la regla del skill. |
| Parafrasear los criterios de aceptación | Se pierde precisión y trazabilidad frente al work item. |
| Asumir el comportamiento ante un requisito ambiguo | Genera retrabajo. Va a la Fase 3. |
| Proponer un componente nuevo sin buscar el existente | Duplica el sistema. Ver Fase 1, punto 2. |
| Plan ordenado por tipo de archivo en vez de por dependencia | No es ejecutable en ese orden. |
| Omitir la sección de cambios estructurales porque "no había" | Ambiguo para quien implementa. Se declara explícitamente. |
| Guardar el plan sin aprobación | El único write requiere aprobación previa. |
| Continuar con la implementación "ya que el plan está aprobado" | Es otro skill, con otras reglas. |

---

## Checklist antes de presentar

- [ ] Memoria del proyecto leída (`CLAUDE.md`, `.claude/`, ADRs, planes previos).
- [ ] Requerimiento localizado por el orden de prioridad definido y leído completo.
- [ ] Criterios de aceptación extraídos verbatim.
- [ ] Repositorio investigado: capas afectadas, reutilización, paridad de superficies, estado actual.
- [ ] Las 5 secciones del documento están presentes y en orden.
- [ ] Sección 2 presente aunque sea para declarar que no aplica.
- [ ] Plan ordenado por dependencia y taggeado por capa/superficie.
- [ ] Tabla de trazabilidad: todos los criterios cubiertos; conteo verificado.
- [ ] Ambigüedades preguntadas, no asumidas; pendientes marcadas si bloquean.
- [ ] Cero archivos de código creados o modificados.
- [ ] Siguiente comando indicado al cerrar.
