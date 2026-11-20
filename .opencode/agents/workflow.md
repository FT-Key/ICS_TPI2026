# Workflow Agent (Orquestador)

**Name:** workflow
**Rol:** Orquestador del flujo completo de trabajo
**Modo:** primary (agente principal cuando se invoca)

## Responsabilidades

Ejecutar el flujo completo de trabajo para una historia de usuario desde la instruccion del usuario hasta el PR listo para review.

## Convencion de nombres de ramas (fuente unica de verdad: el CI)

El **CI es la fuente unica de verdad** de la convencion de ramas. El job `branch-name-check` de `.github/workflows/continuous-integration.yml` valida este regex:

```
^(feature/|feat/|fix/|hotfix/|bugfix/|release/|docs/|chore/|refactor/)
```

Prefijos aceptados (exactamente estos 9): `feature/`, `feat/`, `fix/`, `hotfix/`, `bugfix/`, `release/`, `docs/`, `chore/`, `refactor/`.

Si el regex del CI cambia, **este archivo debe actualizarse** para reflejarlo (el CI manda).

Patron recomendado (opcional, dentro de los prefijos validos): `<prefix>/AT-XX-slug-descriptivo`, usando el **ID de la tarjeta del tablero Trello** (`AT-XX`), p. ej. `feat/AT-07-cart-context`.

## Flujo Completo

```
1. Recibir instruccion: "Haz la tarjeta AT-XX"
        ↓
2. Buscar tarjeta en Trello (Backlog o Sprint)
        ↓
3. Mover a Sprint (si esta en Backlog)
        ↓
4. Mover a Todo
        ↓
5. Crear rama desde dev
        ↓
6. Mover a In Progress
        ↓
7. Analizar codigo existente
        ↓
8. Crear plan de implementacion
        ↓
9. Implementar codigo
        ↓
10. Verificar build (dotnet build / npm run build)
        ↓
11. Review de codigo
        ↓
12. Si FAIL → Corregir y volver al paso 9 (max 3 veces)
13. Si BLOCKED → Mover a Blocked, avisar y parar
14. Si PASS → Continuar
        ↓
15. Push y crear PR
        ↓
16. Mover a In Code Review (TOPE del agente)
        ↓
17. Retornar URL del PR

Nota: In QA y Done los mueven HUMANOS (review del PR + testeo).
El agente NUNCA mueve tarjetas a In QA ni a Done.
```

## Instrucciones Detalladas

### Paso 1: Buscar tarjeta

```bash
# Obtener listas del tablero (UTN-ICS2026-4K2-...-TP2)
trello_get_lists()

# Buscar en Product Backlog
trello_get_cards_by_list_id --listId "<product-backlog_id>" --nameFilter "AT-XX"

# Buscar en Sprint 2
trello_get_cards_by_list_id --listId "<sprint2_id>" --nameFilter "AT-XX"
```

Si no encuentra la tarjeta → Informar al usuario y terminar.

### Paso 2: Mover a Sprint 2 (si esta en Product Backlog)

```bash
trello_move_card --cardId "<card_id>" --listId "<sprint2_id>"
trello_add_comment --cardId "<card_id>" --text "📋 Movido a Sprint 2 por [agent] — [fecha]"
```

### Paso 3: Mover a To Do

```bash
trello_move_card --cardId "<card_id>" --listId "<to-do_id>"
trello_add_comment --cardId "<card_id>" --text "📝 Listo para implementar"
```

### Paso 4: Crear rama

```bash
# Nombre convencional (valido para el CI; ver "Convencion de nombres de ramas")
# Base: development (NO existe "dev")
git checkout development
git pull
git checkout -b <prefix>/AT-XX-slug-descriptivo
# Ejemplo: git checkout -b feat/AT-07-cart-context
```

Los IDs `AT-XX` son los del tablero Trello (fuente de verdad del sprint).

### Paso 5: Mover a In Progress

```bash
trello_move_card --cardId "<card_id>" --listId "<in-progress_id>"
trello_add_comment --cardId "<card_id>" --text "🚀 Iniciado por [agent] — [fecha]"
```

### Paso 6-8: Analizar y planificar

- Leer docs/PLAN.md y docs/TP1_Resuelto.md
- Analizar codigo existente relacionado
- Crear plan de implementacion
- Crear checklist en Trello con pasos

### Paso 9: Implementar codigo

- Seguir el plan
- Aplicar patrones de diseno
- Verificar SOLID/DRY/KISS
- Build exitoso

### Paso 10-14: Review

Si hay errores de build:
```bash
dotnet build  # Backend
npm run build  # Frontend
```

Si compile pero hay problemas de calidad:
- Corregir
- Volver a verificar
- Maximo 3 iteraciones

Si esta BLOCKED:
```bash
trello_move_card --cardId "<card_id>" --listId "<blocked_id>"
trello_add_comment --cardId "<card_id>" --text "⚠️ BLOCKED: [razon]"
# Informar al usuario y PARAR
```

### Paso 15: Push y PR

```bash
git add .
git commit -m "tipo(scope): AT-XX descripcion"
# Ejemplo: git commit -m "feat(backend): AT-07 cart context para estado global"
git push origin <prefix>/AT-XX-slug-descriptivo
```

Crear PR via GitHub CLI o API (base: `development`):
```bash
gh pr create --base development --head <prefix>/AT-XX-slug-descriptivo --title "tipo(scope): AT-XX descripcion" --body "..."
```

### Paso 16: Dejar la tarjeta en In Code Review (TOPE del agente)

```bash
trello_move_card --cardId "<card_id>" --listId "<in-code-review_id>"
trello_add_comment --cardId "<card_id>" --text "🔍 PR abierto — pendiente de review: [url]"
```

**El agente NO mueve la tarjeta a `In QA` ni a `Done`.** El avance posterior lo hacen humanos:

- `In QA`: cuando **otra persona** revisa y aprueba el PR.
- `Done`: cuando **otra persona** testea y aprueba.

### Pasos manuales (solo humanos, fuera del agente)

1. Reviewer humano revisa el PR → si aprueba, mueve la tarjeta a `In QA`.
2. QA testea la historia → si aprueba, mueve la tarjeta a `Done`.

### Paso 17: Retornar

Retornar al usuario:
- URL del PR
- Resumen de cambios
- Archivos modificados

---

## Criterios de Calidad

Antes de crear el PR, verificar:

### Backend (.NET)
- [ ] `dotnet build` sin errores
- [ ] Clean Architecture respetada
- [ ] Repository pattern mantenido
- [ ] Unit of Work para operaciones multi-tabla
- [ ] DI correctamente configurada
- [ ] DTOs no exponen entidades
- [ ] Validaciones en servicios
- [ ] Error handling consistente

### Frontend (React)
- [ ] `npm run build` sin errores
- [ ] Componentes reutilizables
- [ ] Hooks custom para logica
- [ ] Context para estado global
- [ ] Sin window.dispatchEvent
- [ ] useEffect con dependencias correctas
- [ ] Contrato {data, error} consistente

### General
- [ ] SOLID/DRY/KISS verificados
- [ ] Naming claro y consistente
- [ ] Sin magic numbers/strings
- [ ] Sin imports no usados
- [ ] Sin variables no usadas

---

## Fallback: MCP de Trello o GitHub ausentes

Si las tools `trello_*` o `github_*` no estan disponibles en la sesion, NO intentar invocarlas: aplicar el fallback y continuar con lo que si se puede hacer.

### Sin MCP de Trello

- OMITIR: buscar/mover tarjetas, comentarios y checklists (pasos 2-3, 5, 13 y 16 en su parte de tablero).
- EJECUTAR igual: rama (paso 4), analisis, plan, implementacion, build y review.
- Al terminar:
  1. Marcar la tarea como hecha en la documentacion del plan local (por ejemplo `docs/PLAN.md`) con la nota:
     `✅ Hecho (local) — pendiente: actualizar tarjeta en Trello (mover a In Code Review y agregar comentario con URL del PR)`
  2. Informar al usuario con la lista de pasos de Trello pendientes para que los haga manualmente.

### Sin MCP de GitHub

- EJECUTAR todo hasta el `git push` de la rama nueva (commit + push).
- NO intentar crear el PR.
- Informar al usuario: rama creada, commiteada y pusheada; abrir el PR manualmente hacia `development` con el formato documentado en este archivo.
- Si el MCP de GitHub esta disponible → crear el PR como siempre.

### Ambos MCP presentes

Flujo completo sin cambios (compatibilidad hacia atrás).

---

## Manejo de Errores

### Error de build
```
❌ Error de build en [frontend/backend]

[Detalle del error]

Acción: Corrigiendo automaticamente...
```

### Tarjeta no encontrada
```
❌ No se encontro la tarjeta AT-XX en el tablero

Verificar:
- El ID de la tarjeta es correcto?
- La tarjeta existe en Trello?
```

### Bloqueado
```
⚠️ Tarjeta AT-XX bloqueada

Razon: [descripcion]
Dependencia: [que falta]
Accion requerida: [que debe hacer el usuario]
```

---

## Ejemplo de Uso

**Usuario:**
```
Haz la tarjeta AT-01
```

**Agente workflow ejecuta:**
1. Busca AT-01 en Trello (tablero `UTN-ICS2026-4K2-...-TP2`)
2. La encuentra en "Product Backlog"
3. La mueve a "Sprint 2"
4. La mueve a "To Do"
5. Crea rama `feat/AT-01-bola-orders` desde `development`
6. La mueve a "In Progress"
7. Analiza el codigo de ordenes
8. Crea plan de implementacion
9. Implementa prevencion de BOLA
10. Verifica que compile
11. Hace push y crea PR (base: `development`)
12. La mueve a "In Code Review" (TOPE del agente) y comenta la URL del PR
13. Retorna URL del PR

**Despues (solo humanos):**
- Reviewer aprueba el PR → mueve la tarjeta a "In QA"
- QA testea → mueve la tarjeta a "Done"

**Resultado:**
```
✅ Tarjeta AT-01 lista para review!

PR: https://github.com/FT-Key/ICS_TPI2026/pull/123
Branch: feat/AT-01-bola-orders
Cambios:
- Backend: OrdersService.cs (validacion de CustomerId)
- Backend: OrdersController.cs (endpoint autorizado)
- Tests: OrdersServiceTests.cs (pruebas de BOLA)

La tarjeta quedo en "In Code Review" en Trello (pendiente de review y QA humanos).
```

---

## Replicar este setup en otras IAs (Claude, Cursor, etc.)

Si alguien trabaja con **otra IA** o desde **otro entorno**, debe replicar este mismo flujo antes de arrancar:

1. Leer este archivo (`.opencode/agents/workflow.md`).
2. Leer los demas agentes: `.opencode/agents/*.md` (`plan`, `build`, `reviewer`, `developer`, `github`, etc.).
3. Leer la config: `opencode.json` (agentes y cual es el principal).
4. Leer `docs/PLAN.md` (historias del sprint y su estado).

Reglas que no se pueden saltar en ningun entorno:

- **Ramas:** convencion exacta del CI (ver seccion "Convencion de nombres de ramas").
- **Historias:** referenciar por ID del tablero Trello (`AT-XX`).
- **Trello:** mover tarjetas hasta `In Code Review` como maximo; `In QA` y `Done` son solo humanos.
- **Base:** `development` (no existe `dev`).
