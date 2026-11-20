namespace Dsw2025Tpi.Tests.Integration.Infrastructure;

/// <summary>
/// Base de las pruebas de integración: resetea la base antes de cada test,
/// de modo que cada prueba aísla sus datos y puede ejecutarse sola.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
[Trait(TestCategories.Name, TestCategories.Integration)]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected IntegrationTestBase(CustomWebApplicationFactory factory)
    {
        Factory = factory;
    }

    protected CustomWebApplicationFactory Factory { get; }

    protected HttpClient CreateClient() => Factory.CreateClient();

    protected HttpClient CreateAdminClient() => CreateClient().WithAuth(TestJwtTokens.Admin());

    protected HttpClient CreateUserClient(Guid customerId) => CreateClient().WithAuth(TestJwtTokens.User(customerId));

    public Task InitializeAsync() => Factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;
}
