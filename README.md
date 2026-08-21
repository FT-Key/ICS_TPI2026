# ICS - Trabajo Práctico Integrador: Plataforma E-commerce

## Visión General

Proyecto de una plataforma de comercio electrónico (E-commerce) desarrollado como Trabajo Práctico Integrador de la materia Desarrollo de Software. El sistema permite gestionar productos, administrar órdenes de compra y soporta autenticación con roles (Administrador / Cliente).

---

## Arquitectura del Proyecto

El proyecto está compuesto por dos partes principales:

```
ICS/
├── ICS_TPI2026_frontend/    → Frontend (React + Vite)
├── ICS_TPI2026_backend/     → Backend (.NET 8 - C#)
└── README.md                → Este archivo
```

---

## Frontend (`ICS_TPI2026_frontend`)

### Tecnologías utilizadas

| Tecnología | Versión | Uso |
|---|---|---|
| React | 19.1.1 | Framework UI |
| Vite | 7.1.7 | Bundler y dev server |
| React Router DOM | 7.9.4 | Enrutamiento |
| Axios | 1.13.2 | Cliente HTTP |
| React Hook Form | 7.65.0 | Manejo de formularios |
| Tailwind CSS | 4.1.14 | Estilos utility-first |

### Estructura de carpetas

```
src/
├── main.jsx                          → Punto de entrada
├── App.jsx                           → Configuración de rutas
├── index.css / elements.css          → Estilos globales
│
├── modules/
│   ├── auth/                         → Módulo de autenticación
│   │   ├── components/
│   │   │   ├── LoginForm.jsx         → Formulario de login
│   │   │   └── ProtectedRoute.jsx    → Ruta protegida (wrapper)
│   │   ├── context/
│   │   │   └── AuthProvider.jsx      → Context de autenticación (AuthContext + singin/singout)
│   │   ├── hook/
│   │   │   └── useAuth.js            → Hook para consumir el contexto de auth
│   │   ├── helpers/
│   │   │   └── backendError.js       → Mapeo de códigos de error del backend a mensajes
│   │   ├── pages/
│   │   │   └── LoginPage.jsx         → Página de login
│   │   └── services/
│   │       └── login.js              → Servicio de login (POST /api/auth/login)
│   │
│   ├── products/                     → Módulo de productos
│   │   ├── components/
│   │   │   └── CreateProductForm.jsx → Formulario de creación de producto
│   │   ├── helpers/
│   │   │   └── backendError.js       → Mapeo de errores del backend
│   │   ├── pages/
│   │   │   ├── ListProductsPage.jsx  → Listado con paginación y filtros
│   │   │   └── CreateProductPage.jsx → Página wrapper de creación
│   │   └── services/
│   │       ├── list.js               → GET /api/products/admin (paginado, filtrado)
│   │       └── create.js             → POST /api/products
│   │
│   ├── orders/                       → Módulo de órdenes
│   │   ├── pages/
│   │   │   └── ListOrdersPage.jsx    → Placeholder de listado de órdenes
│   │   └── services/
│   │       └── listServices.js       → GET /api/orders (usa fetch nativo)
│   │
│   ├── home/                         → Módulo home del dashboard
│   │   └── pages/
│   │       └── Home.jsx              → Dashboard principal (placeholder con contadores)
│   │
│   ├── templates/
│   │   └── components/
│   │       └── Dashboard.jsx         → Layout del panel admin (sidebar + header + outlet)
│   │
│   └── shared/                       → Componentes reutilizables
│       ├── components/
│       │   ├── Button.jsx            → Botón con variantes (default/secondary)
│       │   ├── Card.jsx              → Card contenedora
│       │   └── Input.jsx             → Input con label y manejo de errores
│       └── api/
│           └── axiosInstance.js      → Instancia de Axios con baseURL, interceptores de auth
```

### Rutas definidas

| Ruta | Componente | Protegida | Descripción |
|---|---|---|---|
| `/` | Listado placeholder | No | Página pública (aún no implementada) |
| `/cart` | Carrito placeholder | No | Carrito de compras (aún no implementado) |
| `/login` | LoginPage | No | Formulario de autenticación |
| `/admin/home` | Home | Sí | Dashboard principal con métricas |
| `/admin/products` | ListProductsPage | Sí | Listado paginado de productos con búsqueda y filtros |
| `/admin/products/create` | CreateProductPage | Sí | Formulario de creación de producto |
| `/admin/orders` | ListOrdersPage | Sí | Listado de órdenes (placeholder) |

### Funcionalidades implementadas en el Frontend

- **Autenticación JWT**: Login/logout con token almacenado en `localStorage`, interceptor que agrega `Authorization: Bearer` automáticamente y redirige al login en error 401.
- **Rutas protegidas**: Componente `ProtectedRoute` que redirige a `/login` si no hay sesión activa.
- **Dashboard responsivo**: Layout con sidebar colapsable en mobile, navegación con `NavLink` y estilos activos.
- **Listado de productos**: Paginación (2, 10, 15, 20 por página), búsqueda por texto, filtro por estado (Todos/Habilitados/Inhabilitados).
- **Creación de productos**: Formulario con validación (react-hook-form): SKU, código interno, nombre, descripción, precio, stock.
- **Componentes reutilizables**: `Button` (con variantes), `Input` (con errores), `Card`.
- **Manejo de errores del backend**: Mapeo de códigos numéricos del backend a mensajes amigables en español.

---

## Backend (`ICS_TPI2026_backend`)

### Tecnologías utilizadas

| Tecnología | Versión | Uso |
|---|---|---|
| .NET | 8.0 | Plataforma |
| C# | 12.0 | Lenguaje |
| Entity Framework Core | 9.0.6 | ORM (SQL Server) |
| Swashbuckle | 6.6.2 | Documentación Swagger/OpenAPI |

### Arquitectura: Capas (Clean Architecture)

```
ICS_TPI2026_backend/
├── Dsw2025Tpi.sln                        → Solución de Visual Studio
│
├── Dsw2025Tpi.Api/                       → Capa de presentación (API REST)
│   ├── Program.cs                        → Configuración y arranque de la aplicación
│   ├── appsettings.json                  → Configuración general
│   ├── appsettings.Development.json      → Configuración de desarrollo
│   ├── Controllers/                      → ⚠️ Carpeta vacía (pendiente de implementar)
│   └── Properties/launchSettings.json    → URLs de publicación
│
├── Dsw2025Tpi.Application/               → Capa de lógica de negocio
│   ├── Services/                         → ⚠️ Carpeta vacía (pendiente)
│   ├── Dtos/                             → ⚠️ Carpeta vacía (pendiente)
│   └── Exceptions/                       → ⚠️ Carpeta vacía (pendiente)
│
├── Dsw2025Tpi.Domain/                    → Capa de dominio
│   ├── Entities/
│   │   └── EntityBase.cs                 → Entidad base abstracta (propiedad Id tipo Guid)
│   └── Interfaces/
│       └── IRepository.cs                → Interfaz del repositorio genérico
│
└── Dsw2025Tpi.Data/                      → Capa de acceso a datos
    ├── Dsw2025TpiContext.cs              → DbContext de Entity Framework (vacío)
    └── Repositories/
        └── EfRepository.cs              → Implementación del repositorio genérico (CRUD completo)
```

### Funcionalidades implementadas en el Backend

- **Infraestructura base**: Solución con arquitectura en capas (Domain → Application → Data → Api).
- **Entidad base**: `EntityBase` con `Id` tipo `Guid` auto-generado.
- **Repositorio genérico**: `EfRepository` implementa `IRepository` con operaciones CRUD completas: `GetById`, `GetAll`, `GetFiltered`, `First`, `Add`, `Update`, `Delete`, con soporte para `Include` de relaciones.
- **Swagger/OpenAPI**: Configurado para entorno de desarrollo.
- **Health checks**: Endpoint `/healthcheck` disponible.
- **Configuración de CORS/credenciales**: Listo para conectar con el frontend.

### Lo que falta implementar en el Backend

- **Controllers**: La carpeta `Controllers` está vacía. No hay endpoints de API expuestos todavía.
- **Entidades de negocio**: Solo existe `EntityBase`. Faltan entidades como `Product`, `Order`, `User`, etc.
- **Servicios de aplicación**: La carpeta `Services` está vacía. No hay lógica de negocio implementada.
- **DTOs**: No hay objetos de transferencia de datos definidos.
- **Migraciones de EF Core**: El `Dsw2025TpiContext` está vacío, no hay migraciones ni tablas configuradas.
- **Autenticación**: No está configurada en el backend (el frontend asume que existe un endpoint `POST /api/auth/login`).
- **Configuración de base de datos**: `appsettings.json` no tiene cadena de conexión.

---

## Comunicación Frontend ↔ Backend

El frontend espera los siguientes endpoints en el backend:

| Método | Endpoint | Descripción | Estado |
|---|---|---|---|
| POST | `/api/auth/login` | Login (devuelve `{ token }`) | ⚠️ No implementado |
| GET | `/api/products/admin` | Listar productos (paginado, con filtros) | ⚠️ No implementado |
| POST | `/api/products` | Crear producto | ⚠️ No implementado |
| GET | `/api/orders` | Listar órdenes | ⚠️ No implementado |

El backend está configurado en `http://localhost:5142` (puerto por defecto de .NET).

---

## Cómo Ejecutar

### Frontend

```bash
cd ICS_TPI2026_frontend
npm install
npm run dev
```

El frontend corre en `http://localhost:5173` (puerto por defecto de Vite).

### Backend

```bash
cd ICS_TPI2026_backend
dotnet run --project Dsw2025Tpi.Api
```

El backend corre en `http://localhost:5142`. Swagger disponible en `/swagger`.

---

## Estado actual del proyecto

| Aspecto | Estado |
|---|---|
| Estructura del proyecto | ✅ Completa |
| Arquitectura backend (capas) | ✅ Configurada |
| Repositorio genérico (CRUD) | ✅ Implementado |
| Frontend - Login | ✅ Funcional |
| Frontend - Dashboard Admin | ✅ Funcional |
| Frontend - CRUD Productos | ✅ Funcional |
| Frontend - Órdenes | ⚠️ Placeholder |
| Frontend - Carrito público | ⚠️ Placeholder |
| Frontend - Registro de usuario | ⚠️ Botón sin implementar |
| Backend - Controllers | ❌ Vacío |
| Backend - Entidades de negocio | ❌ Solo EntityBase |
| Backend - Servicios de negocio | ❌ Vacío |
| Backend - Base de datos / Migraciones | ❌ No configurado |
| Backend - Auth JWT | ❌ No implementado |
