using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Tests.Integration.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Dsw2025Tpi.Tests.Integration.Products;

/// <summary>
/// TP4-07: interacción real entre ProductsController y SQLite (autorización por rol,
/// persistencia, conflicto de SKU, contrato de paginación y filtros por query string).
/// Los asserts de base de datos usan un DbContext scopeado al factory.
/// </summary>
public class ProductsEndpointsTests : IntegrationTestBase
{
    private const string ProductsUrl = "/api/products";

    public ProductsEndpointsTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    private static ProductModel.RequestProductModel NewProductRequest(string sku = "MOU-001") =>
        new(sku, $"INT-{sku}", "Mouse inalambrico", "Mouse optico de 1600 DPI", 1500.50m, 10);

    private static Product NewProduct(string sku, string name, bool isActive = true, string? internalCode = null) =>
        new(sku, internalCode ?? $"INT-{sku}", name, $"Descripcion de {name}", 100m, 5) { IsActive = isActive };

    private Task<int> CountProductsAsync() =>
        Factory.WithDbContextAsync(context => context.Products.CountAsync());

    [Fact]
    public async Task AddProduct_WhenUnauthenticatedOrNotAdmin_ReturnsUnauthorizedOrForbidden()
    {
        // Act
        var anonymous = await CreateClient().PostAsJsonAsync(ProductsUrl, NewProductRequest());
        var asUser = await CreateClient().WithAuth(TestJwtTokens.User()).PostAsJsonAsync(ProductsUrl, NewProductRequest());

        // Assert: 401 sin token, 403 con rol User y nada persistido.
        anonymous.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        asUser.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await CountProductsAsync()).Should().Be(0, "ninguna petición no autorizada debe insertar productos");
    }

    [Fact]
    public async Task AddProduct_WhenAuthenticatedAsAdmin_PersistsInSqliteAndReturnsCreated()
    {
        // Arrange
        var request = NewProductRequest("KEY-777");

        // Act
        var response = await CreateAdminClient().PostAsJsonAsync(ProductsUrl, request);

        // Assert: contrato HTTP.
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ProductModel.ResponseProductModel>();
        body.Should().NotBeNull();
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.AbsolutePath.Should().BeEquivalentTo($"{ProductsUrl}/{body!.Id}");

        // Assert: fila insertada en Products con los valores enviados.
        var saved = await Factory.WithDbContextAsync(context =>
            context.Products.AsNoTracking().SingleOrDefaultAsync(p => p.Id == body.Id));
        saved.Should().NotBeNull();
        saved!.Sku.Should().Be(request.Sku);
        saved.InternalCode.Should().Be(request.InternalCode);
        saved.Name.Should().Be(request.Name);
        saved.Description.Should().Be(request.Description);
        saved.CurrentUnitPrice.Should().Be(request.CurrentUnitPrice);
        saved.StockQuantity.Should().Be(request.StockQuantity);
        saved.IsActive.Should().BeTrue();

        // El Location apunta a un recurso que existe.
        var fetched = await CreateAdminClient().GetAsync(response.Headers.Location);
        fetched.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AddProduct_WhenSkuAlreadyExistsInDatabase_ReturnsConflictStatusCode()
    {
        // Arrange: el SKU ya está en SQLite.
        await Factory.SeedAsync(NewProduct("DUP-001", "Producto existente"));

        // Act
        var response = await CreateAdminClient().PostAsJsonAsync(ProductsUrl, NewProductRequest("DUP-001"));

        // Assert: 409 (nunca 500), mensaje claro y sin duplicar la fila.
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync()).Should().Contain("DUP-001");
        (await CountProductsAsync()).Should().Be(1);
    }

    [Fact]
    public async Task GetProducts_PublicEndpoint_ReturnsOkWithPaginatedJsonStructure()
    {
        // Arrange
        await Factory.SeedAsync(
            NewProduct("AAA-001", "Auriculares"),
            NewProduct("BBB-002", "Bocina"),
            NewProduct("CCC-003", "Camara"));

        // Act: endpoint anónimo.
        var response = await CreateClient().GetAsync($"{ProductsUrl}?pageNumber=1&pageSize=2");

        // Assert: 200 con { productItems: [...], totalItems: n }.
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        root.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(["productItems", "totalItems"]);
        root.GetProperty("totalItems").GetInt32().Should().Be(3, "totalItems es el total sin paginar");

        var items = root.GetProperty("productItems");
        items.GetArrayLength().Should().Be(2);
        items[0].EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(
            ["id", "sku", "internalCode", "name", "description", "currentUnitPrice", "stockQuantity", "isActive"]);
        items.EnumerateArray().Select(i => i.GetProperty("sku").GetString()).Should().Equal("AAA-001", "BBB-002");
    }

    [Fact]
    public async Task GetProducts_WithQueryStringFilters_ReturnsFilteredResultsFromSqlite()
    {
        // Arrange: mezcla de coincidencias por Name, Sku e InternalCode, más ruido e inactivos.
        await Factory.SeedAsync(
            NewProduct("MOU-001", "Mouse optico"),
            NewProduct("ACC-777", "Mousepad gamer"),
            NewProduct("KEY-001", "Teclado", internalCode: "INT-MOUSE-COMBO"),
            NewProduct("MOU-002", "Mouse viejo", isActive: false),
            NewProduct("MON-001", "Monitor 24"),
            NewProduct("CAB-001", "Cable HDMI"));
        Factory.SqlCommands.Clear();

        // Act
        var response = await CreateClient().GetAsync($"{ProductsUrl}?search=MOUSE&pageNumber=1&pageSize=10");

        // Assert: coincidencias case-insensitive en los tres campos, solo activos, ordenadas por Sku.
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<ProductModel.ResponsePagination>();
        page!.TotalItems.Should().Be(3);
        page.ProductItems.Select(p => p.Sku).Should().Equal("ACC-777", "KEY-001", "MOU-001");
        page.ProductItems.Should().OnlyContain(p => p.IsActive);

        // Assert: el filtro se resolvió en SQL (WHERE sobre los tres campos en minúsculas), no en memoria.
        // EF Core 9 traduce string.Contains a instr(...) > 0 en SQLite y a LIKE en SQL Server.
        var productQuery = Factory.SqlCommands.Commands.Should()
            .ContainSingle(c => c.Contains("FROM \"Products\"")).Subject;
        productQuery.Should().Contain("WHERE");
        productQuery.Should().Contain("\"IsActive\"");
        productQuery.Should().Contain("lower(\"p\".\"Name\")")
            .And.Contain("lower(\"p\".\"Sku\")")
            .And.Contain("lower(\"p\".\"InternalCode\")");
        productQuery.Should().MatchRegex(@"instr\(|LIKE", "la búsqueda parcial debe ejecutarse en la base");
    }

    [Fact]
    public async Task GetProducts_WithPageNumberAndSize_ReturnsTheRequestedSlice()
    {
        await Factory.SeedAsync(Enumerable.Range(1, 7)
            .Select(i => NewProduct($"PRD-{i:00}", $"Producto {i:00}"))
            .ToArray<EntityBase>());

        var page = await CreateClient().GetFromJsonAsync<ProductModel.ResponsePagination>($"{ProductsUrl}?pageNumber=2&pageSize=3");

        page!.TotalItems.Should().Be(7);
        page.ProductItems.Select(p => p.Sku).Should().Equal("PRD-04", "PRD-05", "PRD-06");
    }

    [Fact]
    public async Task GetProducts_PublicEndpoint_NeverReturnsInactiveProducts()
    {
        await Factory.SeedAsync(
            NewProduct("ACT-001", "Activo"),
            NewProduct("INA-001", "Inactivo", isActive: false));

        var page = await CreateClient().GetFromJsonAsync<ProductModel.ResponsePagination>($"{ProductsUrl}?status=disabled");

        page!.TotalItems.Should().Be(1);
        page.ProductItems.Should().ContainSingle().Which.Sku.Should().Be("ACT-001");
    }

    [Fact]
    public async Task GetAuthProducts_AdminEndpoint_WhenCalledByAdmin_IncludesInactiveProducts()
    {
        // Arrange
        await Factory.SeedAsync(
            NewProduct("ACT-001", "Activo"),
            NewProduct("INA-001", "Inactivo", isActive: false),
            NewProduct("INA-002", "Inactivo 2", isActive: false));

        // Act
        var all = await CreateAdminClient().GetFromJsonAsync<ProductModel.ResponsePagination>($"{ProductsUrl}/admin?pageNumber=1&pageSize=10");
        var disabled = await CreateAdminClient().GetFromJsonAsync<ProductModel.ResponsePagination>($"{ProductsUrl}/admin?status=disabled");
        var asUser = await CreateClient().WithAuth(TestJwtTokens.User()).GetAsync($"{ProductsUrl}/admin");
        var anonymous = await CreateClient().GetAsync($"{ProductsUrl}/admin");

        // Assert: el admin ve los inactivos; el endpoint sigue protegido por rol.
        all!.TotalItems.Should().Be(3);
        all.ProductItems.Should().Contain(p => !p.IsActive);
        disabled!.ProductItems.Select(p => p.Sku).Should().Equal("INA-001", "INA-002");
        asUser.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        anonymous.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddProduct_WithInvalidStockOrPrice_ReturnsBadRequestWithoutPersisting()
    {
        var zeroStock = NewProductRequest("BAD-001") with { StockQuantity = 0 };
        var zeroPrice = NewProductRequest("BAD-002") with { CurrentUnitPrice = 0 };

        var stockResponse = await CreateAdminClient().PostAsJsonAsync(ProductsUrl, zeroStock);
        var priceResponse = await CreateAdminClient().PostAsJsonAsync(ProductsUrl, zeroPrice);

        stockResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        priceResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await CountProductsAsync()).Should().Be(0);
    }
}
