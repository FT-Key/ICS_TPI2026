using System.Linq.Expressions;
using System.Security.Claims;
using System.Text.Json;
using Dsw2025Tpi.Api.Controllers;
using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Application.Services;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Dsw2025Tpi.Tests;

public class OrderBolaTests
{
    private static readonly Guid UserA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid UserB = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void RequestOrderModel_NoExponeCustomerId()
    {
        var customerIdProperty = typeof(OrderModel.RequestOrderModel).GetProperty("CustomerId");

        Assert.Null(customerIdProperty);
    }

    [Fact]
    public async Task AddOrder_IgnoraCustomerIdDelBody_YCreaLaOrdenParaElUsuarioDelToken()
    {
        var product = new Product("SKU-1", "INT-1", "Producto", "Descripcion", 10m, 5);

        // Arrange: el body simula un ataque BOLA enviando el CustomerId de otro usuario.
        var json = $$"""
        {
          "customerId": "{{UserB}}",
          "shippingAddress": "Calle Falsa 123",
          "billingAddress": "Calle Falsa 123",
          "notes": "",
          "orderItems": [ { "productId": "{{product.Id}}", "quantity": 1 } ]
        }
        """;

        var request = JsonSerializer.Deserialize<OrderModel.RequestOrderModel>(
            json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(request);

        var repository = new FakeRepository(
            new Customer(UserA, "Usuario A", "a@test.com"),
            new Customer(UserB, "Usuario B", "b@test.com"),
            product);

        var controller = CreateController(repository, UserA);

        // Act
        var result = await controller.AddOrder(request);

        // Assert: la orden queda a nombre del usuario autenticado (A), no del body (B).
        var created = Assert.IsType<CreatedAtActionResult>(result);
        var response = Assert.IsType<OrderModel.ResponseOrderModel>(created.Value);
        Assert.Equal(UserA, response.CustomerId);

        var savedOrder = Assert.Single(repository.Orders);
        Assert.Equal(UserA, savedOrder.CustomerId);
    }

    [Fact]
    public async Task AddOrder_SinClaimId_DevuelveUnauthorized()
    {
        var controller = CreateController(new FakeRepository(), authenticatedCustomerId: null);

        var request = new OrderModel.RequestOrderModel("Calle 1", "Calle 2", null, []);

        var result = await controller.AddOrder(request);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    private static OrdersController CreateController(IRepository repository, Guid? authenticatedCustomerId)
    {
        var claims = new List<Claim>();
        if (authenticatedCustomerId.HasValue)
        {
            claims.Add(new Claim("id", authenticatedCustomerId.Value.ToString()));
        }

        return new OrdersController(new OrdersManagementService(repository))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
                }
            }
        };
    }

    private sealed class FakeRepository : IRepository
    {
        private readonly List<EntityBase> _entities;

        public FakeRepository(params EntityBase[] entities)
        {
            _entities = [.. entities];
        }

        public List<Order> Orders => _entities.OfType<Order>().ToList();

        public Task<T?> GetById<T>(Guid id, params string[] include) where T : EntityBase
            => Task.FromResult(_entities.OfType<T>().FirstOrDefault(e => e.Id == id));

        public Task<IEnumerable<T>?> GetAll<T>(params string[] include) where T : EntityBase
            => Task.FromResult<IEnumerable<T>?>(_entities.OfType<T>().ToList());

        public Task<T?> First<T>(Expression<Func<T, bool>> predicate, params string[] include) where T : EntityBase
            => Task.FromResult(_entities.OfType<T>().AsQueryable().FirstOrDefault(predicate));

        public Task<IEnumerable<T>?> GetFiltered<T>(Expression<Func<T, bool>> predicate, params string[] include) where T : EntityBase
            => Task.FromResult<IEnumerable<T>?>(_entities.OfType<T>().AsQueryable().Where(predicate).ToList());

        public Task<T> Add<T>(T entity) where T : EntityBase
        {
            _entities.Add(entity);
            return Task.FromResult(entity);
        }

        public Task<T> Update<T>(T entity) where T : EntityBase
            => Task.FromResult(entity);

        public Task<T> Delete<T>(T entity) where T : EntityBase
        {
            _entities.Remove(entity);
            return Task.FromResult(entity);
        }
    }
}
