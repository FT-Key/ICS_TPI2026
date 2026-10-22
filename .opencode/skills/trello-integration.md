# Skill: Trello Integration

Este skill permite a los subagentes interactuar con el tablero de Trello "TPI Ingenieria y Calidad de Software".

---

## Tablero

**Nombre:** TPI Ingenieria y Calidad de Software
**URL:** https://trello.com/b/tpi-ingenieria-calidad-software

### Listas del Tablero (en orden)

| Lista | Uso | Orden |
|---|---|---|
| **Backlog** | Historias de usuario pendientes | 1 |
| **Sprint 1** | Historias del sprint actual | 2 |
| **Todo** | Historias listas para trabajar | 3 |
| **En Progreso** | Historias siendo implementadas | 4 |
| **Blocked** | Historias bloqueadas | 5 |
| **Revision** | Codigo listo para review | 6 |
| **Hecho** | Historias completadas y mergeadas | 7 |

### Etiquetas

| Etiqueta | Color | Epica |
|---|---|---|
| Seguridad | Rojo | Epica 3 |
| Arquitectura | Naranja | Epica 4 |
| Backend | Azul | Epica 2 |
| Frontend | Verde | Epica 1 |
| Infraestructura | Morado | Epica 5 |
| Bug | Amarillo | Bugs |

---

## Flujo Completo Automatizado

```
┌─────────────────────────────────────────────────────────────┐
│  INSTRUCCION: "Haz la tarjeta US-XX"                        │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  PASO 1: Buscar tarjeta en Trello                           │
│  - Buscar por "US-XX" en todas las listas                   │
│  - Si no existe → Informar al usuario y terminar            │
│  - Si existe → Identificar en que lista esta                │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  PASO 2: Mover a Sprint (si esta en Backlog)                │
│  - Si esta en "Backlog" → Mover a "Sprint 1"                │
│  - Si ya esta en "Sprint 1" o posterior → Continuar        │
│  - Agregar comentario: "📋 Movido a Sprint por [agent]"     │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  PASO 3: Mover a Todo                                       │
│  - Mover tarjeta a lista "Todo"                             │
│  - Agregar comentario: "📝 Listo para implementar"          │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  PASO 4: Crear rama y empezar                               │
│  - Crear rama: feat/[US-XX]-nombre-descriptivo              │
│  - Mover tarjeta a "En Progreso"                            │
│  - Agregar comentario: "🚀 Iniciado por [agent]"            │
│  - Crear checklist con pasos de implementacion              │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  PASO 5: Implementar codigo                                 │
│  - Seguir plan de implementacion                            │
│  - Aplicar patrones de diseno                               │
│  - Verificar SOLID/DRY/KISS                                 │
│  - Build exitoso (dotnet build / npm run build)             │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  PASO 6: Review de codigo                                   │
│  - Si FAIL → Corregir y volver al PASO 5 (max 3 veces)     │
│  - Si PASS → Continuar                                      │
│  - Si BLOCKED → Mover a "Blocked" y avisar al usuario      │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  PASO 7: Crear PR                                           │
│  - Push a la rama                                           │
│  - Crear PR hacia dev                                       │
│  - Formato: feat(backend): US-XX descripcion                │
│  - Agregar checklist de verificacion al PR                  │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  PASO 8: Completar                                          │
│  - Mover tarjeta a "Hecho"                                  │
│  - Agregar comentario: "✅ Completado — PR: [url]"          │
│  - Retornar URL del PR al usuario                           │
└─────────────────────────────────────────────────────────────┘
```

---

## Acciones Disponibles

### 1. Buscar tarjeta por ID

**Cuando:** Al inicio de cualquier tarea

**Comando:**
```bash
# Usar MCP de Trello
trello_get_cards_by_list_id --listId "<backlog_id>" --nameFilter "US-27"
trello_get_cards_by_list_id --listId "<sprint1_id>" --nameFilter "US-27"
```

### 2. Mover tarjeta entre listas

**Cuando:** En cada transicion de estado

**Comandos:**
```bash
# Mover a Sprint
trello_move_card --cardId "<card_id>" --listId "<sprint1_id>"

# Mover a Todo
trello_move_card --cardId "<card_id>" --listId "<todo_id>"

# Mover a En Progreso
trello_move_card --cardId "<card_id>" --listId "<en-progreso_id>"

# Mover a Blocked
trello_move_card --cardId "<card_id>" --listId "<blocked_id>"

# Mover a Revision
trello_move_card --cardId "<card_id>" --listId "<revision_id>"

# Mover a Hecho
trello_move_card --cardId "<card_id>" --listId "<hecho_id>"
```

### 3. Agregar comentario a tarjeta

**Cuando:** En cada paso importante

**Comando:**
```bash
trello_add_comment --cardId "<card_id>" --text "[mensaje]"
```

**Ejemplos:**
```
📋 Movido a Sprint por [agent] — [fecha]
📝 Listo para implementar
🚀 Iniciado por [agent] — [fecha]
🔨 Implementando: [archivos modificados]
⚠️ BLOCKED: [razon del bloqueo]
🔍 Review: PASS/FAIL — [detalles]
✅ Aprobado por Reviewer
✅ Completado — PR: [url]
```

### 4. Agregar checklist a tarjeta

**Cuando:** Al iniciar implementacion

**Comando:**
```bash
trello_create_checklist --cardId "<card_id>" --name "Pasos de implementacion"
trello_add_checklist_item --cardId "<card_id>" --checkListName "Pasos de implementacion" --text "Paso 1"
```

---

## Fallback: MCP de Trello o GitHub ausentes

El flujo se ejecuta siempre que sea posible. Si falta un server MCP, se degrada elegantemente en vez de fallar o abortar la tarea.

**Deteccion:** si las tools `trello_*` o `github_*` no estan disponibles en la sesion, el MCP correspondiente esta ausente. NO intentar invocar tools inexistentes; simplemente aplicar el fallback y continuar con lo que si se puede hacer.

### Sin MCP de Trello

- **OMITIR:** buscar/mover tarjetas, comentarios y checklists (parte de tablero de los pasos 1-4 y 8).
- **EJECUTAR igual:** rama, plan, implementacion, build y review (pasos 4-7 en su parte de codigo).
- Al terminar la tarea:
  1. **Marcar la tarea como hecha en la documentacion del plan local** (por ejemplo `docs/TP3_Plan.md` o el plan de la US correspondiente) con una nota tipo:
     `✅ Hecho (local) — pendiente: actualizar tarjeta en Trello (mover a Hecho y agregar comentario con URL del PR)`
  2. **Informar al usuario** con la lista concreta de pasos de Trello que quedaron pendientes para que los haga manualmente (mover tarjeta, agregar comentarios, etc.).

### Sin MCP de GitHub

- **EJECUTAR** todo hasta el `git push` de la rama nueva (commit + push).
- **NO intentar crear el PR.**
- **Informar al usuario:** rama creada, commiteada y pusheada; abrir el PR manualmente hacia `development` usando el formato de PR documentado en este skill.
- Si el MCP de GitHub esta disponible → crear el PR como siempre.

### Ambos MCP presentes

Flujo completo sin cambios (compatibilidad hacia atrás).

---

## Manejo de BLOCKED

**Cuando una tarjeta esta bloqueada:**

1. Mover tarjeta a lista "Blocked"
2. Agregar comentario con la razon del bloqueo:
```
⚠️ BLOCKED — [fecha]

Razon: [descripcion del problema]
Dependencia: [que falta para desbloquear]
Accion requerida: [que debe hacer el usuario]
```
3. Informar al usuario con el motivo
4. **NO continuar** hasta que el usuario desbloquee

**Ejemplo de bloqueo:**
```
⚠️ BLOCKED — 2026-08-29

Razon: Endpointrequiere ID de usuario que aun no existe en BD
Dependencia: US-25 (creacion de usuarios) debe completarse primero
Accion requerida: Completar US-25 o proporcionar ID de usuario para pruebas
```

---

## Mapeo US-XX a Tarjeta Trello

Las historias de usuario tienen un ID (US-XX) que debe coincidir con el nombre o referencia de la tarjeta en Trello.

| US | Titulo Trello |
|---|---|
| US-17 | JWT secret no en codigo fuente |
| US-18 | Credenciales admin en env vars |
| US-24 | Unit of Work |
| US-23 | Global exception middleware |
| US-21 | Token en HttpOnly cookie |
| US-35 | Backend soporte cookies |
| US-16 | useCart como Context |
| US-59 | AppShell para layout |
| US-17F | Reemplazar dispatchEvent |
| US-25 | Dependencia circular |
| US-27 | BOLA en ordenes |
| US-30 | Docker SQL Server |

---

## Naming de Ramas

```
feat/[US-XX]-nombre-descriptivo     → Nueva funcionalidad
fix/[US-XX]-nombre-descriptivo      → Bug fix
refactor/[US-XX]-nombre-descriptivo → Refactorizacion
chore/[US-XX]-nombre-descriptivo    → Tareas de mantenimiento
```

**Ejemplo:**
```
feat/US-27-prevenir-bola-en-ordenes
```

---

## Conventional Commits

```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

### Types
- `feat`: Nueva funcionalidad
- `fix`: Bug fix
- `refactor`: Refactorizacion sin cambio de comportamiento
- `chore`: Tareas de mantenimiento
- `docs`: Documentacion
- `style`: Formato (no afecta logica)
- `perf`: Mejora de rendimiento

### Scope
`backend`, `frontend`, `api`, `db`, `auth`, `cart`, `orders`, `products`

---

## Formato de Pull Request

```markdown
## [Type] US-XX: Titulo corto descriptivo

### Descripcion
[Que se hizo y por que]

### Cambios realizados
- Archivo 1: que se cambio
- Archivo 2: que se cambio

### Dependencias
- Depende de: [otra PR si aplica]
- Cierra: US-XX

### Checklist
- [ ] Codigo compila sin errores
- [ ] No hay warnings nuevos
- [ ] Clean Architecture respetada
- [ ] Patrones de diseno aplicados
- [ ] SOLID/DRY/KISS verificados

### Screenshots (si aplica)
[Capturas de cambios visuales]
```

---

## IDs de Listas (para usar en comandos)

> **NOTA:** Estos IDs deben obtenerse dinamicamente o configurarse estaticamente.

```javascript
// Obtener IDs de las listas
const lists = await trello_get_lists();
const listMap = {
  backlog: lists.find(l => l.name === "Backlog").id,
  sprint1: lists.find(l => l.name === "Sprint 1").id,
  todo: lists.find(l => l.name === "Todo").id,
  enProgreso: lists.find(l => l.name === "En Progreso").id,
  blocked: lists.find(l => l.name === "Blocked").id,
  revision: lists.find(l => l.name === "Revision").id,
  hecho: lists.find(l => l.name === "Hecho").id
};
```

---

## Notas

- El skill `trello-integration` debe ser invocado por los agentes automaticamente
- Cada movimiento de tarjeta debe incluir un comentario descriptivo
- Las etiquetas se asignan al crear la tarjeta, no al mover
- Si una tarjeta no existe, el agent debe informar al usuario
- **IMPORTANTE:** Si una tarjeta esta bloqueada, DEBE avisarse al usuario antes de continuar
