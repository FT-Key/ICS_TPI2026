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
├── README.md                → Estado actual del proyecto (este archivo)
├── TP1_Resuelto.md          → Modelado, auditoría técnica y plan de refactorización
├── PLAN.md                  → Plan de implementación de SQL Server
└── DB_Config.md             → Configuración de conexión a la base de datos
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

---

## Infraestructura de Base de Datos

| Componente | Estado | Documentación |
|---|---|---|
| SQL Server 2022 (Docker) | ✅ Configurado | `DB_Config.md` |
| Connection string (dev) | ✅ Documentado | `DB_Config.md` |
| Docker Compose | ✅ Documentado | `PLAN.md` (sección Docker Compose) |
| Entity modeling | ✅ Documentado | `PLAN.md` (sección Entidades) y `TP1_Resuelto.md` |
| Azure SQL (producción) | ✅ Planificado | `PLAN.md` (sección Producción) |

**Nota:** La configuración de SQL Server está documentada pero aún no implementada. Ver `PLAN.md` para el plan de ejecución completo.

---

## Archivos de Documentación

| Archivo | Contenido |
|---|---|
| `README.md` | Estado actual del proyecto, estructura, deuda técnica (este archivo) |
| `TP1_Resuelto.md` | Diagramas C4, modelado de entidades (ER), auditoría técnica, plan de refactorización |
| `PLAN.md` | Plan de implementación de SQL Server: entidades, DbContext, Docker, migraciones, Azure |
| `DB_Config.md` | Datos de conexión a SQL Server (usuario, password, puerto) |

---

## Deuda Técnica

### Frontend

---

#### 1. Servicio de órdenes usa `fetch` nativo en lugar de la instancia de Axios

**Archivo:** `src/modules/orders/services/listServices.js`

El módulo de órdenes utiliza `fetch()` nativo con configuración manual de headers y token, mientras que todos los demás servicios (`login.js`, `list.js`, `create.js`) usan la instancia de Axios centralizada en `axiosInstance.js`.

**Patrón violado:** Consistencia en el patrón de acceso a datos (DRY).

**Impacto de no corregir:** Duplicación de lógica de autenticación (el token se lee manualmente de `localStorage` en vez del interceptor). Si cambia la forma de obtención del token (ej: pasar a httpOnly cookies o refresh tokens), hay que recordar actualizar `listServices.js` aparte de `axiosInstance.js`. Riesgo de bugs por divergencia entre los dos caminos de comunicación HTTP.

**Esfuerzo para corregir:** Bajo (~15 min). Reemplazar `fetch` por `instance.get('/api/orders')` y eliminar la configuración manual de headers.

**Impacto de la corrección:** Consistencia total en capa de servicios, interceptor de 401 aplicable también a órdenes, menos código duplicado.

---

#### 2. Interceptor de Axios en `axiosInstance.js` llama a `window.location.href` directamente

**Archivo:** `src/modules/shared/api/axiosInstance.js:26`

En el interceptor de respuesta, cuando se recibe un 401 en rutas `/admin/`, se hace `window.location.href = '/login'`. Esto provoca un reload completo de la aplicación (pierde todo el estado en memoria de React).

**Patrón violado:** Separación de concerns (el módulo de infraestructura HTTP no debería conocer rutas de la aplicación ni forzar navegación).

**Impacto de no corregir:** Cada expiración de token provoca un reload completo del SPA. Si el usuario tenía estado temporal no persistido (formularios a medio llenar, filtros aplicados, scroll position), todo se pierde. Además, el interceptor tiene conocimiento acoplado a la estructura de rutas (`/admin/`), lo que lo hace frágil ante cambios de navegación.

**Esfuerzo para corregir:** Medio (~1-2 horas). Se debería inyectar la función de navegación del router (o un callback) al crear la instancia de Axios, o usar un event bus / custom hook que notifique al contexto de auth y deje que el router maneje la redirección.

**Impacto de la corrección:** Experiencia de usuario más fluida (sin reload), desacoplamiento entre capa HTTP y capa de navegación, más testable.

---

#### 3. `AuthContext` almacena el token en `localStorage` sin abstracción

**Archivo:** `src/modules/auth/context/AuthProvider.jsx:7-8, 15, 25`

El token se guarda y lee directamente de `localStorage` en múltiples archivos: `AuthProvider.jsx` (login/logout), `axiosInstance.js` (interceptor), `listServices.js` (fetch manual). No existe un servicio o utilidad centralizada de manejo de token/storage.

**Patrón violado:** Single Source of Truth / Separación de concerns. La lógica de persistencia del token está desparramada en al menos 3 archivos.

**Impacto de no corregir:** Si se decide cambiar el mecanismo de almacenamiento (ej: httpOnly cookies, sessionStorage, encriptar el token), hay que buscar y modificar cada archivo que acceda directamente a `localStorage`. Riesgo de olvidar algún punto y generar bugs silenciosos.

**Esfuerzo para corregir:** Bajo (~30 min). Crear un módulo `services/tokenStorage.js` con `getToken()`, `setToken()`, `removeToken()` y usarlo en todos los puntos de acceso.

**Impacto de la corrección:** Un solo punto de cambio para futuras mejoras de seguridad, más fácil de testear y auditar.

---

#### 4. `AuthProvider` tiene typo en los nombres de funciones: `singin` / `singout`

**Archivo:** `src/modules/auth/context/AuthProvider.jsx:13, 18`

Las funciones se llaman `singout` y `singin` en lugar de `signout` y `signin`. Esto afecta también a `useAuth.js` que expone los mismos nombres.

**Patrón violado:** Convención de nomenclatura estándar (SignIn/SignOut es el estándar en la industria).

**Impacto de no corregir:** Confusión para cualquier developer nuevo que entre al proyecto. Si se integran libs o documentación que usen `signIn`, habría que hacer un mapping mental constante. En caso de usar autenticación de terceros (Firebase, Auth0), los métodos nativos se llaman `signIn/signOut`, lo que generaría inconsistencia.

**Esfuerzo para corregir:** Bajo (~15 min). Renombrar en `AuthProvider.jsx` y `useAuth.js`. Afecta solo a los archivos internos del módulo auth.

**Impacto de la corrección:** Consistencia con estándares de la industria, menor curva de aprendizaje para nuevos integrantes.

---

#### 5. No existe manejo centralizado de estados de carga y error en las páginas

**Archivo:** `src/modules/products/pages/ListProductsPage.jsx:24, 29-38`

Cada página maneja su propio `useState` de `loading` y `error` de forma ad-hoc. `ListProductsPage` tiene `loading` y usa `try/catch`, pero `Home.jsx` no maneja carga ni errores, y `ListOrdersPage.jsx` es solo un placeholder. No hay un patrón reutilizable (ej: custom hook `useFetch`, componente `LoadingSpinner`, componente `ErrorBoundary`).

**Patrón violado:** Reutilización de lógica橫切ante (cross-cutting concern). Cada componente duplica la lógica de loading/error.

**Impacto de no corregir:** A medida que se agreguen más páginas, cada una implementará loading/error de forma diferente. Inconsistencia visual (algunas muestran "Buscando datos...", otras no muestran nada), código duplicado difícil de mantener.

**Esfuerzo para corregir:** Medio (~2-3 horas). Crear un hook genérico `useAsync` o `useFetch` que maneje `loading`, `error` y `data`, y un componente `Spinner`/`ErrorAlert` reutilizable.

**Impacto de la corrección:** Menos duplicación, experiencia visual consistente, más facilidad para agregar nuevas páginas.

---

#### 6. `Home.jsx` es un placeholder sin datos reales

**Archivo:** `src/modules/home/pages/Home.jsx`

El dashboard principal muestra "Cantidad: #" hardcoded. No consume ningún endpoint del backend para obtener métricas reales (total de productos, total de órdenes, etc.).

**Patrón violado:** Implementación incompleta.

**Impacto de no corregir:** El usuario administrador no tiene una vista real del estado del negocio. La página no aporta valor.

**Esfuerzo para corregir:** Medio (~2-3 horas). Crear un endpoint `GET /api/dashboard/stats` en el backend, un servicio en el frontend, y conectarlo al componente.

**Impacto de la corrección:** Dashboard funcional con datos reales, mayor utilidad para el admin.

---

#### 7. Ruta raíz `/` muestra solo texto hardcoded

**Archivo:** `src/App.jsx:19`

La ruta raíz `/` renderiza `<>Listado de productos</>` como texto plano, sin componente real.

**Patrón violado:** Ruta sin implementar.

**Impacto de no corregir:** El público general (no autenticado) no puede ver productos. Es una funcionalidad core del e-commerce que no existe.

**Esfuerzo para corregir:** Alto (~4-6 horas). Requiere crear un componente `PublicProductListPage`, un servicio público (sin auth), y posiblemente un endpoint público en el backend.

**Impacto de la corrección:** Los visitantes pueden navegar el catálogo, que es el flujo principal de un e-commerce.

---

#### 8. Ruta `/cart` es un placeholder sin implementación

**Archivo:** `src/App.jsx:23`

El carrito de compras renderiza `<>Carrito de compras</>` sin lógica alguna.

**Patrón violado:** Ruta sin implementar.

**Impacto de no corregir:** No hay flujo de compra. El e-commerce no puede procesar pedidos desde la vista pública.

**Esfuerzo para corregir:** Muy alto (~1-2 semanas). Requiere state management del carrito (Context/Redux/Zustand), agregado/eliminación de items, persistencia, cálculo de totales, flujo de checkout, integración con backend.

**Impacto de la corrección:** Flujo de compra completo, funcionalidad core del e-commerce.

---

#### 9. No existe hooks `useProducts` ni `useOrders` para abstraer la lógica de fetching

**Archivos:** `src/modules/products/pages/ListProductsPage.jsx:26-39`

Toda la lógica de fetching, paginación, búsqueda y manejo de estado está inline dentro del componente `ListProductsPage`. No se extrae a un custom hook reutilizable.

**Patrón violado:** Separación de concerns (presentación vs. lógica de datos). El componente mezcla UI con lógica de negocio de fetching.

**Impacto de no corregir:** Si se necesita reutilizar la lógica de listado de productos en otra página (ej: selector de productos para una orden), hay que duplicar todo el código. Los componentes se vuelven difíciles de testear porque la lógica de datos está acoplada al JSX.

**Esfuerzo para corregir:** Medio (~2 horas). Extraer `useProducts({ search, status, page, pageSize })` que devuelva `{ products, total, loading, error }`.

**Impacto de la corrección:** Componentes más limpios, lógica reutilizable, más facile de testear.

---

#### 10. No hay tipado (TypeScript)

**Archivo:** Todo el frontend (`src/**`)

El proyecto usa JavaScript puro sin TypeScript. Ningún componente, servicio o hook tiene tipos definidos.

**Patrón violado:** Seguridad de tipos / Validación en tiempo de compilación.

**Impacto de no corregir:** Errores de runtime que podrían haberse detectado en compilación (ej: pasar un string donde se espera un number, acceder a una propiedad que no existe en un objeto). Mayor dependencia de tests manuales. Refactors más riesgosos porque el compilador no ayuda a encontrar puntos rotos.

**Esfuerzo para corregir:** Muy alto (~1-2 semanas). Renombrar archivos `.jsx` → `.tsx`, definir interfaces para props, servicios, respuestas de API, contextos.

**Impacto de la corrección:** Detección de errores en compilación, mejor IDE support (autocompletado, refactorización segura), documentación viva del código.

---

#### 11. `withCredentials: true` en Axios sin necesidad actual

**Archivo:** `src/modules/shared/api/axiosInstance.js:5`

La instancia de Axios se configura con `withCredentials: true`, lo que envía cookies cross-origin. Sin embargo, el backend actual no usa cookies para autenticación (usa Bearer token en headers).

**Patrón violado:** Configuración innecesaria / Seguridad potencial.

**Impacto de no corregir:** Si el backend no está configurado para aceptar credenciales cross-origin, las peticiones podrían fallar con errores de CORS. Además, enviar cookies innecesariamente puede ser un vector de ataques CSRF si no se maneja correctamente.

**Esfuerzo para corregir:** Bajo (~5 min). Eliminar `withCredentials: true` o hacerlo condicional según el entorno.

**Impacto de la corrección:** Menor superficie de ataque, requests más limpios, menos confusión en configuración CORS.

---

### Backend

---

#### 12. `Dsw2025TpiContext` está completamente vacío

**Archivo:** `Dsw2025Tpi.Data/Dsw2025TpiContext.cs`

El DbContext no tiene `DbSet<T>` para ninguna entidad. No hay tablas definidas, no hay relaciones configuradas, no hay migraciones.

**Patrón violado:** Configuración mínima de persistencia.

**Impacto de no corregir:** El repositorio genérico (`EfRepository`) existe pero es inoperable. No se puede persistir ni consultar nada. Toda la capa de datos es inútil sin esto.

**Esfuerzo para corregir:** Alto (~4-6 horas). Definir todas las entidades (Product, Order, OrderItem, User, etc.), crear `DbSet<T>` para cada una, configurar relaciones con `OnModelCreating` o Fluent API, generar migración inicial.

**Impacto de la corrección:** La capa de datos pasa de ser un esqueleto a ser funcional. Habilita todo el CRUD del repositorio.

---

#### 13. No existen entidades de negocio (solo `EntityBase`)

**Archivo:** `Dsw2025Tpi.Domain/Entities/EntityBase.cs`

Solo hay una clase abstracta base. No hay `Product.cs`, `Order.cs`, `OrderItem.cs`, `User.cs`, ni ninguna entidad de dominio.

**Patrón violado:** Modelo de dominio incompleto.

**Impacto de no corregir:** No hay contrato de datos entre capas. La capa de aplicación no puede definir lógica de negocio porque no hay entidades que representen el dominio.

**Esfuerzo para corregir:** Alto (~4-6 horas). Diseñar el modelo de entidades según las requisitos del e-commerce, definir propiedades, relaciones, validaciones de dominio.

**Impacto de la corrección:** Contrato claro entre capas, posibilidad de implementar lógica de negocio, base para servicios y controllers.

---

#### 14. `IRepository` no define operaciones específicas del dominio

**Archivo:** `Dsw2025Tpi.Domain/Interfaces/IRepository.cs`

El repositorio es 100% genérico (`GetAll<T>`, `GetFiltered<T>`, etc.). No hay métodos específicos como `GetBySku(string sku)`, `GetActiveProducts()`, `GetOrdersByUser(Guid userId)`.

**Patrón violado:** Interface Segregation Principle (ISP) de SOLID — el cliente (servicio de negocio) depende de métodos que no usa. También viola el principio de que el repositorio debe expresar las consultas que el negocio necesita.

**Impacto de no corregir:** Cada servicio de negocio tendrá que hacer casts, usar `GetFiltered` con predicates complejos, o duplicar lógica de consulta. Las consultas específicas del dominio quedan expuestas como lógica genérica, perdiendo semántica y seguridad de tipos.

**Esfuerzo para corregir:** Medio (~3-4 horas). Crear interfaces específicas (`IProductRepository`, `IOrderRepository`) con métodos de dominio, y sus implementaciones concretas en `Data/Repositories/`.

**Impacto de la corrección:** Consultas tipadas y semánticas, mejor encapsulamiento, más fácil de testear y mantener.

---

#### 15. La capa `Application` tiene carpetas vacías sin implementación

**Archivos:** `Dsw2025Tpi.Application/Services/`, `Dsw2025Tpi.Application/Dtos/`, `Dsw2025Tpi.Application/Exceptions/`

Las carpetas están vacías. No hay servicios de negocio, DTOs ni excepciones personalizadas.

**Patrón violado:** Capa de aplicación inexistente. Los controllers (cuando se creen) tendrían que contener toda la lógica de negocio, acoplándolos directamente al repositorio.

**Impacto de no corregir:** Los controllers se vuelven "fat controllers" con lógica de negocio, validación, transformación de datos y acceso a BD todo en un solo lugar. Dificulta testing, reutilización y mantenimiento.

**Esfuerzo para corregir:** Muy alto (~1-2 semanas). Definir servicios (ej: `ProductManagementService`, `OrderManagementService`), DTOs de entrada/salida, excepciones de dominio, reglas de negocio.

**Impacto de la corrección:** Separación real de responsabilidades, lógica de negocio reutilizable y testeable independientemente del controller.

---

#### 16. `EfRepository` no usa Unit of Work pattern

**Archivo:** `Dsw2025Tpi.Data/Repositories/EfRepository.cs`

Cada operación (`Add`, `Update`, `Delete`) llama a `SaveChangesAsync()` individualmente. No hay transacciones que agrupen múltiples operaciones.

**Patrón violado:** Unit of Work pattern.

**Impacto de no corregir:** Si una operación de negocio requiere múltiples cambios en la BD (ej: crear orden + actualizar stock de 5 productos), no se puede garantizar atomicidad. Si falla la actualización del stock después de crear la orden, la BD queda en estado inconsistente (orden creada sin stock descontado).

**Esfuerzo para corregir:** Medio (~2-3 horas). Implementar `IUnitOfWork` con `SaveChangesAsync()` centralizado, que los servicios usen para confirmar o revertir transacciones.

**Impacto de la corrección:** Integridad transaccional, prevención de estados inconsistentes en la BD, cumplimiento del requisito de negocio de verificar stock antes de confirmar orden.

---

#### 17. No hay CORS configurado en el backend

**Archivo:** `Dsw2025Tpi.Api/Program.cs`

`Program.cs` no tiene `builder.Services.AddCors()` ni `app.UseCors()`. El frontend corre en `localhost:5173` y el backend en `localhost:5142` (puertos distintos = cross-origin).

**Patrón violado:** Configuración de seguridad y conectividad.

**Impacto de no corregir:** Las peticiones del frontend al backend serán bloqueadas por el navegador con errores de CORS. La aplicación simplemente no funciona en desarrollo ni en producción.

**Esfuerzo para corregir:** Bajo (~30 min). Agregar `AddCors()` y `UseCors()` en `Program.cs` con la política adecuada (origin del frontend).

**Impacto de la corrección:** La comunicación frontend-backend funciona. Sin esto, nada opera.

---

#### 18. No hay autenticación/autorización configurada

**Archivo:** `Dsw2025Tpi.Api/Program.cs`

No hay `builder.Services.AddAuthentication()` ni `app.UseAuthentication()` ni `app.UseAuthorization()` (el `UseAuthorization()` de la línea 29 no tiene autenticación previa configurada).

**Patrón violado:** Seguridad — la API es completamente abierta.

**Impacto de no corregir:** Cualquiera puede consumir cualquier endpoint sin autenticación. No hay distinción entre admin y cliente. No se puede implementar el requisito de "solo administradores pueden gestionar productos".

**Esfuerzo para corregir:** Alto (~4-6 horas). Configurar JWT Bearer authentication, definir políticas de autorización por rol, proteger controllers con `[Authorize]`.

**Impacto de la corrección:** API segura, control de acceso por roles, cumplimiento de requisitos de negocio.

---

#### 19. No hay manejo de errores global (Exception Filter / Middleware)

**Archivo:** `Dsw2025Tpi.Api/Program.cs`

No hay middleware de manejo de excepciones ni exception filter configurado. Si un controller lanza una excepción, el cliente recibirá un error 500 genérico con stack trace (en desarrollo) o un error vacío (en producción).

**Patrón violado:** Manejo transversal de errores (cross-cutting concern).

**Impacto de no corregir:** Los clientes (frontend) reciben errores sin formato consistente. No se pueden mapear códigos de error a mensajes amigables (el frontend ya tiene el mapeo en `backendError.js` esperando códigos numéricos como 1000, 3000). Stack traces expuestos en producción = riesgo de seguridad.

**Esfuerzo para corregir:** Medio (~2-3 horas). Crear un `GlobalExceptionFilter` o middleware que capture excepciones y las transforme en respuestas HTTP consistentes con códigos de error de negocio.

**Impacto de la corrección:** Respuestas de error predecibles, seguridad (sin stack traces), integración fluida con el mapeo de errores del frontend.

---

#### 20. No hay validación de modelos (FluentValidation / Data Annotations)

**Archivos:** Todo el backend

No hay validación configurada para los modelos de entrada. No hay Data Annotations en entidades ni FluentValidation en la capa de aplicación.

**Patrón violado:** Validación en la entrada — principio de "fail fast".

**Impacto de no corregir:** Datos inválidos pueden llegar hasta la BD (strings vacíos, precios negativos, stocks negativos). El frontend valida parcialmente (react-hook-form), pero el backend no tiene su propia validación, lo que deja la puerta abierta a peticiones maliciosas o directas a la API.

**Esfuerzo para corregir:** Medio (~2-3 horas). Agregar Data Annotations a las entidades (`[Required]`, `[MaxLength]`, `[Range]`) o instalar FluentValidation y crear validators para cada DTO.

**Impacto de la corrección:** Defensa en profundidad (validación en frontend Y backend), protección contra datos corruptos, mensajes de error consistentes.

---

#### 21. No hay logging estructurado ni auditoría

**Archivo:** `Dsw2025Tpi.Api/appsettings.json`, `Program.cs`

El logging está configurado solo con el provider por defecto (Consola). No hay logging estructurado (JSON), ni serilog, ni auditoría de quién hizo qué cambio.

**Patrón violado:** Observabilidad y auditoría.

**Impacto de no corregir:** En producción es imposible diagnosticar problemas, rastrear errores o saber quién modificó un producto o creó una orden. Sin auditoría, no hay cumplimiento de requisitos regulatorios si los hubiera.

**Esfuerzo para corregir:** Bajo-Medio (~1-2 horas). Agregar Serilog con sink de archivo/consola JSON, logging de acciones críticas en servicios.

**Impacto de la corrección:** Diagnóstico rápido en producción, trazabilidad de acciones, cumplimiento de buenas prácticas.

---

#### 22. No hay Health Checks configurados con profundidad

**Archivo:** `Dsw2025Tpi.Api/Program.cs:33`

Solo hay `app.MapHealthChecks("/healthcheck")` sin configuración de checks personalizados. No se verifica la conexión a la BD, ni la disponibilidad de servicios externos.

**Patrón violado:** Observabilidad de infraestructura.

**Impacto de no corregir:** El health check solo verifica que la app esté corriendo, no que funcione correctamente. En un entorno con load balancer o Kubernetes, la app podría recibir tráfico aunque la BD esté caída.

**Esfuerzo para corregir:** Bajo (~30 min). Agregar `AddHealthChecks().AddSqlServer()` para verificar conexión a BD.

**Impacto de la corrección:** Monitoreo real de la salud de la aplicación, fallback automático en orquestadores.

---

#### 23. `Program.cs` no registra dependencias (DI) de servicios ni repositorios

**Archivo:** `Dsw2025Tpi.Api/Program.cs`

No hay `builder.Services.AddScoped<IRepository, EfRepository>()` ni registro de servicios de negocio. El contenedor de dependientes está vacío.

**Patrón violado:** Inversión de Dependencias (DIP de SOLID). Las capas no pueden inyectar dependencias porque no están registradas.

**Impacto de no corregir:** Los controllers (cuando se creen) no pueden recibir servicios o repositorios por inyección de dependencias. Tendrán que instanciar manualmente las dependencias, creando acoplamiento directo y dificultando testing.

**Esfuerzo para corregir:** Bajo (~30 min). Agregar registros DI en `Program.cs` o en un archivo de extensión dedicado.

**Impacto de la corrección:** Desacoplamiento real entre capas, testing facilitado, cumplimiento de SOLID.

---

#### 24. No hay archivos `.csproj` de referencia cruzada entre todas las capas

**Archivos:** `Dsw2025Tpi.Api/Dsw2025Tpi.Api.csproj`, `Dsw2025Tpi.Application/Dsw2025Tpi.Application.csproj`

El proyecto `Api` no referencia a `Application` ni a `Data`. Solo `Data` referencia a `Domain`. La solución actualmente no puede compilar si se agregan dependencias entre capas porque las referencias de proyecto no están configuradas.

**Patrón violado:** Arquitectura por capas — las dependencias deben ir de externo a interno (Api → Application → Domain, Data → Domain).

**Impacto de no corregir:** No se puede inyectar un servicio de `Application` en un controller de `Api` porque `Api` no conoce `Application`. La arquitectura en capas es solo una convención de carpetas, no una restricción real.

**Esfuerzo para corregir:** Bajo (~15 min). Agregar `<ProjectReference>` en los `.csproj` correspondientes.

**Impacto de la corrección:** La arquitectura funciona realmente, las dependencias son explícitas y controladas.

---

### Seguridad y Autenticación

---

#### 25. Sin autenticación JWT configurada en el backend

**Archivo:** `Dsw2025Tpi.Api/Program.cs`

No existe ningún mecanismo de autenticación. `UseAuthorization()` en la línea 29 es código muerto porque no hay `UseAuthentication()` previo. No hay paquete JWT instalado, no hay configuración de tokens, no hay endpoint de login.

**Patrón violado:** Security by Design — no hay autenticación en ninguna capa.

**Impacto de no corregir:** Todos los endpoints son públicos. Cualquiera puede acceder a cualquier recurso sin credenciales. El login del frontend no puede funcionar.

**Esfuerzo para corregir:** Alto (~4-6 horas). Instalar `Microsoft.AspNetCore.Authentication.JwtBearer`, configurar JWT, crear `AuthController`, implementar generación de tokens.

**Impacto de la corrección:** API segura con control de acceso, login funcional, distinction entre admin y cliente.

---

#### 26. Sin hashing de contraseñas

**Archivo:** Todo el backend (no existe `User` entity ni paquete de hashing)

No existe ningún mecanismo de hashing de contraseñas. No hay entidad `User` con campo `PasswordHash`, no hay paquete BCrypt o similar instalado.

**Patrón violado:** Password Storage Best Practice — contraseñas nunca deben guardarse en texto plano.

**Impacto de no corregir:** Si se implementa login sin hashing, las contraseñas quedan en texto plano en la BD. Si la BD es comprometida, todas las credenciales quedan expuestas.

**Esfuerzo para corregir:** Bajo (~1 hora). Instalar `BCrypt.Net-Next`, usar `BCrypt.HashPassword()` y `BCrypt.Verify()`.

**Impacto de la corrección:** Contraseñas seguras en la BD, protección contra ataques de fuerza bruta.

---

#### 27. Token almacenado en localStorage (vulnerable a XSS)

**Archivo:** `src/modules/auth/context/AuthProvider.jsx`

El JWT se almacena en `localStorage`, que es accesible por cualquier script que se ejecute en la página. Si hay un ataque XSS, el atacante puede robar el token.

**Patrón violado:** Secure Token Storage — `localStorage` es vulnerable a XSS.

**Impacto de no corregir:** Robo de sesión via XSS, suplantación de identidad, acceso no autorizado.

**Esfuerzo para corregir:** Medio (~2-3 horas). Mover token a HttpOnly cookie o implementar BFF pattern.

**Impacto de la corrección:** Token protegido contra XSS, mayor seguridad de sesión.

---

#### 28. Sin control de acceso por roles

**Archivos:** Todo el proyecto (frontend y backend)

No existe implementación de roles. El README establece que "los administradores solo pueden gestionar productos" y "los clientes pueden crear y consultar ordenes", pero no hay distinción real. `ProtectedRoute.jsx` solo verifica si hay token, no el rol.

**Patrón violado:** Least Privilege — todos los usuarios tienen los mismos privilegios.

**Impacto de no corregir:** Un cliente podría modificar productos, ver ordenes ajenas, o acceder a funcionalidades de admin.

**Esfuerzo para corregir:** Medio (~2-3 horas). Definir roles como enteros (0=Admin, 1=Client), agregar `[Authorize(Roles = "0")]` en endpoints de admin.

**Impacto de la corrección:** Separación de privilegios, clientes no pueden acceder a funcionalidades de admin.

---

#### 29. Sin CORS configurado

**Archivo:** `Dsw2025Tpi.Api/Program.cs`

No hay configuración CORS. El proxy de Vite safa el problema en desarrollo, pero en producción el frontend no podría comunicarse con el backend.

**Patrón violado:** Cross-Origin Resource Sharing — configuración de seguridad y conectividad.

**Impacto de no corregir:** En producción, todas las peticiones del frontend retornan error CORS. La aplicación no funciona.

**Esfuerzo para corregir:** Bajo (~30 min). Agregar `AddCors()` y `UseCors()` con la política del frontend.

**Impacto de la corrección:** Comunicación frontend-backend funciona en todos los entornos.

---

#### 30. Sin validación de modelos en backend

**Archivos:** Todo el backend

No hay Data Annotations en entidades ni FluentValidation en la capa de aplicación. Datos inválidos pueden llegar hasta la BD (precios negativos, stock negativo, strings vacíos).

**Patrón violado:** Input Validation — principio de "fail fast".

**Impacto de no corregir:** Datos corruptos en la BD, errores de runtime, peticiones maliciosas que bypassan la validación del frontend.

**Esfuerzo para corregir:** Bajo (~1-2 horas). Agregar `[Required]`, `[Range]`, `[MaxLength]` a las entidades.

**Impacto de la corrección:** Defensa en profundidad, datos íntegros en la BD.

---

### Resumen de Deuda Técnica

| # | Ubicación | Problema | Esfuerzo | Impacto de corregir |
|---|---|---|---|---|
| 1 | FE: `orders/services/listServices.js` | Usa `fetch` en vez de Axios | Bajo | Consistencia en capa HTTP |
| 2 | FE: `shared/api/axiosInstance.js` | `window.location.href` en interceptor | Medio | Experiencia de usuario, desacoplamiento |
| 3 | FE: Múltiples archivos | Token en `localStorage` sin abstracción | Bajo | Seguridad, mantenimiento |
| 4 | FE: `auth/context/AuthProvider.jsx` | Typo `singin`/`singout` | Bajo | Convención, integración futura |
| 5 | FE: Múltiples páginas | No hay manejo centralizado de loading/error | Medio | Consistencia, reutilización |
| 6 | FE: `home/pages/Home.jsx` | Dashboard placeholder sin datos | Medio | Valor para el usuario admin |
| 7 | FE: `App.jsx` | Ruta `/` sin implementar | Alto | Funcionalidad core del e-commerce |
| 8 | FE: `App.jsx` | Ruta `/cart` sin implementar | Muy alto | Flujo de compra completo |
| 9 | FE: `products/pages/ListProductsPage.jsx` | Lógica de fetching inline | Medio | Reutilización, testabilidad |
| 10 | FE: Todo el frontend | Sin TypeScript | Muy alto | Seguridad de tipos, mantenibilidad |
| 11 | FE: `shared/api/axiosInstance.js` | `withCredentials: innecesario | Bajo | Seguridad, CORS |
| 12 | BE: `Data/Dsw2025TpiContext.cs` | DbContext vacío | Alto | Funcionalidad de persistencia |
| 13 | BE: `Domain/Entities/` | Solo `EntityBase`, sin entidades | Alto | Modelo de dominio inexistente |
| 14 | BE: `Domain/Interfaces/IRepository.cs` | Solo genérico, sin métodos de dominio | Medio | Consultas semánticas, tipado |
| 15 | BE: `Application/` (carpetas vacías) | Sin servicios, DTOs ni excepciones | Muy alto | Separación de responsabilidades |
| 16 | BE: `Data/Repositories/EfRepository.cs` | Sin Unit of Work | Medio | Atomicidad transaccional |
| 17 | BE: `Api/Program.cs` | Sin CORS configurado | Bajo | Comunicación FE↔BE funciona |
| 18 | BE: `Api/Program.cs` | Sin autenticación/autorización | Alto | Seguridad, control de acceso |
| 19 | BE: `Api/Program.cs` | Sin manejo global de errores | Medio | Respuestas consistentes, seguridad |
| 20 | BE: Todo el backend | Sin validación de modelos | Medio | Integridad de datos |
| 21 | BE: Configuración | Sin logging estructurado | Bajo-Medio | Observabilidad, diagnóstico |
| 22 | BE: `Api/Program.cs` | Health checks superficiales | Bajo | Monitoreo real |
| 23 | BE: `Api/Program.cs` | Sin registro de dependencias (DI) | Bajo | Inversión de dependencias, testing |
| 24 | BE: `.csproj` | Referencias de proyecto incompletas | Bajo | Arquitectura funcional |
| 25 | BE: `Api/Program.cs` | Sin autenticación JWT configurada | Alto | Seguridad — crítica |
| 26 | BE: Todo el backend | Sin hashing de contraseñas | Bajo | Seguridad — crítica |
| 27 | FE: `auth/context/AuthProvider.jsx` | Token en `localStorage` (XSS-vulnerable) | Medio | Seguridad de sesión |
| 28 | FE + BE: Todo el proyecto | Sin control de acceso por roles | Medio | Least Privilege |
| 29 | BE: `Api/Program.cs` | Sin CORS configurado | Bajo | Comunicación cross-origin |
| 30 | BE: Todo el backend | Sin validación de modelos | Bajo | Integridad de datos |
