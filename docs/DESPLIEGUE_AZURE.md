# Despliegue Automático en Azure — Guía Completa

> **Proyecto:** ICS TPI2026 — E-commerce  
> **Stack:** React 19 + .NET 8 + SQL Server  
> **Fecha:** Octubre 2026  
> **Cuenta:** Azure for Students ($100 USD de crédito)
> 
> **Nota:** esta guía documenta el paso a paso histórico de creación de los recursos. El documento consolidado oficial del TP3 (ramas, Dockerfile, pipeline, conexión API↔BD, endpoints y credenciales) es `docs/TP3_Documentacion.md`.

---

## Índice

1. [Creación de la cuenta Azure for Students](#1-creación-de-la-cuenta-azure-for-students)
2. [Resource Group](#2-resource-group)
3. [Azure SQL Server + Base de Datos](#3-azure-sql-server--base-de-datos)
4. [Web App para Contenedores (App Service)](#4-web-app-para-contenedores-app-service)
5. [Firewall de Azure SQL](#5-firewall-de-azure-sql)
6. [Application Settings (Variables de Entorno en Producción)](#6-application-settings-variables-de-entorno-en-producción)
7. [Docker Hub — Cuenta e Imagen](#7-docker-hub--cuenta-e-imagen)
8. [GitHub Secrets](#8-github-secrets)
9. [continuous-integration.yml vs deploy.yml](#9-continuous-integrationyml-vs-deployyml)
10. [Por qué el monorepo requiere working directory](#10-por-qu%C3%A9-el-monorepo-requiere-working-directory)
11. [Variables de entorno: Desarrollo vs Producción](#11-variables-de-entorno-desarrollo-vs-producción)
12. [Diferencias entre la guía del profesor y lo que implementamos](#12-diferencias-entre-la-guía-del-profesor-y-lo-que-implementamos)
13. [Flujo completo de despliegue](#13-flujo-completo-de-despliegue)
14. [Troubleshooting](#14-troubleshooting)

---

## 1. Creación de la cuenta Azure for Students

### Qué es
Microsoft ofrece a estudiantes universitarios **$100 USD de crédito anual** para usar servicios de Azure (SQL, App Service, etc.) sin costo. Se renueva cada año mientras se siga siendo estudiante.

### Requisitos
- Correo universitario (o cuenta personal con verificación de estudiante)
- Tarjeta de crédito/débito (se cobra **$1** para verificar identidad, se devuelve)
- Documento que acredite condición de estudiante

### Pasos de registro
1. Ir a **https://azure.microsoft.com/es-es/free/students/**
2. Click en **"Empezar gratis"** o **"Start free"**
3. Iniciar sesión con la cuenta Microsoft (la universitaria si es posible)
4. Completar datos personales (nombre debe coincidir exactamente con el documento)
5. Seleccionar país, región preferida (usar **Brazil South** si está disponible)
6. Agregar método de pago para verificación
7. Esperar a que Azure valide la cuenta (puede tomar minutos u horas)

### ⚠️ Si te rechazan la cuenta
- **Causa común:** El documento no es claro o el nombre no coincide
- **Solución:** Reaplicar con:
  - Certificado de inscripción con membrete y fecha ("Dated enrollment letter on school letterhead")
  - Foto de documento donde se lea el nombre completo
  - Que el nombre en Billing sea idéntico al del certificado

---

## 2. Resource Group

### Qué es
Un **Resource Group** es un contenedor lógico que agrupa todos los recursos relacionados con un proyecto. Facilita organizar y eliminar todo de una vez.

### Pasos
1. Portal Azure → **Resource groups** → **+ Create**
2. **Subscription:** Azure for Students
3. **Resource group name:** `rg-ics-tp3`
4. **Region:** Brazil South (o East US si Brazil South no está disponible)
5. **Review + Create** → **Create**

### Por qué importa
- Si borras el Resource Group, se borran TODOS los recursos dentro (SQL Server, App Service, etc.)
- Facilita controlar costos (todo queda bajo una misma suscripción)

---

## 3. Azure SQL Server + Base de Datos

### Qué es
- **SQL Server (lógico):** El servidor que maneja autenticación y firewall. Es "lógico" porque no es una VM física, es un servicio gestionado por Microsoft.
- **Database (ics-db):** La base de datos donde vive el esquema (tablas, migraciones, datos).

### Por qué no se dockeriza la DB
La guía del profesor enseña a usar `docker-compose` con SQL Server **local** para desarrollo. En producción, usar una DB gestionada (PaaS) es mejor porque:
- Microsoft se encarga de backups, updates, alta disponibilidad
- No consumes RAM/CPU de tu plan gratuito
- Es más seguro (firewall, encriptación, etc.)

### Pasos para crear el SQL Server
1. Portal Azure → **SQL servers** → **+ Create**
2. **Subscription:** Azure for Students
3. **Resource group:** `rg-ics-tp3`
4. **Server name:** `ics-sql-43739` (debe ser único globalmente)
5. **Region:** Brazil South
6. **Authentication:**
   - Mode: **SQL authentication** (usar usuario y contraseña, no Azure AD)
   - Server admin login: `FT-Key` (o el que prefieras)
   - Password: **GUARDALA EN UN LUGAR SEGURO** (no la pierdas)
7. **Networking:**
   - Allow Azure services and resources: **OFF** por ahora (lo activamos después)
   - Add current client IP: OFF (lo agregamos después)
8. **Review + Create** → **Create**

### Pasos para crear la Database
1. Una vez creado el Server, ir a **SQL databases** → **+ Create**
2. **Resource group:** `rg-ics-tp3`
3. **Database name:** `ics-db`
4. **Compute + storage:** usar el plan **GP_Standard** o **Basic** (el más barato para empezar)
5. **Server:** seleccionar `ics-sql-43739`
6. **Review + Create** → **Create**

### Connection String (obtenerlo)
1. Ir a la **Database** `ics-db`
2. **Connection strings** → **ADO.NET**
3. Copiar el string (tiene forma de):
```
Server=tcp:ics-sql-43739.database.windows.net,1433;Initial Catalog=ics-db;Persist Security Info=False;User ID=FT-Key;Password={your_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```
4. Reemplazar `{your_password}` con la contraseña real del admin

---

## 4. Web App para Contenedores (App Service)

### Qué es
**App Service** es el servicio de hosting de Azure para aplicaciones web. Cuando se elige **"Publicar: Contenedor"**, la App Service recibe una imagen Docker y la ejecuta.

### Por qué "Contenedor" y no "Código"
- **Contenedor:** Azure recibe una imagen Docker construida localmente o desde Docker Hub. Es reproducible y portable.
- **Código:** Azure recibe un `.zip` con binarios compilados. Es más simple pero menos controlado.

**El profesor pidió explícitamente "Web App Contenedor"** porque todo el curso usa Docker.

### Pasos para crear la Web App
1. Portal Azure → **App Services** → **+ Create**
2. **Subscription:** Azure for Students
3. **Resource group:** `rg-ics-tp3`
4. **Name:** `ics-tp3-43739` (debe ser único, será tu URL: `https://ics-tp3-43739.azurewebsites.net`)
5. **Publish:** **Docker Container** ⚠️ (NO "Code")
6. **Operating System:** Linux
7. **Region:** Brazil South
8. **Pricing plans:**
   - **Free F1** (recomendado para TP, 1GB RAM, sin Always On)
   - o **B1 Basic** (si tenés crédito, permite Always On)
9. **Docker:** elegir **"Single Container"** → **"Docker Hub"**
10. **Image:** `fntkey/ics-api:latest` (lo creamos en el paso 7)
11. **Review + Create** → **Create**

### ⚠️ Configuración del contenedor
Después de crear la Web App:
1. **Deployment** → **Container Settings**
2. **Startup Command:** `dotnet Dsw2025Tpi.Api.dll` (especificar el entrypoint)
3. **WEBSITES_PORT:** `8080` (configurado en App Settings, ver paso 6)
4. Guardar → Restart

---

## 5. Firewall de Azure SQL

### Por qué es obligatorio
Azure SQL tiene firewall activado por defecto. **Nadie puede conectarse** (ni tu app) hasta que agregues reglas. Sin esto, `Program.cs` falla al hacer `Database.Migrate()`.

### Pasos
1. Ir al **SQL Server** `ics-sql-43739` (NO a la Database)
2. **Networking** → **Firewalls and virtual networks**
3. Configurar:
   - **Allow Azure services and resources to access this server:** **ON** ⚠️ (permite que App Service se conecte)
   - **+ Add client IP:** click (agrega tu IP actual para pruebas desde PC)
4. **Save**

### Resultado esperado
- Tu IP (`181.85.x.x`) aparece en la lista de reglas
- El toggle "Allow Azure services" está activado
- La Web App (Azure) puede conectarse a la Database

---

## 6. Application Settings (Variables de Entorno en Producción)

### Qué son
Los **App Settings** son pares clave/valor que Azure inyecta como **variables de entorno** dentro del contenedor. .NET los lee con doble guión bajo `__` (que representa `:` en .NET).

**Ejemplo:** `ConnectionStrings__Dsw2025Tpi` en Azure → `ConnectionStrings:Dsw2025Tpi` en `appsettings.json`

### Dónde configurarlos
1. Ir a la **Web App** `ics-tp3-43739`
2. **Configuración** → **Configuración de la aplicación** (pestaña, no "Configuración general")
3. **+ Agregar** para crear cada par

### Lista completa de App Settings

| Name | Value | De dónde sale | Para qué sirve |
|------|-------|---------------|----------------|
| `ConnectionStrings__Dsw2025Tpi` | `Server=tcp:ics-sql-43739.database.windows.net,1433;Initial Catalog=ics-db;...Password=REAL;...` | Azure SQL (paso 3) | Conexión a la base de datos |
| `Jwt__Key` | `clave_random_40_chars` | PowerShell: `[Convert]::ToBase64String((1..40 \| % {Get-Random -Max 256}))` | Firma de tokens JWT (autenticación) |
| `Jwt__Issuer` | `Dsw2025Tpi.Api` | Fijo | Emisor del token JWT |
| `Jwt__Audience` | `Dsw2025Tpi.Client` | Fijo | Audiencia del token JWT (el frontend) |
| `DefaultAdminUser__UserName` | `admin` | Fijo | Usuario admin seed (se crea al iniciar) |
| `DefaultAdminUser__Email` | `admin@example.com` | Fijo | Email del admin seed |
| `DefaultAdminUser__Password` | `AdminTp3$2026!Xyz` | Definido por el equipo | Password del admin seed |
| `DefaultAdminUser__Role` | `Admin` | Fijo | Rol del admin seed |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Fijo | Entorno (afecta logging, Swagger, etc.) |
| `WEBSITES_PORT` | `8080` | Puerto del Dockerfile | Puerto donde App Service expone el contenedor |
| `EnableSwagger` | `true` | Para TP3 Bloque 4 | Idea original: habilitar Swagger en producción. **Importante:** `Program.cs` hoy solo publica Swagger con `IsDevelopment()` y **no lee** este flag, por lo que con `ASPNETCORE_ENVIRONMENT=Production` el `/swagger` responde 404. Falta implementar el flag en el código (ver `docs/TP3_Documentacion.md`, capítulo 6) |

### Por qué doble guión bajo
En .NET, la configuración usa `:` como separador (`ConnectionStrings:Dsw2025Tpi`), pero en Linux las variables de entorno **no pueden tener `:`** (es un carácter reservado). Azure usa `__` como alternativa y .NET lo convierte automáticamente.

### Guardar y reiniciar
1. Después de agregar todos los settings, click **Guardar**
2. Azure muestra popup: **"¿Reiniciar la aplicación?"** → **Sí**
3. Esperar 1-2 minutos a que el contenedor reinicie

---

## 7. Docker Hub — Cuenta e Imagen

### Qué es
**Docker Hub** es el registro público de imágenes Docker. Es como GitHub pero para contenedores. Cuando Azure hace pull de una imagen, la descarga de Docker Hub.

### Crear cuenta
1. Ir a **https://hub.docker.com**
2. Sign up (correo + password)
3. Verificar email
4. **Account settings** → **Personal access tokens** → **Generate new token**
   - Name: `github-actions-ics`
   - Permissions: **Read, Write, Delete**
   - Copiar el token (solo se muestra una vez)

### Login local
```bash
docker login -u fntkey
# Ingresar el password de Docker Hub
```

### Construir y subir la imagen
```bash
# Desde la raíz del backend (ICS_TPI2026_backend/)
docker build -t fntkey/ics-api:latest .
docker push fntkey/ics-api:latest
```

### Verificar
- Ir a **https://hub.docker.com/r/fntkey/ics-api**
- Debe aparecer la imagen pública con tag `latest`

---

## 8. GitHub Secrets

### Qué son
Los **Secrets** son valores cifrados que GitHub Actions usa en pipelines. Evitan que contraseñas queden expuestas en el código.

**Ejemplo en deploy.yml:**
```yaml
password: ${{ secrets.DOCKERHUB_TOKEN }}  # GitHub lo reemplaza en ejecución
```

Si escribieras el token directamente en el `.yml`, cualquiera con acceso al repo lo vería.

### Configurar
1. Ir al repo en GitHub: **Settings** → **Secrets and variables** → **Actions**
2. Pestaña **Secrets** → **+ New repository secret**

### Lista de Secrets

| Name | Value | Para qué sirve |
|------|-------|----------------|
| `DOCKERHUB_TOKEN` | PAT de Docker Hub | Login a Docker Hub para push de imagen |
| `APP_SERVICE_NAME` | `ics-tp3-43739` | Nombre de la App Service (para `azure/webapps-deploy`) |
| `AZURE_WEBAPP_PUBLISH_PROFILE` | XML del `.PublishSettings` | Credenciales de despliegue de Azure |

### Cómo obtener `AZURE_WEBAPP_PUBLISH_PROFILE`
1. App Service → **Overview** → **Get publish profile** (descarga `.PublishSettings`)
2. Abrir el archivo con **Bloc de notas**
3. Copiar **TODO** el XML (desde `<publishData` hasta `</publishData>`)
4. Pegar en GitHub como secret

**⚠️ Si te dice "Basic authentication is disabled":**
- App Service → **Configuración** → **Configuración general** (plataforma)
- Buscar **"Credenciales de publicación de autenticación básica de SCM"** → **Habilitado**
- Guardar → reintentar descargar el perfil

---

## 9. continuous-integration.yml vs deploy.yml

### continuous-integration.yml (CI)
**Propósito:** Validar código en cada PR (Pull Request).

**Se ejecuta cuando:**
- Se abre un PR a `development` o `main`
- Se hace push a una rama `feature/*`

**Qué hace:**
1. **backend-build:** Compila el proyecto .NET (`dotnet build`)
2. **docker-build:** Valida que el Dockerfile funcione (build sin push)
3. **frontend-build:** Compila React/Vite (`npm run build`)
4. **frontend-lint:** Ejecuta ESLint
5. **branch-name-check:** Verifica que el nombre de rama siga el patrón `feature/*`, `fix/*`, `hotfix/*`

**NO despliega nada** — solo valida.

### deploy.yml (CD - Continuous Deployment)
**Propósito:** Desplegar la API en Azure cuando el código está listo.

**Se ejecuta cuando:**
- Se hace push a `main` (merge de PR aprobado)
- Manualmente desde la pestaña Actions (**workflow_dispatch**)

**Qué hace:**
1. **Obtener el código** (`actions/checkout@v4`)
2. **Login en Docker Hub** (`docker/login-action@v3`)
3. **Construir imagen** (`docker build` con tags `:sha` y `:latest`)
4. **Push a Docker Hub** (`docker push`)
5. **Desplegar en Azure** (`azure/webapps-deploy@v3`)
6. **Smoke test** (verificar `/healthcheck` responde 200)

**Despliega a producción** — solo corre en `main`.

### Resumen comparativo

| Característica | continuous-integration.yml | deploy.yml |
|----------------|---------------------------|------------|
| Trigger | PR + push a feature/* | Push a main |
| Objetivo | Validar código | Desplegar a Azure |
| Corre en PRs | ✅ Sí | ❌ No |
| Despliega | ❌ No | ✅ Sí |
| Usa secrets | ❌ No | ✅ Sí |

---

## 10. Por qué el monorepo requiere working directory

### El problema
Nuestro repo tiene **dos proyectos** en carpetas separadas:
```
ICS_TPI2026/
├── ICS_TPI2026_backend/    ← .NET 8 + Dockerfile
├── ICS_TPI2026_frontend/   ← React + Vite
├── .github/workflows/      ← Pipelines
└── docs/
```

Cuando GitHub Actions ejecuta `docker build`, necesita saber **en qué carpeta** está el `Dockerfile`. Si no le decimos, busca en la raíz y falla.

### La solución
En `deploy.yml`, especificamos la ruta relativa al backend:
```yaml
- name: 3. Construir la imagen Docker (.NET 8)
  run: |
    docker build \
      -f ICS_TPI2026_backend/Dockerfile \    # ← Ruta al Dockerfile
      -t fntkey/ics-api:${{ github.sha }} \
      -t fntkey/ics-api:latest \
      ICS_TPI2026_backend                     # ← Contexto de build (donde están los .csproj)
```

**Parámetros clave:**
- `-f ICS_TPI2026_backend/Dockerfile`: Especifica dónde está el Dockerfile
- `ICS_TPI2026_backend` (último argumento): El "build context" — la carpeta desde donde se copian archivos al contenedor

### En continous-integration.yml
El job `docker-build` también usa `working-directory`:
```yaml
- name: Build Docker image
  working-directory: ICS_TPI2026_backend    # ← Cambia al directorio del backend
  run: docker build -t test-build .
```

Sin esto, `docker build` fallaría porque no encuentra el `Dockerfile` en la raíz del repo.

### ¿Por qué no separamos en dos repos?
- **Monorepo** facilita versionado conjunto (mismo commit para frontend y backend)
- Un solo PR puede tocar ambos proyectos
- Menos configuración de CI (un solo workflow para todo)
- El TP3 lo especifica como repo único

---

## 11. Variables de entorno: Desarrollo vs Producción

### Desarrollo local (appsettings.json + docker-compose)

En desarrollo, la configuración vive en **archivos** que NO se suben a Git (o se suben sin secretos reales):

**`appsettings.json`** (desarrollo):
```json
{
  "ConnectionStrings": {
    "Dsw2025Tpi": "Server=localhost,1433;Database=ics-db;User Id=sa;Password=Your_password123!;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "clave_de_desarrollo_no_usar_en_produccion",
    "Issuer": "Dsw2025Tpi.Api",
    "Audience": "Dsw2025Tpi.Client"
  }
}
```

**`docker-compose.yml`** (solo local):
```yaml
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      SA_PASSWORD: "Your_password123!"
    ports:
      - "1433:1433"
  
  api:
    build: .
    environment:
      ConnectionStrings__Dsw2025Tpi: "Server=sqlserver,1433;..."
    depends_on:
      - sqlserver
```

### Producción (Azure App Settings)

En producción, **NO** se usan archivos de configuración. Todo vive en **App Settings** de Azure (ver paso 6).

**¿Por qué no subir `appsettings.json` con secretos a Azure?**
- Si el repo es público (como el nuestro), cualquiera vería las contraseñas
- Azure App Settings está cifrado en reposo
- Fácil rotar secretos sin redeploy (solo reiniciar la app)

### Flujo de configuración en .NET
```csharp
// Program.cs lee la config en este orden (el último gana):
builder.Configuration
    .AddJsonFile("appsettings.json")                    // 1. Archivo base
    .AddJsonFile($"appsettings.{env.EnvironmentName}.json") // 2. Por ambiente
    .AddEnvironmentVariables();                         // 3. Variables de entorno (App Settings)
```

Si `appsettings.json` tiene `"Environment": "Production"`, pero Azure tiene `ASPNETCORE_ENVIRONMENT=Production`, **Azure gana**.

### Tabla resumen

| Config | Desarrollo | Producción (Azure) |
|--------|------------|-------------------|
| Connection string | `appsettings.json` o `docker-compose.yml` | App Settings `ConnectionStrings__Dsw2025Tpi` |
| JWT Key | `appsettings.json` (clave débil) | App Settings `Jwt__Key` (clave fuerte) |
| Admin seed | `appsettings.json` | App Settings `DefaultAdminUser__*` |
| Environment | `appsettings.Development.json` | `ASPNETCORE_ENVIRONMENT=Production` |
| Puerto | 5000/5001 (Kestrel) o 1433 (SQL) | `WEBSITES_PORT=8080` |

---

## 12. Diferencias entre la guía del profesor y lo que implementamos

### Guía del profesor (docs/guia-tp3.md)

El profesor enseña un enfoque **simplificado**:
1. Dockerizar backend
2. Subir imagen a Docker Hub
3. Crear Azure SQL + Web App Contenedor
4. Pipeline GitHub Actions básico

### Lo que implementamos (diferencias)

| Aspecto | Guía del profesor | Nuestra implementación | Por qué cambió |
|---------|-------------------|------------------------|----------------|
| **Registro de imágenes** | Docker Hub (genérico) | Docker Hub (`fntkey/ics-api`) | Igual, pero con cuenta propia verificada |
| **Pipeline** | Un solo `deploy.yml` | `continuous-integration.yml` + `deploy.yml` | Separamos CI (validar) de CD (desplegar) — mejor práctica |
| **Working directory** | No especificado | `-f ICS_TPI2026_backend/Dockerfile` | Porque tenemos monorepo, la guía asume repo simple |
| **App Settings** | Mencionados pero sin lista detallada | 11 settings documentados paso a paso | El TP3 exige documentar cada variable |
| **Firewall SQL** | No detallado | Pasos específicos con "Allow Azure services" | Sin esto la app no conecta (error común) |
| **Publish Profile** | No menciona problema de "Basic auth" | Explica cómo habilitarlo | Azure cambió defaults de seguridad recientemente |
| **DB en Docker** | `docker-compose` con SQL Server local | Azure SQL (PaaS) | Dockerizar la DB en producción consume recursos innecesarios |
| **Smoke test** | No incluido | Step 6 en `deploy.yml` verifica `/healthcheck` | Validar que el deploy funcionó antes de dar por bueno |

### ¿Por qué es mejor lo nuestro?
1. **Separación de responsabilidades:** CI valida, CD despliega
2. **Monorepo-friendly:** Rutas explícitas al backend
3. **Documentado:** Cada paso del Azure está explicado
4. **Seguro:** Secrets en GitHub, no en código
5. **Verificado:** Smoke test post-deploy

---

## 13. Flujo completo de despliegue

### Setup inicial (una sola vez)
```
1. Crear cuenta Azure for Students
2. Crear Resource Group (rg-ics-tp3)
3. Crear SQL Server + Database
4. Crear Web App (Contenedor)
5. Configurar firewall SQL
6. Configurar App Settings
7. Crear cuenta Docker Hub
8. Build + push imagen local
9. Crear GitHub Secrets (3)
```

### Deploy de código (cada vez)
```
1. Crear rama feature/*
2. Commitear cambios
3. Push a origin
4. Abrir PR a development
5. 1 peer review
6. Merge a development
7. Abrir PR development → main
8. 1 peer review
9. Merge a main
10. GitHub Actions ejecuta deploy.yml automáticamente
11. Azure hace pull de imagen nueva
12. Smoke test verifica /healthcheck
```

### Diagrama del flujo
```
Developer                GitHub                 Docker Hub           Azure
    │                      │                       │                   │
    ├── git push ─────────►│                       │                   │
    │                      │                       │                   │
    ├── PR a development ─►│                       │                   │
    │                      ├── continuous-integration.yml              │
    │                      │   (build, lint, validate)                 │
    │                      │                       │                   │
    ├── merge a main ─────►│                       │                   │
    │                      ├── deploy.yml ejecuta  │                   │
    │                      │   docker build ──────►│                   │
    │                      │   docker push ───────►│                   │
    │                      │                       │   azure/webapps-deploy
    │                      │                       │──────────────────►│
    │                      │                       │                   ├── docker pull
    │                      │                       │                   ├── restart
    │                      │                       │                   └── /healthcheck
```

---

## 14. Troubleshooting

### Errores comunes

#### "Login failed for user 'FT-Key'"
- **Causa:** Firewall SQL no permite tu IP o la IP de Azure
- **Solución:** Verificar paso 5 (firewall)

#### "The connection to the database timed out"
- **Causa:** SQL Server no está corriendo o firewall bloquea
- **Solución:** Verificar que el SQL Server esté "Online" en el portal

#### "Cannot open database 'ics-db'"
- **Causa:** La base de datos no existe o el connection string está mal
- **Solución:** Verificar nombre de DB y connection string en App Settings

#### "Docker build failed: no such file or directory"
- **Causa:** El `Dockerfile` no se encuentra (monorepo sin working directory)
- **Solución:** Verificar `-f ICS_TPI2026_backend/Dockerfile` en `deploy.yml`

#### "Publish profile download fails: Basic auth disabled"
- **Causa:** Azure deshabilitó autenticación básica SCM por defecto
- **Solución:** App Service → Configuración → Habilitar "Credenciales de publicación de autenticación básica de SCM"

#### "Container never started"
- **Causa:** Puerto incorrecto o startup command mal configurado
- **Solución:** Verificar `WEBSITES_PORT=8080` y startup command `dotnet Dsw2025Tpi.Api.dll`

#### "App sleeps after 20 minutes"
- **Causa:** Plan F1 Free no tiene Always On
- **Solución:** Subir a plan B1 Basic (cuesta ~$13/mes) o aceptar cold start

### Cómo ver logs

#### Logs de Azure App Service
1. App Service → **Monitoring** → **Log stream**
2. O **Deployment** → **Deployment Center** → **Logs**

#### Logs de Docker (por qué no inicia)
1. App Service → **Deployment** → **Container Settings**
2. Click en **"Logs"** para ver stdout/stderr del contenedor

#### Logs de GitHub Actions
1. Repo → **Actions** → click en el workflow ejecutado
2. Click en el step que falló para ver detalles

### Comandos útiles

#### Probar conexión a SQL desde PC
```bash
sqlcmd -S ics-sql-43739.database.windows.net,1433 -d ics-db -U FT-Key -P "tu_password" -C
```

#### Probar la API
```bash
curl https://ics-tp3-43739.azurewebsites.net/healthcheck
# Debe responder: "Healthy"
```

#### Ver imagen en Docker Hub
```bash
docker pull fntkey/ics-api:latest
docker run -p 8080:8080 fntkey/ics-api:latest
```

---

## Apéndice: Archivos del proyecto

| Archivo | Propósito |
|---------|-----------|
| `ICS_TPI2026_backend/Dockerfile` | Multi-stage build (SDK 8 → aspnet 8) |
| `ICS_TPI2026_backend/docker-compose.yml` | Solo para desarrollo local (SQL + API) |
| `.github/workflows/continuous-integration.yml` | CI: validar PRs |
| `.github/workflows/deploy.yml` | CD: desplegar a Azure |
| `docs/DESPLIEGUE_AZURE.md` | Este documento |

---

## Contacto y recursos

- **Repo GitHub:** https://github.com/FT-Key/ICS_TPI2026
- **Azure Portal:** https://portal.azure.com
- **Docker Hub:** https://hub.docker.com/r/fntkey/ics-api
- **App Service:** https://ics-tp3-43739.azurewebsites.net
- **Guía del profesor:** `docs/guia-tp3.md`
- **TP3 PDF:** `docs/ICS2026_TP3.pdf`

---

**Última actualización:** 19 de Octubre 2026  
**Autor:** Equipo ICS TPI2026
