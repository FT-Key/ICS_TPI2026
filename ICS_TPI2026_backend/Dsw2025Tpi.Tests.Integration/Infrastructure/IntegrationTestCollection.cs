namespace Dsw2025Tpi.Tests.Integration.Infrastructure;

/// <summary>
/// Todas las clases de integración comparten un único factory (y su SQLite) mediante
/// ICollectionFixture; xunit.runner.json desactiva el paralelismo entre colecciones.
/// </summary>
[CollectionDefinition(Name)]
public class IntegrationTestCollection : ICollectionFixture<CustomWebApplicationFactory>
{
    public const string Name = "Integration";
}
