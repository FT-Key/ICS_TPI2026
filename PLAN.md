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

## Orden de Ejecución Recomendado

### Fase 0 — Base de datos (30 min)
1. Crear `docker-compose.yml` con SQL Server 2022
2. Actualizar connection string en `appsettings.json`
3. Eliminar y recrear migraciones EF Core
4. Verificar que el backend arranca y la API responde

### Fase 1 — Seguridad inmediata (1-2 horas)
2. Mover JWT secret y admin credentials a User Secrets / Variables de Entorno
3. Configurar `ExpireInMinutes` en appsettings.json
4. Fortalecer password policy
5. Agregar unique index en `Product.Sku`

### Fase 2 — Calidad de código (2-3 horas)
6. Corregir bugs en DbContext (`BillingAddress`, `Order.Date`)
7. Corregir GUIDs duplicados en seed data
8. Corregir typo `TotatAmount`
9. Eliminar `BaseController.cs`
10. Agregar global exception handling middleware

### Fase 3 — Arquitectura (3-4 horas)
11. Implementar Unit of Work
12. Corregir dependencia circular Application→Data
13. Agregar rate limiting

### Fase 4 — Funcionalidad (4-6 horas)
14. Crear entidad Category
15. Dashboard con datos reales
16. Endpoint `orders/mine` para clientes
17. Health check con SQL Server

---

## Resumen de Archivos del Proyecto

```
ICS/
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
├── README.md
├── PLAN.md                               ← Este archivo
├── TP1_Resuelto.md
└── DB_Config.md
```
