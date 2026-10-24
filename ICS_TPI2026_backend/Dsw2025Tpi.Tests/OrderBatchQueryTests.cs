using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Application.Services;
using Dsw2025Tpi.Data.Repositories;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace Dsw2025Tpi.Tests;

public class OrderBatchQueryTests
{
    private const int ProductCount = 5;
    private const int PurchasedQuantity = 2;

    private readonly ITestOutputHelper _output;

    public OrderBatchQueryTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task AddOrder_ConCincoProductosDistintos_UsaUnaSolaLecturaBatchYUnSaveChanges()
    {
        await using var database = new SqliteTestDatabase();
        var (customerId, products) = await SeedCatalog(database, stock: 10);

        await using var context = database.CreateContext();
        database.ResetCounters();
        var service = new OrdersManagementService(new EfRepository(context));
        var request = new OrderModel.RequestOrderModel(
            "Av. Siempre Viva 742",
            "Av. Siempre Viva 742",
            null,
            products.Select(p => new OrderItemModel.RequestOrderItemModel(PurchasedQuantity, p.Id)).ToList());

        var response = await service.AddOrder(request, customerId);

        WriteCommands(database);

        var productQueries = ProductQueries(database);
        Assert.Single(productQueries);
        Assert.Contains("\"Id\" IN (", productQueries[0], StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, context.SaveChangesCalls);
        Assert.Equal(ProductCount, response.OrderItems.Count);

        await using var verifyContext = database.CreateContext();
        Assert.Equal(1, await verifyContext.Orders.CountAsync());
        Assert.Equal(ProductCount, await verifyContext.OrderItems.CountAsync());

        var stocks = await verifyContext.Products.Select(p => p.StockQuantity).ToListAsync();
        Assert.All(stocks, stock => Assert.Equal(8, stock));
    }

    [Fact]
    public async Task UpdateOrderStatus_ACancelado_RestauraStockConUnaSolaLecturaBatchYUnSaveChanges()
    {
        await using var database = new SqliteTestDatabase();
        var (customerId, products) = await SeedCatalog(database, stock: 10);
        var orderId = await CreateOrder(database, customerId, products);

        await using (var afterCreateContext = database.CreateContext())
        {
            var discountedStocks = await afterCreateContext.Products.Select(p => p.StockQuantity).ToListAsync();
            Assert.All(discountedStocks, stock => Assert.Equal(8, stock));
        }

        await using var context = database.CreateContext();
        database.ResetCounters();
        var service = new OrdersManagementService(new EfRepository(context));

        var response = await service.UpdateOrderStatus(orderId, "cancelled");

        WriteCommands(database);

        var productQueries = ProductQueries(database);
        Assert.Single(productQueries);
        Assert.Contains("\"Id\" IN (", productQueries[0], StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, context.SaveChangesCalls);
        Assert.Equal(OrderStatus.CANCELLED, response.Status);

        await using var verifyContext = database.CreateContext();
        var cancelledOrder = await verifyContext.Orders.SingleAsync(o => o.Id == orderId);
        Assert.Equal(OrderStatus.CANCELLED, cancelledOrder.Status);

        var stocks = await verifyContext.Products.Select(p => p.StockQuantity).ToListAsync();
        Assert.All(stocks, stock => Assert.Equal(10, stock));
    }

    [Fact]
    public async Task AddOrder_SinStockSuficiente_NoPersisteDescuentoParcial()
    {
        await using var database = new SqliteTestDatabase();
        var (customerId, products) = await SeedCatalog(database, stock: 1);

        await using var context = database.CreateContext();
        database.ResetCounters();
        var service = new OrdersManagementService(new EfRepository(context));
        var request = new OrderModel.RequestOrderModel(
            "Av. Siempre Viva 742",
            "Av. Siempre Viva 742",
            null,
            products.Select(p => new OrderItemModel.RequestOrderItemModel(PurchasedQuantity, p.Id)).ToList());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddOrder(request, customerId));

        Assert.Contains("Stock insuficiente", exception.Message);
        Assert.Equal(0, context.SaveChangesCalls);
        Assert.Single(ProductQueries(database));

        await using var verifyContext = database.CreateContext();
        Assert.Equal(0, await verifyContext.Orders.CountAsync());

        var stocks = await verifyContext.Products.Select(p => p.StockQuantity).ToListAsync();
        Assert.All(stocks, stock => Assert.Equal(1, stock));
    }

    [Fact]
    public async Task AddOrder_ConProductoRepetido_SumaCantidadesYDescuentaUnaSolaVez()
    {
        await using var database = new SqliteTestDatabase();
        var (customerId, products) = await SeedCatalog(database, stock: 10);
        var product = products[0];

        await using var context = database.CreateContext();
        database.ResetCounters();
        var service = new OrdersManagementService(new EfRepository(context));
        var request = new OrderModel.RequestOrderModel(
            "Av. Siempre Viva 742",
            "Av. Siempre Viva 742",
            null,
            [
                new OrderItemModel.RequestOrderItemModel(2, product.Id),
                new OrderItemModel.RequestOrderItemModel(3, product.Id)
            ]);

        var response = await service.AddOrder(request, customerId);

        Assert.Equal(2, response.OrderItems.Count);
        Assert.Equal(1, context.SaveChangesCalls);

        await using var verifyContext = database.CreateContext();
        Assert.Equal(2, await verifyContext.OrderItems.CountAsync());

        var persistedStock = await verifyContext.Products
            .Where(p => p.Id == product.Id)
            .Select(p => p.StockQuantity)
            .SingleAsync();
        Assert.Equal(5, persistedStock);
    }

    private static async Task<(Guid CustomerId, List<Product> Products)> SeedCatalog(
        SqliteTestDatabase database,
        int stock)
    {
        await using var context = database.CreateContext();

        var customer = new Customer(Guid.NewGuid(), "Cliente Test", "cliente@test.com");
        var products = Enumerable.Range(1, ProductCount)
            .Select(i => new Product(
                $"SKU-{i:000}",
                $"INT-{i:000}",
                $"Producto {i}",
                "Producto de prueba",
                100m + i,
                stock))
            .ToList();

        context.Customers.Add(customer);
        context.Products.AddRange(products);
        await context.SaveChangesAsync();

        return (customer.Id, products);
    }

    private static async Task<Guid> CreateOrder(
        SqliteTestDatabase database,
        Guid customerId,
        IEnumerable<Product> products)
    {
        await using var context = database.CreateContext();
        var service = new OrdersManagementService(new EfRepository(context));
        var request = new OrderModel.RequestOrderModel(
            "Av. Siempre Viva 742",
            "Av. Siempre Viva 742",
            null,
            products.Select(p => new OrderItemModel.RequestOrderItemModel(PurchasedQuantity, p.Id)).ToList());

        var response = await service.AddOrder(request, customerId);
        return response.Id;
    }

    private static List<string> ProductQueries(SqliteTestDatabase database)
    {
        return database.Commands
            .Where(command => command.Contains("FROM \"Products\"", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private void WriteCommands(SqliteTestDatabase database)
    {
        foreach (var command in database.Commands)
        {
            _output.WriteLine(command);
            _output.WriteLine("---");
        }
    }
}
