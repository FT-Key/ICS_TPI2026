# Pruebas automatizadas del backend (TP4)

La suite se divide en capas y corre en local sin SQL Server: las pruebas unitarias aíslan la base con Moq y las de integración usan SQLite en memoria.

## Proyectos

| Proyecto | Capa | Referencias | Paquetes principales |
|---|---|---|---|
| `Dsw2025Tpi.Tests.Unit` | Unitarias | Application, Domain, Data | xUnit, Moq, FluentAssertions, coverlet.collector |
| `Dsw2025Tpi.Tests.Integration` | Integración | Api (`WebApplicationFactory<Program>`) | xUnit, Microsoft.AspNetCore.Mvc.Testing, Microsoft.EntityFrameworkCore.Sqlite, FluentAssertions, coverlet.collector |
| `Dsw2025Tpi.Tests` | Regresión de sprints anteriores (AT-01, AT-03, AT-05, AT-06) | Api | xUnit, EF Core SQLite |

`Dsw2025Tpi.Tests.Integration` incluye `xunit.runner.json` con `parallelizeTestCollections: false`, porque las pruebas comparten la base SQLite del fixture.

FluentAssertions está fijado en la versión 7.x: desde la 8.0 su licencia deja de ser libre para uso comercial.

## Comandos

Todos se ejecutan desde `ICS_TPI2026_backend/`.

```bash
# Compilar toda la solución tratando las advertencias como errores (igual que el CI)
dotnet build Dsw2025Tpi.sln -c Release /p:TreatWarningsAsErrors=true

# Toda la suite
dotnet test Dsw2025Tpi.sln -c Release

# Solo unitarias
dotnet test Dsw2025Tpi.sln -c Release --filter Category=Unit

# Solo integración
dotnet test Dsw2025Tpi.sln -c Release --filter Category=Integration

# Una clase o un test puntual
dotnet test Dsw2025Tpi.sln --filter "FullyQualifiedName~AddProductTests"

# Resultados .trx y cobertura (quedan en TestResults/, ignorado por git)
dotnet test Dsw2025Tpi.sln -c Release --logger trx --collect:"XPlat Code Coverage" --results-directory TestResults
```

## Convenciones

- **Nombre de los tests:** `Metodo_Escenario_ResultadoEsperado`, por ejemplo `AddProduct_WithDuplicateSku_ThrowsDuplicatedEntityException`.
- **Categoría:** cada clase de prueba lleva `[Trait("Category", "Unit")]` o `[Trait("Category", "Integration")]` (constantes en `TestCategories`), que es lo que filtra el pipeline.
- **Estructura:** Arrange / Act / Assert, con asserts de FluentAssertions.
- **Datos:** cada test arma sus propios datos y puede ejecutarse solo; ninguna prueba usa la base de producción ni secretos reales.
- **Artefactos:** `TestResults/`, `*.trx` y `coverage.*` están en `.gitignore`.

## Arnés de integración (TP4-06)

`Dsw2025Tpi.Tests.Integration/Infrastructure/` levanta la API real en memoria y la conecta a SQLite:

- **`CustomWebApplicationFactory`** (`WebApplicationFactory<Program>`): entorno `Testing`, clave JWT y admin de prueba, y `Database:ApplyMigrations=false`. Reemplaza `Dsw2025TpiContext` y `AuthenticateContext` por SQLite `Data Source=:memory:` con una conexión abierta para cada uno, y crea el esquema con `EnsureCreated` en el arranque.
- **`IntegrationTestCollection`**: `ICollectionFixture` que comparte un único factory. Junto con `parallelizeTestCollections: false`, las pruebas no compiten por la base.
- **`IntegrationTestBase`**: resetea la base antes de cada test con `ResetDatabaseAsync`, así cada prueba aísla sus datos y puede correr sola.
- **Datos:**
  - `SeedAsync(...)` para entidades de dominio.
  - `SeedUserAsync(usuario, contraseña, rol)` para un usuario de Identity con su `Customer` espejo.
  - `WithDbContextAsync(...)` para hacer asserts directos sobre la base.
- **Autenticación:** `TestJwtTokens.Admin()` / `TestJwtTokens.User(customerId)` generan tokens con el mismo issuer, audience y clave que usa la API. `client.WithAuth(token)` agrega el header `Authorization: Bearer`.

La API sigue aplicando las migraciones de SQL Server y la semilla JSON en cualquier otro entorno, porque `Database:ApplyMigrations` vale `true` por defecto.
