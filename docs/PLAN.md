# PLAN — Plan de Implementacion Detallado

**Proyecto:** E-commerce ICS
**Stack:** React 19 + .NET 8 + SQL Server
**Objetivo:** Guia paso a paso desde el estado actual hasta el despliegue en produccion

---

## Orden de Prioridad

1. **Bugs** — Corregir errores que rompen funcionalidad o seguridad
2. **Deuda Tecnica** — Resolver problemas de arquitectura y calidad
3. **Mejoras** — Nuevas funcionalidades y UX

---

## Historias de Usuario — Sprint Backlog (10 Hallazgos TP1)

Estas son las 10 historias mas prioritarias, correspondientes a los hallazgos detallados en `TP1_Resuelto.md`.

### Backend (4 hallazgos)

| ID | Historia | Hallazgo | Prioridad | Esfuerzo |
|---|---|---|---|---|
| **US-17** | Como admin, quiero que el JWT secret no este en el codigo fuente | AT-01 | Critica | Bajo |
| **US-18** | Como admin, quiero que las credenciales admin esten en variables de entorno | AT-02 | Critica | Bajo |
| **US-24** | Como developer, quiero un Unit of Work que atomicice operaciones multi-tabla | AT-07 | Alta | Media |
| **US-23** | Como developer, quiero un global exception handling middleware | AT-09 | Alta | Media |

### Frontend (5 hallazgos)

| ID | Historia | Hallazgo | Prioridad | Esfuerzo |
|---|---|---|---|---|
| **US-21** | Como admin, quiero que el token se almacene en HttpOnly cookie | AT-04 | Alta | Alta |
| **US-35** | Como admin, quiero que el backend soporte autenticacion por cookies | AT-28 | Alta | Media |
| **US-16** | Como developer, quiero convertir useCart a Context para shared state | AT-16 | Alta | Media |
| **US-59** | Como developer, quiero extraer AppShell para eliminar layout duplicado | AT-59 | Alta | Media |
| **US-17F** | Como developer, quiero reemplazar window.dispatchEvent por Context | AT-17 | Media | Media |

### Arquitectura (1 hallazgo)

| ID | Historia | Hallazgo | Prioridad | Esfuerzo |
|---|---|---|---|---|
| **US-25** | Como developer, quiero corregir la dependencia circular Application->Data | AT-06 | Alta | Media |

---

## Historias de Usuario — Hallazgos Adicionales (Clasificados por Prioridad)

### Prioridad Critica

| ID | Historia | Hallazgo | Capa | Esfuerzo |
|---|---|---|---|---|
| **US-27** | Corregir BOLA en creacion de ordenes (validar CustomerId del JWT) | AT-27 | BE | Bajo |
| **US-30** | Migrar de LocalDB a Docker SQL Server | AT-26 | BE | Media |

### Prioridad Alta

| ID | Historia | Hallazgo | Capa | Esfuerzo |
|---|---|---|---|---|
| **US-26** | Corregir bugs en DbContext (BillingAddress, Order.Date, GUIDs) | AT-10,11,13 | BE | Bajo |
| **US-28** | Corregir typo TotatAmount -> TotalAmount en DTOs y frontend | AT-14 | BE+FE | Bajo |
| **US-29** | Eliminar BaseController.cs y imports no usados | AT-08,44 | BE | Bajo |
| **US-31** | Corregir validacion inconsistente UnitPrice | AT-42 | BE | Bajo |
| **US-32** | Agregar unique index en Product.Sku | AT-12 | BE | Bajo |
| **US-33** | Corregir FK cascade Customer->Orders (Restrict) | AT-48 | BE | Bajo |
| **US-34** | Corregir Error 500 por usuario sin rol (retornar 403) | AT-49 | BE | Bajo |
| **US-36** | Corregir createOrder.js contrato {data, error} | AT-15 | FE | Bajo |
| **US-37** | Eliminar fallback fetch en listServices.js | AT-19 | FE | Bajo |
| **US-38** | Corregir interceptor window.location.href (usar navigate) | AT-18 | FE | Media |
| **US-39** | Corregir searchTerm en deps de useEffect (UserPage + OrdersPage) | AT-53,64 | FE | Bajo |
| **US-40** | Corregir useDeleteQuantity closure stale | AT-56 | FE | Bajo |
| **US-41** | Corregir contrato inconsistente en register | AT-57 | FE | Bajo |
| **US-42** | Detectar token expirado al cargar la app | AT-72 | FE | Bajo |
| **US-43** | Auto-loguear despues del registro | AT-73 | FE | Bajo |
| **US-44** | Paginacion en BD (no en memoria) | AT-30 | BE | Media |
| **US-45** | Transaccion en operaciones de stock (race condition) | AT-31 | BE | Media |
| **US-46** | Agregar AsNoTracking en queries de solo lectura | AT-33 | BE | Bajo |
| **US-47** | Password policy fuerte (US-20) | AT-05 | BE | Bajo |
| **US-48** | Rate limiting en login/register (US-19) | -- | BE | Media |
| **US-49** | CustomerValidator invocar en registro | AT-36 | BE | Bajo |
| **US-50** | RegistrationService en Application (no en Controller) | AT-34 | BE | Media |
| **US-51** | Servicios con interfaces (IProductsService, IOrdersService) | AT-51 | BE | Media |
| **US-52** | LoginModel/RegisterModel con Data Annotations | AT-52 | BE | Bajo |
| **US-53** | Mapeo DTO centralizado (DRY) | AT-37 | BE | Media |
| **US-54** | DateTime.UtcNow en vez de DateTime.Now | AT-32 | BE | Bajo |
| **US-55** | DI centralizada en ServiceCollectionExtensions | AT-35 | BE | Bajo |
| **US-56** | CORS configurable por entorno | AT-46 | BE | Bajo |
| **US-57** | Respuesta de registro JSON (no string plano) | AT-47 | BE | Bajo |
| **US-58** | Endpoints GetAuthProducts eliminar o diferenciar | AT-39 | BE | Bajo |
| **US-59** | Soft delete reversible (toggle enable/disable) | AT-40 | BE | Bajo |
| **US-60** | Logger en OrdersManagementService | AT-41 | BE | Bajo |
| **US-61** | TotalAmount computed persistir o calcular en DTO | AT-38 | BE | Bajo |
| **US-62** |_health check con SQL Server | AT-31 | BE | Bajo |

### Prioridad Media

| ID | Historia | Hallazgo | Capa | Esfuerzo |
|---|---|---|---|---|
| **US-63** | Crear entidad Category | -- | BE+FE | Media |
| **US-64** | Dashboard con datos reales (endpoint stats) | -- | BE+FE | Media |
| **US-65** | Endpoint GET /api/orders/mine para clientes | -- | BE+FE | Media |
| **US-66** | Toast notifications (exito/error) | -- | FE | Media |
| **US-67** | Skeleton loaders durante carga | -- | FE | Bajo |
| **US-68** | Diseno responsivo completo (mobile-first) | -- | FE | Media |
| **US-69** | Estilos globales colisionan (elements.css) | AT-67 | FE | Media |
| **US-70** | Accessibility: select/input sin labels | AT-68,69 | FE | Bajo |
| **US-71** | Modal con focus trap, Escape, role="dialog" | AT-70 | FE | Media |
| **US-72** | Dashboard sidebar mobile con overlay backdrop | AT-76 | FE | Bajo |
| **US-73** | Body text-[2rem] corregir a text-base | AT-75 | FE | Bajo |
| **US-74** | setTimeout cleanup en RegisterForm | AT-55 | FE | Bajo |
| **US-75** | useNavigate import no usado eliminar | AT-54 | FE | Bajo |
| **US-76** | Button.jsx if vacio eliminar | AT-62 | FE | Bajo |
| **US-77** | URLs API inconsistentes estandarizar | AT-63 | FE | Bajo |
| **US-78** | SVG icons inline extraer a componentes | AT-66 | FE | Bajo |
| **US-79** | Sin React.memo/useMemo/useCallback agregar | AT-60 | FE | Media |
| **US-80** | TotalItems/totalAmount con useMemo | AT-61 | FE | Bajo |
| **US-81** | fetchOrders con useCallback | AT-65 | FE | Bajo |
| **US-82** | Home descarga 20 ordenes innecesariamente | AT-71 | FE | Bajo |
| **US-83** | navigate en formularios nunca se ejecuta | AT-74 | FE | Bajo |
| **US-84** | Imagenes hardcoded mover a assets/ | AT-21 | FE | Bajo |
| **US-85** | SweetAlert2 desinstalar | AT-22 | FE | Bajo |

### Prioridad Baja

| ID | Historia | Hallazgo | Capa | Esfuerzo |
|---|---|---|---|---|
| **US-86** | Pagina Home con banner y destacados | -- | FE | Media |
| **US-87** | Pagina "Productos" dedicada con filtros | -- | FE | Media |
| **US-88** | Pagina "Sobre Nosotros" | -- | FE | Baja |
| **US-89** | Componente Footer | -- | FE | Baja |
| **US-90** | Ocultar boton carrito sin usuario logueado | -- | FE | Baja |
| **US-91** | Header dinamico segun estado auth | -- | FE | Media |
| **US-92** | Pagina 404 personalizada | -- | FE | Baja |
| **US-93** | Serilog (logging estructurado) | -- | BE | Media |
| **US-94** | API versioning (/api/v1/) | -- | BE | Media |
| **US-95** | Refresh tokens | -- | BE+FE | Alta |
| **US-96** | TypeScript en frontend | -- | FE | Muy Alta |

---

## Fases de Implementacion

### Fase 0 — Setup y Base de Datos (30 min)

**Objetivo:** Entorno funcional con Docker SQL Server

| Paso | Archivo | Accion |
|---|---|---|
| 0.1 | `ICS_TPI2026_backend/docker-compose.yml` | Crear con SQL Server 2022 |
| 0.2 | `Dsw2025Tpi.Api/appsettings.json` | Actualizar connection string a Docker |
| 0.3 | `Dsw2025Tpi.Api/appsettings.Development.json` | Crear override para dev |
| 0.4 | Migraciones EF Core | Eliminar y recrear |
| 0.5 | `.env.development` (frontend) | Alinear puerto del backend |
| 0.6 | `docs/DB_Config.md` | Actualizar documentacion |

**Verificacion:**
- `docker compose up -d` arranca SQL Server
- `dotnet ef database update` aplica migraciones
- Backend arranca y Swagger funciona

---

### Fase 1 — Bugs Criticos (2-3 horas)

**Objetivo:** Resolver problemas de seguridad y funcionalidad rota

| Paso | Historia | Archivos | Accion |
|---|---|---|---|
| 1.1 | US-17 | `appsettings.json`, `Program.cs` | Mover JWT secret a User Secrets |
| 1.2 | US-18 | `appsettings.json`, `Program.cs` | Mover credenciales admin a env vars |
| 1.3 | US-27 | `OrderController.cs`, `OrdersManagementServices.cs` | Validar CustomerId del JWT (BOLA) |
| 1.4 | US-30 | `docker-compose.yml`, `appsettings.json` | Migrar a Docker SQL Server |
| 1.5 | US-26 | `Dsw2025TpiContext.cs`, `Products.json` | Corregir bugs DbContext + GUIDs |
| 1.6 | US-32 | `Dsw2025TpiContext.cs` | Agregar unique index en Sku |
| 1.7 | US-33 | Migracion | Cambiar cascade a Restrict |
| 1.8 | US-34 | `AuthenticateController.cs` | Retornar 403 en vez de 500 |

**Verificacion:**
- JWT secret no esta en appsettings.json
- BOLA corregido (CustomerId del token)
- SQL Server corre en Docker
- No hay GUIDs duplicados

---

### Fase 2 — Bugs Frontend + Contratos (2 horas)

**Objetivo:** Corregir bugs de frontend y inconsistencias

| Paso | Historia | Archivos | Accion |
|---|---|---|---|
| 2.1 | US-36 | `createOrder.js` | Unificar contrato {data, error} |
| 2.2 | US-37 | `listServices.js` | Eliminar fallback fetch |
| 2.3 | US-38 | `axiosInstance.js` | Usar navigate en vez de window.location |
| 2.4 | US-39 | `ListProductsUserPage.jsx`, `ListOrdersPage.jsx` | Agregar searchTerm a deps |
| 2.5 | US-40 | `useDeleteQuantity.js` | Corregir closure stale |
| 2.6 | US-41 | `register.js`, `AuthProvider.jsx` | Corregir contrato register |
| 2.7 | US-42 | `AuthProvider.jsx` | Verificar exp en init |
| 2.8 | US-43 | `AuthProvider.jsx`, `RegisterForm.jsx` | Auto-loguear post registro |

**Verificacion:**
- Todos los servicios retornan {data, error}
- useEffect tiene dependencias correctas
- Token expirado se detecta al cargar

---

### Fase 3 — Deuda Tecnica Backend (3-4 horas)

**Objetivo:** Corregir arquitectura y calidad de codigo

| Paso | Historia | Archivos | Accion |
|---|---|---|---|
| 3.1 | US-25 | `Application.csproj`, `Program.cs` | Eliminar ref a Data, usar DI |
| 3.2 | US-24 | `IUnitOfWork.cs`, `EfRepository.cs` | Implementar Unit of Work |
| 3.3 | US-23 | `GlobalExceptionMiddleware.cs` | Crear middleware ProblemDetails |
| 3.4 | US-29 | `BaseController.cs` | Eliminar codigo muerto |
| 3.5 | US-28 | `OrderModel.cs`, `ListOrdersPage.jsx` | Corregir typo TotatAmount |
| 3.6 | US-31 | `OrderItem.cs` | Corregir validacion UnitPrice |
| 3.7 | US-44 | `EfRepository.cs`, servicios | Paginacion en BD con IQueryable |
| 3.8 | US-45 | `OrdersManagementServices.cs` | Transaccion en operaciones stock |
| 3.9 | US-46 | `EfRepository.cs` | Agregar AsNoTracking |

**Verificacion:**
- Application solo refiere a Domain
- Unit of Work atomiciza operaciones
- Global exception handler retorna ProblemDetails
- No hay codigo muerto

---

### Fase 4 — Calidad Backend (3-4 horas)

**Objetivo:** Establecer buenas practicas y patrones

| Paso | Historia | Archivos | Accion |
|---|---|---|---|
| 4.1 | US-47 | `Program.cs` | Password policy fuerte |
| 4.2 | US-48 | `Program.cs` | Rate limiting en auth |
| 4.3 | US-49 | `OrdersManagementServices.cs` | Invocar CustomerValidator |
| 4.4 | US-50 | `RegistrationService.cs` | Extraer de Controller a Service |
| 4.5 | US-51 | Interfaces + DI | IProductsService, IOrdersService |
| 4.6 | US-52 | `LoginModel.cs`, `RegisterModel.cs` | Data Annotations |
| 4.7 | US-53 | Servicios | Centralizar mapeo DTO |
| 4.8 | US-54 | `Order.cs`, `JwtTokenService.cs` | DateTime.UtcNow |
| 4.9 | US-55 | `ServiceCollectionExtensions.cs` | Centralizar DI |
| 4.10 | US-56 | `Program.cs`, `appsettings.json` | CORS configurable |
| 4.11 | US-57 | `AuthenticateController.cs` | Respuesta JSON registro |
| 4.12 | US-60 | `OrdersManagementServices.cs` | Agregar ILogger |
| 4.13 | US-61 | `Order.cs` o DTOs | TotalAmount persistido |
| 4.14 | US-62 | `Program.cs` | Health check con SQL Server |

**Verificacion:**
- Password policy requiere complejidad
- Rate limiting activo en auth
- Servicios registrados por interfaz
- Logging en todos los servicios

---

### Fase 5 — Seguridad Cookies (4-5 horas)

**Objetivo:** Migrar de localStorage a HttpOnly cookies

| Paso | Historia | Archivos | Accion |
|---|---|---|---|
| 5.1 | US-35 | `AuthenticateController.cs` | Set-Cookie en login |
| 5.2 | US-35 | `JwtCookieMiddleware.cs` | Middleware para leer cookie |
| 5.3 | US-35 | `Program.cs` | CORS AllowCredentials |
| 5.4 | US-21 | `AuthProvider.jsx` | Dejar de usar localStorage |
| 5.5 | US-21 | `axiosInstance.js` | Enviar cookies automaticamente |
| 5.6 | US-21 | `login.js` | Manejar Set-Cookie response |
| 5.7 | -- | `LoginModal.jsx` | Actualizar flujo login |

**Verificacion:**
- Login envia cookie HttpOnly
- Axios envia cookies automaticamente
- localStorage no se usa para token
- CORS permite credentials

---

### Fase 6 — Frontend State Management (3-4 horas)

**Objetivo:** Corregir estado global y eliminar anti-patterns

| Paso | Historia | Archivos | Accion |
|---|---|---|---|
| 6.1 | US-16 | `CartContext.jsx` | Crear Context para carrito |
| 6.2 | US-16 | `useCart.js` | Migrar a Context |
| 6.3 | US-16 | `App.jsx` | Envolver con CartProvider |
| 6.4 | US-59 | `UserLayout.jsx` | Crear layout compartido |
| 6.5 | US-59 | Paginas usuario | Usar UserLayout |
| 6.6 | US-17F | `AuthContext.jsx` | Context para modales |
| 6.7 | US-17F | `LoginModal.jsx`, `RegisterModal.jsx` | Usar Context en vez de dispatch |
| 6.8 | US-17F | Paginas usuario | Eliminar window.addEventListener |

**Verificacion:**
- Carrito sincronizado entre paginas
- Badge del header se actualiza
- No hay window.dispatchEvent
- Layout duplicado eliminado

---

### Fase 7 — Funcionalidad Backend (4-6 horas)

**Objetivo:** Nuevas entidades y endpoints

| Paso | Historia | Archivos | Accion |
|---|---|---|---|
| 7.1 | US-63 | `Category.cs`, DbContext, migracion | Crear entidad Category |
| 7.2 | US-63 | `CategoryController.cs`, Service | CRUD categorias |
| 7.3 | US-64 | `DashboardController.cs` | Endpoint /api/dashboard/stats |
| 7.4 | US-65 | `OrderController.cs` | Endpoint /api/orders/mine |
| 7.5 | US-63 | Frontend modulo categories | CRUD categorias admin |

**Verificacion:**
- Category CRUD funciona
- Dashboard muestra datos reales
- Clientes ven solo sus ordenes

---

### Fase 8 — Frontend UX/UI (6-8 horas)

**Objetivo:** Mejorar experiencia de usuario

| Paso | Historia | Archivos | Accion |
|---|---|---|---|
| 8.1 | US-86 | `HomePage.jsx` | Pagina Home con destacados |
| 8.2 | US-87 | `ProductsPage.jsx` | Pagina Productos con filtros |
| 8.3 | US-88 | `AboutPage.jsx` | Pagina Sobre Nosotros |
| 8.4 | US-89 | `Footer.jsx` | Componente Footer |
| 8.5 | US-90 | `UserHeaderMenu.jsx` | Ocultar carrito sin auth |
| 8.6 | US-91 | `UserHeaderMenu.jsx` | Header dinamico |
| 8.7 | US-66 | `Toast.jsx` | Sistema de notificaciones |
| 8.8 | US-67 | `Skeleton.jsx` | Loaders de carga |
| 8.9 | US-68 | Multiples archivos | Diseno responsivo |
| 8.10 | US-92 | `NotFoundPage.jsx` | Pagina 404 |

**Verificacion:**
- Home muestra productos destacados
- Footer funcional
- Toasts aparecen en acciones
- Skeletons en carga
- Responsivo en mobile

---

### Fase 9 — Pre-Despliegue (2-3 horas)

**Objetivo:** Preparar para produccion

| Paso | Historia | Archivos | Accion |
|---|---|---|---|
| 9.1 | -- | `Dockerfile` (backend) | Crear Dockerfile .NET 8 |
| 9.2 | -- | `Dockerfile` (frontend) | Crear Dockerfile multi-stage |
| 9.3 | -- | `render.yaml` | Configuracion Render |
| 9.4 | -- | `.env.production` | Variables de entorno prod |
| 9.5 | -- | `appsettings.Production.json` | Configuracion prod |
| 9.6 | US-93 | `Program.cs` | Serilog configurado |
| 9.7 | US-94 | `Program.cs` | API versioning |
| 9.8 | -- | CORS | Configurar dominio Vercel |

**Verificacion:**
- Docker build exitoso
- Backend despliega en Render
- Frontend despliega en Vercel
- CORS permite dominio Vercel

---

### Fase 10 — Despliegue Final

**Objetivo:** Produccion funcional

| Paso | Accion |
|---|---|
| 10.1 | Crear branch `release/v1.0` |
| 10.2 | Deploy backend a Render |
| 10.3 | Deploy frontend a Vercel |
| 10.4 | Configurar Monster ASP.NET |
| 10.5 | Aplicar migraciones en produccion |
| 10.6 | Verificar health check |
| 10.7 | Test end-to-end manual |
| 10.8 | Crear PR a main |
| 10.9 | Merge y tag `v1.0.0` |

---

## Coordinacion con Trello

### Tablero: "TPI Ingenieria y Calidad de Software"

```
| Backlog | Sprint 1 | En Progreso | Revision | Hecho |
|---------|----------|-------------|----------|-------|
```

### Etiquetas

| Etiqueta | Color | Epica |
|---|---|---|
| Seguridad | Rojo | Epica 3 |
| Arquitectura | Naranja | Epica 4 |
| Backend | Azul | Epica 2 |
| Frontend | Verde | Epica 1 |
| Infraestructura | Morado | Epica 5 |
| Bug | Amarillo | Bugs |

### Sprint 1 (Sugerencia)

| Historia | Esfuerzo |
|---|---|
| US-17, US-18 (JWT/Creds secret) | 1h |
| US-27 (BOLA) | 0.5h |
| US-26 (Bugs DbContext) | 0.5h |
| US-32 (Unique Index) | 0.25h |
| US-33 (FK Cascade) | 0.25h |
| US-34 (Error 500) | 0.25h |
| US-28 (Typo) | 0.25h |
| US-29 (Codigo muerto) | 0.25h |
| US-30 (Docker SQL) | 1h |
| **Total** | **~4.5h** |

### Sprint 2 (Sugerencia)

| Historia | Esfuerzo |
|---|---|
| US-25 (Dependencia circular) | 1.5h |
| US-24 (Unit of Work) | 2h |
| US-23 (Exception middleware) | 1.5h |
| US-44 (Paginacion BD) | 1.5h |
| US-45 (Transaccion stock) | 1h |
| US-46 (AsNoTracking) | 0.5h |
| **Total** | **~8h** |

### Sprint 3 (Sugerencia)

| Historia | Esfuerzo |
|---|---|
| US-35 (Backend cookies) | 2h |
| US-21 (Frontend cookies) | 3h |
| US-47 a US-62 (Calidad backend) | 4h |
| **Total** | **~9h** |

### Sprint 4 (Sugerencia)

| Historia | Esfuerzo |
|---|---|
| US-16 (Cart Context) | 1.5h |
| US-59 (AppShell) | 2h |
| US-17F (Dispatch -> Context) | 1h |
| US-36 a US-43 (Bugs frontend) | 2h |
| **Total** | **~6.5h** |

---

## Patrones de Diseno a Aplicar

| Patron | Donde Aplicar | Beneficio |
|---|---|---|
| **Repository** | `IRepository<T>` + repositorios especificos | Abstraccion de persistencia |
| **Unit of Work** | `IUnitOfWork` con `SaveChangesAsync` centralizado | Atomicidad |
| **Strategy** | Filtros de busqueda (Producto, Orden) | Variabilidad en query building |
| **Dependency Injection** | Todos los servicios via interfaces | Desacoplamiento, testability |
| **Middleware** | `GlobalExceptionMiddleware` | Cross-cutting concerns |
| **Context (React)** | `AuthContext`, `CartContext`, `ModalContext` | Estado global |
| **Factory** | Creacion de DTOs de respuesta | Centralizacion de mapeo |
| **Template Method** | Servicios base con logica comun | Reutilizacion |

---

## Principios de Diseno

| Principio | Aplicacion |
|---|---|
| **SOLID** | DIP: servicios por interfaces. SRP: un servicio una responsabilidad |
| **DRY** | Mapeo DTO centralizado, componentes reutilizables |
| **KISS** | Validaciones simples, servicios claros |
| **YAGNI** | No implementar refresh tokens hasta que sea necesario |
| **Separation of Concerns** | Clean Architecture estricta |
| **Composition over Inheritance** | React: hooks y contextos componibles |

---

## Resumen de Esfuerzo

| Fase | Horas Estimadas |
|---|---|
| Fase 0 — Setup | 0.5h |
| Fase 1 — Bugs Criticos | 2.5h |
| Fase 2 — Bugs Frontend | 2h |
| Fase 3 — Deuda Tecnica Backend | 3.5h |
| Fase 4 — Calidad Backend | 3.5h |
| Fase 5 — Seguridad Cookies | 4.5h |
| Fase 6 — State Management | 3.5h |
| Fase 7 — Funcionalidad | 5h |
| Fase 8 — UX/UI | 7h |
| Fase 9 — Pre-Despliegue | 2.5h |
| Fase 10 — Despliegue | 1h |
| **Total** | **~35.5h** |
