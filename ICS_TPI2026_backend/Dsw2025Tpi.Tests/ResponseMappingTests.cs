using System.Text.Json;
using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Application.Services;
using Dsw2025Tpi.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Dsw2025Tpi.Tests;

// AT-05: el JSON de cada endpoint de Products y Orders debe mantener exactamente
// la misma estructura (nombres, orden y valores de los campos) tras centralizar el mapeo.
public class ResponseMappingTests
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    private static readonly Guid CustomerId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static string Json(object? value) => JsonSerializer.Serialize(value, WebJson);

    // La forma esperada se arma a mano, sin usar el mapper, para que el test sea independiente de el.
    private static object ExpectedProduct(Product p) => new
    {
        id = p.Id,
        sku = p.Sku,
        internalCode = p.InternalCode,
        name = p.Name,
        description = p.Description,
        currentUnitPrice = p.CurrentUnitPrice,
        stockQuantity = p.StockQuantity,
        isActive = p.IsActive
    };

    private static object ExpectedOrderItem(OrderItem i) => new
    {
        id = i.Id,
        quantity = i.Quantity,
        unitPrice = i.UnitPrice,
        orderId = i.OrderId,
        productId = i.ProductId,
        subTotal = i.Subtotal
    };

    // "totatAmount" respeta el nombre actual del DTO (la respuesta JSON no debe cambiar).
    private static object ExpectedOrder(Order o) => new
    {
        id = o.Id,
        date = o.Date,
        shippingAddress = o.ShippingAddress,
        billingAddress = o.BillingAddress,
        notes = o.Notes,
        customerId = o.CustomerId,
        customerName = o.Customer?.Name,
        status = o.Status,
        totatAmount = o.TotalAmount,
        orderItems = o.OrderItems.Select(ExpectedOrderItem).ToList()
    };

    private static Product NewProduct(string sku, bool isActive = true)
    {
        var product = new Product(sku, $"INT-{sku}", $"Producto {sku}", $"Descripcion {sku}", 10.5m, 7)
        {
            IsActive = isActive
        };
        return product;
    }

    private static Order NewOrder(Customer customer, Product product)
    {
        var order = new Order("Calle 1", "Calle 2", "Notas", customer.Id)
        {
            Date = new DateTime(2026, 10, 24, 12, 30, 0),
            Customer = customer
        };
        order.OrderItems = [new OrderItem(2, product.CurrentUnitPrice, order.Id, product.Id)];
        return order;
    }

    private static ProductsManagementService ProductsService(FakeRepository repository)
        => new(repository, NullLogger<ProductsManagementService>.Instance);

    // ---------- Products ----------

    [Fact]
    public async Task GetProductById_MantieneLaEstructuraDelJson()
    {
        var product = NewProduct("A1");
        var service = ProductsService(new FakeRepository(product));

        var result = await service.GetProductById(product.Id);

        Assert.Equal(Json(ExpectedProduct(product)), Json(result));
    }

    [Fact]
    public async Task GetAllProducts_MantieneLaEstructuraDelJson()
    {
        var active = NewProduct("A1");
        var inactive = NewProduct("A2", isActive: false);
        var service = ProductsService(new FakeRepository(active, inactive));

        var result = await service.GetAllProducts();

        Assert.Equal(Json(new[] { ExpectedProduct(active) }), Json(result));
    }

    [Fact]
    public async Task GetProducts_MantieneLaEstructuraDelJson()
    {
        var first = NewProduct("A1");
        var second = NewProduct("B2");
        var service = ProductsService(new FakeRepository(second, first));

        var result = await service.GetProducts(new ProductModel.FilterProduct(null, null, 1, 10));

        // Ordenado por Sku, con el contenedor de paginacion actual (productItems / total).
        var expected = new { productItems = new[] { ExpectedProduct(first), ExpectedProduct(second) }, total = 2 };
        Assert.Equal(Json(expected), Json(result));
    }

    [Fact]
    public async Task AddProduct_MantieneLaEstructuraDelJson()
    {
        var repository = new FakeRepository();
        var service = ProductsService(repository);

        var result = await service.AddProduct(new ProductModel.RequestProductModel("N1", "INT-N1", "Nuevo", "Desc", 20m, 3));

        var saved = Assert.Single(repository.Products);
        Assert.Equal(Json(ExpectedProduct(saved)), Json(result));
    }

    [Fact]
    public async Task UpdateProduct_MantieneLaEstructuraDelJson()
    {
        var product = NewProduct("A1");
        var service = ProductsService(new FakeRepository(product));

        var result = await service.UpdateProduct(product.Id, new ProductModel.RequestProductModel("A1", "INT-NEW", "Renombrado", "Otra", 30m, 9));

        Assert.Equal("Renombrado", product.Name);
        Assert.Equal(Json(ExpectedProduct(product)), Json(result));
    }

    // ---------- Orders ----------

    [Fact]
    public async Task GetOrderById_MantieneLaEstructuraDelJson()
    {
        var customer = new Customer(CustomerId, "Cliente", "c@test.com");
        var product = NewProduct("A1");
        var order = NewOrder(customer, product);
        var service = new OrdersManagementService(new FakeRepository(customer, product, order));

        var result = await service.GetOrderById(order.Id);

        Assert.Equal(Json(ExpectedOrder(order)), Json(result));
    }

    [Fact]
    public async Task GetAllOrders_MantieneLaEstructuraDelJson()
    {
        var customer = new Customer(CustomerId, "Cliente", "c@test.com");
        var product = NewProduct("A1");
        var order = NewOrder(customer, product);
        var service = new OrdersManagementService(new FakeRepository(customer, product, order));

        var result = await service.GetAllOrders(new OrderModel.SearchOrder(null, null));

        // Contenedor de paginacion actual: items / totalCount.
        var expected = new { items = new[] { ExpectedOrder(order) }, totalCount = 1 };
        Assert.Equal(Json(expected), Json(result));
    }

    [Fact]
    public async Task AddOrder_MantieneLaEstructuraDelJson()
    {
        var customer = new Customer(CustomerId, "Cliente", "c@test.com");
        var product = NewProduct("A1");
        var repository = new FakeRepository(customer, product);
        var service = new OrdersManagementService(repository);

        var request = new OrderModel.RequestOrderModel("Calle 1", "Calle 2", "Notas",
            [new OrderItemModel.RequestOrderItemModel(2, product.Id)]);

        var result = await service.AddOrder(request, CustomerId);

        var saved = Assert.Single(repository.Orders);
        Assert.Equal(Json(ExpectedOrder(saved)), Json(result));
    }

    [Fact]
    public async Task UpdateOrderStatus_MantieneLaEstructuraDelJson()
    {
        var customer = new Customer(CustomerId, "Cliente", "c@test.com");
        var product = NewProduct("A1");
        var order = NewOrder(customer, product);
        var service = new OrdersManagementService(new FakeRepository(customer, product, order));

        var result = await service.UpdateOrderStatus(order.Id, "SHIPPED");

        Assert.Equal(OrderStatus.SHIPPED, order.Status);
        Assert.Equal(Json(ExpectedOrder(order)), Json(result));
    }
}
