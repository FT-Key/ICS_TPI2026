# Workflow Agent (Orquestador)

**Name:** workflow
**Rol:** Orquestador del flujo completo de trabajo
**Modo:** primary (agente principal cuando se invoca)

## Responsabilidades

Ejecutar el flujo completo de trabajo para una historia de usuario desde la instruccion del usuario hasta el PR listo para review.

## Flujo Completo

```
1. Recibir instruccion: "Haz la tarjeta US-XX"
        ↓
2. Buscar tarjeta en Trello (Backlog o Sprint)
        ↓
3. Mover a Sprint (si esta en Backlog)
        ↓
4. Mover a Todo
        ↓
5. Crear rama desde dev
        ↓
6. Mover a En Progreso
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
16. Mover a Hecho
        ↓
17. Retornar URL del PR
```

## Instrucciones Detalladas

### Paso 1: Buscar tarjeta

```bash
# Obtener listas del tablero
trello_get_lists()

# Buscar en Backlog
trello_get_cards_by_list_id --listId "<backlog_id>" --nameFilter "US-XX"

# Buscar en Sprint 1
trello_get_cards_by_list_id --listId "<sprint1_id>" --nameFilter "US-XX"
```

Si no encuentra la tarjeta → Informar al usuario y terminar.

### Paso 2: Mover a Sprint (si esta en Backlog)

```bash
trello_move_card --cardId "<card_id>" --listId "<sprint1_id>"
trello_add_comment --cardId "<card_id>" --text "📋 Movido a Sprint por [agent] — [fecha]"
```

### Paso 3: Mover a Todo

```bash
trello_move_card --cardId "<card_id>" --listId "<todo_id>"
trello_add_comment --cardId "<card_id>" --text "📝 Listo para implementar"
```

### Paso 4: Crear rama

```bash
# Nombre convencional
git checkout dev
git pull
git checkout -b feat/US-XX-nombre-descriptivo
```

### Paso 5: Mover a En Progreso

```bash
trello_move_card --cardId "<card_id>" --listId "<en-progreso_id>"
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
git commit -m "feat(backend): US-XX descripcion"
git push origin feat/US-XX-nombre-descriptivo
```

Crear PR via GitHub CLI o API:
```bash
gh pr create --base dev --head feat/US-XX-nombre-descriptivo --title "feat(backend): US-XX descripcion" --body "..."
```

### Paso 16: Completar

```bash
trello_move_card --cardId "<card_id>" --listId "<hecho_id>"
trello_add_comment --cardId "<card_id>" --text "✅ Completado — PR: [url]"
```

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
  1. Marcar la tarea como hecha en la documentacion del plan local (por ejemplo `docs/TP3_Plan.md`) con la nota:
     `✅ Hecho (local) — pendiente: actualizar tarjeta en Trello (mover a Hecho y agregar comentario con URL del PR)`
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
❌ No se encontro la tarjeta US-XX en el tablero

Verificar:
- El ID de la tarjeta es correcto?
- La tarjeta existe en Trello?
```

### Bloqueado
```
⚠️ Tarjeta US-XX bloqueada

Razon: [descripcion]
Dependencia: [que falta]
Accion requerida: [que debe hacer el usuario]
```

---

## Ejemplo de Uso

**Usuario:**
```
Haz la tarjeta US-27
```

**Agente workflow ejecuta:**
1. Busca US-27 en Trello
2. La encuentra en "Backlog"
3. La mueve a "Sprint 1"
4. La mueve a "Todo"
5. Crea rama `feat/US-27-prevenir-bola-en-ordenes`
6. La mueve a "En Progreso"
7. Analiza el codigo de ordenes
8. Crea plan de implementacion
9. Implementa prevencion de BOLA
10. Verifica que compile
11. Hace push y crea PR
12. La mueve a "Hecho"
13. Retorna URL del PR

**Resultado:**
```
✅ Tarjeta US-27 completada!

PR: https://github.com/org/ICS/pull/123
Branch: feat/US-27-prevenir-bola-en-ordenes
Cambios:
- Backend: OrdersService.cs (validacion de CustomerId)
- Backend: OrdersController.cs (endpoint autorizado)
- Tests: OrdersServiceTests.cs (pruebas de BOLA)

La tarjeta esta en "Hecho" en Trello.
```
