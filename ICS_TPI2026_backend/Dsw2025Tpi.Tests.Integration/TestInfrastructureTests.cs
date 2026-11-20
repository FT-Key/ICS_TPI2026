using Dsw2025Tpi.Api;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace Dsw2025Tpi.Tests.Integration;

/// <summary>
/// Smoke test del proyecto: confirma que Program es accesible para
/// WebApplicationFactory y que el proveedor SQLite en memoria funciona.
/// El arnes completo (factory, esquema, JWT) se construye en TP4-06.
/// </summary>
[Trait(TestCategories.Name, TestCategories.Integration)]
public class TestInfrastructureTests
{
    [Fact]
    public void Program_IsPublicPartial_SoWebApplicationFactoryCanUseIt()
    {
        typeof(WebApplicationFactory<Program>).GetGenericArguments()
            .Should().ContainSingle().Which.Should().Be(typeof(Program));
        typeof(Program).IsPublic.Should().BeTrue();
    }

    [Fact]
    public void SqliteInMemory_WithOpenConnection_ExecutesQueries()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1";

        Convert.ToInt32(command.ExecuteScalar()).Should().Be(1);
    }
}
