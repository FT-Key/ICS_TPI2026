using System.Linq.Expressions;
using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Application.Services;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Dsw2025Tpi.Tests.Unit.Products;

/// <summary>
/// TP4-04: pruebas unitarias del listado de productos (ProductsManagementService.GetProducts):
/// filtrado del catálogo público, matemática de paginación y búsqueda parcial case-insensitive.
/// El mock de IRepository evalúa el predicado real del servicio sobre una lista en memoria,
/// de modo que las pruebas verifican la expresión que luego EF Core traduce a SQL.
/// </summary>
[Trait(TestCategories.Name, TestCategories.Unit)]
public class GetProductsTests
{
    private const int CatalogSize = 15;

    private readonly Mock<IRepository> _repository = new(MockBehavior.Strict);
    private readonly ProductsManagementService _service;

    public GetProductsTests()
    {
        _service = new ProductsManagementService(_repository.Object, Mock.Of<ILogger<ProductsManagementService>>());
    }

    /// <summary>
    /// 15 productos con SKU PRD-01..PRD-15 entregados en orden inverso (para probar el orden por Sku).
    /// Los SKU pares están inactivos: 8 activos y 7 inactivos.
    /// </summary>
    private static List<Product> BuildCatalog()
    {
        return Enumerable.Range(1, CatalogSize)
            .Reverse()
            .Select(i => new Product(
                sku: $"PRD-{i:00}",
                internalCode: $"INT-{i:00}",
                name: $"Producto {i:00}",
                description: $"Descripcion {i:00}",
                currentUnitPrice: 100m + i,
                stockQuantity: i)
            {
                IsActive = i % 2 != 0
            })
            .ToList();
    }

    private void SetupRepository(IEnumerable<Product> catalog)
    {
        var data = catalog.ToList();
        _repository
            .Setup(r => r.GetFiltered(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<string[]>()))
            .ReturnsAsync((Expression<Func<Product, bool>> predicate, string[] _) =>
                data.AsQueryable().Where(predicate).ToList());
    }

    private static ProductModel.FilterProduct Filter(
        string? status = null,
        string? search = null,
        int? pageNumber = null,
        int? pageSize = null) => new(status, search, pageNumber, pageSize);

    [Fact]
    public async Task GetProducts_WhenIncludeInactiveIsFalse_ReturnsOnlyActiveProducts()
    {
        // Arrange: catálogo mixto; el cliente intenta forzar los inactivos con status=disabled.
        var catalog = BuildCatalog();
        SetupRepository(catalog);
        var expectedActive = catalog.Count(p => p.IsActive);

        // Act
        var result = await _service.GetProducts(Filter(status: "disabled", pageNumber: 1, pageSize: 50), includeInactive: false);

        // Assert
        result.TotalItems.Should().Be(expectedActive, "el catálogo público solo cuenta productos activos");
        result.ProductItems.Should().HaveCount(expectedActive)
            .And.OnlyContain(p => p.IsActive, "el endpoint público nunca expone inactivos, aunque se pida status=disabled");
        result.ProductItems.Select(p => p.Sku).Should().NotContain(catalog.Where(p => !p.IsActive).Select(p => p.Sku));
        _repository.Verify(r => r.GetFiltered(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<string[]>()), Times.Once);
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetProducts_WithPaginationFilter_AppliesSkipAndTakeCorrectly()
    {
        // Arrange: el panel admin ve los 15 productos (activos e inactivos).
        SetupRepository(BuildCatalog());

        // Act: segunda página de 5 elementos.
        var result = await _service.GetProducts(Filter(pageNumber: 2, pageSize: 5), includeInactive: true);

        // Assert: Skip((2-1)*5).Take(5) sobre el catálogo ordenado por Sku -> PRD-06..PRD-10.
        result.TotalItems.Should().Be(CatalogSize, "TotalItems es el total filtrado sin paginar");
        result.ProductItems.Should().HaveCount(5);
        result.ProductItems.Select(p => p.Sku).Should().Equal(
            ["PRD-06", "PRD-07", "PRD-08", "PRD-09", "PRD-10"],
            "la paginación se aplica después de ordenar de forma estable por Sku");
    }

    [Theory]
    [InlineData(1, 5, new[] { "PRD-01", "PRD-02", "PRD-03", "PRD-04", "PRD-05" })]
    [InlineData(3, 5, new[] { "PRD-11", "PRD-12", "PRD-13", "PRD-14", "PRD-15" })]
    [InlineData(2, 10, new[] { "PRD-11", "PRD-12", "PRD-13", "PRD-14", "PRD-15" })]
    [InlineData(4, 5, new string[0])]
    public async Task GetProducts_WithPageBoundaries_ReturnsExpectedSlice(int pageNumber, int pageSize, string[] expectedSkus)
    {
        SetupRepository(BuildCatalog());

        var result = await _service.GetProducts(Filter(pageNumber: pageNumber, pageSize: pageSize), includeInactive: true);

        result.TotalItems.Should().Be(CatalogSize);
        result.ProductItems.Select(p => p.Sku).Should().Equal(expectedSkus);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(0, 0)]
    [InlineData(-1, -5)]
    public async Task GetProducts_WithMissingOrInvalidPagination_ReturnsFirstPageWithAllItems(int? pageNumber, int? pageSize)
    {
        SetupRepository(BuildCatalog());

        var result = await _service.GetProducts(Filter(pageNumber: pageNumber, pageSize: pageSize), includeInactive: true);

        result.TotalItems.Should().Be(CatalogSize);
        result.ProductItems.Should().HaveCount(CatalogSize);
        result.ProductItems.Select(p => p.Sku).Should().BeInAscendingOrder(StringComparer.Ordinal);
    }

    [Theory]
    [InlineData("producto 07", "PRD-07")] // Name, en minúsculas
    [InlineData("prd-09", "PRD-09")]      // Sku, en minúsculas
    [InlineData("INT-11", "PRD-11")]      // InternalCode, en mayúsculas
    [InlineData("  Int-13  ", "PRD-13")]  // con espacios y mayúsculas mezcladas
    public async Task GetProducts_WithSearchTerm_FiltersByNameSkuOrInternalCode(string search, string expectedSku)
    {
        // Arrange
        SetupRepository(BuildCatalog());

        // Act
        var result = await _service.GetProducts(Filter(search: search, pageNumber: 1, pageSize: 10), includeInactive: false);

        // Assert: coincidencia parcial y sin distinguir mayúsculas en cualquiera de los tres campos.
        result.TotalItems.Should().Be(1);
        result.ProductItems.Should().ContainSingle().Which.Sku.Should().Be(expectedSku);
    }

    [Fact]
    public async Task GetProducts_WithPartialSearchTerm_ReturnsEveryMatchSortedBySku()
    {
        // "1" aparece en PRD-01, PRD-10..PRD-15 (y sus nombres / códigos internos); solo los activos son públicos.
        SetupRepository(BuildCatalog());

        var result = await _service.GetProducts(Filter(search: "1", pageNumber: 1, pageSize: 20), includeInactive: false);

        result.ProductItems.Select(p => p.Sku).Should().Equal("PRD-01", "PRD-11", "PRD-13", "PRD-15");
        result.TotalItems.Should().Be(4);
    }

    [Fact]
    public async Task GetProducts_WithSearchTermWithoutMatches_ReturnsEmptyPage()
    {
        SetupRepository(BuildCatalog());

        var result = await _service.GetProducts(Filter(search: "inexistente", pageNumber: 1, pageSize: 10), includeInactive: true);

        result.TotalItems.Should().Be(0);
        result.ProductItems.Should().BeEmpty();
    }

    [Theory]
    [InlineData("enabled", 8, true)]
    [InlineData("disabled", 7, false)]
    public async Task GetProducts_AdminWithStatusFilter_ReturnsOnlyProductsInThatStatus(string status, int expectedCount, bool expectedIsActive)
    {
        SetupRepository(BuildCatalog());

        var result = await _service.GetProducts(Filter(status: status, pageNumber: 1, pageSize: 50), includeInactive: true);

        result.TotalItems.Should().Be(expectedCount);
        result.ProductItems.Should().HaveCount(expectedCount).And.OnlyContain(p => p.IsActive == expectedIsActive);
    }

    [Fact]
    public async Task GetProducts_WhenRepositoryReturnsNull_ReturnsEmptyPage()
    {
        _repository
            .Setup(r => r.GetFiltered(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<string[]>()))
            .ReturnsAsync((IEnumerable<Product>?)null);

        var result = await _service.GetProducts(Filter(pageNumber: 1, pageSize: 10), includeInactive: false);

        result.TotalItems.Should().Be(0);
        result.ProductItems.Should().BeEmpty();
    }
}
