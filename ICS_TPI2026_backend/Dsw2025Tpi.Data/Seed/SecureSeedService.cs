using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dsw2025Tpi.Data.Seed;

/// <summary>
/// Servicio de seed seguro que:
/// 1. Recibe un JSON externo (no está en el proyecto)
/// 2. Valida los datos antes de insertar
/// 3. Solo ejecuta si SEED_MODE=development
/// 4. Genera log de inserciones
/// </summary>
public class SecureSeedService
{
    private readonly Dsw2025TpiContext _context;
    private readonly ILogger<SecureSeedService> _logger;

    public SecureSeedService(Dsw2025TpiContext context, ILogger<SecureSeedService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Ejecuta el seed seguro desde un archivo JSON externo.
    /// </summary>
    /// <param name="jsonPath">Ruta completa al archivo JSON externo.</param>
    /// <exception cref="InvalidOperationException">Si SEED_MODE no es "development" o el JSON es inválido.</exception>
    public void SeedFromJson(string jsonPath)
    {
        // 1. Verificar que SEED_MODE=development
        var seedMode = Environment.GetEnvironmentVariable("SEED_MODE");
        if (seedMode != "development")
        {
            _logger.LogWarning("Seed abortado: SEED_MODE='{SeedMode}'. Solo se permite 'development'.", seedMode);
            return;
        }

        _logger.LogInformation("Iniciando seed seguro desde: {JsonPath}", jsonPath);

        // 2. Verificar que el archivo existe
        if (!File.Exists(jsonPath))
        {
            _logger.LogWarning("Archivo de seed no encontrado: {JsonPath}. Saltando seed.", jsonPath);
            return;
        }

        // 3. Leer y deserializar el JSON
        SeedData seedData;
        try
        {
            var json = File.ReadAllText(jsonPath);
            seedData = JsonSerializer.Deserialize<SeedData>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new InvalidOperationException("El archivo JSON de seed está vacío o es inválido.");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error al parsear el archivo JSON de seed: {JsonPath}", jsonPath);
            throw new InvalidOperationException($"Error al parsear JSON de seed: {ex.Message}", ex);
        }

        // 4. Validar el seed data
        var validationErrors = ValidateSeedData(seedData);
        if (validationErrors.Count > 0)
        {
            var errorMessage = $"Errores de validación en seed data:\n{string.Join("\n", validationErrors)}";
            _logger.LogError(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        // 5. Ejecutar seed de productos
        if (seedData.Products.Count > 0)
        {
            SeedProducts(seedData.Products);
        }

        // 6. Ejecutar seed de usuarios (si se implementa en el futuro)
        if (seedData.Users.Count > 0)
        {
            _logger.LogWarning("Seed de usuarios no implementado aún. {Count} usuarios ignorados.", seedData.Users.Count);
        }

        _logger.LogInformation("Seed seguro completado exitosamente.");
    }

    /// <summary>
    /// Valida los datos del seed data.
    /// </summary>
    private List<string> ValidateSeedData(SeedData seedData)
    {
        var errors = new List<string>();

        // Validar versión
        if (string.IsNullOrWhiteSpace(seedData.Version))
        {
            errors.Add("El campo 'Version' es requerido.");
        }

        // Validar seed type
        var allowedSeedTypes = new[] { "development", "test", "staging" };
        if (string.IsNullOrWhiteSpace(seedData.SeedType) || !allowedSeedTypes.Contains(seedData.SeedType.ToLower()))
        {
            errors.Add($"El campo 'SeedType' debe ser uno de: {string.Join(", ", allowedSeedTypes)}.");
        }

        // Validar productos
        for (int i = 0; i < seedData.Products.Count; i++)
        {
            var product = seedData.Products[i];
            var prefix = $"Products[{i}]";

            if (string.IsNullOrWhiteSpace(product.Sku))
                errors.Add($"{prefix}.Sku es requerido.");
            else if (product.Sku.Length > 20)
                errors.Add($"{prefix}.Sku no puede exceder 20 caracteres.");

            if (string.IsNullOrWhiteSpace(product.Name))
                errors.Add($"{prefix}.Name es requerido.");
            else if (product.Name.Length > 60)
                errors.Add($"{prefix}.Name no puede exceder 60 caracteres.");

            if (product.CurrentUnitPrice <= 0)
                errors.Add($"{prefix}.CurrentUnitPrice debe ser mayor a 0.");

            if (product.StockQuantity < 0)
                errors.Add($"{prefix}.StockQuantity no puede ser negativo.");
        }

        // Validar que no haya SKUs duplicados
        var skus = seedData.Products.Select(p => p.Sku).ToList();
        var duplicateSkus = skus.GroupBy(s => s).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicateSkus.Any())
        {
            errors.Add($"SKUs duplicados encontrados: {string.Join(", ", duplicateSkus)}.");
        }

        return errors;
    }

    /// <summary>
    /// Sembrar productos en la base de datos.
    /// </summary>
    private void SeedProducts(List<ProductSeed> products)
    {
        // Verificar si ya hay productos
        if (_context.Products.Any())
        {
            _logger.LogInformation("La tabla Products ya tiene datos. Saltando seed de productos.");
            return;
        }

        _logger.LogInformation("Insertando {Count} productos...", products.Count);

        foreach (var productSeed in products)
        {
            var product = new Domain.Entities.Product(
                sku: productSeed.Sku,
                internalCode: productSeed.InternalCode ?? string.Empty,
                name: productSeed.Name,
                description: productSeed.Description ?? string.Empty,
                currentUnitPrice: productSeed.CurrentUnitPrice,
                stockQuantity: productSeed.StockQuantity
            );

            _context.Products.Add(product);
            _logger.LogInformation("  + Producto: {Sku} - {Name} (Precio: {Price}, Stock: {Stock})",
                product.Sku, product.Name, product.CurrentUnitPrice, product.StockQuantity);
        }

        _context.SaveChanges();
        _logger.LogInformation("Productos insertados exitosamente.");
    }
}
