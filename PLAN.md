# PLAN: Implementación de SQL Server en el Backend

## Objetivo

Configurar la base de datos SQL Server en el backend para que el repositorio genérico (`EfRepository`) pueda persistir y consultar datos. Incluye creación de entidades, configuración del DbContext, connection string, migraciones y preparación para despliegue en la nube.

---

## Estado Actual del Backend

| Componente | Estado |
|---|---|
| `Dsw2025TpiContext.cs` | Vacío (sin `DbSet<T>`, sin `OnModelCreating`) |
| `appsettings.json` | Sin connection string |
| `Program.cs` | Sin `AddDbContext`, sin DI de repositorios |
| `Domain/Entities/` | Solo `EntityBase` abstracto |
| `Api.csproj` | No referencia al proyecto `Data` |
| Migraciones EF Core | No existen |
| SQL Server | No configurado |

**Resultado:** El `EfRepository` tiene CRUD completo pero es inoperable porque no hay contexto de base de datos configurado.

---

## Entorno Propuesto

| Entorno | Motor | Dónde corre | Uso |
|---|---|---|---|
| **Desarrollo** | SQL Server 2022 | Contenedor Docker local | Desarrollo y pruebas |
| **Producción** | Azure SQL Database | Microsoft Azure | Despliegue en la nube |

**Ventaja:** Ambos usan el mismo provider (`Microsoft.EntityFrameworkCore.SqlServer`) y son compatibles. El mismo código funciona en ambos entornos.

---

## Entidades de Dominio a Crear

Todas heredan de `EntityBase` (Guid Id auto-generado).

### Product

| Propiedad | Tipo | Descripción |
|---|---|---|
| Sku | `string` | Código SKU (formato: SKU-XXXX) |
| InternalCode | `string` | Código interno único |
| Name | `string` | Nombre del producto |
| Description | `string?` | Descripción (opcional) |
| CurrentUnitPrice | `decimal` | Precio unitario actual |
| StockQuantity | `int` | Cantidad en stock |
| IsActive | `bool` | Si está habilitado o no |
| CategoryId | `Guid` | FK a Category |

**Relaciones:**
- Muchos a 1 con `Category`
- 1 a Muchos con `OrderItem`

**Restricciones:**
- `Sku` único (index)
- `InternalCode` único (index)
- `CurrentUnitPrice` >= 0
- `StockQuantity` >= 0

---

### Category

| Propiedad | Tipo | Descripción |
|---|---|---|
| Name | `string` | Nombre de la categoría |
| Description | `string?` | Descripción (opcional) |

**Relaciones:**
- 1 a Muchos con `Product`

---

### User

| Propiedad | Tipo | Descripción |
|---|---|---|
| Username | `string` | Nombre de usuario (login) |
| PasswordHash | `string` | Hash de la contraseña (nunca texto plano) |
| FirstName | `string` | Nombre |
| LastName | `string` | Apellido |
| Role | `string` | Rol: "Admin" o "Client" |

**Relaciones:**
- 1 a Muchos con `Order`
- 1 a Muchos con `PaymentMethod`

**Restricciones:**
- `Username` único (index)
- `PasswordHash` no nulo

---

### Order

| Propiedad | Tipo | Descripción |
|---|---|---|
| OrderNumber | `string` | Número único de orden |
| OrderDate | `DateTime` | Fecha de creación |
| Status | `string` | Estado: Pending, Confirmed, Shipped, Delivered, Cancelled |
| TotalAmount | `decimal` | Monto total |
| UserId | `Guid` | FK a User |

**Relaciones:**
- Muchos a 1 con `User`
- 1 a Many con `OrderItem`
- 1 a 1 con `ShippingAddress` (embebido o entidad separada)
- 1 a 1 con `BillingInfo` (embebido o entidad separada)

**Restricciones:**
- `OrderNumber` único (index)
- `TotalAmount` >= 0

---

### OrderItem

| Propiedad | Tipo | Descripción |
|---|---|---|
| Quantity | `int` | Cantidad |
| UnitPrice | `decimal` | Precio unitario al momento de la compra |
| ProductId | `Guid` | FK a Product |
| OrderId | `Guid` | FK a Order |

**Relaciones:**
- Muchos a 1 con `Product`
- Muchos a 1 con `Order`

**Restricciones:**
- `Quantity` >= 1
- `UnitPrice` >= 0

---

### ShippingAddress (embebido en Order o entidad separada)

| Propiedad | Tipo | Descripción |
|---|---|---|
| Street | `string` | Dirección |
| City | `string` | Ciudad |
| State | `string` | Provincia/Estado |
| ZipCode | `string` | Código postal |
| Country | `string` | País |

**Opción recomendada para TPI:** Embeber como `Owned Entity Type` dentro de `Order` con `OwnsOne(o => o.ShippingAddress)`. Simplifica la DB (una tabla menos) y es suficiente para el alcance del proyecto.

---

### BillingInfo (embebido en Order o entidad separada)

| Propiedad | Tipo | Descripción |
|---|---|---|
| TaxId | `string` | CUIT/CUIL/NIF |
| BillingName | `string` | Nombre/Razón social |
| BillingAddress | `string` | Dirección de facturación |

**Opción recomendada:** Igual que `ShippingAddress`, embeber como `Owned Entity Type`.

---

### PaymentMethod

| Propiedad | Tipo | Descripción |
|---|---|---|
| PaymentType | `string` | Tipo: CreditCard, Debit, Cash, Pix |
| LastFourDigits | `string?` | Últimos 4 dígitos (si aplica) |
| IsDefault | `bool` | Si es el método por defecto |
| UserId | `Guid` | FK a User |

**Relaciones:**
- Muchos a 1 con `User`

---

## Diagrama de Relaciones (ER)

```
User 1───────* Order
                 │
                 │ 1
                 │
OrderItem *─────*
   │
   │ *
Product *─────── 1 Category

User 1───────* PaymentMethod

Order ──────── OwnsOne ──── ShippingAddress
Order ──────── OwnsOne ──── BillingInfo
```

---

## Archivos a Modificar y Crear

### Archivos a CREAR

| Archivo | Descripción |
|---|---|
| `Domain/Entities/Product.cs` | Entidad Product |
| `Domain/Entities/Category.cs` | Entidad Category |
| `Domain/Entities/User.cs` | Entidad User |
| `Domain/Entities/Order.cs` | Entidad Order + ShippingAddress + BillingInfo |
| `Domain/Entities/OrderItem.cs` | Entidad OrderItem |
| `Domain/Entities/PaymentMethod.cs` | Entidad PaymentMethod |
| `docker-compose.yml` | Contenedor SQL Server 2022 |
| `Data/Migrations/` | Generada por EF CLI |

### Archivos a MODIFICAR

| Archivo | Cambio |
|---|---|
| `Data/Dsw2025TpiContext.cs` | Agregar `DbSet<T>` para cada entidad, `OnModelCreating` con Fluent API, constructor con `DbContextOptions` |
| `Api/appsettings.json` | Agregar `ConnectionStrings.DefaultConnection` |
| `Api/appsettings.Development.json` | Agregar connection string para dev |
| `Api/Program.cs` | Agregar `AddDbContext`, registrar DI de repositorios |
| `Api/Dsw2025Tpi.Api.csproj` | Agregar `<ProjectReference>` a `Data` |

---

## Configuración de Connection Strings

### Desarrollo (Docker local)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=Dsw2025Tpi;User Id=sa;Password=YourStrong!Password123;TrustServerCertificate=True;"
  }
}
```

### Producción (Azure SQL)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tu-servidor.database.windows.net;Database=Dsw2025Tpi;User Id=tu-usuario;Password=tu-password;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  }
}
```

**Nota:** En Azure, el password se configura via Variables de Entorno o Azure Key Vault, nunca en el archivo JSON.

---

## Docker Compose (Desarrollo Local)

```yaml
version: '3.8'
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: ics-sqlserver
    environment:
      SA_PASSWORD: "YourStrong!Password123"
      ACCEPT_EULA: "Y"
    ports:
      - "1433:1433"
    volumes:
      - sqlserver-data:/var/opt/mssql

volumes:
  sqlserver-data:
```

**Comandos:**
```bash
docker-compose up -d          # Levantar SQL Server
docker-compose down            # Detener
docker-compose down -v         # Detener y eliminar datos (reset completo)
```

---

## Configuración del DbContext

```csharp
public class Dsw2025TpiContext : DbContext
{
    public Dsw2025TpiContext(DbContextOptions<Dsw2025TpiContext> options)
        : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Product
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(p => p.Sku).IsUnique();
            entity.HasIndex(p => p.InternalCode).IsUnique();
            entity.Property(p => p.CurrentUnitPrice).HasColumnType("decimal(18,2)");
            entity.HasOne(p => p.Category)
                  .WithMany(c => c.Products)
                  .HasForeignKey(p => p.CategoryId);
        });

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();
        });

        // Order
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasIndex(o => o.OrderNumber).IsUnique();
            entity.Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");
            entity.HasOne(o => o.User)
                  .WithMany(u => u.Orders)
                  .HasForeignKey(o => o.UserId);

            // Value objects embebidos
            entity.OwnsOne(o => o.ShippingAddress);
            entity.OwnsOne(o => o.BillingInfo);
        });

        // OrderItem
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasOne(oi => oi.Order)
                  .WithMany(o => o.Items)
                  .HasForeignKey(oi => oi.OrderId);
            entity.HasOne(oi => oi.Product)
                  .WithMany(p => p.OrderItems)
                  .HasForeignKey(oi => oi.ProductId);
            entity.Property(oi => oi.UnitPrice).HasColumnType("decimal(18,2)");
        });

        // PaymentMethod
        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            entity.HasOne(pm => pm.User)
                  .WithMany(u => u.PaymentMethods)
                  .HasForeignKey(pm => pm.UserId);
        });
    }
}
```

---

## Configuración de DI en Program.cs

```csharp
// Agregar después de builder.Services.AddHealthChecks();

// EF Core + SQL Server
builder.Services.AddDbContext<Dsw2025TpiContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositorio genérico
builder.Services.AddScoped<IRepository, EfRepository>();
```

---

## Migraciones EF Core

```bash
# Crear migración inicial
dotnet ef migrations add InitialCreate \
    --project Dsw2025Tpi.Data \
    --startup-project Dsw2025Tpi.Api

# Aplicar migración a la DB
dotnet ef database update \
    --project Dsw2025Tpi.Data \
    --startup-project Dsw2025Tpi.Api

# Para recrear la DB desde cero (dev)
dotnet ef database drop \
    --project Dsw2025Tpi.Data \
    --startup-project Dsw2025Tpi.Api
dotnet ef migrations remove \
    --project Dsw2025Tpi.Data \
    --startup-project Dsw2025Tpi.Api
dotnet ef migrations add InitialCreate \
    --project Dsw2025Tpi.Data \
    --startup-project Dsw2025Tpi.Api
dotnet ef database update \
    --project Dsw2025Tpi.Data \
    --startup-project Dsw2025Tpi.Api
```

---

## Referencia de Proyecto (Api.csproj)

```xml
<ItemGroup>
    <ProjectReference Include="..\Dsw2025Tpi.Data\Dsw2025Tpi.Data.csproj" />
</ItemGroup>
```

**Nota:** Actualmente `Api.csproj` solo tiene una referencia a `Swashbuckle`. Sin esta referencia, `Api` no puede usar `Dsw2025TpiContext` ni `EfRepository`.

---

## Orden de Ejecución

| Paso | Acción | Archivos afectados |
|---|---|---|
| 1 | `docker-compose up -d` | `docker-compose.yml` |
| 2 | Crear entidades en `Domain/Entities/` | 6 archivos nuevos |
| 3 | Modificar `Dsw2025TpiContext` | `Data/Dsw2025TpiContext.cs` |
| 4 | Agregar connection string | `Api/appsettings.json`, `Api/appsettings.Development.json` |
| 5 | Agregar referencia de proyecto | `Api/Dsw2025Tpi.Api.csproj` |
| 6 | Registrar DI | `Api/Program.cs` |
| 7 | `dotnet ef migrations add InitialCreate` | `Data/Migrations/` (auto-generado) |
| 8 | `dotnet ef database update` | Tablas creadas en SQL Server |
| 9 | Verificar con Azure Data Studio o SSMS | Tablas: Products, Categories, Users, Orders, OrderItems, PaymentMethods |

---

## Producción: Azure SQL Database

### Pasos en Azure Portal

1. Crear un recurso "Azure SQL Database"
2. Seleccionar tier (Basic para TPI es suficiente, ~5 USD/mes)
3. Configurar firewall: permitir IP del servidor de despliegue
4. Copiar el connection string desde "Connection strings" en el portal
5. Configurar como Variable de Entorno en el servicio de despliegue (App Service, Azure Functions, etc.)

### Diferencias con desarrollo

| Aspecto | Dev (Docker) | Producción (Azure SQL) |
|---|---|---|
| Server | `localhost,1433` | `tu-servidor.database.windows.net` |
| Auth | `sa` / password | Azure AD o SQL auth |
| `TrustServerCertificate` | `True` | `False` (certificado real) |
| `Encrypt` | `True` o `False` | `True` (siempre) |
| Password en JSON | Sí (dev) | No (variables de entorno) |

---

## Riesgos y Consideraciones

| Riesgo | Mitigación |
|---|---|
| Password en `appsettings.json` | Usar User Secrets en dev, Variables de Entorno en prod |
| `TrustServerCertificate=True` solo para dev | En producción se desactiva, se usa certificado CA |
| Migraciones rompen datos en prod | Siempre usar `dotnet ef script` para generar SQL y aplicar manualmente en prod |
| EF Core 9.x vs .NET 8 | El Data project usa EF Core 9.0.6 pero el target framework es net8.0. Verificar compatibilidad (EF Core 9 soporta .NET 8) |
| Puerto 1433 ocupado | Cambiar el port mapping en `docker-compose.yml` (ej: `"1434:1433"`) |

---

## Resumen de Cambios

| # | Archivo | Tipo | Complejidad |
|---|---|---|---|
| 1 | `docker-compose.yml` | Crear | Baja |
| 2 | `Domain/Entities/Product.cs` | Crear | Baja |
| 3 | `Domain/Entities/Category.cs` | Crear | Baja |
| 4 | `Domain/Entities/User.cs` | Crear | Baja |
| 5 | `Domain/Entities/Order.cs` | Crear | Media (value objects embebidos) |
| 6 | `Domain/Entities/OrderItem.cs` | Crear | Baja |
| 7 | `Domain/Entities/PaymentMethod.cs` | Crear | Baja |
| 8 | `Data/Dsw2025TpiContext.cs` | Modificar | Media (Fluent API) |
| 9 | `Api/appsettings.json` | Modificar | Baja |
| 10 | `Api/appsettings.Development.json` | Modificar | Baja |
| 11 | `Api/Dsw2025Tpi.Api.csproj` | Modificar | Baja |
| 12 | `Api/Program.cs` | Modificar | Baja |
| 13 | `Data/Migrations/` | Auto-generado | N/A |

**Esfuerzo total estimado:** ~3-4 horas (incluyendo pruebas)
