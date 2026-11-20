using Dsw2025Tpi.Application.Services;
using Dsw2025Tpi.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Dsw2025Tpi.Tests.Unit;

/// <summary>
/// Smoke test del proyecto: confirma que xUnit, Moq y FluentAssertions estan
/// disponibles y que el servicio de Application se construye sin base de datos.
/// </summary>
[Trait(TestCategories.Name, TestCategories.Unit)]
public class TestInfrastructureTests
{
    [Fact]
    public void ProductsManagementService_WithMockedDependencies_CanBeConstructed()
    {
        var repository = new Mock<IRepository>(MockBehavior.Strict);
        var logger = new Mock<ILogger<ProductsManagementService>>();

        var service = new ProductsManagementService(repository.Object, logger.Object);

        service.Should().NotBeNull();
        repository.VerifyNoOtherCalls();
    }
}
