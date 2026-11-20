using System.Net;
using System.Net.Http.Json;
using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Tests.Integration.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Dsw2025Tpi.Tests.Integration.Orders;

/// <summary>
/// TP4-08: PUT /api/orders/{id}, GET /api/orders/{id} y GET /api/orders contra la API real y SQLite.
/// Se verifica el cambio de estado, la restitución de stock al cancelar y los filtros de consulta.
/// </summary>
public class OrderStatusAndQueryEndpointsTests : OrdersIntegrationTestBase
{
    public OrderStatusAndQueryEndpointsTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    private static string StatusUrl(Guid orderId, string status) => $"{OrdersUrl}/{orderId}?newStatus={status}";

    [Fact]
    public async Task UpdateOrderStatus_WithoutToken_ReturnsUnauthorized()
    {
        var response = await CreateClient().PutAsync(StatusUrl(Guid.NewGuid(), "SHIPPED"), null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateOrderStatus_AsUser_ReturnsForbiddenAndKeepsTheOrderUntouched()
    {
        var customerId = await SeedCustomerAsync();
        var mouse = await SeedProductAsync("MOU-001", 100m, stock: 10);
        var order = await CreateOrderAsync(customerId, NewOrderRequest((mouse, 2)));

        var response = await CreateUserClient(customerId).PutAsync(StatusUrl(order.Id, "CANCELLED"), null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await GetStatusAsync(order.Id)).Should().Be(OrderStatus.PENDING);
        (await GetStockAsync(mouse)).Should().Be(8, "una cancelación rechazada no devuelve stock");
    }

    [Fact]
    public async Task UpdateOrderStatus_ToShipped_PersistsTheStatusAndKeepsTheStock()
    {
        var customerId = await SeedCustomerAsync();
        var mouse = await SeedProductAsync("MOU-001", 100m, stock: 10);
        var order = await CreateOrderAsync(customerId, NewOrderRequest((mouse, 2)));

        var response = await CreateAdminClient().PutAsync(StatusUrl(order.Id, "SHIPPED"), null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadOrderAsync(response)).Status.Should().Be(OrderStatus.SHIPPED);
        (await GetStatusAsync(order.Id)).Should().Be(OrderStatus.SHIPPED);
        (await GetStockAsync(mouse)).Should().Be(8, "solo cancelar devuelve unidades al inventario");
    }

    [Fact]
    public async Task UpdateOrderStatus_ToCancelled_RestoresTheStockOfEveryItem()
    {
        var customerId = await SeedCustomerAsync();
        var mouse = await SeedProductAsync("MOU-001", 100m, stock: 10);
        var keyboard = await SeedProductAsync("KEY-001", 200m, stock: 5);
        var order = await CreateOrderAsync(customerId, NewOrderRequest((mouse, 3), (keyboard, 2)));
        (await GetStockAsync(mouse)).Should().Be(7);
        (await GetStockAsync(keyboard)).Should().Be(3);

        var response = await CreateAdminClient().PutAsync(StatusUrl(order.Id, "CANCELLED"), null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await GetStatusAsync(order.Id)).Should().Be(OrderStatus.CANCELLED);
        (await GetStockAsync(mouse)).Should().Be(10, "se devuelven las 3 unidades");
        (await GetStockAsync(keyboard)).Should().Be(5, "se devuelven las 2 unidades");
    }

    [Fact]
    public async Task UpdateOrderStatus_ToCancelled_RestoresTheSumWhenTheSameProductAppearsTwice()
    {
        var customerId = await SeedCustomerAsync();
        var mouse = await SeedProductAsync("MOU-001", 100m, stock: 10);
        var order = await CreateOrderAsync(customerId, NewOrderRequest((mouse, 2), (mouse, 3)));
        (await GetStockAsync(mouse)).Should().Be(5);

        var response = await CreateAdminClient().PutAsync(StatusUrl(order.Id, "CANCELLED"), null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await GetStockAsync(mouse)).Should().Be(10);
    }

    [Theory]
    [InlineData("INEXISTENTE")]
    [InlineData("99")]
    public async Task UpdateOrderStatus_WithInvalidStatus_ReturnsBadRequestAndKeepsTheOrder(string newStatus)
    {
        var customerId = await SeedCustomerAsync();
        var mouse = await SeedProductAsync("MOU-001", 100m, stock: 10);
        var order = await CreateOrderAsync(customerId, NewOrderRequest((mouse, 2)));

        var response = await CreateAdminClient().PutAsync(StatusUrl(order.Id, newStatus), null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await GetStatusAsync(order.Id)).Should().Be(OrderStatus.PENDING);
        (await GetStockAsync(mouse)).Should().Be(8);
    }

    [Fact]
    public async Task UpdateOrderStatus_WhenTheOrderDoesNotExist_ReturnsNotFound()
    {
        var response = await CreateAdminClient().PutAsync(StatusUrl(Guid.NewGuid(), "SHIPPED"), null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetOrderById_AsAdmin_ReturnsTheOrderWithItsItemsAndCustomer()
    {
        var customerId = await SeedCustomerAsync("Ana Perez");
        var mouse = await SeedProductAsync("MOU-001", 100m, stock: 10);
        var order = await CreateOrderAsync(customerId, NewOrderRequest((mouse, 2)));

        var response = await CreateAdminClient().GetAsync($"{OrdersUrl}/{order.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await ReadOrderAsync(response);
        fetched.Id.Should().Be(order.Id);
        fetched.CustomerId.Should().Be(customerId);
        fetched.TotatAmount.Should().Be(200m);
        fetched.OrderItems.Should().ContainSingle().Which.Quantity.Should().Be(2);
    }

    [Fact]
    public async Task GetOrderById_AsUser_ReturnsForbidden()
    {
        var response = await CreateUserClient(Guid.NewGuid()).GetAsync($"{OrdersUrl}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetOrderById_WhenTheOrderDoesNotExist_ReturnsNotFound()
    {
        var response = await CreateAdminClient().GetAsync($"{OrdersUrl}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAllOrders_AsUser_ReturnsForbidden()
    {
        var response = await CreateUserClient(Guid.NewGuid()).GetAsync(OrdersUrl);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAllOrders_WithoutOrders_ReturnsNoContent()
    {
        var response = await CreateAdminClient().GetAsync(OrdersUrl);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetAllOrders_ExcludesCancelledOrdersAndFiltersByStatusAndCustomer()
    {
        // Arrange: 2 clientes y 3 órdenes (una se cancela, otra se envía).
        var anaId = await SeedCustomerAsync("Ana");
        var luisId = await SeedCustomerAsync("Luis");
        var mouse = await SeedProductAsync("MOU-001", 100m, stock: 50);
        var pendingOfAna = await CreateOrderAsync(anaId, NewOrderRequest((mouse, 1)));
        var cancelledOfAna = await CreateOrderAsync(anaId, NewOrderRequest((mouse, 1)));
        var shippedOfLuis = await CreateOrderAsync(luisId, NewOrderRequest((mouse, 1)));
        var admin = CreateAdminClient();
        (await admin.PutAsync(StatusUrl(cancelledOfAna.Id, "CANCELLED"), null)).EnsureSuccessStatusCode();
        (await admin.PutAsync(StatusUrl(shippedOfLuis.Id, "SHIPPED"), null)).EnsureSuccessStatusCode();

        // Act + Assert: sin filtros solo aparecen las no canceladas.
        var all = await GetPageAsync(admin, OrdersUrl);
        all.TotalCount.Should().Be(2);
        all.Items.Select(o => o.Id).Should().BeEquivalentTo(new[] { pendingOfAna.Id, shippedOfLuis.Id });

        // Filtro por estado (insensible a mayúsculas).
        var shipped = await GetPageAsync(admin, $"{OrdersUrl}?status=shipped");
        shipped.Items.Should().ContainSingle().Which.Id.Should().Be(shippedOfLuis.Id);

        // Filtro por cliente.
        var ofAna = await GetPageAsync(admin, $"{OrdersUrl}?customerId={anaId}");
        ofAna.Items.Should().ContainSingle().Which.Id.Should().Be(pendingOfAna.Id);
    }

    [Fact]
    public async Task GetAllOrders_Paginates()
    {
        var customerId = await SeedCustomerAsync();
        var mouse = await SeedProductAsync("MOU-001", 100m, stock: 50);
        for (var i = 0; i < 3; i++)
            await CreateOrderAsync(customerId, NewOrderRequest((mouse, 1)));

        var page = await GetPageAsync(CreateAdminClient(), $"{OrdersUrl}?pageNumber=2&pageSize=2");

        page.TotalCount.Should().Be(3);
        page.Items.Should().HaveCount(1, "la segunda página de tamaño 2 tiene la tercera orden");
    }

    private Task<OrderStatus> GetStatusAsync(Guid orderId) =>
        Factory.WithDbContextAsync(context => context.Orders
            .AsNoTracking()
            .Where(o => o.Id == orderId)
            .Select(o => o.Status)
            .SingleAsync());

    private static async Task<OrderModel.ResponsePagination> GetPageAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<OrderModel.ResponsePagination>(JsonOptions);
        page.Should().NotBeNull();
        return page!;
    }
}
