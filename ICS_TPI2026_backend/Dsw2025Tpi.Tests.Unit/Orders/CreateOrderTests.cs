using System.Linq.Expressions;
using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Application.Exceptions;
using Dsw2025Tpi.Application.Services;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Domain.Interfaces;
using FluentAssertions;
using Moq;
using InvalidDataException = Dsw2025Tpi.Application.Exceptions.InvalidDataException;

namespace Dsw2025Tpi.Tests.Unit.Orders;

/// <summary>
/// TP4-05: pruebas unitarias de OrdersManagementService.AddOrder (el enunciado lo llama CreateOrder).
/// Se usa MockBehavior.Strict: cualquier llamada no configurada al repositorio hace fallar la prueba,
/// así que en los caminos de error basta con no configurar Add / Update / ExecuteInTransactionAsync
/// para garantizar que no se crean órdenes ni se mutan productos.
/// </summary>
[Trait(TestCategories.Name, TestCategories.Unit)]
public class CreateOrderTests
{
    private static readonly Guid CustomerId = Guid.NewGuid();

    private readonly Mock<IRepository> _repository = new(MockBehavior.Strict);
    private readonly OrdersManagementService _service;

    // Indica si el código está corriendo dentro de ExecuteInTransactionAsync.
    private bool _insideTransaction;

    public CreateOrderTests()
    {
        _service = new OrdersManagementService(_repository.Object);
    }

    private static Product NewProduct(string sku, decimal price, int stock) =>
        new(sku, $"INT-{sku}", $"Producto {sku}", $"Descripcion {sku}", price, stock);

    private static OrderModel.RequestOrderModel Request(params (Product Product, int Quantity)[] items) =>
        new(
            "Av. Siempre Viva 742",
            "Av. Siempre Viva 742",
            "Entregar por la tarde",
            items.Select(i => new OrderItemModel.RequestOrderItemModel(i.Quantity, i.Product.Id)).ToList());

    private void SetupCustomerExists() =>
        _repository
            .Setup(r => r.GetById<Customer>(CustomerId, It.IsAny<string[]>()))
            .ReturnsAsync(new Customer(CustomerId, "Juan Cliente", "juan@example.com"));

    private void SetupCatalog(params Product[] catalog) =>
        _repository
            .Setup(r => r.GetFiltered(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<string[]>()))
            .ReturnsAsync((Expression<Func<Product, bool>> predicate, string[] _) =>
                catalog.AsQueryable().Where(predicate).ToList());

    /// <summary>Configura la transacción y las escrituras del camino feliz.</summary>
    private void SetupWrites()
    {
        _repository
            .Setup(r => r.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns(async (Func<Task> operation, CancellationToken _) =>
            {
                _insideTransaction = true;
                try
                {
                    await operation();
                }
                finally
                {
                    _insideTransaction = false;
                }
            });

        _repository
            .Setup(r => r.Update(It.IsAny<Product>()))
            .Callback(() => _insideTransaction.Should().BeTrue("el descuento de stock debe persistirse dentro de la transacción"))
            .ReturnsAsync((Product product) => product);

        _repository
            .Setup(r => r.Add(It.IsAny<Order>()))
            .Callback(() => _insideTransaction.Should().BeTrue("el alta de la orden debe ocurrir dentro de la transacción"))
            .ReturnsAsync((Order order) => order);
    }

    [Fact]
    public async Task CreateOrder_WithValidItemsAndStock_CalculatesTotalAndDecrementsProductStock()
    {
        // Arrange: 3 x 1500.50 + 2 x 799.99 = 4501.50 + 1599.98 = 6101.48
        var mouse = NewProduct("MOU-001", 1500.50m, stock: 10);
        var keyboard = NewProduct("KEY-001", 799.99m, stock: 5);
        SetupCustomerExists();
        SetupCatalog(mouse, keyboard, NewProduct("NOT-ORDERED", 10m, stock: 1));
        SetupWrites();

        // Act
        var result = await _service.AddOrder(Request((mouse, 3), (keyboard, 2)), CustomerId);

        // Assert: reglas contables.
        result.TotatAmount.Should().Be(6101.48m, "el total es la suma exacta de los subtotales");
        result.OrderItems.Should().HaveCount(2);
        result.OrderItems.Sum(i => i.SubTotal).Should().Be(result.TotatAmount);
        result.OrderItems.Should().ContainSingle(i => i.ProductId == mouse.Id)
            .Which.Should().Match<OrderItemModel.ResponseOrderItemModel>(i =>
                i.Quantity == 3 && i.UnitPrice == 1500.50m && i.SubTotal == 4501.50m);
        result.OrderItems.Should().ContainSingle(i => i.ProductId == keyboard.Id)
            .Which.Should().Match<OrderItemModel.ResponseOrderItemModel>(i =>
                i.Quantity == 2 && i.UnitPrice == 799.99m && i.SubTotal == 1599.98m);
        result.CustomerId.Should().Be(CustomerId);
        result.Status.Should().Be(OrderStatus.PENDING);

        // Assert: reglas de inventario.
        mouse.StockQuantity.Should().Be(7, "se compraron 3 de 10 unidades");
        keyboard.StockQuantity.Should().Be(3, "se compraron 2 de 5 unidades");

        // Assert: interacciones con el repositorio.
        _repository.Verify(r => r.GetById<Customer>(CustomerId, It.IsAny<string[]>()), Times.Once);
        _repository.Verify(r => r.GetFiltered(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<string[]>()), Times.Once);
        _repository.Verify(r => r.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.Update(It.Is<Product>(p => p.Id == mouse.Id && p.StockQuantity == 7)), Times.Once);
        _repository.Verify(r => r.Update(It.Is<Product>(p => p.Id == keyboard.Id && p.StockQuantity == 3)), Times.Once);
        _repository.Verify(r => r.Update(It.IsAny<Product>()), Times.Exactly(2));
        _repository.Verify(r => r.Add(It.Is<Order>(o =>
            o.Id == result.Id &&
            o.CustomerId == CustomerId &&
            o.OrderItems.Count == 2 &&
            o.TotalAmount == 6101.48m)), Times.Once);
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateOrder_WhenProductStockIsInsufficient_ThrowsInsufficientStockException()
    {
        // Arrange: el primer item tiene stock, el segundo no.
        var mouse = NewProduct("MOU-001", 1500m, stock: 10);
        var monitor = NewProduct("MON-001", 90000m, stock: 2);
        SetupCustomerExists();
        SetupCatalog(mouse, monitor);
        // No se configuran Add / Update / ExecuteInTransactionAsync: con el mock estricto, invocarlos falla.

        // Act
        var act = () => _service.AddOrder(Request((mouse, 3), (monitor, 5)), CustomerId);

        // Assert
        var exception = (await act.Should().ThrowAsync<InsufficientStockException>()).Which;
        exception.ProductId.Should().Be(monitor.Id);
        exception.AvailableStock.Should().Be(2);
        exception.RequestedQuantity.Should().Be(5);
        exception.Message.Should().Contain("Stock insuficiente").And.Contain(monitor.Name);

        mouse.StockQuantity.Should().Be(10, "no hay efectos parciales sobre los demás productos");
        monitor.StockQuantity.Should().Be(2, "el stock queda intacto");
        _repository.Verify(r => r.Update(It.IsAny<Product>()), Times.Never);
        _repository.Verify(r => r.Add(It.IsAny<Order>()), Times.Never);
        _repository.Verify(r => r.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrder_WhenCustomerDoesNotExist_ThrowsCustomerNotFoundException()
    {
        // Arrange: el cliente del token no existe; solo se configura esa lectura.
        var mouse = NewProduct("MOU-001", 1500m, stock: 10);
        _repository
            .Setup(r => r.GetById<Customer>(CustomerId, It.IsAny<string[]>()))
            .ReturnsAsync((Customer?)null);

        // Act
        var act = () => _service.AddOrder(Request((mouse, 1)), CustomerId);

        // Assert: se corta antes de leer productos, mutarlos o crear la orden.
        (await act.Should().ThrowAsync<CustomerNotFoundException>())
            .Which.CustomerId.Should().Be(CustomerId);

        mouse.StockQuantity.Should().Be(10);
        _repository.Verify(r => r.GetById<Customer>(CustomerId, It.IsAny<string[]>()), Times.Once);
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateOrder_WithSameProductInSeveralItems_SumsQuantitiesAndUpdatesProductOnce()
    {
        var mouse = NewProduct("MOU-001", 100m, stock: 10);
        SetupCustomerExists();
        SetupCatalog(mouse);
        SetupWrites();

        var result = await _service.AddOrder(Request((mouse, 2), (mouse, 3)), CustomerId);

        result.TotatAmount.Should().Be(500m);
        mouse.StockQuantity.Should().Be(5);
        _repository.Verify(r => r.Update(It.Is<Product>(p => p.Id == mouse.Id)), Times.Once);
        _repository.Verify(r => r.Add(It.IsAny<Order>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrder_WhenSameProductItemsExceedStockTogether_ThrowsInsufficientStockException()
    {
        // 4 + 4 = 8 unidades pedidas sobre un stock de 6: cada item por separado alcanzaría.
        var mouse = NewProduct("MOU-001", 100m, stock: 6);
        SetupCustomerExists();
        SetupCatalog(mouse);

        var act = () => _service.AddOrder(Request((mouse, 4), (mouse, 4)), CustomerId);

        (await act.Should().ThrowAsync<InsufficientStockException>()).Which.RequestedQuantity.Should().Be(8);
        mouse.StockQuantity.Should().Be(6);
    }

    [Fact]
    public async Task CreateOrder_WhenProductDoesNotExist_ThrowsEntityNotFoundException()
    {
        var mouse = NewProduct("MOU-001", 100m, stock: 10);
        var ghost = NewProduct("GHOST", 100m, stock: 10); // no está en el catálogo
        SetupCustomerExists();
        SetupCatalog(mouse);

        var act = () => _service.AddOrder(Request((mouse, 1), (ghost, 1)), CustomerId);

        await act.Should().ThrowAsync<EntityNotFoundException>().WithMessage($"*{ghost.Id}*");
        mouse.StockQuantity.Should().Be(10);
        _repository.Verify(r => r.Add(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrder_WhenRepositoryReturnsNullProducts_ThrowsEntityNotFoundException()
    {
        var mouse = NewProduct("MOU-001", 100m, stock: 10);
        SetupCustomerExists();
        _repository
            .Setup(r => r.GetFiltered(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<string[]>()))
            .ReturnsAsync((IEnumerable<Product>?)null);

        var act = () => _service.AddOrder(Request((mouse, 1)), CustomerId);

        await act.Should().ThrowAsync<EntityNotFoundException>();
        _repository.Verify(r => r.Add(It.IsAny<Order>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public async Task CreateOrder_WithInvalidQuantity_ThrowsInvalidDataExceptionWithoutTouchingRepository(int quantity)
    {
        var mouse = NewProduct("MOU-001", 100m, stock: 10);

        var act = () => _service.AddOrder(Request((mouse, quantity)), CustomerId);

        await act.Should().ThrowAsync<InvalidDataException>();
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateOrder_WithoutItems_ThrowsInvalidDataExceptionWithoutTouchingRepository()
    {
        var act = () => _service.AddOrder(Request(), CustomerId);

        await act.Should().ThrowAsync<InvalidDataException>();
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateOrder_WhenPersistingTheOrderFails_PropagatesTheErrorFromTheTransaction()
    {
        // Si el alta de la orden falla dentro de la transacción, el error se propaga
        // (EfRepository hace rollback del descuento de stock; ver pruebas de integración).
        var mouse = NewProduct("MOU-001", 100m, stock: 10);
        SetupCustomerExists();
        SetupCatalog(mouse);
        SetupWrites();
        _repository
            .Setup(r => r.Add(It.IsAny<Order>()))
            .ThrowsAsync(new InvalidOperationException("Fallo de base de datos simulado"));

        var act = () => _service.AddOrder(Request((mouse, 3)), CustomerId);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Fallo de base de datos simulado");
        _repository.Verify(r => r.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
