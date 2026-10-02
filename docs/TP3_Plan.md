# TP3 — Gestión de Configuración y Pipeline CI/CD — Resumen + Plan de Ejecución

> Fuente única leída: `docs/ICS2026_TP3.pdf` (UTN FRT, Ingeniería y Calidad del Software, 2026).
> Estado: **SOLO PLAN — no se ejecutó ninguna tarea técnica todavía.**

---

## 1. Resumen: qué pide el TP3

**Objetivo oficial:** establecer gestión de la configuración con un modelo de ramificación profesional en Git, contenerizar la API .NET del e-commerce con Docker, y automatizar CI/CD con GitHub Actions hacia un hosting gratuito. Todo planificado, estimado y gestionado en el tablero ágil (Sprint 2) bajo DoR y DoD.

**Sistema base:** `.NET API + React + Base de Datos` (en este repo: `ICS_TPI2026_backend/` .NET 8 + `ICS_TPI2026_frontend/` React).

### Bloque 1 — Modelo de ramificación en Git + políticas (Clase 1)

| Requisito del PDF | Detalle |
|---|---|
| Ramas principales | Configurar en GitHub `main` (producción/estable) y `development` (integración continua). Nota: en este repo hoy el default es `dev`, hay que renombrar/crear `development` o alinear con cátedra. |
| Estrategia de branching | Flujo por ramas `feature/nombre-funcionalidad` o `hotfix/descripcion`. **Prohibido commitear directo a `main` ni a `development`.** |
| Branch Protection Rules | Exigir **al menos 1 Peer Review por PR** antes de mergear a `development` o `main`. |

### Bloque 2 — Dockerización de la API .NET (Clase 2)

| Requisito del PDF | Detalle |
|---|---|
| Dockerfile Multi-Stage | `build/publish` (SDK) + `runtime` liviano (ASP.NET runtime). Optimizado, en el repo. |
| Inyección de variables de entorno | App configurada por entorno (connection strings, JWT, URLs). Nada hardcodeado. |
| Verificación local | Probar el contenedor localmente: la API debe iniciar y responder peticiones. |

### Bloque 3 — GitHub Actions + despliegue gratuito (Clase 3)

| Requisito del PDF | Detalle |
|---|---|
| Workflow | Crear `.github/workflows/deploy.yml` que corra ante `push`/`merge` en ramas de integración/despliegue. |
| Etapas del pipeline | Restore paquetes → build limpio → build imagen Docker (o empaquetado artefacto) → deploy automático. |
| Hosting costo cero (opciones de cátedra) | `Azure App Service (Plan Free)` + `SQL Database (Plan Free)`. |

### Bloque 4 — Habilitar Swagger

> Debe funcionar Swagger desde el **domain público que da Azure** (hoy en `Program.cs` solo se habilita `if (app.Environment.IsDevelopment())` → hay que cambiarlo).

### Entregables requeridos (textual PDF)

1. Tarjetas en Trello + estimaciones, preparar **Sprint 2**.
2. Stories en **Done**, respaldado por acceso al repositorio.
3. Acceso a la API, con Swagger habilitado.
4. **Documentar proceso completo**: estrategia de ramificaciones, Dockerfile, pipeline, conexión API ↔ BD.

---

## 2. Plan de ejecución (cómo llevarlo a cabo, paso a paso)

> Estado actual (02/10/2026): `dev` renombrado a `development` en remoto y default `origin/HEAD -> origin/development`. Rama local alineada.
> Orden ajustado aprobado: A-parcial → D-mínimo (`continuous-integration.yml` sin Docker) → A-completo (activar status checks) → B (agregar job `docker-build`) → C/D-full (`deploy`) → E → F.

```
Orden vigente:
Fase A-parcial (ramas + ruleset único) → Fase D-mínimo (CI front+back sin Docker) → Fase A-completo (exigir checks) → Fase B (Docker local) → Fase C (Azure) → Fase D-full (deploy) → Fase E (Swagger+env) → Fase F (Trello+Sprint2+docs)
```

### Fase A — Git: ramas `main` + `development` protegidas, conventional names

**A.1 Normalizar ramas.** ✅ DONE 02/10/2026: remoto ya es `origin/development` (default) y se borró `origin/dev`. Local alineado con `git branch -m dev development + set-upstream-to origin/development`.

**A.2 Crear `main` estable si no existe contenido correcto**, y no commitear directo nunca más:
```powershell
git checkout main; git pull
git checkout -b feature/nombre-funcionalidad  # o hotfix/descripcion
# ... commits ...
git push -u origin feature/nombre-funcionalidad
# luego PR → review → merge, nunca push directo a main/development
```

**A.3 Proteger con Rulesets (lo que pedís: main y dev protegidas + solo conventional names).**

GitHub hoy recomienda **Rulesets** sobre las viejas Branch Protection Rules. Ruta: `Settings → Rules → Rulesets → New ruleset → Branch`.

Ruleset 1 — `protect-main-development` (único, no uno por rama):
- `Enforcement: Active`, `Bypass: FT-Key / Always allow` solo para emergencias (arreglar el propio ruleset). Todo lo demás por PR.
- `Target branches`: `main` + `development`.
- Reglas: `Restrict creations/updates/deletions` ON, `Require a pull request before merging` con approvals `1` (DoD pide 1 par, no 2), `Dismiss stale approvals` ON, `Require approval of most recent push` ON, `Require conversation resolution` ON, `Block force pushes` ON. `Require status checks` OFF hasta Fase D-mínimo, luego exigir `backend-build`, `frontend-build`, `branch-name-check`. `Require linear history / signed commits / deployments / scanning` OFF.

Ruleset 2 — `conventional-branch-names`: verificado 02/10/2026 que GitHub Free en Branch ruleset NO muestra opción de nombre de rama. Se descarta ruleset y se reemplaza por control compensatorio CI `branch-name-check` en `continuous-integration.yml` (falla si `head_ref` no es `feature/*|hotfix/*|release/*|docs/*`). Patrón de referencia:
```
^(main|development|dev|feature\/[a-z0-9-]+|hotfix\/[a-z0-9-]+|release\/v[0-9]+\.[0-9]+.*|docs\/[a-z0-9-]+)$
```
- Así solo se pueden crear `feature/*`, `hotfix/*`, `release/*`, `docs/*` además de las troncales. Cualquier `mi-rama-test` será rechazada en push/PR.
- Alternativa si el plan Free no deja regex en Ruleset: hacer cumplir por CI con un job `branch-name-check` que falle si `$GITHUB_HEAD_REF` no matchea, + documentarlo como control compensatorio.

Verificación Fase A: intentar `git push origin main` directo → debe fallar; intentar crear rama `test123` → debe fallar o el CI debe marcarla roja; PR sin aprobación → botón Merge bloqueado.

### Fase B — Dockerizar el backend (.NET 8)

**B.1 Dónde va:** `ICS_TPI2026_backend/Dockerfile` (junto al `.sln`, no dentro de `Dsw2025Tpi.Api/`), para que el contexto incluya los 4 proyectos.

**B.2 Dockerfile multi-stage propuesto (no crear aún):**
```dockerfile
# build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Dsw2025Tpi.sln ./
COPY Dsw2025Tpi.Api/*.csproj Dsw2025Tpi.Api/
COPY Dsw2025Tpi.Application/*.csproj Dsw2025Tpi.Application/
COPY Dsw2025Tpi.Domain/*.csproj Dsw2025Tpi.Domain/
COPY Dsw2025Tpi.Data/*.csproj Dsw2025Tpi.Data/
RUN dotnet restore
COPY . .
RUN dotnet publish Dsw2025Tpi.Api/Dsw2025Tpi.Api.csproj -c Release -o /app/publish --no-restore

# runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Dsw2025Tpi.Api.dll"]
```
Por qué así: capa de restore cacheable, publish Release, runtime liviano sin SDK, puerto 8080 (el que exige Azure Container Apps / App Service con contenedor).

**B.3 Variables de entorno (inyección, no hardcode):**
- `ConnectionStrings__Dsw2025Tpi` (Azure SQL), `Jwt__Key/Issuer/Audience`, `DefaultAdminUser__*`, `ASPNETCORE_ENVIRONMENT=Production`.
- En código ya se lee vía `builder.Configuration`, solo hay que **no** dejar secretos en `appsettings.json` y pasarlos por `-e` en local y por App Settings / Secrets en Azure y GitHub Actions.

**B.4 Verificación local (comando planificado):**
```powershell
docker build -t ics-api:tp3 -f ICS_TPI2026_backend/Dockerfile ICS_TPI2026_backend
docker run -d -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Production -e ConnectionStrings__Dsw2025Tpi="..." --name ics-api-tp3 ics-api:tp3
Invoke-RestMethod http://localhost:8080/healthcheck
Invoke-RestMethod http://localhost:8080/swagger/v1/swagger.json
docker logs ics-api-tp3
```
Criterio: `/healthcheck` 200 y `swagger.json` 200 antes de seguir a Azure.

### Fase C — Despliegue costo cero en Azure (App Service Free + SQL Free)

1. Crear en Azure (portal o CLI): Resource Group → `SQL Database (Free)` → copiar connection string → `App Service Plan Free (F1)` → `App Service` (stack .NET 8 o contenedor).
2. Dos estrategias válidas (elegir una y documentarla):
   - **A. Sin contenedor en Azure (más simple):** el pipeline hace `dotnet publish` y `azure/webapps-deploy@v3` con zip. Azure corre la DLL directo.
   - **B. Con contenedor (más fiel al “Docker” del TP):** pipeline hace `docker build/push` a GHCR o ACR y App Service hace pull de la imagen.
3. Configurar en App Service → `Configuration → Application settings`: connection string Azure SQL, JWT, admin, `ASPNETCORE_ENVIRONMENT=Production`, `WEBSITES_PORT=8080` si es contenedor.
4. Migraciones: ya que `Program.cs` hace `dbContext.Database.Migrate()` al arrancar, el deploy aplica solo; alternativa: correr `dotnet ef database update` contra Azure SQL desde local con la connection string de Azure.
5. Verificación: `https://<tu-app>.azurewebsites.net/healthcheck` y `/swagger` responden público.

### Fase D — CI/CD con GitHub Actions (`.github/workflows/deploy.yml`)

### Fase D-mínimo (ahora) — `continuous-integration.yml` monorepo sin Docker + Fase D-full (después)

**Archivo ahora:** `.github/workflows/continuous-integration.yml` con `name: Continuous Integration` (nombre claro pedido por equipo).
**Disparadores:**
```yaml
on:
  push:
    branches: [development]
  pull_request:
    branches: [main, development]
  workflow_dispatch: {}
```

**Jobs ahora (sin Docker):**
1. `backend-build`: `checkout → setup-dotnet 8 → restore Dsw2025Tpi.sln → build Release` en `ICS_TPI2026_backend/`.
2. `frontend-build`: `checkout → setup-node 20 → npm ci → lint → build` en `ICS_TPI2026_frontend/`.
3. `branch-name-check` (solo `pull_request`): falla si `head_ref` no es `feature/*|hotfix/*|release/*|docs/*`. Reemplaza al Ruleset 2 inexistente en Free.

**Después (Fase B/C):** agregar job `docker-build` (valida Dockerfile sin push) y crear `continuous-deployment.yml` para Azure (solo `push a main`: publish/docker push → webapps-deploy → smoke `/healthcheck`).

**Secrets a cargar en GitHub (Settings → Secrets → Actions):** `AZURE_CREDENTIALS` o `AZURE_WEBAPP_PUBLISH_PROFILE`, `AZURE_SQL_CONNECTION_STRING`, `JWT_KEY`, etc. Nunca en el yaml.

Verificación Fase D: PR a `development` corre `build` en verde y exige 1 approval; merge a `main` dispara `deploy` y la URL de Azure actualiza sola.

### Fase E — Swagger público + conexión API↔BD

- Cambio mínimo planificado en `ICS_TPI2026_backend/Dsw2025Tpi.Api/Program.cs`: quitar el `if (IsDevelopment())` y dejar `app.UseSwagger(); app.UseSwaggerUI();` siempre (o condicionado a `!IsProduction` + flag `EnableSwagger=true` en Azure). Sin esto el entregable “Swagger desde el domain de Azure” falla.
- Documentar conexión: diagrama `Browser → Azure App Service (API) → Azure SQL (Free)` + dónde vive cada connection string (App Settings, no código).

### Fase F — Trello Sprint 2 + documentación final

Tarjetas sugeridas (Sprint 2) con estimación:
| Tarjeta | Estimación |
|---|---|
| Configurar ramas main/development + rulesets + conventional names | 2h |
| Dockerfile multi-stage + verificación local | 3h |
| Azure App Service Free + Azure SQL Free + env vars | 3h |
| Pipeline deploy.yml (build+deploy+smoke) | 4h |
| Habilitar Swagger en Azure | 1h |
| Documentación final (estrategia, Dockerfile, pipeline, conexión BD) | 2h |

DoR: tarjeta con criterio de aceptación + responsable. DoD: código en PR con 1 review + CI verde + merge a development/main vía PR + deploy verificado en URL pública + docs actualizadas.

---

## 3. Qué NO se hizo (pendiente de aprobación)

- No se creó/renombró ninguna rama.
- No se tocó ningún Ruleset en GitHub.
- No se creó `Dockerfile` ni `deploy.yml`.
- No se modificó `Program.cs` (Swagger) ni connection strings.
- No se creó nada en Azure ni en Trello.
- Solo se hizo en este turno: (1) habilitar modo plan (ver §4) y (2) crear este archivo.

## 4. Cambio aplicado: modo plan habilitado

Se habilitó el agente `plan` que estaba en `mode: subagent` (no seleccionable):
- `opencode.json` → `agent.plan.mode: subagent → primary` + `permission: { edit: deny, bash/read/glob/grep/task/webfetch/websearch: allow }` (planifica, no edita código).
- `.opencode/agent/plan.md` → frontmatter `mode: subagent → primary` + mismos permisos.
- Verificación: reiniciar opencode y el agente `plan` debe aparecer junto a `workflow` para seleccionar con Tab; debe poder leer/generar planes pero no modificar archivos.

## 5. Próximo paso propuesto

Cuando lo apruebes, el orden de ejecución sería: Fase A → B → C → D → E → F, tarjeta por tarjeta en Trello/Sprint 2 con PRs `feature/tp3-*` hacia `development` y luego `development → main` para el deploy final.
