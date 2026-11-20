using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Domain.Entities;

namespace Dsw2025Tpi.Application.Mappers;

public static class ProductMappingExtensions
{
    public static ProductModel.ResponseProductModel MapToResponse(this Product product) =>
        new(
            product.Id,
            product.Sku,
            product.InternalCode,
            product.Name,
            product.Description,
            product.CurrentUnitPrice,
            product.StockQuantity,
            product.IsActive);
}
