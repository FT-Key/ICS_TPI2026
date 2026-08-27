# PLAN — Estado Actual y Plan de Implementación

## Objetivo

Documento vivo que refleja el estado real del proyecto, lo que falta implementar y el plan priorizado para avanzar. Se actualiza conforme se completa cada item.

---

## Estado Actual del Proyecto

### Backend

| Componente | Estado | Detalle |
|---|---|---|
| Arquitectura en capas | ✅ Implementada | Domain → Application → Data → Api |
| Entidades de dominio | ✅ 4 de 6 | Customer, Product, Order, OrderItem |
| DbContext (Business) | ✅ Configurado | Fluent API, 4 DbSets, seed data |
| DbContext (Identity) | ✅ Configurado | ASP.NET Identity con tablas custom |
| Migraciones EF Core | ✅ Existen | 2 migraciones Business + 1 Identity |
| Repositorio genérico | ✅ Implementado | `EfRepository` con CRUD completo |
| Controllers | ✅ 3 implementados | Auth, Products, Orders |
| Servicios de negocio | ✅ 3 implementados | Products, Orders, JwtToken |
| DTOs | ✅ 6 archivos | Customer, Product, Order, OrderItem, Login, Register |
| Validadores | ✅ 4 implementados | Estáticos, manuales if/throw |
| Excepciones personalizadas | ✅ 3 implementadas | Application, Duplicated, NotFound |
| Autenticación JWT | ✅ Configurada | ASP.NET Identity + JwtBearer |
| CORS | ✅ Configurado | localhost:5173/5174 |
| Swagger | ✅ Configurado | Swashbuckle |
| Seed data | ✅ Configurado | Products, Customers, Orders desde JSON |
| Password hashing | ✅ Implementado | ASP.NET Identity (PBKDF2) |

### Frontend

| Componente | Estado | Detalle |
|---|---|---|
| React 19 + Vite + Tailwind | ✅ Configurado | Build tool y estilos |
| Módulo Auth | ✅ Funcional | Login, Register, ProtectedRoute, context |
| Módulo Products (Admin) | ✅ Funcional | List con paginación/filtros, Create |
| Módulo Products (User) | ✅ Funcional | Catálogo público de productos habilitados |
| Módulo Orders (Admin) | ✅ Funcional | List con búsqueda y filtro por estado |
| Módulo Orders (User) | ✅ Funcional | Crear orden desde carrito |
| Módulo Cart | ✅ Funcional | Carrito con localStorage, checkout |
| Módulo Home | ✅ Funcional | Dashboard con contadores |
| Componentes shared | ✅ 9 componentes | Button, Card, Input, Modal, Pagination, SearchBar, etc. |
| Hooks custom | ✅ 5 hooks | useAuth, useCart, usePagination, useToggleMap, useDeleteQuantity |
| Axios + interceptores | ✅ Configurado | Token injection, 401 handling |
| Layout Dashboard | ✅ Implementado | Sidebar + header + Outlet |

---

## Entidades Implementadas vs Planificadas

### Implementadas

| Entidad | Propiedades | Estado |
|---|---|---|
| **Customer** | Id (Guid), Name, Email, PhoneNumber? | ✅ Creada con validaciones |
| **Product** | Id, Sku, InternalCode, Name, Description, CurrentUnitPrice, StockQuantity, IsActive | ✅ Creada con validaciones |
| **Order** | Id, Date, ShippingAddress?, BillingAddress?, Notes?, TotalAmount (computed), Status (enum), CustomerId FK | ✅ Creada con validaciones |
| **OrderItem** | Id, Quantity, UnitPrice, Subtotal (computed), OrderId FK, ProductId FK | ✅ Creada con validaciones |
| **OrderStatus** | Enum: PENDING=1, PROCESSING=2, SHIPPED=3, DELIVERED=4, CANCELLED=5 | ✅ Creado |

### Pendientes de implementar

| Entidad | Propiedades planificadas | Prioridad |
|---|---|---|
| **Category** | Id, Name, Description | Media — Necesaria para agrupar productos |
| **PaymentMethod** | Id, PaymentType, LastFourDigits, IsDefault, UserId FK | Baja — Funcionalidad extendida |

### Diferencias notables con el plan original

| Aspecto | Plan original | Implementación actual |
|---|---|---|
| Entidad de usuario | `User` (custom) | `IdentityUser` (ASP.NET Identity) + `Customer` (espejo) |
| Roles | Enteros (0=Admin, 1=Client) | Strings ("Admin", "User") via Identity |
| Categorías | Entidad `Category` con FK en Product | No implementada |
| PaymentMethod | Entidad separada | No implementada |
| ShippingAddress | Value Object embebido | Campo `string?` simple en Order |
| BillingInfo | Value Object embebido | Campo `string?` simple en Order |
| OrderNumber | Campo único | No existe (se usa `Id` como identificador) |

---

## Diagrama de Relaciones (ER) — Implementado

```
Customer 1───────* Order
                     │
                     │ 1
                     │
OrderItem *─────────*
   │
   │ *
Product (sin FK a Category)

IdentityUser ────── 1:1 ──── Customer (via Guid)
```

**Notas:**
- `Customer` se crea como espejo de `IdentityUser` al registrar
- `Order.TotalAmount` es computed (`=> OrderItems.Sum(...)`)
- `OrderItem.Subtotal` es computed (`=> Quantity * UnitPrice`)
- `Order.Date` se setea en `DateTime.Now` en el constructor

---

## Seguridad — Estado Actual

### Implementado

| Componente | Estado | Detalle |
|---|---|---|
| JWT Bearer Authentication | ✅ | `JwtTokenService` genera tokens con claims `sub`, `jti`, role, `id` |
| ASP.NET Identity | ✅ | `IdentityUser` + `IdentityRole`, tablas custom en español |
| Password Hashing | ✅ | PBKDF2 via ASP.NET Identity (no BCrypt) |
| Roles | ✅ | "Admin" y "User" seeded al startup |
| Admin por defecto | ✅ | `admin` / `SecurePassword123!` / role Admin |
| CORS | ✅ | `localhost:5173`, `localhost:5174` + credentials |
| Autorización por roles | ✅ | `[Authorize(Roles="Admin")]` en endpoints protegidos |
| Registro de usuarios | ✅ | Crea `IdentityUser` + `Customer` espejo |

### Pendiente / Deuda de seguridad

| Issue | Prioridad | Detalle |
|---|---|---|
| JWT secret en appsettings.json | Crítica | Clave committed a repo, cualquier persona puede forjar tokens |
| Credenciales admin en plaintext | Crítica | `SecurePassword123!` en appsettings.json |
| Sin rate limiting | Alta | Login/register vulnerables a fuerza bruta |
| Token en localStorage | Alta | Vulnerable a XSS |
| Débil password policy | Media | Solo requiere longitud 8, sin complejidad |
| Sin HSTS | Media | No hay middleware `UseHsts()` |
| Sin validación JWT expiry en frontend | Media | jwt-decode no verifica expiración |

---

## Arquitectura — Clean Architecture

### Capas implementadas

```
Dsw2025Tpi.Api/            → Presentación (Controllers, Program.cs, DI)
    ↓ referencia a
Dsw2025Tpi.Application/    → Lógica de negocio (Services, DTOs, Validators, Exceptions)
    ↓ referencia a
Dsw2025Tpi.Domain/         → Modelo de dominio (Entities, Interfaces)
    ↑ referencia a
Dsw2025Tpi.Data/           → Infraestructura (DbContext, Repositories, Migrations)
```

### ⚠️ Problema de arquitectura detectado

`Application.csproj` referencia a `Data.csproj`, lo cual crea una dependencia circular en Clean Architecture. La capa Application no debería conocer la capa de Data directamente — debería depender solo de Domain (interfaces).

```
Flujo correcto:  Api → Application → Domain ← Data
Flujo actual:    Api → Application → Data → Domain  (Application conoce Data)
```

---

## Controllers — Endpoints Implementados

### AuthenticateController (`/api/auth`)

| Método | Ruta | Auth | Descripción |
|---|---|---|---|
| POST | `/api/auth/login` | Anónimo | Login, retorna `{ token }` |
| POST | `/api/auth/register` | Anónimo | Registro, retorna mensaje de éxito |

### ProductsController (`/api/products`)

| Método | Ruta | Auth | Descripción |
|---|---|---|---|
| GET | `/api/products` | Anónimo | Catálogo público (solo activos), paginado |
| GET | `/api/products/{id}` | Admin,User | Detalle de producto |
| POST | `/api/products` | Admin | Crear producto |
| PUT | `/api/products/{id}` | Admin | Actualizar producto |
| PATCH | `/api/products/{id}` | Admin | Soft-delete (desactivar) |
| GET | `/api/products/admin` | Admin | Listado admin (todos los estados), con filtros |

### OrderController (`/api/orders`)

| Método | Ruta | Auth | Descripción |
|---|---|---|---|
| GET | `/api/orders` | Admin | Listar todas las órdenes, paginado |
| POST | `/api/orders` | Admin,User | Crear orden (verifica stock, descuenta) |
| GET | `/api/orders/{id}` | Admin | Detalle de orden con items |
| PUT | `/api/orders/{id}` | Admin | Cambiar estado de orden |

---

## Servicios — Lógica de Negocio

### ProductsManagementService

- `GetProductById(Guid)` — Búsqueda por ID
- `GetAllProducts()` — Todos los activos (sin paginación)
- `GetProducts(FilterProduct)` — Filtro por status + búsqueda textual + paginación
- `AddProduct(Request)` — Validación + unicidad SKU
- `UpdateProduct(Guid, Request)` — Validación + unicidad SKU (excluye self)
- `PatchProduct(Guid)` — Soft-delete, previene doble desactivación

### OrdersManagementService

- `GetOrderById(Guid)` — Con eager loading de OrderItems + Product
- `GetAllOrders(SearchOrder)` — Filtra canceladas, valida CustomerId, parsea status, paginación
- `AddOrder(Request)` — Validación completa, verifica stock por item, descuenta stock, crea Order + OrderItems
- `UpdateOrderStatus(Guid, string)` — Transición de estado, restaura stock al cancelar

### JwtTokenService

- Genera JWT con claims: `sub` (username), `jti` (GUID), role, `id` (IdentityUser.Id)
- Algoritmo: HMAC-SHA256
- Expiración: 60 min (default, no configurable via appsettings)

---

## Lo que Falta Implementar

### Prioridad Crítica (Seguridad)

| # | Item | Esfuerzo | Detalle |
|---|---|---|---|
| 1 | Mover JWT secret a Variables de Entorno | Bajo | Secret en appsettings.json es un riesgo de seguridad |
| 2 | Mover credenciales admin a Variables de Entorno | Bajo | User Secrets en dev, Key Vault en prod |
| 3 | Agregar rate limiting | Medio | `Microsoft.AspNetCore.RateLimiting` en login/register |
| 4 | Configurar expiry JWT configurable | Bajo | Agregar `ExpireInMinutes` a appsettings.json |
| 5 | Fortalecer password policy | Bajo | Requerir mayúscula, número, carácter especial |

### Prioridad Alta (Seguridad — Cookies)

| # | Item | Esfuerzo | Detalle |
|---|---|---|---|
| 6 | Backend: soporte autenticación por cookies | Medio | Set-Cookie en login, leer cookie en endpoints, CORS AllowCredentials |
| 7 | Frontend: migrar token a HttpOnly cookie | Alto | Dejar de usar localStorage, enviar cookies con Axios |
| 8 | Seguridad avanzada de cookies | Alto | Secure + SameSite, refresh token en cookie separada, revocación |

### Prioridad Alta (Infraestructura — Base de Datos)

| # | Item | Esfuerzo | Detalle |
|---|---|---|---|
| 1 | **Migrar de LocalDB a Docker SQL Server** | Medio | LocalDB tiene limitaciones: no soporta conexiones múltiples, inestable, no disponible en Linux/Mac nativo, no apto para producción. Ver sección dedicada abajo. |

### Prioridad Alta (Arquitectura y Calidad)

| # | Item | Esfuerzo | Detalle |
|---|---|---|---|
| 6 | Implementar Unit of Work | Medio | `IUnitOfWork` con `SaveChangesAsync()` centralizado |
| 7 | Corregir dependencia circular Application→Data | Medio | Application solo debe referenciar Domain |
| 8 | Agregar global exception handling middleware | Medio | Reemplazar try/catch ad-hoc en controllers |
| 9 | Eliminar `BaseController.cs` (código muerto) | Bajo | Ningún controller lo usa |
| 10 | Corregir bugs en DbContext | Bajo | `BillingAddress.HasPrecision(15,2)`, `Order.Date.HasMaxLength(10)` |
| 11 | Agregar unique index en `Product.Sku` | Bajo | Prevenir race condition en creación |
| 12 | Corregir GUIDs duplicados en seed data | Bajo | `Products.json` tiene dos productos con mismo GUID |
| 13 | Corregir typo `TotatAmount` → `TotalAmount` en DTOs | Bajo | Afecta frontend también |

### Prioridad Media (Funcionalidad)

| # | Item | Esfuerzo | Detalle |
|---|---|---|---|
| 14 | Crear entidad `Category` | Medio | Para agrupar productos |
| 15 | Crear endpoint `GET /api/dashboard/stats` | Medio | Dashboard admin con datos reales |
| 16 | Agregar `GET /api/orders/mine` para clientes | Medio | Clientes ven solo sus órdenes |
| 17 | Agregar validación de expiración JWT en frontend | Bajo | Verificar `exp` antes de enviar request |
| 18 | Configurar health check con SQL Server | Bajo | `AddHealthChecks().AddSqlServer()` |
| 19 | Agregar Swagger security definition (Bearer) | Bajo | Configurar `AddSecurityDefinition` correctamente |

### Prioridad Baja (Mejoras)

| # | Item | Esfuerzo | Detalle |
|---|---|---|---|
| 20 | Agregar Serilog (logging estructurado) | Medio | JSON logging para producción |
| 21 | Agregar API versioning | Medio | `/api/v1/products` |
| 22 | Crear entidad `PaymentMethod` | Baja | Funcionalidad extendida |
| 23 | Agregar paginación con límite max | Bajo | Prevenir `PageSize=999999` |
| 24 | Implementar refresh tokens | Alto | Rotación de tokens sin re-login |
| 25 | Mover token a HttpOnly cookie | Alto | Protección contra XSS |

---

## Fase 0 — Migración de Base de Datos: LocalDB → Docker SQL Server

### Contexto

Actualmente el backend usa **LocalDB** (`(localdb)\MSSQLLocalDB`) con Windows Authentication. LocalDB tiene limitaciones importantes:

- **No acepta conexiones TCP remotas** — solo funciona localmente con named pipes
- **Inestable** — el motor se inicia/detiene automáticamente, a veces falla
- **No disponible en Linux/Mac** — solo funciona en Windows con SQL Server Express instalado
- **Sin soporte de autenticación por usuario** — usa Windows Auth (Integrated Security)
- **No apto para contenedores Docker** — no se puede empaquetar en un Dockerfile
- **Limitaciones de rendimiento** — 10 GB max, un solo usuario a la vez

### Plan de migración

#### 1. Crear `docker-compose.yml` en la raíz del backend

```yaml
version: "3.8"
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: ics-sqlserver
    environment:
      ACCEPT_EULA: "Y"
      MSSQL_SA_PASSWORD: "Dsw2025Tpi_Sa123!"   # Cambiar en producción
      MSSQL_PID: "Developer"                     # Licencia Developer (gratis)
    ports:
      - "1433:1433"
    volumes:
      - sqlserver-data:/var/opt/mssql
    healthcheck:
      test: /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "Dsw2025Tpi_Sa123!" -Q "SELECT 1" || exit 1
      interval: 10s
      timeout: 5s
      retries: 5

volumes:
  sqlserver-data:
```

#### 2. Actualizar connection string en `appsettings.json`

**Actual (LocalDB):**
```json
"ConnectionStrings": {
  "Dsw2025Tpi": "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=Dsw2025Tpi;Integrated Security=True;"
}
```

**Nuevo (Docker SQL Server):**
```json
"ConnectionStrings": {
  "Dsw2025Tpi": "Data Source=localhost,1433;Initial Catalog=Dsw2025Tpi;User Id=sa;Password=Dsw2025Tpi_Sa123!;TrustServerCertificate=True;"
}
```

> **Nota:** `AuthenticateContext` y `Dsw2025TpiContext` usan la misma connection string (`Dsw2025Tpi`), así que ambas se afectan automáticamente.

#### 3. Eliminar migraciones existentes y recrear

```bash
cd ICS_TPI2026_backend
dotnet ef migrations remove --project Dsw2025Tpi.Data --startup-project Dsw2025Tpi.Api
# Repetir hasta eliminar todas las migraciones
dotnet ef migrations add InitialCreate --project Dsw2025Tpi.Data --startup-project Dsw2025Tpi.Api
dotnet ef database update --project Dsw2025Tpi.Data --startup-project Dsw2025Tpi.Api
```

#### 4. Crear `appsettings.Development.json` (override seguro)

```json
{
  "ConnectionStrings": {
    "Dsw2025Tpi": "Data Source=localhost,1433;Initial Catalog=Dsw2025Tpi;User Id=sa;Password=Dsw2025Tpi_Sa123!;TrustServerCertificate=True;"
  }
}
```

#### 5. Actualizar `DB_Config.md`

Sincronizar el documento con la nueva configuración (Docker como default, LocalDB como alternativa).

### Archivos a modificar

| Archivo | Cambio |
|---|---|
| `docker-compose.yml` (nuevo) | Servicio SQL Server 2022 |
| `appsettings.json` | Connection string → Docker SQL Server |
| `appsettings.Development.json` (nuevo) | Override para desarrollo |
| `DB_Config.md` | Documentar ambas opciones (Docker default, LocalDB fallback) |
| `README.md` | Actualizar instrucciones de ejecución |
| Migraciones EF Core | Eliminar y recrear (cambian los providers de SQL) |

### Verificación

1. `docker compose up -d` → SQL Server arranca en puerto 1433
2. `dotnet ef database update` → Migraciones aplican correctamente
3. `dotnet run --project Dsw2025Tpi.Api` → Backend conecta a Docker SQL Server
4. Swagger funciona, CRUD de productos/órdenes opera correctamente
5. Seed data se carga sin errores

---

## Guía de Setup Rápido (Cómo levantar el proyecto)

### Problemas conocidos y cómo resolverlos

Antes de ejecutar, hay **3 problemas de configuración** que deben resolverse:

#### 1. Versión de .NET

Los proyectos están configurados para **.NET 8** (`TargetFramework: net8.0`), pero si solo tenés .NET 10 instalado, no vas a poder ejecutar el backend. Tenés 2 opciones:

| Opción | Acción | Esfuerzo |
|---|---|---|
| **A (recomendada)** | Instalar .NET 8 SDK desde https://dotnet.microsoft.com/download/dotnet/8.0 | Bajo |
| B | Actualizar todos los `.csproj` de `net8.0` a `net10.0` y los paquetes NuGet a versiones 10.x | Bajo pero requiere recrear migraciones |

Si elegís la opción B, hay que actualizar estos archivos:
- `Dsw2025Tpi.Api/Dsw2025Tpi.Api.csproj` — `TargetFramework` + paquetes NuGet
- `Dsw2025Tpi.Application/Dsw2025Tpi.Application.csproj` — `TargetFramework`
- `Dsw2025Tpi.Data/Dsw2025Tpi.Data.csproj` — `TargetFramework` + paquetes NuGet
- `Dsw2025Tpi.Domain/Dsw2025Tpi.Domain.csproj` — `TargetFramework`
- Luego borrar migraciones existentes y recrear: `dotnet ef migrations add InitialCreate --project Dsw2025Tpi.Data --startup-project Dsw2025Tpi.Api`

#### 2. Base de datos (Docker SQL Server)

Actualmente el proyecto usa **LocalDB** (`(localdb)\MSSQLLocalDB`) que tiene limitaciones importantes. Para migrar a Docker SQL Server:

1. Crear `docker-compose.yml` en `ICS_TPI2026_backend/` con SQL Server 2022 (ver Fase 0 abajo)
2. Ejecutar `docker compose up -d`
3. Actualizar el connection string en `Dsw2025Tpi.Api/appsettings.json`:
   ```json
   "ConnectionStrings": {
     "Dsw2025Tpi": "Data Source=localhost,1433;Initial Catalog=Dsw2025Tpi;User Id=sa;Password=Dsw2025Tpi_Sa123!;TrustServerCertificate=True;"
   }
   ```
4. Eliminar migraciones existentes y recrear:
   ```bash
   dotnet ef migrations remove --project Dsw2025Tpi.Data --startup-project Dsw2025Tpi.Api
   # Repetir hasta que no queden migraciones
   dotnet ef migrations add InitialCreate --project Dsw2025Tpi.Data --startup-project Dsw2025Tpi.Api
   dotnet ef database update --project Dsw2025Tpi.Data --startup-project Dsw2025Tpi.Api
   ```

#### 3. Puertos desalineados (Frontend ↔ Backend)

El frontend (`.env.development`) apunta a `https://localhost:7138` pero el backend arranca con el perfil `http` en `http://localhost:5142`. Hay que alinear:

**Opción A (simple):** Cambiar `.env.development` en el frontend:
```
VITE_BACKEND_URL=http://localhost:5142/
```

**Opción B:** Instalar certificado HTTPS de dev y arrancar con el perfil HTTPS:
```bash
dotnet dev-certs https --trust
dotnet run --project Dsw2025Tpi.Api --launch-profile https
```

### Pasos para levantar el proyecto

#### Prerrequisitos

- .NET 8 SDK (o .NET 10 con upgrade)
- Docker Desktop
- Node.js 18+

#### Backend

```bash
cd ICS_TPI2026_backend
docker compose up -d          # Levantar SQL Server
# (Opcional) Aplicar migraciones si es la primera vez:
# dotnet ef database update --project Dsw2025Tpi.Data --startup-project Dsw2025Tpi.Api
dotnet run --project Dsw2025Tpi.Api
```

El backend arranca en `http://localhost:5142` (perfil http) o `https://localhost:7138` (perfil https). Swagger en `/swagger`.

#### Frontend

```bash
cd ICS_TPI2026_frontend
# Asegurarse de que .env.development tenga la URL correcta del backend
npm install
npm run dev
```

El frontend arranca en `http://localhost:5173`. Proxy de Vite redirige `/api` al backend.

---

## Backlog — Deuda Técnica y Mejoras (Historias de Usuario)

### Épica 1: UX/UI del Frontend

| ID | Historia de Usuario | Prioridad | Esfuerzo | Estado |
|---|---|---|---|---|
| **US-01** | Como visitante, quiero ver una **página Home** con banner de bienvenida, productos destacados y un CTA para registrarme, para que la plataforma no sea solo un listado de productos. | Alta | Media | Pendiente |
| **US-02** | Como visitante, quiero ver una **página "Productos"** dedicada con filtros, búsqueda y categorías, para encontrar lo que busco fácilmente. | Alta | Media | Pendiente |
| **US-03** | Como visitante, quiero ver una **página "Sobre Nosotros"** con información de la empresa, equipo y contacto, para generar confianza. | Media | Baja | Pendiente |
| **US-04** | Como visitante, quiero ver un **footer** con links útiles (contacto, redes sociales, políticas, FAQ), para navegar la plataforma. | Media | Baja | Pendiente |
| **US-05** | Como usuario no logueado, **no quiero ver el botón del carrito** en el header, para que la UI sea coherente (solo usuarios logueados pueden comprar). | Alta | Baja | Pendiente |
| **US-06** | Como usuario, quiero un **header que cambie según mi estado** (logueado/no logueado): si no estoy logueado mostrar "Login / Register", si estoy logueado mostrar "Mi cuenta / Carrito / Logout". | Alta | Media | Pendiente |
| **US-07** | Como usuario, quiero ver **toast notifications** (éxito/error) al realizar acciones (agregar al carrito, crear orden, login), para saber si la operación fue exitosa. | Media | Baja | Pendiente |
| **US-08** | Como usuario, quiero ver **skeletons/loaders** mientras se cargan los productos, para que la experiencia sea fluida. | Media | Baja | Pendiente |
| **US-09** | Como usuario, quiero un **diseño responsivo completo** (mobile-first) en todas las páginas, no solo en el dashboard admin. | Alta | Media | Pendiente |
| **US-10** | Como visitante, quiero ver una **página 404** personalizada cuando navegue a una ruta inexistente. | Baja | Baja | Pendiente |

### Épica 2: Funcionalidad Backend

| ID | Historia de Usuario | Prioridad | Esfuerzo | Estado |
|---|---|---|---|---|
| **US-11** | Como admin, quiero un **dashboard con datos reales** (total de productos, órdenes, clientes, ingresos), para tomar decisiones informadas. | Alta | Media | Pendiente |
| **US-12** | Como cliente, quiero ver **solo mis órdenes** (`GET /api/orders/mine`), para no depender del admin. | Alta | Media | Pendiente |
| **US-13** | Como admin, quiero poder **gestionar categorías** (CRUD), para agrupar y filtrar productos. | Media | Media | Pendiente |
| **US-14** | Como admin, quiero poder **gestionar clientes** (CRUD), para administrar la base de usuarios. | Media | Media | Pendiente |
| **US-15** | Como admin, quiero **editar órdenes** (cambiar dirección, notas), para corregir errores. | Media | Baja | Pendiente |
| **US-16** | Como cliente, quiero **recuperar contraseña** por email, para no quedar bloqueado. | Alta | Media | Pendiente |

### Épica 3: Seguridad

| ID | Historia de Usuario | Prioridad | Esfuerzo | Estado |
|---|---|---|---|---|
| **US-17** | Como admin, quiero que el **JWT secret no esté en el código fuente**, para que la plataforma sea segura en producción. | Crítica | Baja | Pendiente |
| **US-18** | Como admin, quiero que las **credenciales admin estén en variables de entorno**, para que no se expongan en el repo. | Crítica | Baja | Pendiente |
| **US-19** | Como admin, quiero **rate limiting en login/register**, para prevenir ataques de fuerza bruta. | Alta | Media | Pendiente |
| **US-20** | Como admin, quiero una **password policy más fuerte** (mayúsculas, números, caracteres especiales), para mejorar la seguridad. | Alta | Baja | Pendiente |
| **US-21** | Como admin, quiero que el token se almacene en **HttpOnly cookie** en vez de localStorage, para prevenir ataques XSS. Requiere cambios en frontend (leer/escribir cookies) y backend (enviar cookie en login, leer de cookie en endpoints). | Alta | Alta | Pendiente |
| **US-22** | Como admin, quiero **refresh tokens**, para que los usuarios no tengan que re-loguearse cada 60 min. | Media | Alta | Pendiente |
| **US-35** | Como admin, quiero que el **backend soporte autenticación por cookies** (enviar Set-Cookie en login, leer cookie en endpoints protegidos, CORS con AllowCredentials), para que la migración de localStorage a cookies funcione. | Alta | Media | Pendiente |
| **US-36** | Como admin, quiero **medidas de seguridad avanzadas en cookies** (HttpOnly + Secure + SameSite, refresh token en cookie separada, revocación de tokens), para que las cookies no den falsa sensación de seguridad. | Media | Alta | Pendiente |

### Épica 4: Calidad de Código

| ID | Historia de Usuario | Prioridad | Esfuerzo | Estado |
|---|---|---|---|---|
| **US-23** | Como developer, quiero un **global exception handling middleware**, para que los errores 500 no se filen al frontend. | Alta | Media | Pendiente |
| **US-24** | Como developer, quiero un **Unit of Work** que atomicice las operaciones multi-tabla (ej: crear orden + descontar stock), para evitar inconsistencias. | Alta | Media | Pendiente |
| **US-25** | Como developer, quiero corregir la **dependencia circular** Application→Data, para que la arquitectura Clean Architecture sea correcta. | Media | Media | Pendiente |
| **US-26** | Como developer, quiero **corregir los bugs** en el DbContext (BillingAddress.HasPrecision, Order.Date.HasMaxLength) y los GUIDs duplicados en seed data. | Alta | Baja | Pendiente |
| **US-27** | Como developer, quiero **corregir el typo** `TotatAmount` → `TotalAmount` en DTOs y frontend. | Media | Baja | Pendiente |
| **US-28** | Como developer, quiero **eliminar BaseController.cs** (código muerto) y los imports no usados. | Baja | Baja | Pendiente |
| **US-29** | Como developer, quiero **TypeScript en el frontend**, para tipado estático y mejor mantenibilidad. | Media | Muy Alta | Pendiente |

### Épica 5: Infraestructura

| ID | Historia de Usuario | Prioridad | Esfuerzo | Estado |
|---|---|---|---|---|
| **US-30** | Como admin, quiero **migrar de LocalDB a Docker SQL Server**, para tener un entorno estable y portable. | Alta | Media | Pendiente |
| **US-31** | Como admin, quiero un **health check que verifique SQL Server**, para detectar problemas de conectividad. | Media | Baja | Pendiente |
| **US-32** | Como admin, quiero **Serilog** (logging estructurado), para poder diagnosticar problemas en producción. | Media | Media | Pendiente |
| **US-33** | Como admin, quiero **API versioning** (`/api/v1/products`), para poder evolucionar la API sin romper clientes existentes. | Baja | Media | Pendiente |
| **US-34** | Como admin, quiero un **Dockerfile** para el backend y el frontend, para desplegar en cualquier entorno. | Media | Media | Pendiente |

---

## Orden de Ejecución Recomendado

### Fase 0 — Setup y Base de datos (30 min)
1. Resolver versión de .NET (instalar .NET 8 o upgrade a .NET 10)
2. Crear `docker-compose.yml` con SQL Server 2022
3. Actualizar connection string en `appsettings.json`
4. Eliminar y recrear migraciones EF Core
5. Alinear puertos (`.env.development` ↔ backend)
6. Verificar que el backend arranca y la API responde

### Fase 1 — Seguridad inmediata (1-2 horas)
7. Mover JWT secret y admin credentials a User Secrets / Variables de Entorno
8. Configurar `ExpireInMinutes` en appsettings.json
9. Fortalecer password policy
10. Agregar unique index en `Product.Sku`

### Fase 2 — Calidad de código (2-3 horas)
11. Corregir bugs en DbContext (`BillingAddress`, `Order.Date`)
12. Corregir GUIDs duplicados en seed data
13. Corregir typo `TotatAmount`
14. Eliminar `BaseController.cs`
15. Agregar global exception handling middleware

### Fase 3 — Arquitectura (3-4 horas)
16. Implementar Unit of Work
17. Corregir dependencia circular Application→Data
18. Agregar rate limiting

### Fase 4 — Seguridad cookies (3-4 horas)
19. Backend: configurar Set-Cookie en login endpoint
20. Backend: crear middleware para leer JWT de cookie
21. Backend: actualizar CORS para AllowCredentials
22. Frontend: migrar de localStorage a HttpOnly cookies
23. Frontend: actualizar Axios para enviar cookies
24. Implementar refresh token en cookie separada
25. Agregar medidas de seguridad (Secure, SameSite)

### Fase 5 — Funcionalidad Backend (4-6 horas)
26. Crear entidad Category
27. Dashboard con datos reales
28. Endpoint `orders/mine` para clientes
29. Health check con SQL Server

### Fase 6 — Frontend UX/UI (6-8 horas)
30. Crear página Home con productos destacados
31. Crear página "Productos" dedicada
32. Crear página "Sobre Nosotros"
33. Crear componente Footer
34. Ocultar botón carrito si no hay usuario logueado
35. Header dinámico según estado de autenticación
36. Toast notifications y skeleton loaders
37. Diseño responsivo completo (mobile-first)
38. Página 404 personalizada

---

## Coordinación con Tablero Trello (Scrum Manual)

### Estructura del Tablero

El tablero Trello se organiza con las siguientes listas:

```
| Backlog | Sprint 1 | En Progreso | Revisión | Hecho |
|---------|----------|-------------|----------|-------|
```

### Épicas en el Tablero

Cada épica se representa con una **etiqueta de color** en Trello:

| Etiqueta | Épica | Color sugerido |
|----------|-------|----------------|
| Seguridad | Épica 3 | Rojo |
| Arquitectura | Épica 4 | Naranja |
| Backend | Épica 2 | Azul |
| Frontend | Épica 1 | Verde |
| Infraestructura | Épica 5 | Morado |

### Historias de Usuario → Tarjetas Trello

Cada historia de usuario (US-XX) del backlog se convierte en una tarjeta en Trello con:

**Formato de tarjeta:**
```
[Título de la historia]

Descripción:
- Criterios de aceptación
- Archivos a modificar
- Dependencias con otras historias

Checklist:
- [ ] Paso 1
- [ ] Paso 2
- [ ] ...
```

### Historias de Usuario para el Tablero

#### Épica 3: Seguridad (prioridad para Sprint 1)

| Tarjeta Trello | US | Descripción |
|----------------|-----|-------------|
| Mover JWT secret a env vars | US-17 | User Secrets en dev, variables de entorno en prod |
| Mover credenciales admin a env vars | US-18 | User Secrets en dev, variables de entorno en prod |
| Rate limiting en auth | US-19 | Microsoft.AspNetCore.RateLimiting |
| Password policy fuerte | US-20 | RequireDigit, RequireUppercase, etc. |
| Backend: soporte cookies | US-25 | Set-Cookie en login, middleware de lectura, CORS |
| Frontend: migrar a cookies | US-21 | LocalStorage → HttpOnly cookies |
| Seguridad avanzada cookies | US-26 | Secure, SameSite, refresh token, revocación |

#### Épica 4: Calidad de Código

| Tarjeta Trello | US | Descripción |
|----------------|-----|-------------|
| Global exception middleware | US-23 | ProblemDetails consistente |
| Unit of Work | US-24 | Atomicidad en operaciones multi-tabla |
| Dependencia circular | US-25 | Application solo refiere a Domain |
| Bugs DbContext | US-26 | BillingAddress, Order.Date, GUIDs duplicados |
| Typo TotatAmount | US-27 | Renombrar en DTOs y frontend |
| Eliminar código muerto | US-28 | BaseController.cs |

### Flujo de Trabajo en Trello

1. **Backlog:** Todas las historias están aquí inicialmente
2. **Sprint 1:** Se mueven las historias priorizadas para el sprint actual
3. **En Progreso:** Cuando alguien empieza a trabajar en una historia
4. **Revisión:** Cuando el código está listo para review
5. **Hecho:** Cuando está mergeado y verificado

### Reglas de Coordinación

- **Una historia a la vez** por persona (evitar WIP limit)
- **Dependencias claras:** Si una historia depende de otra, marcarla en la tarjeta
- **Definition of Done:**
  - [ ] Código implementado
  - [ ] Tests pasan (si existen)
  - [ ] No rompe funcionalidad existente
  - [ ] Documentación actualizada (si aplica)
  - [ ] Tarjeta movida a "Hecho"

### Sugerencia de Sprint 1 (2 semanas)

| Prioridad | Historias | Esfuerzo estimado |
|-----------|-----------|-------------------|
| Crítica | US-17, US-18 | 1 hora |
| Alta | US-19, US-20, US-25 | 4 horas |
| Media | US-26, US-27, US-28 | 3 horas |
| **Total Sprint 1** | | **~8 horas** |

### Sugerencia de Sprint 2 (2 semanas)

| Prioridad | Historias | Esfuerzo estimado |
|-----------|-----------|-------------------|
| Alta | US-21 (cookies frontend), US-25 (cookies backend) | 6 horas |
| Media | US-26 (seguridad cookies), US-23 (exception middleware) | 5 horas |
| **Total Sprint 2** | | **~11 horas** |

---

## Resumen de Archivos del Proyecto

```
ICS/
├── docs/
│   ├── PLAN.md                               ← Este archivo
│   ├── TP1_Resuelto.md                       ← 9 deudas técnicas destacadas
│   ├── TP1_Hallazgos_Adicionales.md          ← Bugs + deuda técnica (20 hallazgos)
│   ├── DB_Config.md                          ← Configuración de base de datos
│   └── ICS2026_TP1.pdf                       ← Enunciado del TP1
│
├── ICS_TPI2026_backend/
│   ├── Dsw2025Tpi.sln
│   ├── Dsw2025Tpi.Api/
│   │   ├── Program.cs                    ← Configuración, DI, startup
│   │   ├── appsettings.json              ← JWT key, connection string, admin creds
│   │   ├── Controllers/
│   │   │   ├── BaseController.cs         ← ⚠️ Código muerto
│   │   │   ├── AuthenticateController.cs
│   │   │   ├── ProductsController.cs
│   │   │   └── OrderController.cs
│   │   └── DependencyInjection/
│   │       └── ServiceCollectionExtensions.cs
│   ├── Dsw2025Tpi.Application/
│   │   ├── Dtos/                         ← 6 archivos DTO
│   │   ├── Services/
│   │   │   ├── JwtTokenService.cs
│   │   │   ├── ProducstManagementServices.cs  ← ⚠️ Typo en nombre
│   │   │   └── OrdersManagementServices.cs
│   │   ├── Validation/                   ← 4 validadores estáticos
│   │   └── Exceptions/                   ← 3 excepciones custom
│   ├── Dsw2025Tpi.Domain/
│   │   ├── Entities/
│   │   │   ├── EntityBase.cs
│   │   │   ├── Customer.cs
│   │   │   ├── Product.cs
│   │   │   ├── Order.cs
│   │   │   ├── OrderItem.cs
│   │   │   └── OrderStatus.cs
│   │   └── Interfaces/
│   │       └── IRepository.cs
│   └── Dsw2025Tpi.Data/
│       ├── Dsw2025TpiContext.cs
│       ├── AuthenticateContext.cs
│       ├── Repositories/
│       │   └── EfRepository.cs
│       ├── Helpers/
│       │   └── DbContextExtensions.cs
│       ├── Sources/                      ← Seed data JSON
│       └── Migrations/
│
├── ICS_TPI2026_frontend/
│   └── src/
│       ├── main.jsx
│       ├── App.jsx
│       └── modules/
│           ├── auth/                     ← Login, Register, ProtectedRoute
│           ├── products/                 ← CRUD admin + catálogo público
│           ├── orders/                   ← List admin + create
│           ├── cart/                     ← Carrito + checkout
│           ├── home/                     ← Dashboard admin
│           ├── shared/                   ← 9 componentes + hooks + API
│           └── templates/                ← Dashboard layout
│
└── README.md
```
