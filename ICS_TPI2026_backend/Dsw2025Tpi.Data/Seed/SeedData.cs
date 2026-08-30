namespace Dsw2025Tpi.Data.Seed;

/// <summary>
/// Modelo principal para el seed data externo.
/// Define el formato esperado del JSON de seed.
/// </summary>
public class SeedData
{
    /// <summary>
    /// Versión del esquema de seed.
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Tipo de seed: "development", "test", "staging".
    /// Solo se permite "development" para ejecutar el seed.
    /// </summary>
    public string SeedType { get; set; } = string.Empty;

    /// <summary>
    /// Lista de productos a sembrar.
    /// </summary>
    public List<ProductSeed> Products { get; set; } = new();

    /// <summary>
    /// Lista de usuarios a sembrar.
    /// </summary>
    public List<UserSeed> Users { get; set; } = new();
}

/// <summary>
/// Modelo para productos en el seed data.
/// </summary>
public class ProductSeed
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? InternalCode { get; set; }
    public decimal CurrentUnitPrice { get; set; }
    public int StockQuantity { get; set; }
}

/// <summary>
/// Modelo para usuarios en el seed data.
/// </summary>
public class UserSeed
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public string? PhoneNumber { get; set; }
}
