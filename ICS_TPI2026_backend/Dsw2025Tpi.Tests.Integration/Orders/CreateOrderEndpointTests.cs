using System.Net;
using System.Net.Http.Json;
using System.Text;
using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Tests.Integration.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Dsw2025Tpi.Tests.Integration.Orders;

/// <summary>
/// TP4-08: POST /api/orders contra la API real y SQLite. Se verifica la transacción completa:
/// autenticación, alta de Order y OrderItems, y el descuento real de stock (o su ausencia ante errores).
/// </summary>
public class CreateOrderEndpointTests : OrdersIntegrationTestBase
{
    public CreateOrderEndpointTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task AddOrder_WithoutToken_ReturnsUnauthorizedAndPersistsNothing()
    {
        var mouse = await SeedProductAsync("MOU-001", 1500m, stock: 10);

        var response = await CreateClient().PostAsJsonAsync(OrdersUrl, NewOrderRequest((mouse, 1)));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await CountOrdersAsync()).Should().Be(0);
        (await GetStockAsync(mouse)).Should().Be(10, "una petición no autenticada no puede tocar el inventario");
    }

    [Theory]
    [InlineData(TestJwtTokens.UserRole)]
    [InlineData(TestJwtTokens.AdminRole)]
    public async Task AddOrder_WithValidItems_PersistsOrderAndDecrementsStock(string role)
    {
        // Arrange: 3 x 1500.50 + 2 x 799.99 = 4501.50 + 1599.98 = 6101.48
        var customerId = await SeedCustomerAsync();
        var mouse = await SeedProductAsync("MOU-001", 1500.50m, stock: 10);
        var keyboard = await SeedProductAsync("KEY-001", 799.99m, stock: 5);
        var client = CreateClient().WithAuth(TestJwtTokens.CreateToken(role, customerId));

        // Act
        var response = await client.PostAsJsonAsync(OrdersUrl, NewOrderRequest((mouse, 3), (keyboard, 2)));

        // Assert: contrato HTTP.
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await ReadOrderAsync(response);
        response.Headers.Location!.AbsolutePath.Should().Be($"{OrdersUrl}/{created.Id}");
        created.CustomerId.Should().Be(customerId);
        created.Status.Should().Be(OrderStatus.PENDING);
        created.TotatAmount.Should().Be(6101.48m);
        created.OrderItems.Should().HaveCount(2);

        // Assert: fila de Orders y filas de OrderItems realmente insertadas.
        var saved = await Factory.WithDbContextAsync(context => context.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .SingleAsync(o => o.Id == created.Id));
        saved.CustomerId.Should().Be(customerId);
        saved.Status.Should().Be(OrderStatus.PENDING);
        saved.ShippingAddress.Should().Be(DefaultAddress);
        saved.BillingAddress.Should().Be(DefaultAddress);
        saved.Notes.Should().Be("Entregar por la tarde");
        saved.OrderItems.Should().HaveCount(2);
        saved.OrderItems.Should().ContainSingle(i => i.ProductId == mouse.Id)
            .Which.Should().Match<OrderItem>(i => i.Quantity == 3 && i.UnitPrice == 1500.50m && i.Subtotal == 4501.50m);
        saved.OrderItems.Should().ContainSingle(i => i.ProductId == keyboard.Id)
            .Which.Should().Match<OrderItem>(i => i.Quantity == 2 && i.UnitPrice == 799.99m && i.Subtotal == 1599.98m);

        // Assert: el inventario se descontó de verdad en la base.
        (await GetStockAsync(mouse)).Should().Be(7, "se compraron 3 de 10 unidades");
        (await GetStockAsync(keyboard)).Should().Be(3, "se compraron 2 de 5 unidades");

        // El Location apunta a una orden que existe (solo un Admin puede consultarla).
        var fetched = await CreateAdminClient().GetAsync(response.Headers.Location);
        fetched.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AddOrder_KeepsTheUnitPriceOfTheMomentOfPurchase_WhenTheProductPriceChangesLater()
    {
        var customerId = await SeedCustomerAsync();
        var mouse = await SeedProductAsync("MOU-001", 1500m, stock: 10);
        var created = await CreateOrderAsync(customerId, NewOrderRequest((mouse, 2)));

        await Factory.WithDbContextAsync(async context =>
        {
            var product = await context.Products.SingleAsync(p => p.Id == mouse.Id);
            product.CurrentUnitPrice = 9999m;
            await context.SaveChangesAsync();
        });

        var fetched = await ReadOrderAsync(await CreateAdminClient().GetAsync($"{OrdersUrl}/{created.Id}"));

        fetched.OrderItems.Should().ContainSingle().Which.UnitPrice.Should().Be(1500m, "la orden conserva el precio histórico");
        fetched.TotatAmount.Should().Be(3000m);
    }

    [Fact]
    public async Task AddOrder_WhenStockIsInsufficient_ReturnsBadRequestAndLeavesEveryStockUntouched()
    {
        // Arrange: el primer item tiene stock, el segundo no.
        var customerId = await SeedCustomerAsync();
        var mouse = await SeedProductAsync("MOU-001", 1500m, stock: 10);
        var monitor = await SeedProductAsync("MON-001", 90000m, stock: 2);

        // Act
        var response = await CreateUserClient(customerId)
            .PostAsJsonAsync(OrdersUrl, NewOrderRequest((mouse, 3), (monitor, 5)));

        // Assert: 400 con un mensaje que explica el problema de stock.
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should()
            .Contain("Stock insuficiente").And.Contain(monitor.Name);

        // Assert: no se creó la orden y no hay efectos parciales sobre el inventario.
        (await CountOrdersAsync()).Should().Be(0);
        (await CountOrderItemsAsync()).Should().Be(0);
        (await GetStockAsync(mouse)).Should().Be(10, "el producto con stock suficiente tampoco se descuenta");
        (await GetStockAsync(monitor)).Should().Be(2);
    }

    [Fact]
    public async Task AddOrder_WithSameProductInSeveralItems_SumsTheQuantitiesAgainstTheStock()
    {
        var customerId = await SeedCustomerAsync();
        var mouse = await SeedProductAsync("MOU-001", 100m, stock: 10);

        var created = await CreateOrderAsync(customerId, NewOrderRequest((mouse, 2), (mouse, 3)));

        created.TotatAmount.Should().Be(500m);
        (await GetStockAsync(mouse)).Should().Be(5, "se descuentan 2 + 3 unidades");
    }

    [Fact]
    public async Task AddOrder_WhenRepeatedItemsExceedTheStockTogether_ReturnsBadRequestAndKeepsTheStock()
    {
        // 4 + 4 = 8 unidades pedidas sobre un stock de 6: cada item por separado alcanzaría.
        var customerId = await SeedCustomerAsync();
        var mouse = await SeedProductAsync("MOU-001", 100m, stock: 6);

        var response = await CreateUserClient(customerId)
            .PostAsJsonAsync(OrdersUrl, NewOrderRequest((mouse, 4), (mouse, 4)));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await GetStockAsync(mouse)).Should().Be(6);
        (await CountOrdersAsync()).Should().Be(0);
    }

    [Fact]
    public async Task AddOrder_WhenAProductDoesNotExist_ReturnsBadRequestAndKeepsTheStockOfTheOthers()
    {
        var customerId = await SeedCustomerAsync();
        var mouse = await SeedProductAsync("MOU-001", 100m, stock: 10);
        var ghost = NewProduct("GHOST", 100m, stock: 10); // nunca se inserta en SQLite

        var response = await CreateUserClient(customerId)
            .PostAsJsonAsync(OrdersUrl, NewOrderRequest((mouse, 1), (ghost, 1)));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain(ghost.Id.ToString());
        (await GetStockAsync(mouse)).Should().Be(10);
        (await CountOrdersAsync()).Should().Be(0);
    }

    [Theory]
    [InlineData("SinItems")]
    [InlineData("CantidadCero")]
    [InlineData("SinDireccionDeEnvio")]
    [InlineData("SinDireccionDeFacturacion")]
    public async Task AddOrder_WithInvalidBody_ReturnsBadRequestAndPersistsNothing(string scenario)
    {
        var customerId = await SeedCustomerAsync();
        var mouse = await SeedProductAsync("MOU-001", 100m, stock: 10);
        var valid = NewOrderRequest((mouse, 1));
        var request = scenario switch
        {
            "SinItems" => valid with { OrderItems = [] },
            "CantidadCero" => NewOrderRequest((mouse, 0)),
            "SinDireccionDeEnvio" => valid with { ShippingAddress = "  " },
            "SinDireccionDeFacturacion" => valid with { BillingAddress = null },
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null)
        };

        var response = await CreateUserClient(customerId).PostAsJsonAsync(OrdersUrl, request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await CountOrdersAsync()).Should().Be(0);
        (await GetStockAsync(mouse)).Should().Be(10);
    }

    [Fact]
    public async Task AddOrder_WithAnotherCustomerIdInTheBody_IgnoresItAndUsesTheTokenCustomer()
    {
        // BOLA (AT-01): el cliente del token no puede crear órdenes a nombre de otro.
        var attackerId = await SeedCustomerAsync("Atacante");
        var victimId = await SeedCustomerAsync("Victima");
        var mouse = await SeedProductAsync("MOU-001", 100m, stock: 10);
        var json = $$"""
            {"customerId":"{{victimId}}","shippingAddress":"Calle 1","billingAddress":"Calle 1","notes":null,
             "orderItems":[{"quantity":1,"productId":"{{mouse.Id}}"}]}
            """;

        var response = await CreateUserClient(attackerId)
            .PostAsync(OrdersUrl, new StringContent(json, Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var saved = await Factory.WithDbContextAsync(context => context.Orders.AsNoTracking().SingleAsync());
        saved.CustomerId.Should().Be(attackerId);
        saved.CustomerId.Should().NotBe(victimId);
    }

    [Fact]
    public async Task AddOrder_WhenTheTokenCustomerDoesNotExist_ReturnsBadRequestAndKeepsTheStock()
    {
        var unknownCustomerId = Guid.NewGuid(); // el token es válido pero no hay fila en Customers
        var mouse = await SeedProductAsync("MOU-001", 100m, stock: 10);

        var response = await CreateUserClient(unknownCustomerId)
            .PostAsJsonAsync(OrdersUrl, NewOrderRequest((mouse, 1)));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain(unknownCustomerId.ToString());
        (await GetStockAsync(mouse)).Should().Be(10);
        (await CountOrdersAsync()).Should().Be(0);
    }

    [Fact]
    public async Task AddOrder_WhenInsertingTheOrderFailsInTheDatabase_RollsBackTheStockDecrement()
    {
        // Arrange: un trigger de SQLite hace fallar el INSERT en Orders DESPUÉS de que el servicio
        // ya descontó el stock dentro de la transacción. La transacción tiene que revertirlo.
        var customerId = await SeedCustomerAsync();
        var mouse = await SeedProductAsync("MOU-001", 100m, stock: 10);
        await Factory.WithDbContextAsync(context => context.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER fail_order_insert BEFORE INSERT ON \"Orders\" BEGIN SELECT RAISE(ABORT, 'fallo simulado'); END;"));

        try
        {
            // Act: sin middleware global de excepciones, TestServer propaga el error no controlado.
            HttpStatusCode? status = null;
            try
            {
                var response = await CreateUserClient(customerId)
                    .PostAsJsonAsync(OrdersUrl, NewOrderRequest((mouse, 3)));
                status = response.StatusCode;
            }
            catch (Exception)
            {
                // Esperado: el fallo de la base llega como excepción desde el host de pruebas.
            }

            // Assert
            status.Should().NotBe(HttpStatusCode.Created, "el INSERT de la orden falló");
            (await GetStockAsync(mouse)).Should().Be(10, "la transacción revierte el descuento de stock");
            (await CountOrdersAsync()).Should().Be(0);
            (await CountOrderItemsAsync()).Should().Be(0);
        }
        finally
        {
            // La base en memoria se comparte entre tests: hay que quitar el trigger siempre.
            await Factory.WithDbContextAsync(context =>
                context.Database.ExecuteSqlRawAsync("DROP TRIGGER IF EXISTS fail_order_insert;"));
        }
    }
}
