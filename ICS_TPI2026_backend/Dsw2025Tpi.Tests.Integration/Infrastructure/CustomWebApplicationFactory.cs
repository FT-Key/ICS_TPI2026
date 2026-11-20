using Dsw2025Tpi.Api;
using Dsw2025Tpi.Data;
using Dsw2025Tpi.Domain.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dsw2025Tpi.Tests.Integration.Infrastructure;

/// <summary>
/// TP4-06: levanta la API real (Program) en memoria con TestServer y reemplaza SQL Server por
/// SQLite en memoria. Se usan dos conexiones abiertas durante toda la vida del factory, una por
/// cada DbContext (dominio e Identity), porque EnsureCreated no crea tablas en una base que ya
/// tiene otras. Nunca se usa la base de producción ni secretos reales.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string EnvironmentName = "Testing";

    private readonly SqliteConnection _domainConnection = new("Data Source=:memory:");
    private readonly SqliteConnection _identityConnection = new("Data Source=:memory:");

    public CustomWebApplicationFactory()
    {
        _domainConnection.Open();
        _identityConnection.Open();

        // Sin redirecciones automáticas: los tests verifican 201 + Location, 401, 403, etc.
        ClientOptions.AllowAutoRedirect = false;
        ClientOptions.BaseAddress = new Uri("http://localhost");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(EnvironmentName);

        // Configuración de prueba. UseSetting entra en la configuración del host antes de que
        // Program lea "Jwt:Key", así que la API valida exactamente los tokens que generamos.
        builder.UseSetting("Jwt:Key", TestJwtTokens.SigningKey);
        builder.UseSetting("Jwt:Issuer", TestJwtTokens.Issuer);
        builder.UseSetting("Jwt:Audience", TestJwtTokens.Audience);
        builder.UseSetting("Jwt:ExpireInMinutes", "30");
        builder.UseSetting("DefaultAdminUser:UserName", TestUsers.AdminUserName);
        builder.UseSetting("DefaultAdminUser:Email", "admin@tests.local");
        builder.UseSetting("DefaultAdminUser:Password", TestUsers.AdminPassword);
        builder.UseSetting("DefaultAdminUser:Role", "Admin");
        builder.UseSetting("ConnectionStrings:Dsw2025Tpi", "Data Source=:memory:");
        builder.UseSetting("Database:ApplyMigrations", "false");
        // TestServer no expone HTTPS: se silencia el aviso de UseHttpsRedirection.
        builder.UseSetting("Logging:LogLevel:Microsoft.AspNetCore.HttpsPolicy", "Error");

        builder.ConfigureServices(services =>
        {
            ReplaceWithSqlite<Dsw2025TpiContext>(services, _domainConnection);
            ReplaceWithSqlite<AuthenticateContext>(services, _identityConnection);
        });
    }

    private static void ReplaceWithSqlite<TContext>(IServiceCollection services, SqliteConnection connection)
        where TContext : DbContext
    {
        // EF Core 9 registra la configuración del proveedor (UseSqlServer) como
        // IDbContextOptionsConfiguration<TContext>; hay que quitarla junto con las opciones.
        services.RemoveAll<DbContextOptions<TContext>>();
        services.RemoveAll<IDbContextOptionsConfiguration<TContext>>();
        services.AddDbContext<TContext>(options => options.UseSqlite(connection));
    }

    /// <summary>Crea un scope y devuelve el contexto de dominio para sembrar datos o hacer asserts.</summary>
    public async Task<T> WithDbContextAsync<T>(Func<Dsw2025TpiContext, Task<T>> action)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<Dsw2025TpiContext>();
        return await action(context);
    }

    public Task WithDbContextAsync(Func<Dsw2025TpiContext, Task> action) =>
        WithDbContextAsync(async context =>
        {
            await action(context);
            return true;
        });

    /// <summary>Inserta entidades de dominio (Customer, Product, Order...) en SQLite.</summary>
    public Task SeedAsync(params EntityBase[] entities) =>
        WithDbContextAsync(async context =>
        {
            context.AddRange(entities);
            await context.SaveChangesAsync();
        });

    /// <summary>
    /// Crea un usuario de Identity con el rol indicado y su Customer espejo (mismo Id),
    /// como hace AuthenticateController.Register. Devuelve el Id del usuario/cliente.
    /// </summary>
    public async Task<Guid> SeedUserAsync(string userName, string password, string role)
    {
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        var user = new IdentityUser { UserName = userName, Email = $"{userName}@tests.local", EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

        await userManager.AddToRoleAsync(user, role);

        var customerId = Guid.Parse(user.Id);
        var context = scope.ServiceProvider.GetRequiredService<Dsw2025TpiContext>();
        context.Customers.Add(new Customer(customerId, userName, user.Email));
        await context.SaveChangesAsync();

        return customerId;
    }

    /// <summary>
    /// Deja la base de dominio vacía entre tests (orden respetando las claves foráneas) y
    /// elimina los usuarios creados por las pruebas, conservando roles y el admin por defecto.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        await WithDbContextAsync(async context =>
        {
            await context.OrderItems.ExecuteDeleteAsync();
            await context.Orders.ExecuteDeleteAsync();
            await context.Products.ExecuteDeleteAsync();
            await context.Customers.ExecuteDeleteAsync();
        });

        using var scope = Services.CreateScope();
        var identity = scope.ServiceProvider.GetRequiredService<AuthenticateContext>();
        await identity.Users
            .Where(u => u.UserName != TestUsers.AdminUserName)
            .ExecuteDeleteAsync();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _domainConnection.Dispose();
            _identityConnection.Dispose();
        }
    }
}
