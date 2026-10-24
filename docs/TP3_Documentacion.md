# TP N° 3 — Gestión de Configuración y Pipeline CI/CD

**Ingeniería y Calidad del Software — UTN FRT — Comisión 4K2 — 2026**

- **Proyecto:** Plataforma E-commerce (API .NET 8 + React + SQL Server)
- **Repositorio:** https://github.com/FT-Key/ICS_TPI2026
- **Fecha:** 21 de octubre de 2026

---

## Índice

1. [Introducción y alcance](#1-introducción-y-alcance)
2. [Estrategia de ramificaciones](#2-estrategia-de-ramificaciones)
3. [Dockerfile y dockerización de la API](#3-dockerfile-y-dockerización-de-la-api)
4. [Pipeline de CI/CD (GitHub Actions)](#4-pipeline-de-cicd-github-actions)
5. [Conexión entre la API y la base de datos](#5-conexión-entre-la-api-y-la-base-de-datos)
6. [Swagger](#6-swagger)
7. [Despliegue en Azure (costo cero)](#7-despliegue-en-azure-costo-cero)
8. [Frontend en Vercel, endpoints y credenciales](#8-frontend-en-vercel-endpoints-y-credenciales)
9. [Verificación del funcionamiento](#9-verificación-del-funcionamiento)
10. [Problemas encontrados y soluciones](#10-problemas-encontrados-y-soluciones)
11. [Anexo: archivos del proyecto](#11-anexo-archivos-del-proyecto)

---

## 1. Introducción y alcance

### 1.1 Objetivo del TP

Establecer una estrategia de gestión de la configuración mediante un modelo de ramificación profesional en Git, contenerizar la API .NET del e-commerce con Docker y automatizar el ciclo de Integración Continua y Despliegue Continuo (CI/CD) con GitHub Actions hacia una plataforma de hosting gratuita.

### 1.2 Qué cubre este documento

El enunciado pide documentar el proceso completo, indicando **estrategia de ramificaciones, Dockerfile, pipeline y conexión entre la API y la base de datos**. Cada uno de esos puntos tiene su capítulo:

| Requisito del enunciado | Capítulo |
|---|---|
| Estrategia de ramificaciones | 2 |
| Dockerfile | 3 |
| Pipeline | 4 |
| Conexión entre API y base de datos | 5 |
| Swagger (habilitado desde la URL de Azure con el flag `EnableSwagger`) | 6 |
| Hosting gratuito (Azure App Service Free + SQL Database Free) | 7 |
| Frontend (Vercel), endpoints públicos y credenciales | 8 |

### 1.3 Visión general de la solución

```
 Desarrollador
      │  git push (feature/*)
      ▼
 ┌──────────────┐   Pull Request    ┌───────────────┐   Pull Request   ┌────────┐
 │ feature/*    │ ────────────────► │ development   │ ───────────────► │  main  │
 │ hotfix/*     │   1 aprobación    │ (integración) │   1 aprobación   │(estable)│
 └──────────────┘   + CI            └───────────────┘   + CI           └───┬────┘
                                                                           │ push
                                                                           ▼
                                                              GitHub Actions: deploy.yml
                                                                           │
                          docker build + push                              │
                    ┌──────────────────────────────┐                      │
                    │         Docker Hub           │ ◄────────────────────┤
                    │      fntkey/ics-api          │                      │
                    └──────────────┬───────────────┘                      │
                                   │ pull de la imagen                    │ webapps-deploy
                                   ▼                                      ▼
                    ┌──────────────────────────────────────────────────────────┐
                    │  Azure App Service (Linux, contenedor, plan Free F1)     │
                    │  API .NET 8 ── puerto 8080 ──► /swagger  /healthcheck    │
                    └──────────────────────────┬───────────────────────────────┘
                                               │ ConnectionStrings__Dsw2025Tpi
                                               ▼
                                   ┌───────────────────────┐
                                   │  Azure SQL Database   │
                                   └───────────────────────┘
```

### 1.4 Tecnologías

| Componente | Tecnología |
|---|---|
| Backend | .NET 8 (ASP.NET Core Web API), Entity Framework Core, ASP.NET Identity, JWT |
| Frontend | React (Vite), desplegado en Vercel |
| Base de datos | SQL Server (local: contenedor Docker; producción: Azure SQL Database) |
| Contenedores | Docker, Docker Compose |
| Registro de imágenes | Docker Hub (`fntkey/ics-api`) |
| CI/CD | GitHub Actions |
| Hosting | Azure App Service para contenedores (Linux, plan Free F1) |

### 1.5 Estructura del repositorio (monorepo)

```
ICS_TPI2026/
├── .github/workflows/
│   ├── continuous-integration.yml    ← CI: valida cada PR
│   └── deploy.yml                    ← CD: despliega a Azure
├── ICS_TPI2026_backend/
│   ├── Dockerfile                    ← multi-stage
│   ├── .dockerignore
│   ├── docker-compose.yml            ← entorno local (SQL Server + API)
│   ├── Dsw2025Tpi.sln
│   ├── Dsw2025Tpi.Api/               ← Program.cs, controllers, appsettings
│   ├── Dsw2025Tpi.Application/
│   ├── Dsw2025Tpi.Domain/
│   └── Dsw2025Tpi.Data/              ← DbContext, migraciones, seeds
├── ICS_TPI2026_frontend/
├── docs/
└── README.md
```

Como el repositorio contiene backend y frontend, el `Dockerfile` vive dentro de `ICS_TPI2026_backend/`, junto al `.sln`, de modo que el contexto de build incluya los cuatro proyectos de la solución.

---

## 2. Estrategia de ramificaciones

### 2.1 Ramas del repositorio

| Rama | Se crea desde | Se fusiona hacia | Propósito |
|---|---|---|---|
| `main` | — | — | Versión **estable / producción**. Cada cambio que llega a `main` se despliega a Azure. |
| `development` | `main` | `main` | Rama de **integración continua**: reúne las funcionalidades terminadas antes de pasar a producción. |
| `feature/nombre-funcionalidad` | `development` | `development` | Desarrollo de una funcionalidad o tarea. |
| `hotfix/descripcion` | `main` | `main` y `development` | Corrección urgente de un error en producción. |

Ejemplos reales del repositorio: `feature/tp3-docker`, `feature/tp3-deploy`, `feature/tp3-git-workflow-docs`, `fix/tp3-azure-fixes`.

### 2.2 Reglas

1. **No se permite commitear directamente sobre `main` ni sobre `development`.** Todo cambio entra por Pull Request.
2. Los nombres van en minúsculas y con guiones: `feature/nombre-funcionalidad` o `hotfix/descripcion`.
3. Cada rama de trabajo es corta y atiende una sola tarea.
4. Los commits describen en una frase qué hace el cambio.

### 2.3 Protección de ramas (Branch Protection)

En GitHub (*Settings → Rules*) se configuró la protección de `main` y `development` con estas reglas:

| Regla | Efecto |
|---|---|
| Pull Request obligatorio | No se puede hacer merge sin abrir un PR. |
| Al menos **1 aprobación** (Peer Review) | Un integrante **distinto del autor** debe aprobar el PR. |
| Push directo bloqueado | Un `git push` directo a `main` o `development` es rechazado por GitHub. |

**Verificación realizada** (tarjeta [Git] del Sprint 2):

- Se intentó un push directo a `main`/`development` y GitHub lo rechazó.
- Se abrió un PR de prueba y GitHub exigió la aprobación antes de habilitar el merge.

### 2.4 Control del nombre de rama mediante CI

GitHub, en el plan gratuito, no permite restringir por regla el *nombre* de las ramas. Como control compensatorio, el pipeline de CI incluye el job `branch-name-check`, que corre en cada Pull Request y **falla** si la rama de origen no empieza con uno de estos prefijos:

`feature/`, `feat/`, `fix/`, `hotfix/`, `bugfix/`, `release/`, `docs/`, `chore/`, `refactor/`

Las ramas `main` y `development` están exceptuadas (se usan como origen al promover `development` → `main`). La convención base del TP (`feature/*` y `hotfix/*`) está incluida en esta lista; el resto de los prefijos se admiten por compatibilidad con las ramas que ya existían en el repositorio (`feat/US-27-...`, `fix/US-28-...`).

### 2.5 Flujo de trabajo (Pull Request)

```
1. git checkout development && git pull
2. git checkout -b feature/nombre-funcionalidad
3. ... cambios y commits ...
4. git push -u origin feature/nombre-funcionalidad
5. Abrir PR  feature/nombre-funcionalidad  →  development
6. Corre el pipeline de CI automáticamente sobre el PR
7. Un compañero revisa y aprueba (1 aprobación mínima)
8. Se fusiona el PR y se elimina la rama
9. Para publicar: PR  development  →  main  (misma revisión)
10. El merge a main dispara el despliegue automático (capítulo 4)
```

### 2.6 Flujo de un hotfix

```
1. git checkout main && git pull
2. git checkout -b hotfix/descripcion
3. Corregir, commitear y subir la rama
4. PR hotfix/descripcion → main   (revisión + CI) → se despliega
5. PR hotfix/descripcion → development para que las ramas no diverjan
```

### 2.7 Resumen

| Aspecto | Decisión |
|---|---|
| Modelo | Ramas por característica (`feature/*`) y por corrección (`hotfix/*`) sobre dos ramas permanentes |
| Ramas permanentes | `main` (producción) y `development` (integración) |
| Integración | Solo mediante Pull Request |
| Revisión | Mínimo 1 aprobación de un par distinto del autor |
| Control automático | CI valida compilación, imagen Docker, lint del frontend y nombre de rama |
| Entrega | Merge a `main` ⇒ despliegue automático |

---

## 3. Dockerfile y dockerización de la API

### 3.1 Dockerfile multi-stage

Archivo: `ICS_TPI2026_backend/Dockerfile`

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

> El Dockerfile **no** fija `ASPNETCORE_ENVIRONMENT`: el entorno se inyecta desde fuera (docker-compose en local, Application Settings en Azure), de modo que la misma imagen sirve para cualquier entorno (ver secciones 3.5, 5.5 y 6.1).

### 3.2 Explicación línea por línea

**Etapa `build`** (compilación y publicación)

| Instrucción | Qué hace y por qué |
|---|---|
| `FROM ...dotnet/sdk:8.0 AS build` | Parte de la imagen con el **SDK de .NET 8**, que trae el compilador y `dotnet restore/publish`. Esta etapa se descarta al final. |
| `WORKDIR /src` | Directorio de trabajo dentro de la imagen. |
| `COPY Dsw2025Tpi.sln ./` y `COPY ...*.csproj` | Copia **solo** la solución y los archivos de proyecto (uno por cada capa: Api, Application, Domain, Data). |
| `RUN dotnet restore` | Restaura los paquetes NuGet. Al ejecutarse antes de copiar el código, Docker **cachea esta capa**: mientras no cambien los `.csproj`, los paquetes no se vuelven a descargar y el build es mucho más rápido. |
| `COPY . .` | Recién ahora copia el resto del código fuente. |
| `RUN dotnet publish ... -c Release -o /app/publish --no-restore` | Compila en modo **Release** y deja la aplicación lista para ejecutar en `/app/publish`. `--no-restore` evita repetir el restore ya hecho. |

**Etapa `runtime`** (ejecución liviana)

| Instrucción | Qué hace y por qué |
|---|---|
| `FROM ...dotnet/aspnet:8.0 AS runtime` | Parte de la imagen **solo con el runtime de ASP.NET**, sin SDK. Es considerablemente más chica y tiene menos superficie de ataque. |
| `WORKDIR /app` | Directorio de ejecución. |
| `COPY --from=build /app/publish .` | Copia **únicamente** el resultado publicado desde la etapa `build`. La imagen final no contiene el SDK ni el código fuente. |
| `ENV ASPNETCORE_URLS=http://+:8080` | Indica a Kestrel en qué URL y puerto escuchar: todas las interfaces, puerto 8080. |
| `EXPOSE 8080` | Documenta que el contenedor atiende en el puerto 8080. |
| `ENTRYPOINT ["dotnet", "Dsw2025Tpi.Api.dll"]` | Comando que arranca la API al iniciar el contenedor. |

### 3.3 Por qué multi-stage

| Beneficio | Detalle |
|---|---|
| Imagen final liviana | Solo contiene el runtime y la aplicación publicada. |
| Menor superficie de ataque | No incluye SDK, herramientas de compilación ni código fuente. |
| Builds rápidos | La capa de `dotnet restore` se reutiliza mientras no cambien los `.csproj`. |
| Reproducibilidad | El mismo Dockerfile produce la misma imagen en local, en CI y en Azure. |

### 3.4 Archivo `.dockerignore`

Archivo: `ICS_TPI2026_backend/.dockerignore`. Excluye del contexto de build lo que no debe viajar a la imagen:

```
**/bin/
**/obj/
**/.vs/
.git/
.gitignore
.gitattributes
.env
README.md
test/
testC/
docker-compose.yml
Dockerfile
.dockerignore
```

Excluir `bin/` y `obj/` es importante: son artefactos de compilación de Windows que, si se copiaran, podrían contaminar el build de Linux dentro del contenedor.

### 3.5 Inyección de variables de entorno

La API no tiene valores de entorno fijos en el código: lee su configuración a través de `builder.Configuration`, que combina `appsettings.json` con las **variables de entorno** (las variables de entorno tienen prioridad). Para el contenedor se inyectan:

| Variable | Valor | Para qué sirve |
|---|---|---|
| `ASPNETCORE_URLS` | `http://+:8080` | URL y puerto de escucha de la API. |
| `ASPNETCORE_ENVIRONMENT` | según entorno (`Development` en local, `Production` en Azure) | Entorno de ejecución (controla, entre otras cosas, Swagger; ver capítulo 6). |
| `ConnectionStrings__Dsw2025Tpi` | cadena de conexión | Conexión a la base de datos (ver capítulo 5). |
| `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience` | según entorno | Firma y validación de tokens JWT. |
| `DefaultAdminUser__*` | según entorno | Usuario administrador inicial. |

En Linux las variables de entorno no pueden contener `:`, por eso .NET usa el **doble guión bajo** (`__`) como separador: `ConnectionStrings__Dsw2025Tpi` equivale a `ConnectionStrings:Dsw2025Tpi` en `appsettings.json`.

Los valores del Dockerfile son solo valores por defecto: se pueden **sobrescribir al ejecutar** el contenedor con `-e`:

```bash
docker run -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Development dsw2025tpi-api:local
```

### 3.6 Docker Compose (entorno local)

Archivo: `ICS_TPI2026_backend/docker-compose.yml`. Levanta la base de datos y la API juntas:

```yaml
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: ics-sqlserver
    environment:
      - ACCEPT_EULA=Y
      - MSSQL_SA_PASSWORD=YourStrong!Password123
    ports:
      - "1433:1433"
    volumes:
      - sqldata:/var/opt/mssql
    healthcheck:
      test: ["CMD-SHELL", "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P YourStrong!Password123 -No -Q 'SELECT 1'"]
      interval: 10s
      timeout: 5s
      retries: 10
      start_period: 20s

  api:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: ics-backend
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:8080
      - ConnectionStrings__Dsw2025Tpi=Server=sqlserver,1433;Database=Dsw2025Tpi;User Id=sa;Password=YourStrong!Password123;TrustServerCertificate=True;Encrypt=False;
      - Jwt__Key=${JWT_KEY}
      - DefaultAdminUser__Password=${ADMIN_PASSWORD}
    ports:
      - "8080:8080"
    depends_on:
      sqlserver:
        condition: service_healthy

volumes:
  sqldata:
```

Puntos clave:

- **`healthcheck` + `depends_on: condition: service_healthy`:** la API no arranca hasta que SQL Server responde. Sin esto, la API intenta migrar la base antes de que exista el servidor y se cae (ver capítulo 10).
- **Volumen `sqldata`:** los datos de la base persisten entre reinicios.
- **Host `sqlserver`:** dentro de la red de Compose, la API se conecta a la base usando el **nombre del servicio** como hostname.
- El compose es solo para desarrollo local; en producción la base es Azure SQL.

### 3.7 Verificación local del contenedor

Con Docker Desktop en ejecución, desde `ICS_TPI2026_backend/`:

```bash
# Construir la imagen
docker build -t dsw2025tpi-api:local .

# Levantar base de datos + API (recomendado)
docker compose up -d

# Probar
curl http://localhost:8080/healthcheck      # responde 200 "Healthy"
# Navegador: http://localhost:8080/swagger  # responde 200
```

Criterios de aceptación verificados:

1. El Dockerfile tiene una etapa de build (`sdk:8.0`) y una de runtime (`aspnet:8.0`); la imagen final no incluye el SDK ni el código fuente.
2. La URL y el entorno se inyectan por variables de entorno (`ASPNETCORE_URLS` y `ASPNETCORE_ENVIRONMENT`) y se pueden sobrescribir con `docker run -e`.
3. Al ejecutar `docker build` y `docker run -p 8080:8080`, el contenedor inicia y `http://localhost:8080/swagger` responde 200.

---

## 4. Pipeline de CI/CD (GitHub Actions)

El pipeline se divide en **dos workflows** con responsabilidades separadas: uno **valida** (CI) y otro **despliega** (CD).

| | `continuous-integration.yml` (CI) | `deploy.yml` (CD) |
|---|---|---|
| Objetivo | Validar que el código compila y cumple las reglas | Publicar la API en Azure |
| Disparadores | `push` a `development`, `pull_request` hacia `main` y `development`, manual | `push` a `main`, manual (`workflow_dispatch`) |
| Despliega | No | Sí |
| Usa secrets | No | Sí |

### 4.1 CI — `continuous-integration.yml`

```yaml
name: Continuous Integration

on:
  push:
    branches: [development]
  pull_request:
    branches: [main, development]
  workflow_dispatch: {}
```

**Jobs** (corren en paralelo sobre `ubuntu-latest`):

| Job | Qué hace |
|---|---|
| `backend-build` | `actions/checkout@v4` → `actions/setup-dotnet@v4` (.NET 8) → `dotnet restore Dsw2025Tpi.sln` → `dotnet build Dsw2025Tpi.sln --no-restore -c Release /p:TreatWarningsAsErrors=true`. Trabaja en `ICS_TPI2026_backend`. **Las advertencias del compilador se tratan como errores.** |
| `docker-build` | `docker build -f ICS_TPI2026_backend/Dockerfile -t ics-api:ci ICS_TPI2026_backend`. Valida que el Dockerfile construya la imagen, **sin publicarla**. |
| `frontend-build` | `setup-node@v4` (Node 20, con caché de npm) → `npm ci` → `npm run build`. Trabaja en `ICS_TPI2026_frontend`. |
| `frontend-lint` | `npm ci` → `npm run lint -- --max-warnings 0` (ESLint sin tolerar ninguna advertencia). |
| `branch-name-check` | Solo en `pull_request`. Falla si el nombre de la rama de origen no usa un prefijo permitido (sección 2.4). |

> **Monorepo:** como backend y frontend viven en carpetas distintas, cada job fija su carpeta con `defaults.run.working-directory`, y el `docker build` indica explícitamente la ruta al Dockerfile (`-f`) y el contexto de build.

### 4.2 CD — `deploy.yml`

```yaml
name: Build and Deploy to Azure App Service (Publish Profile)

on:
  push:
    branches: [main]
  workflow_dispatch:

env:
  DOCKERHUB_USERNAME: fntkey
  IMAGE_NAME: ics-api
  BACKEND_DIR: ICS_TPI2026_backend
```

**Pasos del job `build-and-deploy`** (`ubuntu-latest`):

| # | Paso | Detalle |
|---|---|---|
| 1 | Obtener el código | `actions/checkout@v4` |
| 2 | Iniciar sesión en Docker Hub | `docker/login-action@v3` con `secrets.DOCKERHUB_TOKEN` |
| 3 | Construir la imagen Docker (.NET 8) | `docker build -f ICS_TPI2026_backend/Dockerfile` con **dos tags**: `fntkey/ics-api:<SHA del commit>` y `fntkey/ics-api:latest` |
| 4 | Subir la imagen a Docker Hub | `docker push` de ambos tags |
| 5 | Desplegar en Azure App Service | `azure/webapps-deploy@v3` con `secrets.APP_SERVICE_NAME` y `secrets.AZURE_WEBAPP_PUBLISH_PROFILE`; despliega la imagen con el **tag del SHA** |
| 6 | Smoke test | Hasta 12 intentos, 10 s entre cada uno, contra `https://<url-de-azure>/healthcheck`. Si ninguno responde 200 el job falla. |

Decisiones de diseño:

- **Tag por SHA:** cada despliegue usa una imagen identificada por el commit exacto (`${{ github.sha }}`). Esto da trazabilidad y permite volver a una versión anterior. El tag `latest` se mantiene como referencia a la última versión.
- **Smoke test:** el workflow solo termina en verde si la API **realmente respondió** tras el despliegue (`/healthcheck`), no solo si el comando de deploy no falló. Los reintentos cubren el tiempo que tarda Azure en descargar la imagen y reiniciar el contenedor.
- **Etapas que pide el enunciado:** restauración de paquetes y compilación limpia (CI: `dotnet restore` + `dotnet build`, y dentro del `docker build` con `dotnet restore` + `dotnet publish -c Release`), construcción de la imagen Docker (paso 3) y despliegue automático (pasos 4 y 5).

### 4.3 Secrets de GitHub

Las credenciales **nunca** se escriben en los archivos `.yml`: se guardan cifradas en *Settings → Secrets and variables → Actions*.

| Secret | Contenido | Uso |
|---|---|---|
| `DOCKERHUB_TOKEN` | Token de acceso (PAT) de Docker Hub | Login para subir la imagen |
| `APP_SERVICE_NAME` | Nombre del App Service en Azure | Destino del despliegue |
| `AZURE_WEBAPP_PUBLISH_PROFILE` | XML del *publish profile* del App Service | Credenciales de despliegue |

El nombre de usuario de Docker Hub (`fntkey`) no es un dato sensible y está como variable de entorno del workflow.

**Cómo obtener el publish profile:** App Service → *Overview* → *Get publish profile* (descarga un `.PublishSettings`); se abre con un editor de texto y se copia **todo** el XML al secret. Si Azure informa *"Basic authentication is disabled"*, hay que habilitar *Credenciales de publicación de autenticación básica de SCM* en la configuración general del App Service.

### 4.4 Flujo completo

```
 Developer        GitHub                    Docker Hub              Azure
     │               │                           │                    │
     │ push feature  │                           │                    │
     ├──────────────►│                           │                    │
     │ PR → develop  │                           │                    │
     ├──────────────►│ CI: build, docker-build,  │                    │
     │               │ lint, branch-name-check   │                    │
     │ aprobación    │                           │                    │
     │ y merge       │                           │                    │
     │ PR → main     │                           │                    │
     ├──────────────►│ CI + aprobación           │                    │
     │ merge a main  │                           │                    │
     ├──────────────►│ deploy.yml                │                    │
     │               │ docker build              │                    │
     │               │ docker push ─────────────►│                    │
     │               │ webapps-deploy ───────────┼───────────────────►│ pull de la imagen
     │               │                           │                    │ reinicio
     │               │ smoke test /healthcheck ◄─┼────────────────────┤ 200 OK
```

---

## 5. Conexión entre la API y la base de datos

### 5.1 Cómo se conecta la API

La API usa **Entity Framework Core con el proveedor SQL Server**. Hay dos contextos de base de datos y ambos usan la **misma cadena de conexión**, bajo la clave `Dsw2025Tpi`:

| Contexto | Contenido | Registro |
|---|---|---|
| `Dsw2025TpiContext` | Datos de negocio (productos, clientes, órdenes) | `AddDomainServices` → `UseSqlServer(configuration.GetConnectionString("Dsw2025Tpi"))` |
| `AuthenticateContext` | Tablas de ASP.NET Identity (usuarios y roles) | `Program.cs` → `UseSqlServer(builder.Configuration.GetConnectionString("Dsw2025Tpi"))` |

### 5.2 Qué hace la API al iniciar

Al arrancar, antes de aceptar peticiones, `Program.cs` ejecuta en orden:

1. `dbContext.Database.Migrate()` — aplica las **migraciones** pendientes de `Dsw2025TpiContext`. Si la base no existe, la crea.
2. `authContext.Database.Migrate()` — idem para `AuthenticateContext`.
3. **Seeds** de datos de ejemplo: `Sources/Products.json`, `Sources/Customers.json`, `Sources/Orders.json`.
4. Crea los **roles** definidos en la sección `Roles` (`Admin`, `User`).
5. Crea el **usuario administrador** inicial desde la sección `DefaultAdminUser` (si no existe) y le asigna su rol. Si falta algún dato de esa sección, la API se detiene con un error explícito.

Consecuencia importante: **si la API no puede conectarse a la base, no arranca.** Por eso la base debe estar disponible y accesible antes de iniciar la API.

### 5.3 De dónde sale la cadena de conexión

.NET combina varias fuentes de configuración; **la última tiene prioridad**:

```
1. appsettings.json                    (valores base, para desarrollo local)
2. appsettings.{Entorno}.json          (por entorno)
3. Variables de entorno                (inyectadas por Docker / Azure)  ← gana
```

| Escenario | Origen de la cadena | Servidor de base de datos |
|---|---|---|
| Local sin Docker (Visual Studio) | `appsettings.json` | SQL Server LocalDB (`(localdb)\MSSQLLocalDB`, autenticación integrada de Windows) |
| Local con Docker Compose | Variable `ConnectionStrings__Dsw2025Tpi` del `docker-compose.yml` | Contenedor `sqlserver` en el puerto 1433 (host `sqlserver` dentro de la red de Compose) |
| Producción (Azure) | *Application Setting* `ConnectionStrings__Dsw2025Tpi` del App Service | Azure SQL Database |

Esto cumple el principio del TP de **no tener valores fijos en el código**: la misma imagen Docker funciona en cualquier entorno, cambiando solo las variables.

### 5.4 Formato de la cadena

**Docker Compose (local):**

```
Server=sqlserver,1433;Database=Dsw2025Tpi;User Id=sa;Password=<contraseña>;TrustServerCertificate=True;Encrypt=False;
```

**Azure SQL Database (producción):**

```
Server=tcp:<servidor>.database.windows.net,1433;Initial Catalog=<base>;Persist Security Info=False;User ID=<usuario>;Password=<contraseña>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

En Azure la conexión va **cifrada** (`Encrypt=True`) y se valida el certificado del servidor.

### 5.5 Configuración en Azure

**Application Settings del App Service** (*Configuración → Configuración de la aplicación*). Azure las inyecta como variables de entorno dentro del contenedor:

| Setting | Para qué sirve |
|---|---|
| `ConnectionStrings__Dsw2025Tpi` | Cadena de conexión a Azure SQL |
| `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience` | Firma y validación de JWT (la clave es un valor aleatorio largo, distinto al de desarrollo) |
| `DefaultAdminUser__UserName`, `__Email`, `__Password`, `__Role` | Administrador inicial (usuario `admin`; ver capítulo 8) |
| `ASPNETCORE_ENVIRONMENT` | `Production`: entorno de ejecución del deploy (Swagger se habilita igual por el flag `EnableSwagger`; ver capítulo 6) |
| `EnableSwagger` | `true`: habilita Swagger en la URL pública aunque el entorno sea `Production` (ver capítulo 6) |
| `WEBSITES_PORT` | `8080`: puerto en el que escucha el contenedor, para que App Service sepa a dónde enrutar |

Los valores secretos viven **solo** en Azure (cifrados en reposo) y no en el repositorio.

**Firewall de Azure SQL.** Azure SQL bloquea por defecto toda conexión. En el servidor SQL (*Networking*) se activó **"Allow Azure services and resources to access this server"**, que permite que el App Service se conecte. Para consultar desde una PC propia hay que agregar además la IP del cliente.

### 5.6 Recursos creados en Azure

| Recurso | Nombre | Detalle |
|---|---|---|
| Resource Group | `rg-ics-tp3` | Agrupa todos los recursos del TP |
| Región | Brazil South | |
| SQL Server (lógico) | `ics-sql-43739` | Autenticación SQL |
| Base de datos | `ics-db` | Azure SQL Database |
| App Service | `ics-tp3-43739` | Linux, publicación por **Contenedor**, plan **Free F1** |
| Imagen | `fntkey/ics-api` | Docker Hub |

### 5.7 Diagrama de conexión

```
Navegador / Frontend (Vercel)
        │  HTTPS
        ▼
Azure App Service (contenedor, puerto 8080)
   ├─ Controllers  →  /api/auth, /api/products, /api/orders
   ├─ /healthcheck (con chequeo a BD + rate limiting), /swagger (flag EnableSwagger)
   ├─ Dsw2025TpiContext  ──┐
   └─ AuthenticateContext ─┤  ConnectionStrings__Dsw2025Tpi
                           ▼
                Azure SQL Database (ics-db)
                (firewall: "Allow Azure services")
```

### 5.8 CORS

La API permite peticiones del frontend desde estos orígenes: `http://localhost:5173`, `http://localhost:5174` y `https://ics-tpi-2026.vercel.app`.

---

## 6. Swagger

El TP exige que Swagger funcione desde la URL (dominio) que provee Azure.

### 6.1 Cómo está implementado

En `Program.cs` Swagger se registra con `AddSwaggerGen` (con definición de seguridad *Bearer* para probar los endpoints protegidos con JWT) y se publica mediante:

```csharp
var enableSwagger = app.Configuration.GetValue<bool>("EnableSwagger");
if (app.Environment.IsDevelopment() || enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
```

Es decir, Swagger se publica cuando el entorno es **`Development`** **o** cuando el flag de configuración **`EnableSwagger=true`** esté presente. Como el Dockerfile no fija el entorno, ambos valores se inyectan desde fuera:

- **Local** (`docker-compose.yml`): `ASPNETCORE_ENVIRONMENT=Development` → Swagger sí está disponible en `http://localhost:8080/swagger` (el flag no hace falta en desarrollo).
- **Azure (deploy)**: Application Settings con `ASPNETCORE_ENVIRONMENT=Production` y **`EnableSwagger=true`** → Swagger queda disponible en la URL pública aunque el entorno sea `Production`.

> Verificado el 21/10/2026 antes del cambio: con `Production` y sin flag efectivo, `/swagger` respondía `404`. Con el flag implementado en `Program.cs`, tras el próximo deploy a `main` la URL pública debe responder `200`.

### 6.2 URLs

| Entorno | URL | Estado |
|---|---|---|
| Local | `http://localhost:8080/swagger` | 200 (entorno `Development` vía compose) |
| Azure (deploy) | `https://ics-tp3-43739-eveqhxemd5cmdee7.brazilsouth-01.azurewebsites.net/swagger` | 200 tras el deploy con `EnableSwagger=true` |
| Healthcheck (Azure) | `https://ics-tp3-43739-eveqhxemd5cmdee7.brazilsouth-01.azurewebsites.net/healthcheck` | 200 |

### 6.3 Consideración de seguridad

El deploy en Azure mantiene `ASPNETCORE_ENVIRONMENT=Production`: es la opción correcta para un entorno público y evita las páginas de error detalladas de desarrollo. Swagger se habilita **por flag explícito** (`EnableSwagger=true` en Application Settings), de modo que quede expuesto para el TP pero apagado por defecto en cualquier otro entorno donde no se configure (por defecto `false` en `appsettings.json`).

---

## 7. Despliegue en Azure (costo cero)

Se utilizaron las opciones gratuitas sugeridas por la cátedra: **Azure App Service (plan Free)** y **SQL Database**.

### 7.1 Orden de creación

1. **Resource Group** `rg-ics-tp3` (región Brazil South).
2. **Azure SQL Server** (`ics-sql-43739`, autenticación SQL) y **Database** `ics-db`.
3. **Firewall del SQL Server:** activar *Allow Azure services and resources to access this server*.
4. **App Service** (`ics-tp3-43739`): publicación **Contenedor**, sistema operativo **Linux**, plan **Free F1**; origen de la imagen: Docker Hub, `fntkey/ics-api`.
5. **Application Settings** del App Service (sección 5.5), incluyendo `WEBSITES_PORT=8080`.
6. **Docker Hub:** cuenta, repositorio `ics-api` y token de acceso (PAT).
7. **Secrets de GitHub** (sección 4.3).
8. Merge a `main` → `deploy.yml` construye, publica y despliega.

### 7.2 Por qué contenedor y no código

Publicar como **contenedor** permite que Azure ejecute exactamente la misma imagen que se probó en local y en CI, garantizando reproducibilidad. Es además la opción más coherente con la dockerización pedida en el TP.

### 7.3 Por qué la base no está en un contenedor en producción

En local se usa SQL Server en Docker por comodidad. En producción se usa **Azure SQL Database** (servicio gestionado) porque Microsoft se encarga de backups y actualizaciones, no consume recursos del plan gratuito del App Service y el acceso se controla con firewall y cifrado.

### 7.4 Limitaciones del plan gratuito

El plan **Free F1** no tiene *Always On*: tras un período de inactividad el contenedor se suspende y la primera petición tarda más (*cold start*). Esto es lo que justifica los reintentos del *smoke test* del pipeline.

---

## 8. Frontend en Vercel, endpoints y credenciales

### 8.1 Frontend (Vercel)

| Campo | Valor |
|---|---|
| URL pública | `https://ics-tpi-2026.vercel.app` |
| Proyecto | `ics-tpi-2026` |
| Build | Framework detectado automáticamente (Vite): `npm run build` → salida `dist/` |
| Comandos | Install: `npm install` · Build: `npm run build` |
| Configuración | No usa `vercel.json`; las rutas SPA se resuelven con el *rewrite* por defecto de Vercel |

**Cómo el frontend encuentra la API.** El cliente axios usa `import.meta.env.VITE_BACKEND_URL` como `baseURL` (`src/modules/shared/api/axiosInstance.js`). Esa variable se configura en el panel de Vercel (*Settings → Environment Variables*) y **no** viaja en el repositorio (el `.env` local está en `.gitignore`):

| Entorno | `VITE_BACKEND_URL` |
|---|---|
| Local (Docker) | `http://localhost:8080` |
| Local (`dotnet run`) | `http://localhost:5142` |
| Vercel (producción) | `https://ics-tp3-43739-eveqhxemd5cmdee7.brazilsouth-01.azurewebsites.net` |

En desarrollo local, Vite además proxya `/api` hacia ese backend (`vite.config.js`), por lo que también funcionan las llamadas relativas.

### 8.2 Endpoints públicos (producción)

Cadena completa: **Navegador → Vercel (frontend) → Azure App Service (API) → Azure SQL**.

**Frontend (rutas SPA):**

| Ruta | Pantalla |
|---|---|
| `/` | Catálogo de productos |
| `/cart` | Carrito |
| `/login` · `/register` | Autenticación |
| `/admin` · `/admin/products` · `/admin/orders` · `/admin/users` | Panel de administración (requiere rol `Admin`) |

**Backend (API REST)** — base: `https://ics-tp3-43739-eveqhxemd5cmdee7.brazilsouth-01.azurewebsites.net`:

| Método | Ruta | Auth | Descripción |
|---|---|---|---|
| `POST` | `/api/auth/login` | — | Setea el JWT en una cookie HttpOnly y devuelve el perfil |
| `POST` | `/api/auth/logout` | — | Elimina la cookie de sesión |
| `POST` | `/api/auth/register` | — | Registra un usuario |
| `GET` | `/api/products` | — | Catálogo |
| `GET` | `/api/products/{id}` | — | Detalle de producto |
| `POST` · `PUT` · `PATCH` | `/api/products`… | Bearer (Admin) | Alta / modificación de productos |
| `GET` | `/api/products/admin` | Bearer (Admin) | Listado administrativo |
| `GET` · `POST` | `/api/orders` | Bearer | Órdenes del usuario |
| `GET` · `PUT` | `/api/orders/{id}` | Bearer | Detalle / actualización de orden |
| `GET` | `/healthcheck` | — | Salud del servicio. Además del 200, hace un chequeo mínimo a la BD (`CanConnectAsync` con timeout de 5 s) para que el monitor despierte también la base. Protegido con rate limiting: 30 req/min por IP (`429` al excederlo), para que nadie pueda abusar del endpoint y saturar la base o la app. Lo usa el smoke test del pipeline y el monitor de disponibilidad |

### 8.3 Credenciales de acceso

**Usuario administrador** (gestiona productos, órdenes y usuarios desde `/admin`):

| Entorno | Usuario | Contraseña | Origen del valor |
|---|---|---|---|
| Local (dev) | `admin` | la definida por cada integrante en `ADMIN_PASSWORD` (archivo `.env`, no versionado; ver `.env.example`) | `.env` → `docker-compose.yml` → `DefaultAdminUser__Password` |
| Producción (Azure) | `admin` | `AdminTp3$2026!Xyz` | App Service → *Application Settings* → `DefaultAdminUser__Password` |

Ambos con email `admin@example.com` y rol `Admin`. La API crea este usuario al arrancar si no existe (`Program.cs`, seeds).

**Secretos fuera del repositorio.** `appsettings.json` ya no contiene `Jwt:Key` ni `DefaultAdminUser:Password` (tarjetas AT-01 y AT-02). En local se inyectan por variables de entorno desde un `.env` (junto al `docker-compose.yml`, ignorado por git) con `JWT_KEY` y `ADMIN_PASSWORD`; `.env.example` documenta las claves. Para `dotnet run` se usan *user-secrets*. Si falta alguno, la API no arranca. La contraseña debe cumplir las reglas de Identity (mayúscula, minúscula, número y carácter no alfanumérico).

**Base de datos (solo local):** usuario `sa`, contraseña `YourStrong!Password123` (definidos en `docker-compose.yml` y `docs/DB_Config.md`). En Azure el acceso lo tiene la propia App Service vía la *connection string* de los Application Settings; no hay acceso directo desde una PC sin agregar la IP al firewall del SQL Server.

> Son credenciales de un TP con datos de prueba; en un sistema real no se versionan contraseñas en el repositorio.

### 8.4 Variables de entorno (resumen)

| Variable | Local | Azure (deploy) | Vercel |
|---|---|---|---|
| `ConnectionStrings__Dsw2025Tpi` | compose: `Server=sqlserver,1433;...` | `Server=tcp:ics-sql-43739.database.windows.net,1433;...` (Application Settings) | — |
| `Jwt__Key` | `.env` (`JWT_KEY`) vía compose, o user-secrets | Application Settings (clave aleatoria distinta) | — |
| `Jwt__Issuer` · `Jwt__Audience` | `appsettings.json` | Application Settings | — |
| `DefaultAdminUser__Password` | `.env` (`ADMIN_PASSWORD`) vía compose, o user-secrets | Application Settings (`AdminTp3$2026!Xyz`) | — |
| `DefaultAdminUser__UserName` · `__Email` · `__Role` | `appsettings.json` | Application Settings | — |
| `ASPNETCORE_ENVIRONMENT` | `Development` (compose) | `Production` | — |
| `EnableSwagger` | `false` (`appsettings.json`) | `true` (Application Settings) | — |
| `WEBSITES_PORT` | — (`docker -p 8080:8080`) | `8080` | — |
| `VITE_BACKEND_URL` | `.env` (gitignored) | — | `https://ics-tp3-43739-eveqhxemd5cmdee7.brazilsouth-01.azurewebsites.net` |

---

## 9. Verificación del funcionamiento

| # | Verificación | Resultado esperado |
|---|---|---|
| 1 | Push directo a `main` o `development` | Rechazado por GitHub |
| 2 | PR hacia `main`/`development` sin aprobación | El botón de merge queda bloqueado |
| 3 | PR con nombre de rama inválido (ej. `mi-rama`) | `branch-name-check` falla |
| 4 | `docker build -t dsw2025tpi-api:local .` | La imagen se construye sin errores |
| 5 | `docker compose up -d` | `sqlserver` queda *healthy* y luego inicia la `api` |
| 6 | `http://localhost:8080/swagger` | Responde 200 |
| 7 | `docker run -e ASPNETCORE_ENVIRONMENT=Development ...` | La variable sobrescribe el valor por defecto de la imagen |
| 8 | PR a `development`/`main` | Corren `backend-build`, `docker-build`, `frontend-build`, `frontend-lint` y `branch-name-check` |
| 9 | Merge a `main` | `deploy.yml` termina en verde de punta a punta, incluido el *smoke test* |
| 10 | `GET <url-azure>/healthcheck` | Responde 200 (incluye chequeo mínimo a la BD, ver §8.2) |
| 11 | `<url-azure>/swagger` | 200 tras el deploy: el flag `EnableSwagger=true` habilita Swagger aunque el entorno sea `Production` (ver capítulo 6) |
| 12 | Endpoints que leen la base (ej. `GET /api/products`) | Devuelven datos: la API está conectada a Azure SQL |

Comandos útiles:

```bash
docker compose logs api          # logs de la API
docker images                    # imágenes locales
curl https://<url-azure>/healthcheck
```

Logs en la nube: App Service → *Monitoring → Log stream*; GitHub → *Actions* → ejecución del workflow.

---

## 10. Problemas encontrados y soluciones

| Problema | Causa | Solución |
|---|---|---|
| La API se cae al iniciar con una excepción en `Database.Migrate()` (`Program.cs`) | La API arrancó antes de que SQL Server estuviera listo (por ejemplo, al iniciar solo el contenedor `api` desde Docker Desktop, que **no respeta** `depends_on`) | Levantar todo el stack con `docker compose up -d`; el compose espera el `healthcheck` de SQL antes de iniciar la API |
| `/swagger` respondía 404 en Azure | Swagger solo se publicaba en entorno `Development` y el deploy corre con `Production` | Se condicionó Swagger al flag `EnableSwagger` (`Program.cs`): se habilita en `Development` o cuando `EnableSwagger=true` (Application Settings de Azure; ver capítulo 6) |
| Fallan los seeds dentro del contenedor | Linux distingue mayúsculas y minúsculas en rutas de archivos (Windows no) | Se corrigieron los nombres de los archivos de seeds para que coincidan exactamente con el código |
| Build de Docker lento o con archivos de Windows | Se copiaban `bin/` y `obj/` al contexto | Se agregó `.dockerignore` |
| `docker build` falla en CI por no encontrar el Dockerfile | Repositorio monorepo: el Dockerfile no está en la raíz | Indicar la ruta con `-f ICS_TPI2026_backend/Dockerfile` y el contexto `ICS_TPI2026_backend` |
| La API no conecta con Azure SQL | El firewall de Azure SQL bloquea todo por defecto | Activar *Allow Azure services and resources to access this server* |
| No se puede descargar el publish profile (*Basic authentication is disabled*) | Azure desactiva por defecto la autenticación básica de SCM | Habilitar *Credenciales de publicación de autenticación básica de SCM* |
| El contenedor nunca inicia en App Service | App Service no sabe en qué puerto escucha | Definir `WEBSITES_PORT=8080` |
| El *smoke test* podía fallar justo después del deploy | El contenedor tarda en reiniciarse y descargar la imagen (plan Free) | El smoke test reintenta hasta 12 veces con 10 s de espera |
| El frontend publicado en Vercel no podía llamar a la API | La política CORS de la API no incluía el origen de Vercel | Se agregó `https://ics-tpi-2026.vercel.app` a los orígenes permitidos |
| Los despliegues no eran trazables | La imagen se desplegaba con el tag `latest` | El deploy usa el tag con el **SHA del commit** |
| El workflow de CI no corría (YAML inválido) | Error de sintaxis en `runs-on` del job `frontend-build` | Se corrigió el YAML |
| "App se duerme" tras 20 minutos | El plan Free F1 no tiene Always On | Aceptar el *cold start* o subir a un plan pago |

---

## 11. Anexo: archivos del proyecto

| Archivo | Propósito |
|---|---|
| `ICS_TPI2026_backend/Dockerfile` | Imagen multi-stage de la API (SDK 8.0 → ASP.NET 8.0) |
| `ICS_TPI2026_backend/.dockerignore` | Exclusiones del contexto de build |
| `ICS_TPI2026_backend/docker-compose.yml` | Entorno local: SQL Server + API |
| `ICS_TPI2026_backend/Dsw2025Tpi.Api/Program.cs` | Configuración de la API, migraciones, seeds, Swagger, CORS, healthcheck |
| `ICS_TPI2026_backend/Dsw2025Tpi.Api/appsettings.json` | Configuración base para desarrollo local |
| `.github/workflows/continuous-integration.yml` | Pipeline de CI |
| `.github/workflows/deploy.yml` | Pipeline de CD hacia Azure |
| `README.md` | Convención de ramas y flujo de PR |
| `docs/DESPLIEGUE_AZURE.md` | Guía paso a paso de la creación de los recursos en Azure |

### Glosario

| Término | Significado |
|---|---|
| **CI** | Integración Continua: validar automáticamente cada cambio (compilar, lint, construir la imagen). |
| **CD** | Despliegue Continuo: publicar automáticamente a producción al integrar cambios. |
| **Multi-stage** | Dockerfile con varias etapas; solo la última forma la imagen final. |
| **Pull Request (PR)** | Solicitud para fusionar una rama en otra, sujeta a revisión. |
| **Peer Review** | Revisión del código por un compañero antes del merge. |
| **Publish profile** | Archivo con las credenciales para desplegar a un App Service. |
| **Smoke test** | Prueba rápida posterior al despliegue para confirmar que la aplicación responde. |
| **App Settings** | Variables de configuración de Azure, inyectadas como variables de entorno. |
