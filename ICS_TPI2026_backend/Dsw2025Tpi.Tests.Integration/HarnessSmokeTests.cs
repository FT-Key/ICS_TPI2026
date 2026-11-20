using System.Net;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Tests.Integration.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Dsw2025Tpi.Tests.Integration;

/// <summary>TP4-06: smoke tests del arnés (API real + SQLite + JWT de prueba).</summary>
public class HarnessSmokeTests : IntegrationTestBase
{
    public HarnessSmokeTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Healthcheck_AgainstSqlite_ReturnsOkHealthy()
    {
        var response = await CreateClient().GetAsync("/healthcheck");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be("Healthy");
    }

    [Fact]
    public async Task Database_UsesSqliteInMemoryProvider_NotSqlServer()
    {
        var provider = await Factory.WithDbContextAsync(context => Task.FromResult(context.Database.ProviderName));

        provider.Should().Be("Microsoft.EntityFrameworkCore.Sqlite");
    }

    [Fact]
    public async Task SeedAsync_PersistsEntitiesThatTheApiCanRead()
    {
        await Factory.SeedAsync(new Product("SMK-001", "INT-SMK", "Producto semilla", "Desc", 10m, 3));

        var response = await CreateClient().GetAsync("/api/products?search=smk-001");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain("\"totalItems\":1");
    }

    [Fact]
    public async Task ResetDatabase_BeforeEachTest_StartsWithEmptyDomainTables()
    {
        // Este test corre después de otros que siembran datos en la misma base compartida.
        var counts = await Factory.WithDbContextAsync(async context => new[]
        {
            await context.Products.CountAsync(),
            await context.Customers.CountAsync(),
            await context.Orders.CountAsync(),
            await context.OrderItems.CountAsync()
        });

        counts.Should().OnlyContain(count => count == 0);
    }

    [Theory]
    [InlineData(TestJwtTokens.AdminRole, HttpStatusCode.OK)]
    [InlineData(TestJwtTokens.UserRole, HttpStatusCode.Forbidden)]
    public async Task TestJwt_IsAcceptedByTheApiAndCarriesTheRole(string role, HttpStatusCode expected)
    {
        var client = CreateClient().WithAuth(TestJwtTokens.CreateToken(role));

        var response = await client.GetAsync("/api/products/admin");

        response.StatusCode.Should().Be(expected);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutTokenOrWithExpiredToken_ReturnsUnauthorized()
    {
        var anonymous = await CreateClient().GetAsync("/api/products/admin");
        var expired = await CreateClient()
            .WithAuth(TestJwtTokens.CreateToken(TestJwtTokens.AdminRole, lifetime: TimeSpan.FromMinutes(-10)))
            .GetAsync("/api/products/admin");

        anonymous.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        expired.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DefaultAdminUser_FromTestConfiguration_CanLogInThroughTheRealEndpoint()
    {
        var response = await CreateClient().PostAsync(
            "/api/auth/login",
            new StringContent(
                $"{{\"username\":\"{TestUsers.AdminUserName}\",\"password\":\"{TestUsers.AdminPassword}\"}}",
                System.Text.Encoding.UTF8,
                "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        cookies!.Should().Contain(c => c.StartsWith("token="));
    }

    [Fact]
    public async Task SeedUserAsync_CreatesIdentityUserAndMirrorCustomer()
    {
        var customerId = await Factory.SeedUserAsync("cliente-smoke", "Cliente-Smoke-1!", TestJwtTokens.UserRole);

        var customer = await Factory.WithDbContextAsync(context => context.Customers.SingleOrDefaultAsync(c => c.Id == customerId));
        customer.Should().NotBeNull();
        customer!.Name.Should().Be("cliente-smoke");
    }
}
