# ICS — Trabajo Practico Integrador: Plataforma E-commerce

**Materia:** Ingenieria y Calidad del Software
**Proyecto:** E-commerce ICS
**Stack:** React 19 + .NET 8 (C#) + SQL Server
**Estado:** En desarrollo

**Tablero Trello:** [TPI Ingenieria y Calidad de Software](https://trello.com/b/tpi-ingenieria-calidad-software)

---

## Vision General

Plataforma de comercio electronico desarrollada como Trabajo Practico Integrador. El sistema permite gestionar productos, administrar ordenes de compra, soporta autenticacion con roles (Administrador / Cliente) e incluye un catalogo publico con carrito de compras.

---

## Arquitectura

### C4 Model — Nivel 1: Contexto

```mermaid
C4Context
    title Diagrama de Contexto - E-commerce ICS

    Person(cliente, "Cliente", "Navega catalogo, crea ordenes, paga")
    Person(admin, "Administrador", "Gestiona productos, ordenes y usuarios")

    System(ecommerce, "E-commerce ICS", "Plataforma de comercio electronico con JWT y roles")

    System_Ext(sqlserver, "Azure SQL Database", "Base de datos relacional en la nube")
    System_Ext(VERCEL, "Vercel", "Despliegue del frontend React")
    System_Ext(azure, "Azure App Service", "Despliegue del backend .NET (contenedor Docker)")

    Rel(cliente, ecommerce, "Navega catalogo, crea ordenes", "HTTPS")
    Rel(admin, ecommerce, "Gestiona productos y ordenes", "HTTPS")
    Rel(ecommerce, sqlserver, "Lee/escribe datos", "EF Core / TCP 1433")
    Rel(ecommerce, azure, "Backend API desplegado", "HTTPS")
```

### C4 Model — Nivel 2: Contenedores

```mermaid
C4Container
    title Diagrama de Contenedores - E-commerce ICS

    Person(cliente, "Cliente", "Visitante del sitio")
    Person(admin, "Administrador", "Gestiona el e-commerce")

    Container_Boundary(frontend, "Frontend - React 19 + Vite + Tailwind") {
        Container(spa, "SPA React 19", "React + Vite + Tailwind", "Catalogo publico, carrito, checkout, panel admin")
    }

    Container_Boundary(backend, "Backend - .NET 8 Clean Architecture") {
        Container(api, "API Controllers", "ASP.NET Core Web API", "Auth, Products, Orders")
        Container(app, "Application Layer", "Services / DTOs / Validators / Exceptions", "Logica de negocio")
        Container(domain, "Domain Layer", "Entities + Interfaces", "Modelo de dominio")
        Container(data, "Data Layer", "EF Core + Repositories", "Persistencia")
    }

    System_Ext(sqlserver, "Azure SQL Database", "Base de datos relacional")

    Rel(cliente, spa, "Usa", "HTTPS")
    Rel(admin, spa, "Usa", "HTTPS")
    Rel(spa, api, "Consume API", "JSON/HTTPS via Axios + JWT Bearer")
    Rel(api, app, "Delega logica")
    Rel(app, domain, "Usa entidades e interfaces")
    Rel(app, data, "Usa repositorio")
    Rel(data, sqlserver, "Consulta/persiste", "EF Core / TCP 1433")

    UpdateLayoutConfig($c4ShapeInRow="3", $c4BoundaryInRow="1")
```

### C4 Model — Nivel 3: Componentes (Backend)

```mermaid
graph TB
    subgraph "API Layer (Dsw2025Tpi.Api)"
        AC[AuthenticateController]
        PC[ProductsController]
        OC[OrderController]
        DI[DependencyInjection]
        Program[Program.cs]
    end

    subgraph "Application Layer (Dsw2025Tpi.Application)"
        PMS[ProductsManagementService]
        OMS[OrdersManagementService]
        JTS[JwtTokenService]
        DTO[DTOs: Customer, Product, Order, Login, Register]
        VAL[Validators: Customer, Product, Order, OrderItem]
        EXC[Exceptions: Application, Duplicated, NotFound]
    end

    subgraph "Domain Layer (Dsw2025Tpi.Domain)"
        ENT[Entities: Customer, Product, Order, OrderItem]
        IREP[IRepository<T>]
    end

    subgraph "Data Layer (Dsw2025Tpi.Data)"
        CTX[Dsw2025TpiContext]
        ACTX[AuthenticateContext]
        EREP[EfRepository<T>]
        SEED[Seed Data: JSON]
        MIG[Migrations]
    end

    AC --> PMS
    AC --> JTS
    PC --> PMS
    OC --> OMS
    PMS --> IREP
    OMS --> IREP
    JTS --> ENT
    EREP --> CTX
    EREP --> ACTX
    CTX --> ENT
    DI --> EREP
    DI --> PMS
    DI --> OMS
```

---

## Modelo de Dominio

### Entidades

```mermaid
classDiagram
    class EntityBase {
        +Guid Id
    }

    class Customer {
        +string Name
        +string Email
        +string? PhoneNumber
    }

    class Product {
        +string Sku
        +string InternalCode
        +string Name
        +string? Description
        +decimal CurrentUnitPrice
        +int StockQuantity
        +bool IsActive
    }

    class Order {
        +DateTime Date
        +string? ShippingAddress
        +string? BillingAddress
        +string? Notes
        +decimal TotalAmount
        +OrderStatus Status
        +Guid CustomerId
    }

    class OrderItem {
        +int Quantity
        +decimal UnitPrice
        +decimal Subtotal
        +Guid OrderId
        +Guid ProductId
    }

    class OrderStatus {
        <<enum>>
        PENDING = 1
        PROCESSING = 2
        SHIPPED = 3
        DELIVERED = 4
        CANCELLED = 5
    }

    EntityBase <|-- Customer
    EntityBase <|-- Product
    EntityBase <|-- Order
    EntityBase <|-- OrderItem
    Customer "1" --> "*" Order : places
    Order "1" --> "*" OrderItem : contains
    OrderItem "*" --> "1" Product : references
    Order --> OrderStatus : has
```

### Diagrama ER

```mermaid
erDiagram
    Customer ||--o{ Order : places
    Order ||--|{ OrderItem : contains
    OrderItem }o--|| Product : references

    Customer {
        Guid Id PK
        string Name
        string Email
        string PhoneNumber
    }

    Product {
        Guid Id PK
        string Sku UK
        string InternalCode
        string Name
        string Description
        decimal CurrentUnitPrice
        int StockQuantity
        bool IsActive
    }

    Order {
        Guid Id PK
        DateTime Date
        string ShippingAddress
        string BillingAddress
        string Notes
        decimal TotalAmount
        int Status
        Guid CustomerId FK
    }

    OrderItem {
        Guid Id PK
        int Quantity
        decimal UnitPrice
        decimal Subtotal
        Guid OrderId FK
        Guid ProductId FK
    }
```

---

## Stack Tecnologico

### Frontend

| Tecnologia | Version | Uso |
|---|---|---|
| React | 19.1.1 | Framework UI |
| Vite | 7.1.7 | Bundler y dev server |
| React Router DOM | 7.9.4 | Enrutamiento |
| Axios | 1.13.2 | Cliente HTTP |
| React Hook Form | 7.65.0 | Formularios |
| Tailwind CSS | 4.1.14 | Estilos utility-first |
| jwt-decode | 4.0.0 | Decodificacion de JWT |

**Despliegue:** Vercel — https://ics-tpi-2026.vercel.app (frontend SPA estatico)

### Backend

| Tecnologia | Version | Uso |
|---|---|---|
| .NET | 8.0 | Plataforma |
| C# | 12.0 | Lenguaje |
| Entity Framework Core | 9.0.6 | ORM (SQL Server) |
| ASP.NET Identity | -- | Autenticacion y usuarios |
| JWT Bearer | -- | Autenticacion JWT |
| Swashbuckle | 6.6.2 | Swagger/OpenAPI |

**Despliegue:** Azure App Service (contenedor Docker, plan Free F1) — https://ics-tp3-43739-eveqhxemd5cmdee7.brazilsouth-01.azurewebsites.net

### Base de Datos

| Tecnologia | Uso |
|---|---|
| SQL Server | Base de datos relacional |
| Azure SQL Database | Base de datos en la nube (plan gratuito) |

**Conexion:** EF Core via TCP 1433

---

## Estructura del Proyecto

```
ICS/
├── ICS_TPI2026_frontend/          → Frontend (React 19 + Vite + Tailwind)
│   └── src/
│       ├── modules/
│       │   ├── auth/              → Login, Register, ProtectedRoute
│       │   ├── products/          → CRUD admin + catalogo publico
│       │   ├── orders/            → List admin + create
│       │   ├── cart/              → Carrito + checkout
│       │   ├── home/              → Dashboard admin
│       │   ├── shared/            → 9 componentes + hooks + API
│       │   └── templates/         → Dashboard layout
│       └── ...
│
├── ICS_TPI2026_backend/           → Backend (.NET 8 Clean Architecture)
│   ├── Dsw2025Tpi.Api/            → Presentacion (Controllers, DI)
│   ├── Dsw2025Tpi.Application/    → Logica de negocio
│   ├── Dsw2025Tpi.Domain/         → Modelo de dominio
│   └── Dsw2025Tpi.Data/           → Infraestructura (EF Core)
│
├── docs/
│   ├── PLAN.md                    → Plan de implementacion
│   ├── TP1_Resuelto.md            → Auditoria tecnica
│   ├── TP1_Hallazgos_Adicionales.md → Bugs + deuda tecnica
│   ├── DB_Config.md               → Configuracion de BD
│   ├── guia-tp3.md                → Guia de la catedra (TP3)
│   ├── TP3_Plan.md                → Plan de ejecucion del TP3
│   ├── TP3_Documentacion.md       → Documentacion consolidada del TP3
│   └── DESPLIEGUE_AZURE.md        → Guia paso a paso del despliegue en Azure
│
└── README.md                      → Este archivo
```

---

## Clean Architecture

```
Dsw2025Tpi.Api/            → Presentacion (Controllers, Program.cs, DI)
    ↓ referencia a
Dsw2025Tpi.Application/    → Logica de negocio (Services, DTOs, Validators)
    ↓ referencia a
Dsw2025Tpi.Domain/         → Modelo de dominio (Entities, Interfaces)
    ↑ referencia a
Dsw2025Tpi.Data/           → Infraestructura (DbContext, Repositories)
```

**Regla:** La capa Application solo depende de Domain (interfaces). Data implementa las interfaces y se registra via DI en Api.

---

## Endpoints API

### AuthenticateController (`/api/auth`)

| Metodo | Ruta | Auth | Descripcion |
|---|---|---|---|
| POST | `/api/auth/login` | Anonimo | Login, retorna `{ token }` |
| POST | `/api/auth/register` | Anonimo | Registro de usuario |

### ProductsController (`/api/products`)

| Metodo | Ruta | Auth | Descripcion |
|---|---|---|---|
| GET | `/api/products` | Anonimo | Catalogo publico (solo activos), paginado |
| GET | `/api/products/{id}` | Admin,User | Detalle de producto |
| POST | `/api/products` | Admin | Crear producto |
| PUT | `/api/products/{id}` | Admin | Actualizar producto |
| PATCH | `/api/products/{id}` | Admin | Soft-delete (desactivar) |
| GET | `/api/products/admin` | Admin | Listado admin con filtros, paginado |

### OrderController (`/api/orders`)

| Metodo | Ruta | Auth | Descripcion |
|---|---|---|---|
| GET | `/api/orders` | Admin | Listar todas las ordenes, paginado |
| POST | `/api/orders` | Admin,User | Crear orden (verifica y descuenta stock) |
| GET | `/api/orders/{id}` | Admin | Detalle de orden con items |
| PUT | `/api/orders/{id}` | Admin | Cambiar estado de orden |

---

## Rutas Frontend

| Ruta | Componente | Protegida | Rol | Descripcion |
|---|---|---|---|---|
| `/` | ListProductsUserPage | No | -- | Catalogo publico |
| `/cart` | CartPage | No | -- | Carrito + checkout |
| `/login` | LoginPage | No | -- | Login |
| `/register` | RegisterPage | No | -- | Registro |
| `/admin` | Dashboard (layout) | Si | Admin | Shell panel admin |
| `/admin/home` | Home | Si | Admin | Dashboard metricas |
| `/admin/products` | ListProductsPage | Si | Admin | Listado productos |
| `/admin/products/create` | CreateProductPage | Si | Admin | Crear producto |
| `/admin/orders` | ListOrdersPage | Si | Admin | Listado ordenes |

---

## Como Ejecutar

### Prerrequisitos

- .NET 8 SDK
- Docker Desktop
- Node.js 18+

### 1. Base de datos (Docker SQL Server)

```bash
cd ICS_TPI2026_backend
docker compose up -d
```

### 2. Backend

```bash
cd ICS_TPI2026_backend
dotnet run --project Dsw2025Tpi.Api
```

Backend en `http://localhost:5142` (o en `http://localhost:8080` si se levanta con `docker compose up -d`). Swagger en `/swagger` (solo en entorno `Development`).

### 3. Frontend

```bash
cd ICS_TPI2026_frontend
npm install
npm run dev
```

Frontend en `http://localhost:5173`. Proxy redirige `/api` al backend.

### Variables de entorno

El frontend (`.env`, no versionado) debe apuntar al puerto del backend:

```
VITE_BACKEND_URL=http://localhost:8080
```

En produccion, `VITE_BACKEND_URL` se configura en el panel de Vercel apuntando a la URL de Azure (ver `docs/TP3_Documentacion.md`, capitulo 8).

---

## Flujo de Trabajo con Git

### Ramas

| Rama | Origen | Destino | Uso |
|---|---|---|---|
| `main` | - | - | Version estable, lista para produccion. Protegida: push directo rechazado. |
| `development` | `main` | `main` | Integracion de las funcionalidades terminadas. Protegida: push directo rechazado. |
| `feature/nombre-funcionalidad` | `development` | `development` | Nueva funcionalidad o tarea. Ej: `feature/tp3-docker`. |
| `hotfix/descripcion` | `main` | `main` y `development` | Correccion urgente de un error en produccion. Ej: `hotfix/jwt-expiracion`. |

Los nombres van en minusculas y con guiones. Las ramas de trabajo siempre siguen una de las dos convenciones: `feature/nombre-funcionalidad` o `hotfix/descripcion`.

### Flujo de Pull Request

1. Crear la rama desde la base correcta (`development` para `feature/*`, `main` para `hotfix/*`).
2. Commitear con [Conventional Commits](https://www.conventionalcommits.org/) (`feat:`, `fix:`, `docs:`, `ci:`...).
3. Subir la rama y abrir un Pull Request hacia la rama destino.
4. El pipeline de CI corre automaticamente sobre el PR.
5. Un integrante del equipo distinto del autor revisa el PR y lo aprueba. Sin al menos 1 aprobacion, GitHub no permite fusionar.
6. Se fusiona el PR desde GitHub y se elimina la rama.
7. En un `hotfix/*`, repetir el PR hacia la otra rama (`main` y `development`) para que no diverjan.

---

## Despliegue

| Componente | Plataforma | Configuracion |
|---|---|---|
| Frontend | Vercel — https://ics-tpi-2026.vercel.app | Build: `npm run build`, salida `dist/`; env `VITE_BACKEND_URL` en el dashboard |
| Backend | Azure App Service (Free F1, contenedor) — https://ics-tp3-43739-eveqhxemd5cmdee7.brazilsouth-01.azurewebsites.net | Imagen `fntkey/ics-api` (Docker Hub); deploy via `.github/workflows/deploy.yml` |
| Base de datos | Azure SQL Database (Free) | Servidor `ics-sql-43739`, base `ics-db`; firewall "Allow Azure services" |
| CI/CD | GitHub Actions | `continuous-integration.yml` (CI) + `deploy.yml` (CD) |

### Credenciales de acceso

| Entorno | Usuario admin | Contraseña | Base de datos |
|---|---|---|---|
| Local | `admin` | `SecurePassword123!` (en `appsettings.json`) | `sa` / `YourStrong!Password123` (en `docker-compose.yml`) |
| Produccion (Azure) | `admin` | `AdminTp3$2026!Xyz` (en Application Settings) | via connection string de la App Service |

Detalles y variables de entorno completas: `docs/TP3_Documentacion.md`, capitulo 8.

---

## Seguridad

| Componente | Estado |
|---|---|
| JWT Bearer | HMAC-SHA256, 60 min expiracion |
| ASP.NET Identity | Tablas custom en espanol |
| Password hashing | PBKDF2 via Identity |
| CORS | Configurado para produccion |
| Roles | "Admin", "User" seeded |
| Autorizacion por roles | `[Authorize(Roles="Admin")]` |

---

## Deuda Tecnica

Ver `docs/TP1_Resuelto.md` para las 9 deudas tecnicas destacadas y `docs/TP1_Hallazgos_Adicionales.md` para el listado completo (29 hallazgos).

---

## Documentacion

| Archivo | Contenido |
|---|---|
| `README.md` | Arquitectura, modelo de dominio, endpoints (este archivo) |
| `docs/PLAN.md` | Plan de implementacion priorizado y roadmap |
| `docs/TP1_Resuelto.md` | Auditoria tecnica con 9 hallazgos destacados |
| `docs/TP1_Hallazgos_Adicionales.md` | 20+ bugs, deuda tecnica y optimizaciones |
| `docs/DB_Config.md` | Configuracion de conexion a la base de datos |
| `docs/TP3_Documentacion.md` | Documentacion consolidada del TP3: ramas, Dockerfile, pipeline, API-BD, despliegues, endpoints y credenciales |
| `docs/TP3_Plan.md` | Plan de ejecucion del TP3 (fases y pendientes) |
| `docs/DESPLIEGUE_AZURE.md` | Guia paso a paso del despliegue en Azure |
| `docs/guia-tp3.md` | Guia de la catedra (TP3) |
