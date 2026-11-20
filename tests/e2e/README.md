# TP4-09 — Pruebas E2E con Postman/Newman

Flujo de compra de punta a punta contra la API real:

1. **Registro** de un usuario efímero (`POST /api/auth/register`).
2. **Login** (`POST /api/auth/login`) y captura del JWT en `{{token}}`.
3. **Selección** de un producto activo con stock suficiente (`GET /api/products`).
4. **Compra** de 3 unidades (`POST /api/orders`) → `201 Created`.
5. **Verificación** del stock descontado (`GET /api/products/{id}`).
6. **Escenario negativo**: compra por encima del stock → `400 Bad Request` y stock intacto.

Cada request valida el status esperado y el esquema JSON de la respuesta. La compra se verifica
contra el stock inicial leído del catálogo.

## Archivos

| Archivo | Uso |
|---|---|
| `TP4-E2E.postman_collection.json` | Colección Postman/Newman con los 7 requests y sus asserts |
| `TP4.postman_environment.json` | Environment local (`http://localhost:5080`) con `baseUrl`, `purchaseQuantity` y `testUserPassword` |
| `TP4.azure.postman_environment.json` | Mismas variables, con `baseUrl` apuntando al App Service de Azure |
| `package.json` | Scripts `test:e2e` y `test:e2e:azure` (Newman + reporters JUnit/HTML) |
| `results/` | Salida generada (JUnit y HTML); ignorada por git |

## Requisitos

- Node.js 18 o superior.
- API levantada (local o desplegada) con la base de datos accesible.
- Para el entorno local, `Jwt__Key` y `DefaultAdminUser__Password` configurados como en el
  [README del backend](../../ICS_TPI2026_backend/README.md).

## Ejecución local

El environment local apunta a `http://localhost:5080`, el mismo puerto que usa el job de CI.
Con la API en otro puerto (`dotnet run` usa `5142`; `docker compose up -d`, `8080`) se
sobreescribe `baseUrl`:

```bash
cd tests/e2e
npm ci
npm run test:e2e                                               # API en :5080
npm run test:e2e -- --env-var baseUrl=http://localhost:5142    # API con dotnet run
```

El script genera:

- `results/e2e-junit.xml` — reporte JUnit para CI.
- `results/e2e-report.html` — reporte HTML navegable.

Newman imprime además el resumen por consola (`--reporters cli,junit,htmlextra`).

## Ejecución contra Azure

El script `test:e2e:azure` usa `TP4.azure.postman_environment.json`, que apunta al App Service:

```bash
cd tests/e2e
npm ci
npm run test:e2e:azure
```

Genera `results/e2e-azure-junit.xml` y `results/e2e-azure-report.html`. O indicando la URL
explícitamente con el environment local:

```bash
npm run test:e2e -- \
  --env-var baseUrl=https://ics-tp3-43739-eveqhxemd5cmdee7.brazilsouth-01.azurewebsites.net
```

En PowerShell:

```powershell
npm run test:e2e -- --env-var baseUrl=https://ics-tp3-43739-eveqhxemd5cmdee7.brazilsouth-01.azurewebsites.net
```

## Variables del environment

| Variable | Valor por defecto | Descripción |
|---|---|---|
| `baseUrl` | `http://localhost:5080` | URL base de la API (sin `/` final) |
| `purchaseQuantity` | `3` | Unidades que se compran en el flujo principal |
| `testUserPassword` | *(vacío)* | Password del usuario efímero que crea la colección |

`testUserPassword` **no es una credencial de administrador**: se usa para registrar un usuario
`User` nuevo en cada corrida (`e2e_<timestamp>`). El repositorio no versiona ninguna contraseña:
si la variable viene vacía, la colección genera una aleatoria al registrar el usuario. Para fijarla
(por ejemplo, para reproducir una corrida), pasarla desde una variable de entorno:

```bash
npm run test:e2e -- --env-var testUserPassword="$E2E_TEST_USER_PASSWORD"
```

## Asserts incluidos

| Request | Status | Validaciones |
|---|---|---|
| `POST /api/auth/register` | `200` | Usuario creado |
| `POST /api/auth/login` | `200` | Esquema JSON, rol `User`, token capturado en `{{token}}`, `customerId` resuelto |
| `GET /api/products` | `200` | Esquema del catálogo, producto activo con `stockQuantity >= purchaseQuantity` |
| `POST /api/orders` | `201` | Esquema de la orden, estado `PENDING`, producto/cantidad, total = precio × cantidad |
| `GET /api/products/{id}` | `200` | Esquema del producto, `stock final = stock inicial - purchaseQuantity` |
| `POST /api/orders` (sin stock) | `400` | Mensaje con `Stock insuficiente` |
| `GET /api/products/{id}` | `200` | Stock intacto tras el rechazo |

## Notas de compatibilidad

- **Ruta de login:** la tarjeta menciona `POST /api/authenticate`; la API expone
  `POST /api/auth/login`. La colección usa la ruta real.
- **JWT en cookie HttpOnly (AT-06):** el contrato actual devuelve el perfil y entrega el JWT en la
  cookie `token`. La colección lo captura y lo guarda en la variable de colección `{{token}}`, que
  se envía como `Authorization: Bearer`. También acepta el contrato anterior con `{ token }` en el
  body, para poder correr contra un despliegue que todavía no tiene AT-06.
- **`total` vs `totalItems` (TP4-02):** el esquema del catálogo acepta ambos nombres. El entorno
  desplegado usa `total`; el código actual usa `totalItems`.
- **`customerId` en el body:** la orden lo envía por compatibilidad con despliegues anteriores a
  AT-01. El contrato actual lo ignora y toma el cliente del claim `id` del JWT.
- **Registro previo:** el admin seed no tiene fila espejo en `Customers`; la orden se crea a nombre
  del cliente autenticado, por eso la colección registra un usuario `User` antes del login.

## Efectos de la corrida

Cada ejecución crea un usuario de prueba, una orden y descuenta `purchaseQuantity` unidades del
primer producto activo con stock suficiente. El escenario negativo **no** modifica el inventario.
Usar un entorno de pruebas o una cuenta/DB descartable; no apuntar a datos productivos sensibles.

## CI

El job `backend-e2e-tests` de `.github/workflows/continuous-integration.yml` (definido en TP4-10 y
requerido por la rama protegida):

1. Levanta SQL Server 2022 como service container en `ubuntu-latest`.
2. Compila y arranca la API en `http://localhost:5080`.
3. Ejecuta `npm ci && npm run test:e2e` con esta colección.
4. Publica `tests/e2e/results/` (JUnit + HTML) como artifact `backend-e2e-results`.

La colección no requiere secrets: la password del usuario de prueba se genera en cada corrida.
