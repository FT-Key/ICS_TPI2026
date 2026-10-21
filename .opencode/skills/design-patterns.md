# Skill: Design Patterns para C# y React

Este skill define los patrones de diseno que el Builder debe aplicar y el Reviewer debe verificar.

---

## 1. Repository Pattern

**Donde:** Backend — Capa Data y Domain

**Objetivo:** Abstraer el acceso a datos, desacoplar la logica de persistencia.

### Implementacion

```csharp
// Domain/Interfaces/IRepository.cs
public interface IRepository<T> where T : EntityBase
{
    Task<T?> GetById(Guid id);
    Task<IEnumerable<T>> GetAll();
    Task<T> Add(T entity);
    Task<T> Update(T entity);
    Task Delete(Guid id);
}
```

### Reglas
- SIEMPRE usar IRepository<T> en servicios, nunca el DbContext directamente
- Crear repositorios especificos cuando se necesiten queries custom (IProductRepository, IOrderRepository)
- Los repositorios especificos heredan de IRepository<T>

---

## 2. Unit of Work Pattern

**Donde:** Backend — Capa Data

**Objetivo:** Atomicidad en operaciones multi-tabla.

### Implementacion

```csharp
// Domain/Interfaces/IUnitOfWork.cs
public interface IUnitOfWork : IDisposable
{
    IRepository<T> Repository<T>() where T : EntityBase;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
```

### Reglas
- SIEMPRE usar UoW en operaciones que tocan multiples tablas (crear orden + descontar stock)
- SaveChangesAsync centralizado, no por cada operacion individual
- Dispose al finalizar

---

## 3. Strategy Pattern

**Donde:** Backend — Filtros de busqueda, Frontend — Logica condicional

**Objetivo:** Variabilidad en algoritmos sin modificar el contexto.

### Ejemplo Backend

```csharp
// Estrategias de filtrado para productos
public interface IProductFilterStrategy
{
    IQueryable<Product> Apply(IQueryable<Product> query, string searchTerm);
}

public class ProductNameFilter : IProductFilterStrategy { ... }
public class ProductSkuFilter : IProductFilterStrategy { ... }
```

### Ejemplo Frontend

```javascript
// Estrategias de validacion
const validationStrategies = {
  email: (value) => /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value),
  phone: (value) => /^\d{10,15}$/.test(value),
  required: (value) => value.trim().length > 0,
};
```

### Reglas
- Usar cuando hay multiples variantes del mismo comportamiento
- No usar para casos simples (un if/else es suficiente)

---

## 4. Dependency Injection Pattern

**Donde:** Backend — Todo el proyecto, Frontend — React Context

**Objetivo:** Desacoplamiento, testabilidad, flexibilidad.

### Backend

```csharp
// Program.cs o ServiceCollectionExtensions.cs
services.AddScoped<IProductsManagementService, ProductsManagementService>();
services.AddScoped<IOrdersManagementService, OrdersManagementService>();
services.AddScoped<IUnitOfWork, UnitOfWork>();
services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
```

### Frontend

```jsx
// Context como DI container
<AuthProvider>    {/* Proporciona auth */}
<CartProvider>    {/* Proporciona carrito */}
<ModalProvider>   {/* Proporciona modales */}
```

### Reglas
- Backend: SIEMPRE registrar por interfaz, nunca por tipo concreto
- Frontend: Usar Context para estado global, no props drilling profundo
- No inyectar dependencias circulares

---

## 5. Middleware Pattern

**Donde:** Backend — Capa API

**Objetivo:** Cross-cutting concerns (exceptions, logging, auth, rate limiting).

### Implementacion

```csharp
// Middleware de excepciones globales
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // Manejar excepcion y retornar ProblemDetails
        }
    }
}

// Registro en Program.cs
app.UseMiddleware<GlobalExceptionMiddleware>();
```

### Reglas
- SIEMPRE usar para exception handling global
- Orden correcto: Exception -> Logging -> Auth -> CORS -> Controllers
- No poner logica de negocio en middleware

---

## 6. Factory Pattern

**Donde:** Backend — Creacion de DTOs, Frontend — Creacion de componentes

**Objetivo:** Centralizar creacion de objetos complejos.

### Backend

```csharp
// Factory para crear DTOs de respuesta
public static class OrderResponseFactory
{
    public static ResponseOrderModel CreateFromEntity(Order order)
    {
        return new ResponseOrderModel
        {
            Id = order.Id,
            TotalAmount = order.TotalAmount,
            Status = order.Status.ToString(),
            Items = order.OrderItems.Select(oi => OrderItemResponseFactory.Create(oi)).ToList()
        };
    }
}
```

### Frontend

```jsx
// Factory para crear endpoints
const createApiEndpoint = (baseUrl) => ({
  get: (params) => axios.get(`${baseUrl}`, { params }),
  post: (data) => axios.post(`${baseUrl}`, data),
  put: (id, data) => axios.put(`${baseUrl}/${id}`, data),
  delete: (id) => axios.delete(`${baseUrl}/${id}`),
});
```

### Reglas
- Usar cuando la creacion de objetos es compleja o se repite
- No usar para objetos simples

---

## 7. Context Pattern (React)

**Donde:** Frontend — Estado global

**Objetivo:** Estado compartido sin props drilling.

### Implementacion

```jsx
// CartContext.jsx
const CartContext = createContext();

export function CartProvider({ children }) {
  const [cart, setCart] = useState(() => {
    const saved = localStorage.getItem('cart');
    return saved ? JSON.parse(saved) : [];
  });

  const addItem = (product) => { ... };
  const removeItem = (sku) => { ... };
  const totalItems = cart.reduce((sum, item) => sum + item.quantity, 0);

  return (
    <CartContext.Provider value={{ cart, addItem, removeItem, totalItems }}>
      {children}
    </CartContext.Provider>
  );
}

export const useCart = () => useContext(CartContext);
```

### Reglas
- SIEMPRE usar para estado que comparten multiples componentes
- No usar para estado local de un solo componente
- Un Context = una responsabilidad (no meter todo en un solo Context)

---

## 8. Template Method Pattern

**Donde:** Backend — Servicios con logica comun

**Objetivo:** Reutilizar logica variando pasos especificos.

### Ejemplo

```csharp
// Servicio base con logica comun
public abstract class BaseService<T> where T : EntityBase
{
    protected readonly IRepository<T> _repository;
    protected readonly ILogger _logger;

    protected virtual async Task<T> GetById(Guid id)
    {
        _logger.LogInformation("Getting {Entity} with id {Id}", typeof(T).Name, id);
        var entity = await _repository.GetById(id);
        if (entity == null) throw new EntityNotFoundException(typeof(T).Name, id);
        return entity;
    }

    protected abstract Task Validate(T entity);
}

// Servicio especifico
public class ProductsService : BaseService<Product>
{
    protected override async Task Validate(Product entity)
    {
        if (await _repository.Any(p => p.Sku == entity.Sku && p.Id != entity.Id))
            throw new DuplicatedEntityException("SKU", entity.Sku);
    }
}
```

### Reglas
- Usar cuando hay logica comun entre servicios
- No abusar — si solo hay 2 servicios, no justifica la abstraccion

---

## Checklist para Reviewer

### Backend
- [ ] Repository pattern: servicios usan IRepository, no DbContext
- [ ] Unit of Work: operaciones multi-tabla atomicas
- [ ] DI: servicios registrados por interfaz
- [ ] Middleware: exception handling global
- [ ] Factory: mapeo DTO centralizado
- [ ] Strategy: filtros de busqueda implementados

### Frontend
- [ ] Context: estado global con Context (auth, cart, modales)
- [ ] DI: Context como contenedor de dependencias
- [ ] Factory: creacion de componentes/endpoints centralizada
- [ ] Strategy: validaciones modulares
