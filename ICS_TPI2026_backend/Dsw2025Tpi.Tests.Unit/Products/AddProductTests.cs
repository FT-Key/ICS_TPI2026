using System.Linq.Expressions;
using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Application.Exceptions;
using Dsw2025Tpi.Application.Services;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using InvalidDataException = Dsw2025Tpi.Application.Exceptions.InvalidDataException;

namespace Dsw2025Tpi.Tests.Unit.Products;

/// <summary>
/// TP4-03: pruebas unitarias de ProductsManagementService.AddProduct.
/// La base de datos se aísla con un mock estricto de IRepository: cualquier llamada
/// no configurada hace fallar la prueba, lo que permite verificar el orden de las interacciones.
/// </summary>
[Trait(TestCategories.Name, TestCategories.Unit)]
public class AddProductTests
{
    private readonly Mock<IRepository> _repository = new(MockBehavior.Strict);
    private readonly Mock<ILogger<ProductsManagementService>> _logger = new();
    private readonly ProductsManagementService _service;

    public AddProductTests()
    {
        _service = new ProductsManagementService(_repository.Object, _logger.Object);
    }

    private static ProductModel.RequestProductModel ValidRequest(
        string sku = "MOU-001",
        decimal price = 1500.50m,
        int stock = 10) =>
        new(sku, "INT-MOU-001", "Mouse inalambrico", "Mouse optico de 1600 DPI", price, stock);

    /// <summary>
    /// Configura First&lt;Product&gt; evaluando el predicado real del servicio contra el catálogo dado,
    /// así el mock solo devuelve un producto si la consulta efectivamente filtra por ese SKU.
    /// </summary>
    private void SetupExistingCatalog(params Product[] catalog)
    {
        _repository
            .Setup(r => r.First(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<string[]>()))
            .ReturnsAsync((Expression<Func<Product, bool>> predicate, string[] _) =>
                catalog.AsQueryable().FirstOrDefault(predicate));
    }

    [Fact]
    public async Task AddProduct_WithValidData_ReturnsProductAndCallsRepositoryAdd()
    {
        // Arrange
        var request = ValidRequest();
        SetupExistingCatalog(new Product("OTRO-SKU", "INT-9", "Teclado", "Teclado mecanico", 900m, 5));
        _repository
            .Setup(r => r.Add(It.IsAny<Product>()))
            .ReturnsAsync((Product product) => product);

        // Act
        var result = await _service.AddProduct(request);

        // Assert: la respuesta refleja los datos enviados y el producto nace activo.
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Sku.Should().Be(request.Sku);
        result.InternalCode.Should().Be(request.InternalCode);
        result.Name.Should().Be(request.Name);
        result.Description.Should().Be(request.Description);
        result.CurrentUnitPrice.Should().Be(request.CurrentUnitPrice + 1, "falla a proposito: evidencia del gate");
        result.StockQuantity.Should().Be(request.StockQuantity);
        result.IsActive.Should().BeTrue("todo producto nuevo se crea habilitado");

        // Assert: se consultó el SKU una vez y se persistió exactamente un producto con esos valores.
        _repository.Verify(r => r.First(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<string[]>()), Times.Once);
        _repository.Verify(r => r.Add(It.Is<Product>(p =>
            p.Id == result.Id &&
            p.Sku == request.Sku &&
            p.InternalCode == request.InternalCode &&
            p.Name == request.Name &&
            p.Description == request.Description &&
            p.CurrentUnitPrice == request.CurrentUnitPrice &&
            p.StockQuantity == request.StockQuantity &&
            p.IsActive)), Times.Once);
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddProduct_WithDuplicateSku_ThrowsDuplicatedEntityException()
    {
        // Arrange: el repositorio ya contiene un producto con el mismo SKU.
        var request = ValidRequest(sku: "DUP-001");
        SetupExistingCatalog(new Product("DUP-001", "INT-OLD", "Producto existente", "Ya cargado", 100m, 3));

        // Act
        var act = () => _service.AddProduct(request);

        // Assert
        (await act.Should().ThrowAsync<DuplicatedEntityException>())
            .WithMessage("*DUP-001*", "el mensaje debe indicar qué SKU está repetido");

        _repository.Verify(r => r.First(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<string[]>()), Times.Once);
        _repository.Verify(r => r.Add(It.IsAny<Product>()), Times.Never);
        _repository.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(0, 10)]      // precio en cero
    [InlineData(-25.5, 10)]  // precio negativo
    [InlineData(1500, 0)]    // stock en cero
    [InlineData(1500, -3)]   // stock negativo
    public async Task AddProduct_WithInvalidStockOrPrice_ThrowsInvalidDataException(double price, int stock)
    {
        // Arrange: no se configura ningún método del mock estricto;
        // si el servicio llegara a tocar el repositorio, Moq lanzaría MockException.
        var request = ValidRequest(price: (decimal)price, stock: stock);

        // Act
        var act = () => _service.AddProduct(request);

        // Assert: la validación ocurre antes de consultar o escribir en el repositorio.
        await act.Should().ThrowAsync<InvalidDataException>();

        _repository.Verify(r => r.First(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<string[]>()), Times.Never);
        _repository.Verify(r => r.Add(It.IsAny<Product>()), Times.Never);
        _repository.VerifyNoOtherCalls();
    }
}
