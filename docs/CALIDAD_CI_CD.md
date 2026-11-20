# Calidad y CI/CD (TP4)

Documento del flujo de calidad automatizado: que se ejecuta en cada push y pull
request, que pruebas bloquean el despliegue y como reproducir el circuito en
local.

## Flujo de ramas y gate

```
PR -> development ──► CI: build + unit + integration + e2e + lint
                        │
                        ├─ falla -> PR bloqueado (checks obligatorios)
                        └─ pasa  -> merge habilitado
                                      │
                                      ▼
                    release PR development -> main
                                      │
                                      ▼
              CI sobre main ──► ¿success? ──► Deploy a Azure
                                      │
                                      └─ falla -> deploy NO se ejecuta
```

Reglas:

- `push` a `development` y `main`, y todo PR hacia esas ramas, ejecutan la suite completa.
- El deploy solo corre desde `main` y solo si el workflow **Continuous Integration**
  termino en `success` para ese mismo commit.
- Un PR con tests en rojo no se puede mergear: `development` y `main` exigen los
  status checks obligatorios (ver mas abajo).

## Workflows

| Workflow | Archivo | Disparador |
|---|---|---|
| Continuous Integration | `.github/workflows/continuous-integration.yml` | push a `development`/`main`, PR a `main`/`development`, manual |
| Build and Deploy to Azure | `.github/workflows/deploy.yml` | `workflow_run` del CI sobre `main`, manual |

### Jobs del CI

| Job | Que hace | Runner |
|---|---|---|
| `backend-unit-tests` | `dotnet test --filter Category=Unit` en todos los proyectos de prueba | self-hosted `ics-wsl` |
| `backend-integration-tests` | `dotnet test --filter Category=Integration` (WebApplicationFactory + SQLite) | self-hosted `ics-wsl` |
| `backend-e2e-tests` | Levanta la API con SQL Server y corre Newman | `ubuntu-latest` (necesita Docker) |
| `backend-build` | `dotnet build` con `TreatWarningsAsErrors` | self-hosted `ics-wsl` |
| `docker-build` | Build de la imagen de la API | `ubuntu-latest` |
| `frontend-build` / `frontend-lint` | Build y lint del frontend | self-hosted `ics-wsl` |
| `branch-name-check` | Valida el prefijo de la rama del PR (incluye `test/`) | self-hosted `ics-wsl` |

`backend-build` y `docker-build` tienen `needs` de los tres jobs de prueba: si
cualquiera falla, no se construye ni se publica nada.

Cada job de pruebas:

- usa cache de NuGet con `actions/cache@v4`;
- tiene `timeout-minutes`;
- publica `.trx`, cobertura (Cobertura XML) y JUnit con `actions/upload-artifact@v4`;
- falla si el filtro no ejecuto ninguna prueba (un `--filter` sin coincidencias
  devuelve exit code 0, por eso el script lo verifica explicitamente).

El workflow tiene `concurrency` con `cancel-in-progress: true`: un push nuevo
cancela la ejecucion anterior del mismo ref.

### Gate de despliegue

`deploy.yml` ya no escucha `push`. Se dispara con `workflow_run` cuando termina
el CI sobre `main` y el job tiene esta condicion:

```yaml
if: >
  github.event_name == 'workflow_dispatch' ||
  (github.event.workflow_run.conclusion == 'success' &&
   github.event.workflow_run.event == 'push')
```

Ademas:

- checkoutea el `head_sha` del run de CI, no el ultimo commit de la rama;
- escribe un `Step Summary` con el resultado del gate;
- si `SLACK_WEBHOOK_URL` esta configurado como secret, notifica el fallo a Slack.

> `workflow_run` solo funciona si `deploy.yml` existe en la rama default
> (`development`). Por eso este cambio se mergea primero a `development`.

## Status checks obligatorios

Configurados en **Settings > Branches** para `development` y `main`:

- `backend-unit-tests`
- `backend-integration-tests`
- `backend-e2e-tests`
- `backend-build`
- `docker-build`
- `frontend-build`
- `frontend-lint`
- `branch-name-check`

Con `Require branches to be up to date before merging` y
`Do not allow bypassing the above settings` para que el gate aplique tambien al
administrador del repositorio.

## Pruebas E2E (Postman + Newman)

Carpeta `tests/e2e`:

| Archivo | Uso |
|---|---|
| `TP4-E2E.postman_collection.json` | Coleccion de TP4-09: registro, login, seleccion de producto con stock, compra de 3 unidades, verificacion de stock, caso de stock insuficiente (400) y stock intacto; valida esquema JSON en cada respuesta |
| `TP4.postman_environment.json` | Entorno local (`http://localhost:5080`) |
| `TP4.azure.postman_environment.json` | Entorno desplegado en Azure |

El detalle de variables y asserts esta en [`tests/e2e/README.md`](../tests/e2e/README.md).

El login devuelve el JWT en una cookie `HttpOnly`; la coleccion extrae el token
del header `Set-Cookie` y lo reenvia como `Authorization: Bearer`, de modo que
no depende del cookie jar del cliente ni del flag `Secure`.

Comandos:

```bash
cd tests/e2e
npm ci

# Contra una API local
npm run test:e2e

# Contra el entorno de Azure
npm run test:e2e:azure
```

Los reportes JUnit y HTML quedan en `tests/e2e/results/`.

Para levantar la API local hace falta SQL Server (la app usa
`UseSqlServer` y aplica migraciones al arrancar). Con Docker:

```bash
# Elegi un valor local: no se versiona ninguno por defecto.
export TP4_SQL_PASSWORD='<password-local-de-8-o-mas-caracteres>'

docker run -d --name tp4-sql -p 1433:1433 \
  -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD="$TP4_SQL_PASSWORD" \
  mcr.microsoft.com/mssql/server:2022-latest
```

Y despues, con las variables de conexion y JWT:

```bash
export ASPNETCORE_ENVIRONMENT=Development
export ASPNETCORE_URLS=http://localhost:5080
export ConnectionStrings__Dsw2025Tpi="Server=localhost,1433;Database=Dsw2025Tpi;User Id=sa;Password=$TP4_SQL_PASSWORD;TrustServerCertificate=True;"
export Jwt__Key="$(openssl rand -hex 32)"
export DefaultAdminUser__Password='<password-local-para-el-admin>'
dotnet run --project ICS_TPI2026_backend/Dsw2025Tpi.Api --no-launch-profile
```

## Reproducir la suite en local

```bash
# Unitarias
dotnet test ICS_TPI2026_backend/Dsw2025Tpi.sln --filter "Category=Unit"

# Integracion (SQLite en memoria, no necesita SQL Server)
dotnet test ICS_TPI2026_backend/Dsw2025Tpi.sln --filter "Category=Integration"

# Mismo comando que usa el CI (genera trx + JUnit + cobertura)
bash scripts/ci/run-dotnet-tests.sh Unit unit
```

El **SDK 8** es obligatorio: los tests apuntan a `net8.0`. Si en la maquina
solo hay .NET 10, `dotnet test` falla con
`The PipeWriter ... does not implement PipeWriter.UnflushedBytes` (es una
incompatibilidad de `TestServer` con el runtime nuevo, no un fallo de las
pruebas).

## Evidencias para el informe

1. **Pipeline en rojo bloqueando el deploy**: PR a `development` con un test que
   falla a proposito. Captura de los checks en rojo y del merge bloqueado.
2. **Pipeline en verde desplegando**: merge a `development`, release PR a `main`,
   captura del CI verde y del run de deploy ejecutandose.
3. **Deploy no ejecutado**: captura del workflow de deploy en estado `skipped`
   cuando el CI fallo.

## Limitaciones conocidas

- El runner self-hosted `ics-wsl` necesita estar encendido (`.\runner.ps1`). Si
  la PC esta apagada, los jobs quedan en `Queued` hasta 24 h.
- El E2E corre en `ubuntu-latest` porque necesita Docker para SQL Server; el
  runner de WSL no tiene Docker.
- La suite no cubre el frontend (el TP4 pide xUnit sobre la logica de negocio
  del backend).
- `tests/e2e` escribe un usuario de prueba y una orden por ejecucion. En Azure
  conviene correrlo contra un entorno de staging o limpiar esos registros.
