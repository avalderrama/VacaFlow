# US-026 — README

**Must** · `M` · Depende de: `TE-002`, `TE-003` · Traza: `TC-09`, `TC-11`

## 1. Entendimiento

**Descripción verbatim (Backlog.md):** *"Covers prerequisites, starting the API, starting the web application, the SQLite file location, the reset procedure, the seeded accounts of §3.6, the endpoint summary, scope limitations and the deferred backlog. A reviewer following it reaches the full workflow unaided."*

Esta redacción es la única fuente de criterios de aceptación — no está en formato Given/When/Then como otras historias, sino como una lista de secciones obligatorias. `Intent.md` §12 (deliverable 5) confirma la misma lista casi palabra por palabra: *"Setup, how to run API and Web, how to access SQLite, seeded accounts, endpoint summary, scope limitations, deferred backlog."*

**Estado actual del `README.md`:** existe pero quedó desactualizado desde el work package 3.1 (skeleton sin comportamiento). Falta:
- Instrucciones para levantar la aplicación web (Next.js ya está scaffoldeado desde US-017, el README todavía dice "not yet scaffolded").
- Resumen de endpoints (no existe ninguna tabla/lista).
- Un banner de estado desactualizado ("solution skeleton in place, no business behaviour yet") que ya no es cierto — el MVP completo (`AC-01`–`AC-14`) está implementado.
- Una nota de "open decision" sobre asignación de manager (`OQ-01`) que ya fue resuelta por el seeder (`TE-003`).
- **Bug real:** el comando documentado para levantar la API usa `--urls http://localhost:5080`, pero `next.config.mjs` reenvía `/api/*` a `http://localhost:5217` (el puerto real de `launchSettings.json`). Un revisor que siga el README literalmente levanta una web que no puede hablar con la API.

**Lo que ya está bien y se conserva:** prerequisitos (tabla de versiones), comando de build, comando de test, procedimiento de reset, tabla de cuentas sembradas, estructura de carpetas, tests de arquitectura, convenciones, tabla de documentación, stack, notas y nota de seguridad.

**Alcance — entra:**
- Reescribir el banner de estado para reflejar que el MVP (`AC-01`–`AC-14`) está completo.
- Corregir el puerto de la API a `5217` (o quitar el `--urls` explícito y usar el perfil `http` por defecto).
- Agregar la sección "Starting the web application" (prerequisitos de Node ya en la tabla, `npm install`, `npm run dev`, puerto `3000`, dependencia del API corriendo en `5217`).
- Agregar la sección "Endpoint summary" con los 13 endpoints reales (`/api/auth/*`, `/api/absence-types`, `/api/requests/*`), agrupados como en `Intent.md` §7.5.
- Agregar una sección "Scope limitations" que enlace a `Intent.md` §5/§6 en vez de duplicar las tablas.
- Agregar una sección "Deferred backlog" que resuma los ítems diferidos (`OS-01`–`OS-25`) por categoría, enlazando a `Intent.md` §6 y a `docs/Backlog.md` Parte B.
- Quitar la nota de "open decision" `OQ-01` (resuelta) y actualizar la tabla de documentación si aplica.

**Alcance — no entra:**
- No se modifica código fuente, configuración de build, tests ni `launchSettings.json`.
- No se crea `docs/user-stories/US-026.md` — la historia vive en `Backlog.md`.
- No se genera el prototipo HTML en inglés (`US-028`) ni el paquete/video (`US-029`) — quedan fuera de esta historia.

## 2. Cambios estructurales / de base

No se requieren cambios de esquema, configuración ni dependencias. Es una edición de un único archivo Markdown.

## 3. Plan ordenado por dependencia

| # | Capa | Acción | Artefacto | Notas |
|---|------|--------|-----------|-------|
| 1 | Docs | Modificar | `README.md` | Único artefacto. Reescritura de banner de estado, corrección de puerto de API, nueva sección "Starting the web application", nueva sección "Endpoint summary", nueva sección "Scope limitations", nueva sección "Deferred backlog", eliminación de la nota `OQ-01` resuelta |

Sin dependencias entre sub-cambios — todos son ediciones del mismo archivo, se hacen en una sola pasada.

## 4. Casos de uso y tabla de trazabilidad

| Historia | Criterio (verbatim, de la descripción de la historia) | Ítems del plan que lo cubren | Cómo se verifica |
|---|---|---|---|
| US-026 | "Covers prerequisites" | #1 | Inspección visual — tabla de prerequisitos ya presente, se conserva |
| US-026 | "starting the API" | #1 | Inspección visual — comando corregido al puerto `5217`; se ejecuta y se confirma `GET /health` |
| US-026 | "starting the web application" | #1 | Inspección visual — se ejecuta `npm run dev` siguiendo el README y se confirma que la web carga en `:3000` y llama a la API sin error de CORS/404 |
| US-026 | "the SQLite file location" | #1 | Inspección visual — ya presente ("API project folder"), se conserva |
| US-026 | "the reset procedure" | #1 | Inspección visual — ya presente, se conserva |
| US-026 | "the seeded accounts of §3.6" | #1 | Inspección visual — tabla ya presente y coincide con Backlog.md §3.6, se conserva |
| US-026 | "the endpoint summary" | #1 | Inspección visual — nueva tabla/lista con los 13 endpoints reales, verificados contra el código de `RequestEndpoints.cs`, `AuthEndpoints.cs`, `AbsenceTypeEndpoints.cs` |
| US-026 | "scope limitations" | #1 | Inspección visual — nueva sección con enlace a `Intent.md` §5/§6 |
| US-026 | "the deferred backlog" | #1 | Inspección visual — nueva sección resumiendo `OS-01`–`OS-25` |
| US-026 | "A reviewer following it reaches the full workflow unaided" | #1 | Verificación end-to-end: seguir el README desde cero (build, run API, run web, sign-in con cuenta sembrada, crear/enviar/decidir una request) sin consultar ningún otro documento |

Conteo: 10 criterios de entrada → 10 cubiertos.

## 5. Supuestos y decisiones

- **Puerto de la API:** se documenta `5217` (perfil `http` de `launchSettings.json`, el mismo que usa `next.config.mjs`) en vez del `5080` actual, porque es el único puerto con el que la web funciona. Impacto si es incorrecto: bajo — es un hecho verificable en el propio repo, no una interpretación.
- **No se duplica contenido de `Intent.md`/`Backlog.md` en las secciones nuevas de "Scope limitations" y "Deferred backlog":** se resume y se enlaza, siguiendo el patrón que el README ya usa en la sección "Documentation". Evita que el README y los documentos madre diverjan.
- **El banner de estado se reemplaza, no se elimina:** se conserva el patrón de "banner de estado" en la parte superior, pero reflejando que el MVP (`AC-01`–`AC-14`) está completo, ya que sería engañoso dejar "no business behaviour yet".

No hay ambigüedades duales — la descripción de la historia y `Intent.md` §12 coinciden verbatim en la lista de secciones requeridas, así que no hay preguntas pendientes.
