# Skill: Code Quality — Principios y Buenas Practicas

Este skill define los principios de diseno y buenas practicas que el Builder debe aplicar y el Reviewer debe verificar.

---

## 1. SOLID

### S — Single Responsibility Principle (SRP)

Un modulo, clase o funcion debe tener una unica razon para cambiar.

**Backend:**
```csharp
// MAL: Un servicio hace todo
class OrderService {
    CreateOrder() { ... }
    SendEmail() { ... }
    GenerateReport() { ... }
}

// BIEN: Cada servicio tiene una responsabilidad
class OrdersManagementService { CreateOrder() { ... } }
class EmailService { SendEmail() { ... } }
class ReportService { GenerateReport() { ... } }
```

**Frontend:**
```jsx
// MAL: Un componente maneja UI + logica + datos
function ProductPage() {
  const [products, setProducts] = useState([]);
  const fetchProducts = async () => { ... };
  const handleDelete = async (id) => { ... };
  return <div>...</div>;
}

// BIEN: Separar concerns
function ProductPage() {
  const { products, fetchProducts, handleDelete } = useProducts();
  return <ProductList products={products} onDelete={handleDelete} />;
}
```

### O — Open/Closed Principle (OCP)

Abierto a extension, cerrado a modificacion.

```csharp
// MAL: Modificar clase existente para nuevo tipo
public class NotificationService {
    public void Send(string type, string message) {
        if (type == "email") { ... }
        else if (type == "sms") { ... }  // Agregar caso = modificar clase
    }
}

// BIEN: Extender via interfaces
public interface INotificationSender {
    Task SendAsync(string message);
}

public class EmailSender : INotificationSender { ... }
public class SmsSender : INotificationSender { ... }
```

### L — Liskov Substitution Principle (LSP)

Subtipos deben ser sustituibles por sus tipos base.

```csharp
// BIEN: Cualquier IRepository<T> funciona
public async Task<Product> GetProduct(Guid id) {
    return await _repository.GetById<Product>(id);  // No importa la implementacion
}
```

### I — Interface Segregation Principle (ISP)

Interfaces pequenas y especificas, no interfaces monoliticas.

```csharp
// MAL: Interfaz gigante
public interface IRepository<T> {
    Task<T> GetById(Guid id);
    Task<IEnumerable<T>> GetAll();
    Task<T> Add(T entity);
    Task<T> Update(T entity);
    Task Delete(Guid id);
    Task<int> Count();
    Task<bool> Any(Expression<Func<T, bool>> predicate);
    // ... 20 metodos mas
}

// BIEN: Interfaces segregadas
public interface IReadRepository<T> {
    Task<T?> GetById(Guid id);
    Task<IEnumerable<T>> GetAll();
}

public interface IWriteRepository<T> {
    Task<T> Add(T entity);
    Task<T> Update(T entity);
    Task Delete(Guid id);
}
```

### D — Dependency Inversion Principle (DIP)

Depender de abstracciones, no de concreciones.

```csharp
// MAL: Depende de implementacion concreta
public class ProductsController {
    private readonly EfRepository<Product> _repo;  // Concreta
}

// BIEN: Depende de abstraccion
public class ProductsController {
    private readonly IRepository<Product> _repo;  // Interfaz
}
```

---

## 2. DRY (Don't Repeat Yourself)

No duplicar logica. Si se repite 2+ veces, extraer a metodo/componente/function.

**Backend:**
```csharp
// MAL: Mapeo repetido 4 veces
var response = new ResponseOrderModel {
    Id = order.Id,
    TotalAmount = order.TotalAmount,
    Status = order.Status.ToString()
};

// BIEN: Metodo centralizado
public static ResponseOrderModel MapToResponse(Order order) {
    return new ResponseOrderModel {
        Id = order.Id,
        TotalAmount = order.TotalAmount,
        Status = order.Status.ToString()
    };
}
```

**Frontend:**
```jsx
// MAL: Logica de fetch repetida en cada pagina
function ProductsPage() {
  const [data, setData] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  useEffect(() => {
    fetch('/api/products')
      .then(res => res.json())
      .then(setData)
      .catch(setError)
      .finally(() => setLoading(false));
  }, []);
}

// BIEN: Hook reutilizable
function useFetch(url) {
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  useEffect(() => {
    fetch(url)
      .then(res => res.json())
      .then(setData)
      .catch(setError)
      .finally(() => setLoading(false));
  }, [url]);
  return { data, loading, error };
}
```

---

## 3. KISS (Keep It Simple, Stupid)

Soluciones simples. Si una solucion es complicada, probablemente hay una mas simple.

```csharp
// MAL: Solucion compleja
public bool IsValidEmail(string email) {
    var regex = new Regex(@"^[\w!#$%&'*+/=?^`{|}~-]+(?:\.[\w!#$%&'*+/=?^`{|}~-]+)*@(?:[\w](?:[\w-]*[\w])?\.)+[\w](?:[\w-]*[\w])?$");
    return regex.IsMatch(email) && email.Length <= 320 && !email.Contains("..");
}

// BIEN: Solucion simple
public bool IsValidEmail(string email) {
    return !string.IsNullOrWhiteSpace(email)
        && email.Length <= 320
        && email.Contains("@")
        && email.Contains(".");
}
```

---

## 4. YAGNI (You Aren't Gonna Need It)

No implementar funcionalidad que no se necesita ahora.

```csharp
// MAL: Implementar refresh tokens cuando nadie los pidio
public class RefreshTokenService { ... }

// BIEN: Solo implementar lo que se necesita
// Si en el futuro se necesitan refresh tokens, se implementa entonces
```

---

## 5. Clean Architecture

```
Api -> Application -> Domain <- Data
```

**Reglas estrictas:**
- Api referencia a Application y Domain
- Application solo referencia a Domain
- Data referencia a Domain e implementa interfaces
- Domain NO referencia a nada (es el nucleo puro)

```csharp
// MAL: Application referencia a Data
// Dsw2025Tpi.Application.csproj:
//   <ProjectReference Include="..\Dsw2025Tpi.Data\..." />  // VIOLACION

// BIEN: Application solo refiere a Domain
// Dsw2025Tpi.Application.csproj:
//   <ProjectReference Include="..\Dsw2025Tpi.Domain\..." />
```

---

## 6. Error Handling Consistente

**Backend:**
```csharp
// Usar excepciones custom, no Exception generica
throw new EntityNotFoundException("Product", id);
throw new DuplicatedEntityException("SKU", sku);
throw new ApplicationException("message");

// Retornar ProblemDetails en errores
return Problem(
    detail: "Product not found",
    statusCode: 404,
    title: "Not Found"
);
```

**Frontend:**
```javascript
// Contrato consistente {data, error}
const result = await apiCall();
if (result.error) {
    setError(result.error);
} else {
    setData(result.data);
}
```

---

## 7. Naming Conventions

### Backend (C#)
| Tipo | Convencion | Ejemplo |
|---|---|---|
| Clases | PascalCase | `ProductsManagementService` |
| Metodos | PascalCase | `GetProductById()` |
| Propiedades | PascalCase | `CurrentUnitPrice` |
| Variables | camelCase | `productName` |
| Parametros | camelCase | `productId` |
| Interfaces | I prefix | `IRepository<T>` |
| Enums | PascalCase | `OrderStatus` |
| Constants | PascalCase | `MaxRetries` |

### Frontend (JavaScript/JSX)
| Tipo | Convencion | Ejemplo |
|---|---|---|
| Componentes | PascalCase | `LoginForm.jsx` |
| Functions | camelCase | `fetchProducts()` |
| Variables | camelCase | `productName` |
| Constants | UPPER_SNAKE | `API_BASE_URL` |
| Hooks | use prefix | `useCart()`, `useAuth()` |
| Context | Context suffix | `AuthContext`, `CartContext` |

---

## 8. Security Checklist

### Backend
- [ ] Secrets en variables de entorno, nunca en appsettings.json
- [ ] Password policy: RequireDigit, RequireUppercase, RequireLowercase, RequireNonAlphanumeric
- [ ] Rate limiting en endpoints de autenticacion
- [ ] Validacion de inputs en servicios
- [ ] Parameterized queries via EF Core
- [ ] BOLA: CustomerId del JWT, no del body
- [ ] CORS configurable por entorno
- [ ] HTTPS redirect

### Frontend
- [ ] Token en HttpOnly cookie (no localStorage)
- [ ] No exponer secrets en codigo fuente
- [ ] Validacion de inputs antes de enviar
- [ ] Manejo correcto de errores 401/403
- [ ] No usar withCredentials sin cookies

---

## 9. React Best Practices

```jsx
// 1. useEffect con dependencias correctas
useEffect(() => {
    fetchProducts(searchTerm);
}, [searchTerm]);  // SIEMPRE incluir dependencias

// 2. Cleanup en useEffect
useEffect(() => {
    const timer = setTimeout(() => { ... }, 2000);
    return () => clearTimeout(timer);  // SIEMPRE cleanup
}, []);

// 3. Memoizacion cuando sea necesario
const totalItems = useMemo(
    () => cart.reduce((sum, item) => sum + item.quantity, 0),
    [cart]
);

const handleDelete = useCallback((id) => {
    setItems(prev => prev.filter(item => item.id !== id));
}, []);

// 4. No usar window.dispatchEvent
// MAL: window.dispatchEvent(new Event('open-login'))
// BIEN: Context o estado levantado

// 5. Componentes pequenos y enfocados
// Un componente = una responsabilidad
```

---

## 10. Checklist General para Reviewer

### Codigo
- [ ] Naming claro y consistente
- [ ] Sin magic numbers/strings
- [ ] Sin imports no usados
- [ ] Sin variables no usadas
- [ ] Funciones/metodos cortos (< 50 lineas)
- [ ] Sin anidamiento profundo (> 3 niveles)
- [ ] Comentarios solo en codigo complejo

### Arquitectura
- [ ] Clean Architecture respetada
- [ ] Patrones de diseno aplicados correctamente
- [ ] SOLID/DRY/KISS/YAGNI verificados
- [ ] No hay dependencias circulares

### Seguridad
- [ ] Secrets fuera del codigo fuente
- [ ] Inputs validados
- [ ] Autorizacion en endpoints
- [ ] No hay BOLA

### Performance
- [ ] No hay N+1 queries
- [ ] Paginacion en BD (no en memoria)
- [ ] AsNoTracking en lecturas
- [ ] Memoizacion en React cuando aplica
