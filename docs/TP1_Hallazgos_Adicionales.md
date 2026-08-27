# TP1 — Hallazgos Adicionales: Bugs y Deuda Técnica

**Proyecto:** E-commerce ICS (React 19 + .NET 8 + SQL Server)
**Fecha:** 2026
**Referencia:** Ver `TP1_Resuelto.md` para las 10 deudas técnicas destacadas (presentación TP1)

---

## 1. Backend

### 1.1 Bugs

---

##### BUG AT-27 — Broken Object-Level Authorization en creación de órdenes (CRÍTICO)

**Ubicación:** `OrderController.cs` (AddOrder) y `OrdersManagementServices.cs` (AddOrder)

**Tipo:** Seguridad (Critical — BOLA — OWASP API Security Top 10)

**Descripción:**
El endpoint `POST /api/orders` recibe un `CustomerId` en el cuerpo del request. Ni el controller ni el servicio validan que ese `CustomerId` coincida con el usuario autenticado. El JWT contiene un claim `"id"` con el userId (`JwtTokenService.cs` línea 36: `new Claim("id", userId)`), pero nadie lo extrae ni lo valida.

**Evidencia:**
```csharp
// OrderController.cs — AddOrder recibe el CustomerId del body sin validarlo contra el JWT
[HttpPost]
[Authorize(Roles = "Admin, User")]
public async Task<IActionResult> AddOrder([FromBody] OrderModel.RequestOrderModel request)
{
    var Order = await _service.AddOrder(request); // request.CustomerId viene del body
    ...
}

// OrdersManagementServices.cs — Solo valida que el customer exista, no que sea el del token
var customer = await _repository.GetById<Customer>(request.CustomerId);
if (customer == null)
    throw new EntityNotFoundException($"Customer con ID {request.CustomerId} not found.");
```

**Consecuencia:** Cualquier usuario autenticado (rol "User") puede crear órdenes a nombre de cualquier otro cliente simplemente pasando un `customerId` diferente en el body.

**Recomendación:** En `AddOrder`, leer el `CustomerId` del claim del JWT (`User.FindFirst("id")`) en vez de confiar en el valor del body, o agregar una verificación explícita de rol antes de aceptar un `CustomerId` distinto al del token.

---

##### BUG AT-25 — OrderItemValidator no se llama (ALTO)

**Ubicación:** `OrdersManagementServices.cs` línea 114

**Tipo:** Bug (High — Validation Bypass)

**Descripción:**
El `OrderItemValidator` existe pero nunca se invoca desde `OrderValidator.Validate()` ni desde `OrdersManagementService.AddOrder()`. La validación de items individuales se salta.

**Consecuencia:** Se pueden crear `OrderItems` con `Quantity = 0` o `Quantity` negativa. Si `Quantity = 0`, se crea un item sin sentido y se descuenta 0 stock. Si `Quantity < 0`, se descuenta stock negativo (el producto ganaría stock).

**Recomendación:** Llamar a `OrderItemValidator.Validate()` por cada item en `OrderValidator` o en el servicio.

---

##### BUG AT-13 — GUIDs duplicados en seed data (MEDIO)

**Ubicación:** `Dsw2025Tpi.Data/Sources/Products.json` líneas 2 y 21

**Tipo:** Bug (Medium — Duplicate Seed Data)

**Descripción:**
Dos productos en `Products.json` tienen el mismo GUID: `b9ed5544-42dc-439c-b4d7-15e720089caa`. Cuando `AddRange` recibe dos entidades con el mismo `Id` (primary key), EF Core lanza `InvalidOperationException`.

**Consecuencia:** La aplicación crashea al iniciar si la tabla Products está vacía (primera ejecución o después de un `dotnet ef database drop`).

**Recomendación:** Asignar GUIDs únicos a cada producto en el seed data.

---

##### BUG AT-12 — Sin unique index en Product.Sku (ALTO)

**Ubicación:** `Dsw2025TpiContext.cs` líneas 66-68

**Tipo:** Bug (High — Race Condition)

**Descripción:**
El servicio verifica unicidad de SKU en código (`ProductsManagementService.AddProduct()` línea 47-48: `_repository.First<Product>(p => p.Sku == request.Sku)`), pero no hay constraint de unique en la base de datos. Dos requests concurrentes pueden pasar la verificación al mismo tiempo e insertar productos con el mismo SKU.

**Consecuencia:** Race condition: dos productos con mismo SKU en la BD bajo carga concurrente.

**Recomendación:** Agregar `entity.HasIndex(p => p.Sku).IsUnique()` en `OnModelCreating`.

---

##### BUG AT-15 — createOrder.js rompe contrato {data, error} (MEDIO)

**Ubicación:** Frontend — `orders/services/createOrder.js`

**Tipo:** Calidad (Medium — Inconsistent Contract)

**Descripción:**
Todos los servicios retornan `{data, error}` vía `handleApiCall`. `createOrder.js` retorna solo `{data}` en éxito y `{error}` en fallo, sin la clave `error` en éxito ni `data` en fallo.

**Evidencia:**
```javascript
// createOrder.js
return { data };        // éxito: falta error: null
return { error };       // fallo: falta data: null

// Otros servicios (estándar)
return { data: response.data, error: null };    // éxito
return { data: null, error: { message: ... } };  // fallo
```

**Consecuencia:** Funciona por accidente (undefined es falsy) pero romperá si alguien agrega chequeo explícito `error === null`.

**Recomendación:** Unificar todos los servicios para usar `handleApiCall` consistentemente.

---

### 1.2 Deuda Técnica

---

##### Deuda AT-03 — Sin repositorio genérico por entidad (MEDIO)

**Ubicación:** `Dsw2025Tpi.Domain/Interfaces/IRepository.cs`, `Dsw2025Tpi.Data/Repositories/EfRepository.cs`

**Tipo:** Arquitectura (Medium — Generic Repository Limitation)

**Descripción:**
Existe un solo repositorio genérico `EfRepository<T>` que funciona para todas las entidades. Si bien es funcional, no permite personalizar consultas específicas por entidad (ej: búsquedas avanzadas de productos, historial de órdenes con includes, etc.). Cada entidad tendría su propio repositorio con queries optimizadas.

**Consecuencias:**
- Queries complejas se implementan en el servicio de aplicación, no en el repositorio
- No se pueden optimizar includes/filtrado por entidad
- Viola el patrón Repository específico que permite encapsular lógica de acceso a datos por entidad

**Recomendación:** Crear repositorios específicos (`IProductRepository`, `IOrderRepository`, `ICustomerRepository`) que hereden de `IRepository<T>` y agreguen métodos propios. Mantener el genérico para operaciones CRUD simples.

---

##### Deuda AT-05 — Password policy débil (MEDIO)

**Ubicación:** `Program.cs` líneas 74-77

**Tipo:** Seguridad (Medium — Weak Password Policy)

**Descripción:**
La política de contraseñas solo requiere longitud 8, sin reglas de complejidad.

**Evidencia:**
```csharp
options.Password.RequiredLength = 8;
// No hay: RequireDigit, RequireLowercase, RequireUppercase, RequireNonAlphanumeric
```

**Consecuencia:** Contraseñas débiles como "12345678" son válidas.

**Recomendación:** Agregar `RequireDigit`, `RequireUppercase`, `RequireLowercase`, `RequireNonAlphanumeric`.

---

##### Deuda AT-28 — Backend no soporta autenticación por cookies (ALTO)

**Ubicación:** `AuthenticateController.cs`, `Program.cs` (configuración JWT)

**Tipo:** Seguridad / Arquitectura (High — Backend Cookie Support Missing)

**Descripción:**
El backend está configurado exclusivamente para autenticación vía header `Authorization: Bearer <token>`. No tiene configuración para recibir tokens en cookies HttpOnly. Si se migra el frontend a cookies (AT-04), el backend debe adaptarse para:
1. **Enviar** el token como cookie en el endpoint de login (`Set-Cookie` header).
2. **Leer** el token de la cookie en los endpoints protegidos (en vez del header Authorization).
3. **Configurar** los CORS para permitir credenciales (`Access-Control-Allow-Credentials: true`).

**Evidencia:**
```csharp
// AuthenticateController.cs — Actualmente retorna el token en el body JSON
return Ok(new { token = tokenString, expiration = expiration });

// Program.cs — CORS no permite credenciales
builder.Services.AddCors(options => {
    options.AddPolicy("AllowFrontend", policy => {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
              // Falta: .AllowCredentials()
    });
});
```

**Consecuencia:** Sin este cambio en el backend, la migración a cookies del frontend (AT-04) no funcionará.

**Recomendación:**
1. En `AuthenticateController.Login`: configurar `Response.Cookies.Append()` con `HttpOnly = true`, `Secure = true`, `SameSite = Strict`.
2. Crear middleware o filtro que lea el JWT de la cookie cuando el header Authorization esté vacío.
3. Agregar `.AllowCredentials()` al CORS policy.

**Impacto:** Alto | **Esfuerzo:** Medio (~2-3 horas)

---

##### Deuda AT-29 — Sin medidas avanzadas de seguridad en cookies (MEDIO)

**Ubicación:** A configurar al implementar AT-04 y AT-28

**Tipo:** Seguridad (Medium — Cookie Security Hardening)

**Descripción:**
Usar cookies para JWT no es automáticamente seguro. Se deben aplicar múltiples capas de protección para mitigar riesgos como CSRF,窃听, y session fixation:

**Medidas necesarias:**
1. **HttpOnly:** Impide acceso desde JavaScript (mitiga XSS).
2. **Secure:** Solo envía la cookie por HTTPS (mitiga interceptación de tráfico).
3. **SameSite=Strict/Lax:** Mitiga CSRF al no enviar la cookie en requests cross-origin.
4. **Token de corta duración + Refresh Token:** Reduce la ventana de exposición si el token es comprometido.
5. **Token de-refresh en cookie separada:** El refresh token también debe ser HttpOnly y Secure.
6. **Revocación de tokens:** Mecanismo para invalidar tokens al cerrar sesión (blacklist o control por BD).

**Consecuencia:** Sin estas medidas, migrar a cookies puede dar una falsa sensación de seguridad mientras otros vectores de ataque permanecen abiertos.

**Recomendación:** Implementar progresivamente:
- Fase 1: HttpOnly + Secure + SameSite (básico)
- Fase 2: Refresh token en cookie separada
- Fase 3: Token revocation黑名单 o session tracking en BD

**Impacto:** Medio | **Esfuerzo:** Alto (~4-6 horas para implementación completa)

---

##### Deuda AT-10 — BillingAddress.HasPrecision(15,2) en campo string (BAJO)

**Ubicación:** `Dsw2025TpiContext.cs` línea 46

**Tipo:** Calidad (Low — Semantic Error)

**Descripción:**
El campo `BillingAddress` es de tipo `string` pero tiene configurado `HasPrecision(15,2)`, que es para campos numéricos/decimales. SQL Server ignora esta configuración en columnas `nvarchar(max)`.

**Consecuencia:** La aplicación funciona correctamente en runtime. Es una configuración semántica incorrecta (code smell) pero no causa comportamiento anormal.

**Recomendación:** Eliminar `HasPrecision(15,2)` de `BillingAddress`. Solo mantener `HasMaxLength(60)`.

---

##### Deuda AT-11 — Order.Date.HasMaxLength(10) en campo DateTime (BAJO)

**Ubicación:** `Dsw2025TpiContext.cs` línea 41

**Tipo:** Calidad (Low — Meaningless Configuration)

**Descripción:**
El campo `Date` es de tipo `DateTime` pero tiene `HasMaxLength(10)`, que es sin sentido para un campo de fecha. SQL Server ignora `maxLength` en columnas `datetime2`.

**Consecuencia:** La aplicación funciona correctamente en runtime. Configuración sin efecto práctico, genera confusión.

**Recomendación:** Eliminar `HasMaxLength(10)` de `Order.Date`.

---

##### Deuda AT-14 — Typo TotatAmount en DTO y frontend (MEDIO)

**Ubicación:** `Application/Dtos/OrderModel.cs` línea 13, Frontend `orders/pages/ListOrdersPage.jsx` línea 116

**Tipo:** Calidad (Medium — Typo Propagated)

**Descripción:**
El DTO tiene la propiedad llamada `TotatAmount` en lugar de `TotalAmount`. El frontend trabaja con este typo.

**Consecuencia:** Código confuso para nuevos desarrolladores. El typo se propaga entre frontend y backend.

**Recomendación:** Renombrar a `TotalAmount` en ambos lados.

---

##### Deuda AT-26 — Base de datos LocalDB no apta para producción (ALTO)

**Ubicación:** `Dsw2025Tpi.Api/appsettings.json` línea 2, `DB_Config.md`

**Tipo:** Infraestructura (High — Not Production Ready)

**Descripción:**
El backend usa `(localdb)\MSSQLLocalDB` con Integrated Security. LocalDB tiene limitaciones significativas: solo Windows, un usuario a la vez, inestable, sin autenticación por usuario, no apto para Docker, 10 GB max.

**Consecuencia:** Imposible desplegar en Docker, colaboradores con Mac/Linux no pueden ejecutar el backend, inestabilidad en desarrollo diario.

**Recomendación:** Migrar a Docker SQL Server 2022 (`mcr.microsoft.com/mssql/server:2022-latest`) con autenticación SQL.

---

##### Deuda AT-24 — Inconsistencia en tipos de excepciones entre validadores (BAJO)

**Ubicación:** `Dsw2025Tpi.Application/Validation/`

**Tipo:** Calidad (Low — Inconsistent Error Handling)

**Descripción:**
Cada validador lanza un tipo de excepción diferente:
- `ProductValidator`: `ApplicationException` para la mayoría, `ArgumentException` para stock
- `OrderValidator`: `InvalidOperationException`
- `CustomerValidator`: `InvalidOperationException`
- `OrderItemValidator`: nunca se llama desde `OrderValidator`

**Consecuencia:** El controller tiene que catchear múltiples tipos de excepción. Lógica de manejo de errores dispersa e inconsistente.

**Recomendación:** Estandarizar un solo tipo de excepción de validación (ej: `ValidationException`) o usar el `ApplicationException` existente.

---

##### Deuda AT-08 — BaseController.cs es código muerto (BAJO)

**Ubicación:** `Dsw2025Tpi.Api/Controllers/BaseController.cs`

**Tipo:** Calidad (Low — Dead Code)

**Descripción:**
`BaseController` es una clase abstracta que hereda de `Controller` y sobreescribe `OnActionExecuted`. Ningún controller del proyecto la usa — todos heredan de `ControllerBase`.

**Consecuencia:** Código muerto que genera confusión.

**Recomendación:** Eliminar `BaseController.cs`.

---

##### Deuda AT-23 — Typo en nombre de archivo ProducstManagementServices (BAJO)

**Ubicación:** `Dsw2025Tpi.Application/Services/ProducstManagementServices.cs`

**Tipo:** Calidad (Low — Filename Typo)

**Descripción:**
El archivo se llama `ProducstManagementServices.cs` en lugar de `ProductsManagementServices.cs`.

**Consecuencia:** Confusión al buscar archivos.

**Recomendación:** Renombrar el archivo.

---

## 2. Frontend

### 2.1 Bugs

---

##### Bug AT-19 — listServices.js tiene fallback fetch mixto con Axios (MEDIO)

**Ubicación:** Frontend — `orders/services/listServices.js` líneas 16-38

**Tipo:** Calidad (Medium — Inconsistent Pattern)

**Descripción:**
Este servicio tiene un fallback manual de `fetch()` después de que Axios falla. Ningún otro servicio hace esto. Es probablemente un artefacto de debugging.

**Consecuencia:** Dos patrones de acceso a datos en el mismo proyecto. El fallback no usa `handleApiCall`, retorna forma diferente de datos. En producción puede fallar si no hay proxy de Vite.

**Recomendación:** Eliminar el fallback fetch. Usar solo la instancia de Axios.

---

### 2.2 Deuda Técnica

---

##### Deuda AT-20 — withCredentials innecesario en Axios (BAJO)

**Ubicación:** Frontend — `shared/api/axiosInstance.js` línea 5

**Tipo:** Seguridad (Low — Unnecessary Configuration)

**Descripción:**
La instancia Axios se configura con `withCredentials: true`, pero el backend usa Bearer token, no cookies.

**Consecuencia:** Envío innecesario de cookies, potencial vector CSRF.

**Recomendación:** Eliminar `withCredentials: true` o hacerlo condicional.

---

##### Deuda AT-21 — Imágenes hardcoded externas y base64 inline (BAJO)

**Ubicación:** Frontend — `UserHeaderMenu.jsx` línea 88, `MobileSideMenu.jsx` línea 32, `CartPage.jsx` línea 101, `ListProductsUserPage.jsx` línea 16

**Tipo:** Calidad (Low — External Dependencies)

**Descripción:**
Hay imágenes de CDN externo (freepik, flaticon) y un base64 de 400+ chars inline en el componente.

**Consecuencia:** No funciona offline. Dependencia de CDNs de terceros. Bundle inflado por base64 inline.

**Recomendación:** Mover todas las imágenes a `assets/` local.

---

##### Deuda AT-22 — SweetAlert2 instalado pero nunca usado (BAJO)

**Ubicación:** Frontend — `package.json`

**Tipo:** Calidad (Low — Dead Dependency)

**Descripción:**
`SweetAlert2` está en las dependencias pero nunca se importa en ningún archivo del source.

**Consecuencia:** Bundle size inflado innecesariamente.

**Recomendación:** Eliminar de `package.json`: `npm uninstall sweetalert2`.

---

## 3. Resumen Clasificado

### Backend — Bugs

| ID | Hallazgo | Severidad |
|----|----------|-----------|
| AT-27 | BOLA en creación de órdenes | Crítico |
| AT-13 | GUIDs duplicados en seed data | Medio |
| AT-12 | Sin unique index en Product.Sku | Alto |
| AT-25 | OrderItemValidator no se llama | Alto |
| AT-15 | createOrder.js inconsistente | Medio |

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

### Frontend — Bugs

| ID | Hallazgo | Severidad |
|----|----------|-----------|
| AT-19 | listServices.js fallback fetch mixto | Medio |

### Frontend — Deuda Técnica

| ID | Hallazgo | Severidad |
|----|----------|-----------|
| AT-20 | withCredentials innecesario | Bajo |
| AT-21 | Imágenes hardcoded externas | Bajo |
| AT-22 | SweetAlert2 no usado | Bajo |
