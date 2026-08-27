# TP1 — Hallazgos Adicionales: Bugs, Deuda Técnica y Optimización

**Proyecto:** E-commerce ICS (React 19 + .NET 8 + SQL Server)
**Fecha:** 2026
**Referencia:** Ver `TP1_Resuelto.md` para las 9 deudas técnicas destacadas (presentación TP1)

---

## 1. Backend

### 1.1 Bugs

---

##### BUG AT-27 — Broken Object-Level Authorization en creación de órdenes (CRÍTICO)

**Ubicación:** `OrderController.cs` (AddOrder) y `OrdersManagementServices.cs` (AddOrder)

**Tipo:** Seguridad (Critical — BOLA — OWASP API Security Top 10)

**Descripción:**
El endpoint `POST /api/orders` recibe un `CustomerId` en el cuerpo del request. Ni el controller ni el servicio validan que ese `CustomerId` coincida con el usuario autenticado. El JWT contiene un claim `"id"` con el userId (`JwtTokenService.cs` línea 36: `new Claim("id", userId)`), pero nadie lo extrae ni lo valida.

**Consecuencia:** Cualquier usuario autenticado (rol "User") puede crear órdenes a nombre de cualquier otro cliente simplemente pasando un `customerId` diferente en el body.

**Recomendación:** En `AddOrder`, leer el `CustomerId` del claim del JWT (`User.FindFirst("id")`) en vez de confiar en el valor del body, o agregar una verificación explícita de rol antes de aceptar un `CustomerId` distinto al del token.

---

##### BUG AT-25 — OrderItemValidator no se llama (ALTO)

**Ubicación:** `OrdersManagementServices.cs` línea 114

**Tipo:** Bug (High — Validation Bypass)

**Descripción:**
El `OrderItemValidator` existe pero nunca se invoca desde `OrderValidator.Validate()` ni desde `OrdersManagementService.AddOrder()`. La validación de items individuales se salta.

**Consecuencia:** Se pueden crear `OrderItems` con `Quantity = 0` o `Quantity` negativa, afectando stock y total de la orden.

**Recomendación:** Llamar a `OrderItemValidator.Validate()` por cada item en `OrderValidator` o en el servicio.

---

##### BUG AT-13 — GUIDs duplicados en seed data (MEDIO)

**Ubicación:** `Dsw2025Tpi.Data/Sources/Products.json` líneas 2 y 21

**Tipo:** Bug (Medium — Duplicate Seed Data)

**Descripción:**
Dos productos en `Products.json` tienen el mismo GUID: `b9ed5544-42dc-439c-b4d7-15e720089caa`. Cuando `AddRange` recibe dos entidades con el mismo `Id`, EF Core lanza `InvalidOperationException`.

**Consecuencia:** La aplicación crashea al iniciar si la tabla Products está vacía.

**Recomendación:** Asignar GUIDs únicos a cada producto en el seed data.

---

##### BUG AT-12 — Sin unique index en Product.Sku (ALTO)

**Ubicación:** `Dsw2025TpiContext.cs` líneas 66-68

**Tipo:** Bug (High — Race Condition)

**Descripción:**
El servicio verifica unicidad de SKU en código, pero no hay constraint de unique en la base de datos. Dos requests concurrentes pueden crear productos con el mismo SKU.

**Consecuencia:** Race condition: dos productos con mismo SKU en la BD bajo carga concurrente.

**Recomendación:** Agregar `entity.HasIndex(p => p.Sku).IsUnique()` en `OnModelCreating`.

---

##### BUG AT-48 — FK cascade Customer→Orders destruye historial (ALTO)

**Ubicación:** Migración `Initial_Model.cs` líneas 62-66

**Tipo:** Integridad de datos (High — Destructive Cascade)

**Descripción:**
Si se elimina un Customer, todas sus Orders (y OrderItems) se eliminan por cascada. Borrar un cliente destruye todo su historial de compras, inaceptable en un sistema de facturación.

**Consecuencia:** Pérdida permanente de datos de auditoría, legales y contables.

**Recomendación:** Cambiar a `ReferentialAction.Restrict` (impedir borrar cliente con órdenes) o `SetNull` (mantener órdenes sin cliente).

---

##### BUG AT-49 — Error 500 por usuario sin rol (MEDIO)

**Ubicación:** `AuthenticateController.cs` líneas 55-60

**Tipo:** Manejo de errores (Medium — Wrong HTTP Status)

**Descripción:**
Un usuario autenticado sin rol asignado recibe un 500 Internal Server Error. Revela que la autenticación fue exitosa pero la autorización falló.

**Consecuencia:** Expone información interna y confunde al cliente.

**Recomendación:** Retornar 403 Forbidden con mensaje genérico.

---

##### BUG AT-42 — Validación inconsistente UnitPrice (BAJO)

**Ubicación:** `OrderItem.cs` líneas 41-47

**Tipo:** Validación (Low — Inconsistent Logic)

**Descripción:**
`UnitPrice` valida `value < 0` (permite 0) pero el mensaje dice "debe ser mayor a 0". Además es inconsistente con `Quantity` que usa `value <= 0`.

**Consecuencia:** Un `OrderItem` puede tener precio 0, generando subtotal 0.

**Recomendación:** Cambiar a `if (value <= 0)` para ser consistente.

---

### 1.2 Deuda Técnica

---

##### Deuda AT-03 — Sin repositorio genérico por entidad (MEDIO)

**Ubicación:** `IRepository.cs`, `EfRepository.cs`

**Tipo:** Arquitectura (Medium — Generic Repository Limitation)

**Descripción:**
Un solo repositorio genérico `EfRepository<T>` para todas las entidades. No permite personalizar consultas específicas por entidad.

**Consecuencia:** Queries complejas en el servicio de aplicación, no se pueden optimizar includes/filtrado por entidad.

**Recomendación:** Crear repositorios específicos (`IProductRepository`, `IOrderRepository`, `ICustomerRepository`).

---

##### Deuda AT-05 — Password policy débil (MEDIO)

**Ubicación:** `Program.cs` líneas 74-77

**Tipo:** Seguridad (Medium — Weak Password Policy)

**Descripción:**
Solo requiere longitud 8, sin reglas de complejidad.

**Consecuencia:** Contraseñas débiles como "12345678" son válidas.

**Recomendación:** Agregar `RequireDigit`, `RequireUppercase`, `RequireLowercase`, `RequireNonAlphanumeric`.

---

##### Deuda AT-28 — Backend no soporta autenticación por cookies (ALTO)

**Ubicación:** `AuthenticateController.cs`, `Program.cs`

**Tipo:** Seguridad / Arquitectura (High — Backend Cookie Support Missing)

**Descripción:**
El backend está configurado solo para header `Authorization: Bearer`. No tiene configuración para recibir tokens en cookies HttpOnly.

**Consecuencia:** Sin este cambio, la migración a cookies del frontend (AT-04) no funcionará.

**Recomendación:** Configurar `Set-Cookie` en login, middleware para leer cookie, CORS con `AllowCredentials`.

---

##### Deuda AT-29 — Sin medidas avanzadas de seguridad en cookies (MEDIO)

**Ubicación:** A configurar al implementar AT-04 y AT-28

**Tipo:** Seguridad (Medium — Cookie Security Hardening)

**Descripción:**
Usar cookies no es automáticamente seguro. Se necesitan: HttpOnly, Secure, SameSite, refresh token en cookie separada, revocación de tokens.

**Consecuencia:** Falsa sensación de seguridad si solo se cambia de localStorage a cookies sin hardening.

**Recomendación:** Implementar progresivamente: Fase 1 (HttpOnly+Secure+SameSite), Fase 2 (refresh token), Fase 3 (revocación).

---

##### Deuda AT-10 — BillingAddress.HasPrecision(15,2) en campo string (BAJO)

**Ubicación:** `Dsw2025TpiContext.cs` línea 46

**Tipo:** Calidad (Low — Semantic Error)

**Descripción:**
Campo `string` con `HasPrecision(15,2)` (para numéricos). SQL Server ignora la configuración.

**Consecuencia:** Configuración semántica incorrecta, genera confusión.

**Recomendación:** Eliminar `HasPrecision(15,2)`, solo mantener `HasMaxLength(60)`.

---

##### Deuda AT-11 — Order.Date.HasMaxLength(10) en campo DateTime (BAJO)

**Ubicación:** `Dsw2025TpiContext.cs` línea 41

**Tipo:** Calidad (Low — Meaningless Configuration)

**Descripción:**
Campo `DateTime` con `HasMaxLength(10)`, sin sentido para fechas.

**Consecuencia:** Configuración sin efecto práctico, genera confusión.

**Recomendación:** Eliminar `HasMaxLength(10)` de `Order.Date`.

---

##### Deuda AT-14 — Typo TotatAmount en DTO y frontend (MEDIO)

**Ubicación:** `OrderModel.cs` línea 13, `ListOrdersPage.jsx` línea 116

**Tipo:** Calidad (Medium — Typo Propagated)

**Descripción:**
El DTO tiene `TotatAmount` en lugar de `TotalAmount`. El frontend trabaja con este typo.

**Consecuencia:** Código confuso, el typo se propaga entre capas.

**Recomendación:** Renombrar a `TotalAmount` en ambos lados.

---

##### Deuda AT-26 — Base de datos LocalDB no apta para producción (ALTO)

**Ubicación:** `appsettings.json` línea 2

**Tipo:** Infraestructura (High — Not Production Ready)

**Descripción:**
LocalDB: solo Windows, un usuario a la vez, inestable, no apto para Docker.

**Consecuencia:** Imposible desplegar en Docker, colaboradores con Mac/Linux no pueden ejecutar el backend.

**Recomendación:** Migrar a Docker SQL Server 2022.

---

##### Deuda AT-24 — Inconsistencia en tipos de excepciones entre validadores (BAJO)

**Ubicación:** `Dsw2025Tpi.Application/Validation/`

**Tipo:** Calidad (Low — Inconsistent Error Handling)

**Descripción:**
Cada validador lanza un tipo diferente: `ApplicationException`, `ArgumentException`, `InvalidOperationException`.

**Consecuencia:** El controller debe catchear múltiples tipos de excepción.

**Recomendación:** Estandarizar un solo tipo de excepción de validación.

---

##### Deuda AT-08 — BaseController.cs es código muerto (BAJO)

**Ubicación:** `BaseController.cs`

**Tipo:** Calidad (Low — Dead Code)

**Descripción:**
Ningún controller usa `BaseController`. Todos heredan de `ControllerBase`.

**Consecuencia:** Código muerto que genera confusión.

**Recomendación:** Eliminar `BaseController.cs`.

---

##### Deuda AT-23 — Typo en nombre de archivo (BAJO)

**Ubicación:** `ProducstManagementServices.cs`

**Tipo:** Calidad (Low — Filename Typo)

**Descripción:**
El archivo se llama `ProducstManagementServices.cs` en lugar de `ProductsManagementServices.cs`.

**Consecuencia:** Confusión al buscar archivos.

**Recomendación:** Renombrar el archivo.

---

##### Deuda AT-32 — DateTime.Now en vez de DateTime.UtcNow (MEDIO)

**Ubicación:** `Order.cs` línea 14, `JwtTokenService.cs` línea 44

**Tipo:** Code smell (Medium — Timezone Issues)

**Descripción:**
`DateTime.Now` usa la zona horaria del servidor. Si el servidor se reconfigura o hay múltiples instancias, las fechas quedan inconsistentes y los tokens pueden expirar antes/después de lo esperado.

**Consecuencia:** Inconsistencia entre registros, tokens con expiración impredecible.

**Recomendación:** Usar `DateTime.UtcNow` o `DateTimeOffset.UtcNow`. Considerar un servicio de reloj inyectable (`IClock`) para testing.

---

##### Deuda AT-34 — AuthenticateController accede a Repository directamente (MEDIO)

**Ubicación:** `AuthenticateController.cs` líneas 19, 114

**Tipo:** Arquitectura (Medium — Layer Violation)

**Descripción:**
El controller inyecta `IRepository` y lo usa directamente para persistir `Customer`. No existe un `RegistrationService` en Application.

**Consecuencia:** La capa API conoce el repositorio directamente. Lógica no reutilizable, difícil de testear.

**Recomendación:** Crear `RegistrationService` en Application que encapsule la creación de IdentityUser + Customer.

---

##### Deuda AT-35 — DI inconsistente entre archivos (BAJO)

**Ubicación:** `Program.cs`, `ServiceCollectionExtensions.cs`

**Tipo:** Configuración (Low — Inconsistent DI)

**Descripción:**
La configuración de dependencias está distribuida sin patrón claro entre `Program.cs` y `ServiceCollectionExtensions.cs`.

**Consecuencia:** Dificulta entender qué está registrado y dónde.

**Recomendación:** Centralizar la DI en `ServiceCollectionExtensions.cs` con métodos por capa.

---

##### Deuda AT-36 — CustomerValidator nunca se invoca (MEDIO)

**Ubicación:** `CustomerValidator.cs`

**Tipo:** Dead code / Falta de validación (Medium — Unused Validator)

**Descripción:**
`CustomerValidator.Validate()` no se llama en ningún archivo. El registro de usuarios crea Customers sin validar email, nombre ni teléfono.

**Consecuencia:** Un usuario podría registrarse con email inválido o nombre vacío.

**Recomendación:** Llamar a `CustomerValidator.Validate()` en el controller o en un servicio de registro.

---

##### Deuda AT-37 — Mapeo DTO repetido masivamente (MEDIO)

**Ubicación:** `OrdersManagementServices.cs`, `ProducstManagementServices.cs`

**Tipo:** DRY (Medium — Massive Code Duplication)

**Descripción:**
El mapeo de `Order` a `ResponseOrderModel` se repite 4 veces. El mapeo de `Product` a `ResponseProductModel` se repite 5 veces.

**Consecuencia:** Si se agrega un campo, hay que modificar 4-5 lugares. Propenso a errores.

**Recomendación:** Extraer métodos `MapToResponse()` o usar AutoMapper/Mapster.

---

##### Deuda AT-38 — TotalAmount computed property no persistida (MEDIO)

**Ubicación:** `Order.cs` línea 26

**Tipo:** EF misuse (Medium — Unpersisted Computed Property)

**Descripción:**
`TotalAmount => OrderItems.Sum(p => p.Subtotal)` es una propiedad computada sin backing field. EF Core no puede traducirla a SQL y puede causar errores si `OrderItems` no está incluido.

**Consecuencia:** Posibles errores en queries, valor 0 si no se hace `Include`, no se puede ordenar/filtrar en BD.

**Recomendación:** Hacerlo persistente con `RecalculateTotal()` o calcular en el DTO de respuesta.

---

##### Deuda AT-39 — Endpoints GetAuthProducts idénticos a GetProducts (BAJO)

**Ubicación:** `ProductsController.cs` líneas 33-43 vs 133-144

**Tipo:** Dead code (Low — Duplicate Endpoints)

**Descripción:**
Ambos endpoints llaman al mismo servicio con el mismo filtro. La única diferencia es la autorización (`AllowAnonymous` vs `Admin`). El usuario anónimo ve los mismos datos que el admin.

**Consecuencia:** Endpoint redundante que confunde.

**Recomendación:** Eliminar `GetAuthProducts` o agregar diferenciadores reales (ej: admin ve productos desactivados).

---

##### Deuda AT-40 — Soft delete irreversible (MEDIO)

**Ubicación:** `ProducstManagementServices.cs` líneas 87-96

**Tipo:** Falta de funcionalidad (Medium — Irreversible State Change)

**Descripción:**
`PatchProduct` solo permite pasar de `IsActive=true` a `false`. No hay forma de reactivar un producto.

**Consecuencia:** Un admin no puede revertir una desactivación accidental sin manipular la BD.

**Recomendación:** Crear toggle o dos endpoints: `disable` y `enable`.

---

##### Deuda AT-41 — OrdersManagementService sin logger (MEDIO)

**Ubicación:** `OrdersManagementServices.cs`

**Tipo:** Observabilidad (Medium — No Logging)

**Descripción:**
A diferencia de `ProductsManagementService`, `OrdersManagementService` no inyecta `ILogger`.

**Consecuencia:** Sin trazas de operaciones críticas (creación de órdenes, cancelaciones).

**Recomendación:** Inyectar `ILogger<OrdersManagementService>` y agregar logging.

---

##### Deuda AT-44 — Usings innecesarios (BAJO)

**Ubicación:** Múltiples archivos (`ProductsController.cs`, `ProducstManagementServices.cs`, `OrdersManagementServices.cs`, `JwtTokenService.cs`)

**Tipo:** Code smell (Low — Unused Imports)

**Descripción:**
`Azure.Core`, `Microsoft.Identity.Client`, `Microsoft.AspNetCore.Http.HttpResults` importados pero no usados.

**Consecuencia:** Dependencias visuales falsas, ruido en el código.

**Recomendación:** Eliminar usings no utilizados.

---

##### Deuda AT-46 — CORS hardcodeado sin configuración por entorno (MEDIO)

**Ubicación:** `Program.cs` líneas 62-69

**Tipo:** Configuración (Medium — Hardcoded CORS)

**Descripción:**
Los orígenes CORS son URLs hardcodeadas de desarrollo (`localhost:5173`, `localhost:5174`). Sin configuración para producción.

**Consecuencia:** Si se despliega a producción con otro dominio, el frontend no podrá comunicarse.

**Recomendación:** Mover orígenes CORS a `appsettings.json` / variables de entorno.

---

##### Deuda AT-47 — Respuesta de registro es string plano (BAJO)

**Ubicación:** `AuthenticateController.cs` línea 124

**Tipo:** Inconsistencia (Low — Inconsistent Response Format)

**Descripción:**
`return Ok("Usuario registrado exitosamente.")` es el único endpoint que retorna un string plano. Todos los demás retornan JSON estructurado.

**Consecuencia:** Los clientes no pueden parsear la respuesta de forma consistente.

**Recomendación:** Retornar `Ok(new { message = "Usuario registrado exitosamente." })`.

---

##### Deuda AT-50 — Sin pruebas unitarias ni de integración (ALTO)

**Ubicación:** Directorios `test/` y `testC/` (solo JSON de testing manual)

**Tipo:** Testing (High — No Automated Tests)

**Descripción:**
No existe un proyecto de pruebas (xUnit, NUnit, MSTest). No hay ningún archivo `.cs` de test.

**Consecuencia:** Sin pruebas automatizadas, cualquier cambio puede introducir regresiones silenciosas.

**Recomendación:** Crear proyecto `Dsw2025Tpi.Tests` con pruebas unitarias para servicios, validadores y entidades, y pruebas de integración para endpoints.

---

##### Deuda AT-51 — Servicios registrados sin interfaz (MEDIO)

**Ubicación:** `ServiceCollectionExtensions.cs` líneas 15-16

**Tipo:** SOLID (Medium — DIP Violation)

**Descripción:**
`ProductsManagementService` y `OrdersManagementService` se registran por tipo concreto, no por interfaz.

**Consecuencia:** DIP violado, difícil de testear con mocks, no se puede intercambiar implementación.

**Recomendación:** Crear interfaces `IProductsManagementService` e `IOrdersManagementService`.

---

##### Deuda AT-52 — LoginModel/RegisterModel sin Data Annotations (MEDIO)

**Ubicación:** `LoginModel.cs`, `RegisterModel.cs`

**Tipo:** Validación (Medium — No Input Validation)

**Descripción:**
Sin atributos `[Required]`, `[EmailAddress]`, `[StringLength]`, `[MinLength]` en ninguna propiedad.

**Consecuencia:** Un request con campos vacíos pasa el model binding sin error.

**Recomendación:** Agregar atributos de validación a cada propiedad del record.

---

##### Deuda AT-53 — User enumeration por timing side-channel (MEDIO)

**Ubicación:** `AuthenticateController.cs` líneas 37-51

**Tipo:** Seguridad (Medium — User Enumeration)

**Descripción:**
El error 500 por usuario sin rol (AT-49) confirma que el usuario existe, rompiendo la protección de mensajes genéricos de login.

**Consecuencia:** Un atacante puede diferenciar entre "usuario no existe" vs "contraseña incorrecta" midiendo tiempos de respuesta.

**Recomendación:** Unificar ambas ramas en una sola operación que no revele información diferenciada.

---

### 1.3 Optimización / Performance

---

##### OPT AT-30 — Paginación en memoria (ALTO)

**Ubicación:** `ProducstManagementServices.cs` líneas 106-127, `OrdersManagementServices.cs` líneas 68-109

**Tipo:** Performance (High — In-Memory Pagination)

**Descripción:**
`GetFiltered` ejecuta `ToListAsync()` trayendo TODOS los registros a memoria. Luego `.Skip().Take()` se aplica sobre la lista ya materializada en C#.

**Consecuencia:** Con 100.000 productos, si el usuario pide página 1 con 10 items, se cargan los 100.000 a memoria y se descartan 99.990.

**Recomendación:** Implementar paginación en el repositorio con `IQueryable` antes de materializar.

---

##### OPT AT-31 — Race condition en stock sin protección transaccional (ALTO)

**Ubicación:** `OrdersManagementServices.cs` líneas 132-153, 192-201

**Tipo:** Integridad de datos (High — Concurrent Stock Modification)

**Descripción:**
Cada `Update` ejecuta `SaveChanges()` individualmente. Dos peticiones concurrentes pueden leer `StockQuantity = 5`, ambas validar `5 >= 3`, y ambas decrementar. Resultado: `StockQuantity = -1`.

**Consecuencia:** Stock puede quedar en valores negativos bajo carga concurrente.

**Recomendación:** Envolver en transacción explícita, usar `SELECT ... FOR UPDATE`, o validar/decrementar en una sola query SQL.

---

##### OPT AT-33 — Sin AsNoTracking en queries de solo lectura (MEDIO)

**Ubicación:** `EfRepository.cs` líneas 31-48

**Tipo:** Performance (Medium — Missing AsNoTracking)

**Descripción:**
Ningún método de lectura usa `.AsNoTracking()`. EF Core rastrea todas las entidades innecesariamente.

**Consecuencia:** Duplica memoria, reduce rendimiento, el `DbContext` mantiene referencias que nunca libera.

**Recomendación:** Agregar `.AsNoTracking()` a consultas de lectura.

---

## 2. Frontend

### 2.1 Bugs

---

##### BUG AT-15 — createOrder.js rompe contrato {data, error} (MEDIO)

**Ubicación:** `orders/services/createOrder.js`

**Tipo:** Calidad (Medium — Inconsistent Contract)

**Descripción:**
Todos los servicios retornan `{data, error}` vía `handleApiCall`. `createOrder.js` retorna solo `{data}` en éxito y `{error}` en fallo.

**Consecuencia:** Funciona por accidente (undefined es falsy) pero romperá con chequeo explícito `error === null`.

**Recomendación:** Unificar todos los servicios para usar `handleApiCall` consistentemente.

---

##### BUG AT-19 — listServices.js tiene fallback fetch mixto con Axios (MEDIO)

**Ubicación:** `orders/services/listServices.js` líneas 16-38

**Tipo:** Calidad (Medium — Inconsistent Pattern)

**Descripción:**
Fallback manual de `fetch()` después de que Axios falla. Ningún otro servicio hace esto. Lee token de `localStorage` directamente, retorna forma diferente de datos.

**Consecuencia:** Dos patrones de acceso a datos, posible crash silencioso en consumidores.

**Recomendación:** Eliminar el fallback fetch. Usar solo la instancia de Axios.

---

##### BUG AT-53 — `searchTerm` no en dependencias de `useEffect` en ListProductsUserPage (ALTO)

**Ubicación:** `products/pages/ListProductsUserPage.jsx` línea 77-79

**Tipo:** Bug (High — Stale Closure)

**Descripción:**
`useEffect` solo depende de `[pagination.pageNumber, pagination.pageSize]` pero `fetchProducts` usa `searchTerm`. El efecto NO se re-ejecuta al cambiar el término de búsqueda.

**Consecuencia:** El usuario debe presionar explícitamente el botón de búsqueda, comportamiento inconsistente con `ListProductsPage`.

**Recomendación:** Agregar `searchTerm` y `status` al array de dependencias.

---

##### BUG AT-64 — `searchTerm` no en dependencias de `useEffect` en ListOrdersPage (ALTO)

**Ubicación:** `orders/pages/ListOrdersPage.jsx` línea 44-46

**Tipo:** Bug (High — Stale Closure)

**Descripción:** Mismo problema que AT-53 pero en la página de órdenes.

**Recomendación:** Agregar `searchTerm` al array de dependencias.

---

##### BUG AT-56 — `useDeleteQuantity.increment` usa closure stale (MEDIO)

**Ubicación:** `shared/hooks/useDeleteQuantity.js` líneas 11-15

**Tipo:** Bug (Medium — Stale Closure in Hook)

**Descripción:**
`get(sku)` lee del estado closure del render actual, no del `prev` en `setDeleteQuantities`. Si se llama múltiples veces rápido, usa valores desactualizados.

**Consecuencia:** Skips en la secuencia de incremento.

**Recomendación:** Usar `(prev[sku] ?? 1) + 1` en vez de `get(sku) + 1`.

---

##### BUG AT-57 — Contrato `{data, error}` inconsistente en register (MEDIO)

**Ubicación:** `auth/services/register.js`, `auth/context/AuthProvider.jsx`

**Tipo:** Code smell (Medium — Inconsistent Contract)

**Descripción:**
`register` retorna `{ error }` pero no `{ data }`. El componente intenta desestructurar `{ data, error }` recibiendo `undefined` en `data`.

**Consecuencia:** Contrato implícito frágil que puede romperse.

**Recomendación:** Retornar siempre `{ data, error }` desde el AuthProvider.

---

##### BUG AT-72 — Token expirado no se detecta al cargar la app (MEDIO)

**Ubicación:** `auth/context/AuthProvider.jsx` líneas 23-26

**Tipo:** Estado inconsistente (Medium — Stale Auth State)

**Descripción:**
`isAuthenticated` se inicializa con `Boolean(localStorage.getItem('token'))`. Si el token está expirado, el usuario ve la UI como autenticado hasta recibir un 401.

**Consecuencia:** Flash de contenido protegido antes de ser redirigido.

**Recomendación:** Decodificar el JWT al init y verificar `exp < Date.now()`.

---

##### BUG AT-73 — register no auto-loguea ni retorna `{data}` (MEDIO)

**Ubicación:** `auth/context/AuthProvider.jsx` líneas 77-80

**Tipo:** Lógica incompleta (Medium — Incomplete Registration Flow)

**Descripción:**
Después de registrar, el usuario debe ir a login manualmente. La función nunca retorna `{ data }`.

**Consecuencia:** UX deficiente, interfaz inconsistente con `signin`.

**Recomendación:** Auto-loguear después del registro o retornar `{ data, error }` consistentemente.

---

### 2.2 Deuda Técnica

---

##### Deuda AT-04 — Token en localStorage vulnerable a XSS (ALTO)

**Ubicación:** `auth/context/AuthProvider.jsx`, `AuthenticateController.cs`

**Tipo:** Seguridad (High — XSS Vulnerability)

**Descripción:**
El JWT se almacena en localStorage, accesible por cualquier script. Migrar a cookies requiere cambios en ambas capas.

**Consecuencia:** Robo de sesión vía XSS.

**Recomendación:** Mover a HttpOnly cookie. Ver AT-28 (backend) y AT-29 (seguridad cookies).

---

##### Deuda AT-16 — useCart no es shared state (ALTO)

**Ubicación:** `cart/hooks/useCart.js`

**Tipo:** Arquitectura (High — State Management Issue)

**Descripción:**
Cada llamada a `useCart()` crea instancia independiente de estado. No hay Context como `AuthContext`.

**Consecuencia:** Badge del carrito no se actualiza, estado desincronizado entre páginas.

**Recomendación:** Envolver `useCart` en un Context para estado global.

---

##### Deuda AT-17 — window.dispatchEvent para comunicación entre componentes (MEDIO)

**Ubicación:** `LoginModal.jsx`, `RegisterModal.jsx`, `ListProductsUserPage.jsx`, `CartPage.jsx`

**Tipo:** Calidad (Medium — Anti-Pattern)

**Descripción:**
Los modales se comunican via `window.dispatchEvent('open-login')`. Bypass del flujo de datos de React.

**Consecuencia:** Invisible en React DevTools, imposible de testear, acoplamiento invisible.

**Recomendación:** Usar Context o estado levantado en un componente padre común.

---

##### Deuda AT-18 — Interceptor usa window.location.href (MEDIO)

**Ubicación:** `shared/api/axiosInstance.js` líneas 21-36

**Tipo:** Calidad (Medium — Tight Coupling)

**Descripción:**
El interceptor hace `window.location.href = '/login'` en 401, causando reload completo de la SPA.

**Consecuencia:** Pierde todo el estado en memoria de React, acoplado a la estructura de rutas.

**Recomendación:** Inyectar navegación via callback o usar contexto de auth.

---

##### Deuda AT-20 — withCredentials innecesario en Axios (BAJO)

**Ubicación:** `shared/api/axiosInstance.js` línea 5

**Tipo:** Seguridad (Low — Unnecessary Configuration)

**Descripción:**
`withCredentials: true` pero el backend usa Bearer token, no cookies.

**Consecuencia:** Envío innecesario de cookies, potencial vector CSRF.

**Recomendación:** Eliminar `withCredentials: true` o hacerlo condicional.

---

##### Deuda AT-21 — Imágenes hardcoded externas y base64 inline (BAJO)

**Ubicación:** `UserHeaderMenu.jsx`, `MobileSideMenu.jsx`, `CartPage.jsx`, `ListProductsUserPage.jsx`

**Tipo:** Calidad (Low — External Dependencies)

**Descripción:**
Imágenes de CDN externo (freepik, flaticon) y base64 inline.

**Consecuencia:** No funciona offline, dependencia de CDNs, bundle inflado.

**Recomendación:** Mover todas las imágenes a `assets/` local.

---

##### Deuda AT-22 — SweetAlert2 instalado pero nunca usado (BAJO)

**Ubicación:** `package.json`

**Tipo:** Calidad (Low — Dead Dependency)

**Descripción:**
`SweetAlert2` en dependencias pero nunca se importa.

**Consecuencia:** Bundle size inflado innecesariamente.

**Recomendación:** `npm uninstall sweetalert2`.

---

##### Deuda AT-54 — `useNavigate` importado no usado en ListOrdersPage (BAJO)

**Ubicación:** `orders/pages/ListOrdersPage.jsx` línea 2

**Tipo:** Dead code (Low — Unused Import)

**Descripción:** `useNavigate` importado pero la variable `navigate` nunca se declara ni se usa.

**Recomendación:** Eliminar la línea de import.

---

##### Deuda AT-55 — `setTimeout` sin cleanup en RegisterForm (MEDIO)

**Ubicación:** `auth/components/RegisterForm.jsx` línea 38-44

**Tipo:** Memory leak (Medium — Missing Cleanup)

**Descripción:**
`setTimeout` se ejecuta sin guardarse su ID y sin `clearTimeout`. Si el componente se desmonta antes de 2s, el callback se ejecuta sobre componente desmontado.

**Recomendación:** Guardar ID y retornar `clearTimeout` en cleanup.

---

##### Deuda AT-58 — Duplicación masiva ListProductsPage vs ListProductsUserPage (ALTO)

**Ubicación:** `products/pages/ListProductsPage.jsx` (185 líneas), `ListProductsUserPage.jsx` (260 líneas)

**Tipo:** DRY (High — Massive Code Duplication)

**Descripción:**
Ambos componentes comparten: estado, lógica de fetch, estructura JSX, Pagination, "no se encontraron productos".

**Consecuencia:** Mantener dos copias synchronizadas es propenso a errores.

**Recomendación:** Extraer componente `ProductListView` o hook `useProductList`.

---

##### Deuda AT-59 — Código duplicado menú+modales en ListProductsUserPage y CartPage (ALTO)

**Ubicación:** `ListProductsUserPage.jsx`, `CartPage.jsx`

**Tipo:** DRY (High — Duplicate Layout Code)

**Descripción:**
Ambos replican: `open-login`/`open-register` via `window.addEventListener`, uso de `UserHeaderMenu`+`MobileSideMenu`+`LoginModal`+`RegisterModal`, cálculo de `totalItems`.

**Consecuencia:** Duplicación de lógica de layout en cada página.

**Recomendación:** Crear componente `AppShell` o `UserLayout` que encapsule header, mobile menu y modales.

---

##### Deuda AT-62 — `Button.jsx` tiene `if` vacío (BAJO)

**Ubicación:** `shared/components/Button.jsx` líneas 2-3

**Tipo:** Dead code (Low — Empty Conditional)

**Descripción:** `if (!['button', 'reset', 'submit'].includes(type)) {}` — el bloque está vacío.

**Recomendación:** Eliminar el bloque if vacío o implementar el comportamiento pretendido.

---

##### Deuda AT-63 — URLs API inconsistentes (BAJO)

**Ubicación:** Múltiples servicios (`create.js`, `list.js`, `listUser.js`, `createOrder.js`, `login.js`)

**Tipo:** Code smell (Low — Inconsistent URL Patterns)

**Descripción:** Algunas URLs empiezan con `/api/` y otras con `api/` (sin barra inicial).

**Recomendación:** Estandarizar todas con el mismo patrón.

---

##### Deuda AT-66 — SVG icons inline duplicados (BAJO)

**Ubicación:** `ListProductsPage.jsx`, `ListOrdersPage.jsx`

**Tipo:** DRY (Low — Duplicate SVG Icons)

**Descripción:** Los mismos SVG de "agregar" y "buscar" se copian inline en cada componente.

**Recomendación:** Extraer como componentes (`<SearchIcon />`, `<PlusIcon />`).

---

##### Deuda AT-67 — Estilos globales en `elements.css` colisionan con componentes (MEDIO)

**Ubicacion:** `src/elements.css`, `Input.jsx`, `Select` en páginas

**Tipo:** CSS conflict (Medium — Global Style Collision)

**Descripción:**
Estilos globales aplican `p-1.5` a TODOS los `<input>` y `<select>`. El componente `Input` aplica sus propios estilos encima, generando conflicto de especificidad.

**Consecuencia:** Tamaños de padding inconsistentes entre inputs.

**Recomendación:** Eliminar estilos globales, mover toda la estilización a componentes reutilizables.

---

##### Deuda AT-68 — `<select>` sin `<label>` ni `aria-label` (MEDIO)

**Ubicación:** `ListProductsPage.jsx`, `ListOrdersPage.jsx`

**Tipo:** Accesibilidad (Medium — Missing Labels)

**Descripción:** Los `<select>` de filtro no tienen label asociado ni `aria-label`.

**Consecuencia:** Un usuario con lector de pantalla no puede saber qué hace ese select.

**Recomendación:** Agregar `<label className="sr-only">` o `aria-label`.

---

##### Deuda AT-69 — Inputs de búsqueda sin `aria-label` (MEDIO)

**Ubicación:** `ListProductsPage.jsx`, `ListOrdersPage.jsx`

**Tipo:** Accesibilidad (Medium — Missing Labels)

**Descripción:** Inputs de búsqueda tienen solo `placeholder="Buscar"` pero no `aria-label`. El placeholder no es sustituto de label para lectores de pantalla.

**Recomendación:** Agregar `aria-label="Buscar productos"` o usar componente `Input` con prop `label`.

---

##### Deuda AT-70 — `Modal` sin focus trap, Escape, ni `role="dialog"` (MEDIO)

**Ubicación:** `shared/components/Modal.jsx`

**Tipo:** Accesibilidad (Medium — Incomplete Modal Pattern)

**Descripción:**
Sin `role="dialog"`, sin `aria-modal="true"`, no atrapa foco, no se cierra con Escape, no retorna foco al elemento que lo abrió.

**Consecuencia:** Experiencia deficiente para usuarios con teclado y lectores de pantalla.

**Recomendación:** Implementar `useFocusTrap` o usar `@headlessui/react Dialog`.

---

##### Deuda AT-74 — `navigate` en formularios nunca se ejecuta (BAJO)

**Ubicación:** `auth/components/RegisterForm.jsx`, `auth/components/LoginForm.jsx`

**Tipo:** Dead code (Low — Unreachable Branch)

**Descripción:** La rama `else { navigate('/login') }` nunca se ejecuta porque `onSuccess` siempre está presente.

**Recomendación:** Eliminar la dependencia de `useNavigate` si `onSuccess` siempre está presente.

---

##### Deuda AT-75 — `body` tiene `text-[2rem]` en mobile (MEDIO)

**Ubicación:** `src/index.css` línea 5

**Tipo:** UX/CSS (Medium — Inconsistent Typography)

**Descripción:**
Font-size base es `2rem` (~32px) en mobile, mucho más grande que lo normal (16px). En `sm:` se reduce a `text-base`, causando salto visual brusco.

**Recomendación:** Usar `text-base` (16px) en mobile y escalar con componentes individuales.

---

##### Deuda AT-76 — Dashboard sidebar mobile sin overlay backdrop (MEDIO)

**Ubicación:** `templates/components/Dashboard.jsx` líneas 74-94

**Tipo:** UX/Accesibilidad (Medium — Missing Overlay)

**Descripción:**
El sidebar mobile se despliega pero no tiene overlay oscuro detrás. El usuario puede interactuar con el contenido principal detrás del menú.

**Recomendación:** Agregar overlay backdrop (`bg-black/40`) que cierre el menú al clickearse.

---

### 2.3 Optimización / Performance

---

##### OPT AT-60 — Sin `React.memo`, `useMemo`, `useCallback` en todo el proyecto (MEDIO)

**Ubicación:** Todos los `.jsx` del proyecto

**Tipo:** Performance (Medium — No Memoization)

**Descripción:**
No hay ninguna llamada a `React.memo`, `useMemo`, o `useCallback`. Cada cambio de estado re-renderiza todos los componentes hijos.

**Consecuencia:** Re-renders innecesarios, objetos de props recreados en cada render.

**Recomendación:** Aplicar `React.memo` a componentes hoja, `useCallback` para funciones pasadas como props, `useMemo` para valores computados.

---

##### OPT AT-61 — `totalItems`/`totalAmount` sin `useMemo` (BAJO)

**Ubicación:** `cart/pages/CartPage.jsx` líneas 39-40, `ListProductsUserPage.jsx` línea 31

**Tipo:** Performance (Low — Unnecessary Recalculation)

**Descripción:** `cart.reduce(...)` se recalcula en cada render aunque `cart` no haya cambiado.

**Recomendación:** `useMemo(() => cart.reduce(...), [cart])`.

---

##### OPT AT-65 — `fetchOrders` sin `useCallback` (BAJO)

**Ubicación:** `orders/pages/ListOrdersPage.jsx` línea 64

**Tipo:** Performance (Low — Unnecessary Re-render)

**Descripción:** `fetchOrders` se redefine en cada render, causando re-render innecesario del `Button`.

**Recomendación:** Envolver en `useCallback`.

---

##### OPT AT-71 — Home descarga 20 órdenes para solo contar (BAJO)

**Ubicación:** `home/pages/Home.jsx` línea 17

**Tipo:** Performance (Low — Unnecessary Data Fetch)

**Descripción:** `getOrders('', '', 1, 20)` trae 20 objetos completos solo para obtener `totalCount`.

**Recomendación:** Usar `pageSize=1` o crear endpoint dedicado de conteo.

---

## 3. Resumen Clasificado

### Backend — Bugs

| ID | Hallazgo | Severidad |
|----|----------|-----------|
| AT-27 | BOLA en creación de órdenes | Crítico |
| AT-25 | OrderItemValidator no se llama | Alto |
| AT-13 | GUIDs duplicados en seed data | Medio |
| AT-12 | Sin unique index en Product.Sku | Alto |
| AT-48 | FK cascade Customer→Orders destruye historial | Alto |
| AT-49 | Error 500 por usuario sin rol | Medio |
| AT-42 | Validación inconsistente UnitPrice | Bajo |

### Backend — Deuda Técnica

| ID | Hallazgo | Severidad |
|----|----------|-----------|
| AT-03 | Sin repositorio genérico por entidad | Medio |
| AT-05 | Password policy débil | Medio |
| AT-28 | Backend no soporta autenticación por cookies | Alto |
| AT-29 | Sin medidas avanzadas de seguridad en cookies | Medio |
| AT-10 | BillingAddress.HasPrecision en string | Bajo |
| AT-11 | Order.Date.HasMaxLength en DateTime | Bajo |
| AT-14 | Typo TotatAmount | Medio |
| AT-26 | LocalDB no apto para producción | Alto |
| AT-24 | Inconsistencia en tipos de excepciones | Bajo |
| AT-08 | BaseController.cs código muerto | Bajo |
| AT-23 | Typo en nombre de archivo | Bajo |
| AT-32 | DateTime.Now vs UtcNow | Medio |
| AT-34 | Controller accede a Repository directamente | Medio |
| AT-35 | DI inconsistente entre archivos | Bajo |
| AT-36 | CustomerValidator nunca se invoca | Medio |
| AT-37 | Mapeo DTO repetido (DRY) | Medio |
| AT-38 | TotalAmount computed no persistida | Medio |
| AT-39 | Endpoints idénticos | Bajo |
| AT-40 | Soft delete irreversible | Medio |
| AT-41 | Sin logger en servicio crítico | Medio |
| AT-44 | Usings innecesarios | Bajo |
| AT-46 | CORS hardcodeado | Medio |
| AT-47 | Respuesta string plano | Bajo |
| AT-50 | Sin pruebas unitarias | Alto |
| AT-51 | Servicios registrados sin interfaz | Medio |
| AT-52 | LoginModel/RegisterModel sin Data Annotations | Medio |
| AT-53 | User enumeration por timing | Medio |

### Backend — Optimización

| ID | Hallazgo | Severidad |
|----|----------|-----------|
| AT-30 | Paginación en memoria | Alto |
| AT-31 | Race condition en stock sin transacción | Alto |
| AT-33 | Sin AsNoTracking en lecturas | Medio |

### Frontend — Bugs

| ID | Hallazgo | Severidad |
|----|----------|-----------|
| AT-15 | createOrder.js rompe contrato | Medio |
| AT-19 | listServices.js fallback fetch | Medio |
| AT-53 | searchTerm no en deps useEffect (UserPage) | Alto |
| AT-64 | searchTerm no en deps useEffect (OrdersPage) | Alto |
| AT-56 | useDeleteQuantity closure stale | Medio |
| AT-57 | Contrato inconsistente register | Medio |
| AT-72 | Token expirado no se detecta | Medio |
| AT-73 | register no auto-loguea | Medio |

### Frontend — Deuda Técnica

| ID | Hallazgo | Severidad |
|----|----------|-----------|
| AT-04 | Token en localStorage | Alto |
| AT-16 | useCart no es shared state | Alto |
| AT-17 | window.dispatchEvent anti-pattern | Medio |
| AT-18 | Interceptor con window.location.href | Medio |
| AT-20 | withCredentials innecesario | Bajo |
| AT-21 | Imágenes hardcoded | Bajo |
| AT-22 | SweetAlert2 no usado | Bajo |
| AT-54 | useNavigate import no usado | Bajo |
| AT-55 | setTimeout sin cleanup | Medio |
| AT-58 | Duplicación ListProducts pages | Alto |
| AT-59 | Código duplicado menú+modales | Alto |
| AT-62 | Button.jsx if vacío | Bajo |
| AT-63 | URLs API inconsistentes | Bajo |
| AT-66 | SVG icons inline duplicados | Bajo |
| AT-67 | Estilos globales colisionan | Medio |
| AT-68 | select sin label/aria-label | Medio |
| AT-69 | Inputs sin aria-label | Medio |
| AT-70 | Modal sin focus trap/Escape | Medio |
| AT-74 | navigate nunca se ejecuta | Bajo |
| AT-75 | body text-[2rem] en mobile | Medio |
| AT-76 | Dashboard sidebar sin overlay | Medio |

### Frontend — Optimización

| ID | Hallazgo | Severidad |
|----|----------|-----------|
| AT-60 | Sin React.memo/useMemo/useCallback | Medio |
| AT-61 | totalItems/totalAmount sin useMemo | Bajo |
| AT-65 | fetchOrders sin useCallback | Bajo |
| AT-71 | Home descarga 20 órdenes innecesariamente | Bajo |
