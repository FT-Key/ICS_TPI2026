using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Tests.Integration.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Dsw2025Tpi.Tests.Integration.Orders;

/// <summary>
/// TP4-08: utilidades comunes de las pruebas de integración de órdenes (siembra de clientes y
/// productos, armado de requests y lectura directa de SQLite para verificar persistencia e inventario).
/// </summary>
public abstract class OrdersIntegrationTestBase : IntegrationTestBase
{
    protected const string OrdersUrl = "/api/orders";
    protected const string DefaultAddress = "Av. Siempre Viva 742";

    // La API serializa OrderStatus como texto (JsonStringEnumConverter en Program).
    protected static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    protected OrdersIntegrationTestBase(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    /// <summary>Inserta un cliente en SQLite; su Id es el que debe llevar el claim "id" del JWT.</summary>
    protected async Task<Guid> SeedCustomerAsync(string name = "Cliente Test")
    {
        var id = Guid.NewGuid();
        await Factory.SeedAsync(new Customer(id, name, $"{id:N}@tests.local"));
        return id;
    }

    protected async Task<Product> SeedProductAsync(string sku, decimal price, int stock)
    {
        var product = NewProduct(sku, price, stock);
        await Factory.SeedAsync(product);
        return product;
    }

    /// <summary>Producto sin persistir (sirve para simular un producto inexistente).</summary>
    protected static Product NewProduct(string sku, decimal price, int stock) =>
        new(sku, $"INT-{sku}", $"Producto {sku}", $"Descripcion {sku}", price, stock);

    protected static OrderModel.RequestOrderModel NewOrderRequest(params (Product Product, int Quantity)[] items) =>
        new(
            DefaultAddress,
            DefaultAddress,
            "Entregar por la tarde",
            items.Select(i => new OrderItemModel.RequestOrderItemModel(i.Quantity, i.Product.Id)).ToList());

    protected Task<int> GetStockAsync(Product product) =>
        Factory.WithDbContextAsync(context => context.Products
            .AsNoTracking()
            .Where(p => p.Id == product.Id)
            .Select(p => p.StockQuantity)
            .SingleAsync());

    protected Task<int> CountOrdersAsync() =>
        Factory.WithDbContextAsync(context => context.Orders.CountAsync());

    protected Task<int> CountOrderItemsAsync() =>
        Factory.WithDbContextAsync(context => context.OrderItems.CountAsync());

    protected static async Task<OrderModel.ResponseOrderModel> ReadOrderAsync(HttpResponseMessage response)
    {
        var order = await response.Content.ReadFromJsonAsync<OrderModel.ResponseOrderModel>(JsonOptions);
        order.Should().NotBeNull();
        return order!;
    }

    /// <summary>Crea una orden por la API como el cliente indicado y exige 201 Created.</summary>
    protected async Task<OrderModel.ResponseOrderModel> CreateOrderAsync(Guid customerId, OrderModel.RequestOrderModel request)
    {
        var response = await CreateUserClient(customerId).PostAsJsonAsync(OrdersUrl, request);
        response.StatusCode.Should().Be(HttpStatusCode.Created, "la precondición de la prueba es una orden creada");
        return await ReadOrderAsync(response);
    }
}
