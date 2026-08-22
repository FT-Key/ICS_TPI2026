# ICS — Trabajo Práctico Integrador: Plataforma E-commerce

## Visión General

Plataforma de comercio electrónico desarrollada como Trabajo Práctico Integrador de la materia Ingeniería y Calidad del Software. El sistema permite gestionar productos, administrar órdenes de compra, soporta autenticación con roles (Administrador / Cliente) y incluye un catálogo público con carrito de compras.

---

## Arquitectura del Proyecto

```
ICS/
├── ICS_TPI2026_frontend/    → Frontend (React 19 + Vite + Tailwind)
├── ICS_TPI2026_backend/     → Backend (.NET 8 - C# - Clean Architecture)
├── README.md                → Estado actual del proyecto (este archivo)
├── TP1_Resuelto.md          → Auditoría técnica y plan de refactorización
├── PLAN.md                  → Plan de implementación y roadmap
└── DB_Config.md             → Configuración de conexión a la base de datos
```

---

## Frontend (`ICS_TPI2026_frontend`)

### Tecnologías utilizadas

| Tecnología | Versión | Uso |
|---|---|---|
| React | 19.1.1 | Framework UI |
| Vite | 7.1.7 | Bundler y dev server |
| React Router DOM | 7.9.4 | Enrutamiento (`createBrowserRouter`) |
| Axios | 1.13.2 | Cliente HTTP con interceptores |
| React Hook Form | 7.65.0 | Manejo de formularios |
| Tailwind CSS | 4.1.14 | Estilos utility-first |
| jwt-decode | 4.0.0 | Decodificación de tokens JWT |

### Estructura de carpetas

```
src/
├── main.jsx                              → Punto de entrada
├── App.jsx                               → Configuración de rutas
├── index.css / elements.css              → Estilos globales
│
├── modules/
│   ├── auth/                             → Módulo de autenticación
│   │   ├── components/
│   │   │   ├── LoginForm.jsx             → Formulario de login
│   │   │   ├── LoginModal.jsx            → Modal de login
│   │   │   ├── RegisterForm.jsx          → Formulario de registro
│   │   │   ├── RegisterModal.jsx         → Modal de registro
│   │   │   └── ProtectedRoute.jsx        → Ruta protegida (wrapper)
│   │   ├── context/
│   │   │   └── AuthProvider.jsx          → Context de autenticación
│   │   ├── hooks/
│   │   │   └── useAuth.js                → Hook para contexto de auth
│   │   ├── pages/
│   │   │   ├── LoginPage.jsx             → Página de login
│   │   │   └── RegisterPage.jsx          → Página de registro
│   │   └── services/
│   │       ├── login.js                  → POST /api/auth/login
│   │       └── register.js               → POST /api/auth/register
│   │
│   ├── products/                         → Módulo de productos
│   │   ├── components/
│   │   │   └── CreateProductForm.jsx     → Formulario de creación
│   │   ├── pages/
│   │   │   ├── ListProductsPage.jsx      → Listado admin (todos los estados)
│   │   │   ├── ListProductsUserPage.jsx  → Catálogo público (solo activos)
│   │   │   └── CreateProductPage.jsx     → Página wrapper de creación
│   │   └── services/
│   │       ├── list.js                   → GET /api/products/admin
│   │       ├── listUser.js               → GET /api/products (público)
│   │       └── create.js                 → POST /api/products
│   │
│   ├── orders/                           → Módulo de órdenes
│   │   ├── pages/
│   │   │   └── ListOrdersPage.jsx        → Listado admin con búsqueda y filtros
│   │   └── services/
│   │       ├── listServices.js           → GET /api/orders
│   │       └── createOrder.js            → POST /api/orders
│   │
│   ├── cart/                             → Módulo de carrito
│   │   ├── hooks/
│   │   │   └── useCart.js                → Estado del carrito (localStorage)
│   │   └── pages/
│   │       └── CartPage.jsx              → Carrito + checkout
│   │
│   ├── home/                             → Dashboard admin
│   │   └── pages/
│   │       └── Home.jsx                  → Dashboard con contadores
│   │
│   ├── templates/
│   │   └── components/
│   │       └── Dashboard.jsx             → Layout panel admin (sidebar + header + outlet)
│   │
│   └── shared/                           → Componentes y utilidades reutilizables
│       ├── api/
│       │   └── axiosInstance.js           → Instancia Axios + interceptores
│       ├── components/
│       │   ├── Button.jsx                → Botón con variantes
│       │   ├── Card.jsx                  → Card contenedora
│       │   ├── ErrorBanner.jsx           → Banner de error (string o lista)
│       │   ├── Input.jsx                 → Input con label y errores
│       │   ├── MobileSideMenu.jsx        → Menú lateral mobile
│       │   ├── Modal.jsx                 → Modal con backdrop
│       │   ├── Pagination.jsx            → Paginación prev/next + page size
│       │   ├── SearchBar.jsx             → Barra de búsqueda
│       │   └── UserHeaderMenu.jsx        → Header desktop con auth
│       ├── helpers/
│       │   └── apiHandler.js             → Wrapper {data, error} para API calls
│       └── hooks/
│           ├── useDeleteQuantity.js       → Gestión de cantidades de eliminación
│           ├── usePagination.js           → Lógica de paginación
│           └── useToggleMap.js            → Toggle booleano multi-key
```

### Rutas definidas

| Ruta | Componente | Protegida | Rol requerido | Descripción |
|---|---|---|---|---|
| `/` | ListProductsUserPage | No | — | Catálogo público de productos habilitados |
| `/cart` | CartPage | No | — | Carrito de compras + checkout |
| `/login` | LoginPage | No | — | Formulario de autenticación |
| `/register` | RegisterPage | No | — | Formulario de registro |
| `/admin` | Dashboard (layout) | Sí | Admin | Shell del panel admin |
| `/admin/home` | Home | Sí | Admin | Dashboard principal con métricas |
| `/admin/products` | ListProductsPage | Sí | Admin | Listado de productos (todos los estados) |
| `/admin/products/create` | CreateProductPage | Sí | Admin | Formulario de creación de producto |
| `/admin/orders` | ListOrdersPage | Sí | Admin | Listado de órdenes con búsqueda y filtros |

### Funcionalidades implementadas

- **Autenticación JWT**: Login/logout con token en `localStorage`, interceptor que agrega `Authorization: Bearer` automáticamente y redirige en 401.
- **Registro de usuarios**: Formulario con react-hook-form, crea usuario + customer en el backend.
- **Rutas protegidas**: Componente `ProtectedRoute` que verifica token y rol del usuario.
- **Catálogo público**: Productos habilitados visibles para cualquier visitante.
- **Carrito de compras**: Agregar/quitar productos, persistencia en `localStorage`, cálculo de totales.
- **Checkout**: Crear orden desde el carrito con dirección de envío, facturación y notas.
- **Dashboard responsivo**: Layout con sidebar colapsable en mobile, navegación con `NavLink`.
- **Listado de productos (admin)**: Paginación, búsqueda por texto, filtro por estado (Todos/Habilitados/Inhabilitados).
- **Creación de productos**: Formulario con validación: SKU, código interno, nombre, descripción, precio, stock.
- **Listado de órdenes (admin)**: Paginación, búsqueda por nombre de cliente, filtro por estado.
- **Componentes reutilizables**: Button, Card, Input, Modal, Pagination, SearchBar, ErrorBanner, etc.
- **Manejo de errores del backend**: Mapeo de errores del backend a mensajes amigables en español (`apiHandler.js`).

---

## Backend (`ICS_TPI2026_backend`)

### Tecnologías utilizadas

| Tecnología | Versión | Uso |
|---|---|---|
| .NET | 8.0 | Plataforma |
| C# | 12.0 | Lenguaje |
| Entity Framework Core | 9.0.6 | ORM (SQL Server) |
| ASP.NET Core Identity | — | Autenticación y gestión de usuarios |
| Microsoft.AspNetCore.Authentication.JwtBearer | — | Autenticación JWT |
| Swashbuckle | 6.6.2 | Documentación Swagger/OpenAPI |

### Arquitectura: Clean Architecture (4 capas)

```
ICS_TPI2026_backend/
├── Dsw2025Tpi.sln                            → Solución
│
├── Dsw2025Tpi.Api/                           → CAPA DE PRESENTACIÓN
│   ├── Program.cs                            → Configuración, DI, startup, seed
│   ├── appsettings.json                      → JWT, connection string, roles
│   ├── Controllers/
│   │   ├── AuthenticateController.cs         → POST login, POST register
│   │   ├── ProductsController.cs             → CRUD productos
│   │   └── OrderController.cs               → CRUD órdenes
│   └── DependencyInjection/
│       └── ServiceCollectionExtensions.cs    → AddDomainServices()
│
├── Dsw2025Tpi.Application/                   → CAPA DE LÓGICA DE NEGOCIO
│   ├── Dtos/
│   │   ├── CustomerModel.cs                  → Request/Response
│   │   ├── ProductModel.cs                   → Request/Response/Filter/Pagination
│   │   ├── OrderModel.cs                     → Request/Response/Search/Pagination
│   │   ├── OrderItemModel.cs                 → Request/Response
│   │   ├── LoginModel.cs                     → Login record
│   │   └── RegisterModel.cs                  → Register record
│   ├── Services/
│   │   ├── JwtTokenService.cs                → Generación de JWT
│   │   ├── ProducstManagementServices.cs     → Lógica de productos
│   │   └── OrdersManagementServices.cs       → Lógica de órdenes
│   ├── Validation/
│   │   ├── CustomerValidator.cs
│   │   ├── ProductValidator.cs
│   │   ├── OrderValidator.cs
│   │   └── OrderItemValidator.cs
│   └── Exceptions/
│       ├── ApplicationException.cs
│       ├── DuplicatedEntityException.cs
│       └── EntityNotFoundException.cs
│
├── Dsw2025Tpi.Domain/                        → CAPA DE DOMINIO
│   ├── Entities/
│   │   ├── EntityBase.cs                     → Guid Id
│   │   ├── Customer.cs                       → Name, Email, PhoneNumber
│   │   ├── Product.cs                        → SKU, Name, Price, Stock, IsActive
│   │   ├── Order.cs                          → Addresses, Status, FK→Customer
│   │   ├── OrderItem.cs                      → Qty, Price, FK→Order, FK→Product
│   │   └── OrderStatus.cs                   → Enum: PENDING..CANCELLED
│   └── Interfaces/
│       └── IRepository.cs                    → Genérico CRUD
│
└── Dsw2025Tpi.Data/                          → CAPA DE INFRAESTRUCTURA
    ├── Dsw2025TpiContext.cs                  → DbContext de negocio (4 DbSets)
    ├── AuthenticateContext.cs                 → IdentityDbContext (ASP.NET Identity)
    ├── Repositories/
    │   └── EfRepository.cs                   → Implementación genérica del repositorio
    ├── Helpers/
    │   └── DbContextExtensions.cs            → Seed data desde JSON
    ├── Sources/                              → Datos de seed (Products, Customers, Orders)
    └── Migrations/                           → Migraciones EF Core
```

### Entidades de dominio

| Entidad | Propiedades principales | Restricciones | Relaciones |
|---|---|---|---|
| **Customer** | Name, Email, PhoneNumber? | Email requerido (MaxLength 320), Name requerido (MaxLength 60) | 1→N Order |
| **Product** | Sku, InternalCode, Name, Description, CurrentUnitPrice, StockQuantity, IsActive | Sku requerido (MaxLength 20), Name requerido (MaxLength 60), Price > 0, Stock >= 0 | 1→N OrderItem |
| **Order** | Date, ShippingAddress?, BillingAddress?, Notes?, TotalAmount (computed), Status, CustomerId FK | Status = enum OrderStatus | N→1 Customer, 1→N OrderItem |
| **OrderItem** | Quantity, UnitPrice, Subtotal (computed), OrderId FK, ProductId FK | Quantity > 0, UnitPrice >= 0 | N→1 Order, N→1 Product |
| **OrderStatus** | Enum: PENDING=1, PROCESSING=2, SHIPPED=3, DELIVERED=4, CANCELLED=5 | — | — |

### Endpoints implementados

| Método | Endpoint | Auth | Descripción |
|---|---|---|---|
| POST | `/api/auth/login` | Anónimo | Login, retorna `{ token }` |
| POST | `/api/auth/register` | Anónimo | Registro de usuario |
| GET | `/api/products` | Anónimo | Catálogo público (solo activos), paginado |
| GET | `/api/products/{id}` | Admin,User | Detalle de producto |
| POST | `/api/products` | Admin | Crear producto |
| PUT | `/api/products/{id}` | Admin | Actualizar producto |
| PATCH | `/api/products/{id}` | Admin | Soft-delete (desactivar) |
| GET | `/api/products/admin` | Admin | Listado admin con filtros, paginado |
| GET | `/api/orders` | Admin | Listar todas las órdenes, paginado |
| POST | `/api/orders` | Admin,User | Crear orden (verifica y descuenta stock) |
| GET | `/api/orders/{id}` | Admin | Detalle de orden con items |
| PUT | `/api/orders/{id}` | Admin | Cambiar estado de orden |
| GET | `/healthcheck` | Anónimo | Health check básico |

### Configuración de seguridad actual

| Componente | Estado | Detalle |
|---|---|---|
| JWT Bearer | ✅ Configurado | HMAC-SHA256, 60 min expiración |
| ASP.NET Identity | ✅ Configurado | Tablas custom en español |
| Password hashing | ✅ PBKDF2 | Via ASP.NET Identity |
| CORS | ✅ Configurado | `localhost:5173`, `localhost:5174` |
| Roles | ✅ Seeded | "Admin", "User" |
| Admin por defecto | ✅ | `admin` / `SecurePassword123!` |
| Autorización por roles | ✅ | `[Authorize(Roles="Admin")]` en endpoints protegidos |
| HTTPS redirect | ✅ | `UseHttpsRedirection()` |

---

## Comunicación Frontend ↔ Backend

| Método | Endpoint | Frontend lo usa en | Estado |
|---|---|---|---|
| POST | `/api/auth/login` | `auth/services/login.js` | ✅ Implementado |
| POST | `/api/auth/register` | `auth/services/register.js` | ✅ Implementado |
| GET | `/api/products` | `products/services/listUser.js` | ✅ Implementado |
| GET | `/api/products/admin` | `products/services/list.js` | ✅ Implementado |
| POST | `/api/products` | `products/services/create.js` | ✅ Implementado |
| GET | `/api/orders` | `orders/services/listServices.js` | ✅ Implementado |
| POST | `/api/orders` | `orders/services/createOrder.js` | ✅ Implementado |

El frontend se conecta al backend via proxy de Vite (`/api` → `VITE_BACKEND_URL`). En producción, se configura la URL del backend directamente.

---

## Cómo Ejecutar

### Requisitos previos

- .NET 8 SDK
- Node.js 18+
- SQL Server (Docker o local)

### Frontend

```bash
cd ICS_TPI2026_frontend
npm install
npm run dev
```

El frontend corre en `http://localhost:5173`. Proxy de Vite redirige `/api` al backend.

### Backend

```bash
cd ICS_TPI2026_backend
dotnet run --project Dsw2025Tpi.Api
```

El backend corre en `https://localhost:7138`. Swagger disponible en `/swagger`.

### Base de datos

El backend usa SQL Server (LocalDB en desarrollo). Las migraciones se aplican automáticamente al iniciar. El seed data (productos, clientes, órdenes) se carga desde archivos JSON en `Data/Sources/`.

---

## Estado actual del proyecto

| Aspecto | Estado |
|---|---|
| **Backend** | |
| Arquitectura en capas | ✅ Implementada |
| Entidades de dominio (4) | ✅ Customer, Product, Order, OrderItem |
| DbContext + Fluent API | ✅ Configurado |
| Migraciones EF Core | ✅ Existen |
| Seed data | ✅ Configurado |
| Repositorio genérico | ✅ Implementado |
| Controllers (3) | ✅ Auth, Products, Orders |
| Servicios de negocio (3) | ✅ Products, Orders, JWT |
| DTOs (6 archivos) | ✅ Implementados |
| Validadores (4) | ✅ Estáticos, manuales |
| Excepciones personalizadas (3) | ✅ Implementadas |
| Autenticación JWT | ✅ Configurada |
| CORS | ✅ Configurado |
| Roles por endpoints | ✅ Implementado |
| **Frontend** | |
| Autenticación JWT | ✅ Login, logout, interceptor |
| Registro de usuarios | ✅ Formulario funcional |
| Rutas protegidas | ✅ Con verificación de rol |
| Catálogo público | ✅ Productos habilitados |
| Carrito de compras | ✅ Con localStorage |
| Checkout / crear orden | ✅ Funcional |
| Dashboard admin | ✅ Con contadores |
| CRUD Productos (admin) | ✅ List + Create |
| Listado Órdenes (admin) | ✅ Con búsqueda y filtros |
| Layout responsivo | ✅ Sidebar + header |
| Componentes reutilizables | ✅ 9 componentes |
| **Pendiente** | |
| Entidad Category | ❌ No implementada |
| Paginación del catálogo público | ⚠️ Carga todos los productos |
| Dashboard con datos reales del backend | ⚠️ Contadores estáticos |
| Orders/mine para clientes | ⚠️ No existe endpoint |
| Unit of Work | ❌ No implementado |
| Global exception handling | ❌ No implementado |
| Rate limiting | ❌ No configurado |
| TypeScript (frontend) | ❌ JavaScript puro |

---

## Deuda Técnica

### CRÍTICA — Seguridad

| # | Ubicación | Problema | Esfuerzo |
|---|---|---|---|
| 1 | BE: `appsettings.json` | JWT secret key committed a repo (cualquiera puede forjar tokens) | Bajo |
| 2 | BE: `appsettings.json` | Credenciales admin en plaintext (`SecurePassword123!`) | Bajo |
| 3 | BE: `appsettings.json` | JWT `ExpireInMinutes` no configurado (default 60 min silencioso) | Bajo |
| 4 | BE: `Program.cs` | Sin rate limiting en login/register (vulnerable a fuerza bruta) | Medio |
| 5 | BE: `Program.cs` | Password policy débil (solo requiere longitud 8) | Bajo |
| 6 | FE: `auth/context/AuthProvider.jsx` | Token JWT en `localStorage` (vulnerable a XSS) | Medio |
| 7 | FE: `shared/api/axiosInstance.js` | Sin validación de expiración JWT en frontend | Bajo |

### ALTA — Arquitectura y Calidad

| # | Ubicación | Problema | Esfuerzo |
|---|---|---|---|
| 8 | FE: `orders/services/createOrder.js` | Rompe contrato `{ data, error }` (retorna `{ data }` sin `error`) | Bajo |
| 9 | FE: `cart/hooks/useCart.js` | No es shared state — cada llamada crea instancia nueva (badge desincronizado) | Medio |
| 10 | FE: Múltiples archivos | `window.dispatchEvent` para comunicación entre componentes (anti-patrón) | Medio |
| 11 | BE: `Data/Repositories/EfRepository.cs` | Sin Unit of Work — operaciones multi-stock no atómicas | Medio |
| 12 | BE: `Application/Dsw2025Tpi.Application.csproj` | Referencia a `Data` (dependencia circular en Clean Architecture) | Medio |
| 13 | BE: `Controllers/BaseController.cs` | Código muerto — ningún controller lo hereda | Bajo |
| 14 | BE: `Program.cs` | Sin global exception handling middleware | Medio |
| 15 | BE: `Data/Dsw2025TpiContext.cs` | `BillingAddress` (string) tiene `HasPrecision(15,2)` — bug | Bajo |
| 16 | BE: `Data/Dsw2025TpiContext.cs` | `Order.Date` tiene `HasMaxLength(10)` — sin sentido en DateTime | Bajo |
| 17 | BE: `Data/Dsw2025TpiContext.cs` | Sin unique index en `Product.Sku` (race condition) | Bajo |
| 18 | BE: `Data/Sources/Products.json` | GUIDs duplicados en seed data | Bajo |

### MEDIA — Calidad de Código

| # | Ubicación | Problema | Esfuerzo |
|---|---|---|---|
| 19 | FE: `orders/services/listServices.js` | Fallback `fetch()` mixto con Axios (patrón inconsistente) | Bajo |
| 20 | FE: `shared/api/axiosInstance.js` | Interceptor usa `window.location.href` (reload completo del SPA) | Medio |
| 21 | BE: `Application/Services/ProducstManagementServices.cs` | Typo en nombre de archivo ("Producst") | Bajo |
| 22 | BE: `Application/Dtos/OrderModel.cs` | Typo: `TotatAmount` debería ser `TotalAmount` | Bajo |
| 23 | FE: `orders/pages/ListOrdersPage.jsx` | Trabaja con typo del backend: `order.totatAmount` | Bajo |
| 24 | FE: `shared/api/axiosInstance.js` | `withCredentials: innecesario (backend usa Bearer token, no cookies) | Bajo |
| 25 | FE: Múltiples archivos | Imágenes hardcoded externas (CDN de terceros) | Bajo |
| 26 | FE: `products/pages/ListProductsUserPage.jsx` | Base64 inline de 400+ chars como imagen por defecto | Bajo |
| 27 | FE: `package.json` | `SweetAlert2` instalado pero nunca importado | Bajo |
| 28 | BE: `Application/Validation/` | Inconsistencia en tipos de excepciones entre validadores | Bajo |
| 29 | BE: `Application/Services/OrdersManagementServices.cs` | Imports no usados (`Azure.Core`, `Microsoft.IdentityModel.Tokens`) | Bajo |
| 30 | BE: `test/` y `testC/` | Directorios duplicados con contenido idéntico | Bajo |

### BAJA — Mejoras

| # | Ubicación | Problema | Esfuerzo |
|---|---|---|---|
| 31 | FE: Todo el frontend | Sin TypeScript (JavaScript puro) | Muy alto |
| 32 | FE: `shared/components/Pagination.jsx` | Page sizes empiezan en 2 (solo testing) | Bajo |
| 33 | FE: `home/pages/Home.jsx` | Contadores estáticos, no consume API de stats | Medio |
| 34 | FE: `index.html` | Título dice "unidad-5" (leftover del curso) | Bajo |
| 35 | BE: Configuración | Sin Serilog (logging estructurado) | Medio |
| 36 | BE: Configuración | Sin API versioning | Medio |
| 37 | BE: Configuración | Health check no verifica SQL Server | Bajo |

---

## Archivos de Documentación

| Archivo | Contenido |
|---|---|
| `README.md` | Estado actual del proyecto, estructura, funcionalidades, deuda técnica (este archivo) |
| `TP1_Resuelto.md` | Diagramas C4, modelado de entidades, auditoría técnica, plan de refactorización |
| `PLAN.md` | Plan de implementación, roadmap priorizado, entidad actual del proyecto |
| `DB_Config.md` | Datos de conexión a SQL Server |
