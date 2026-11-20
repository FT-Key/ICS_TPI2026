using Dsw2025Tpi.Application.Dtos;
using InvalidDataException = Dsw2025Tpi.Application.Exceptions.InvalidDataException;

namespace Dsw2025Tpi.Application.Validation
{
    public static class ProductValidator
    {
        // TP4-02: todas las validaciones de datos lanzan InvalidDataException y
        // se ejecutan antes de cualquier acceso al repositorio.
        public static void Validate(ProductModel.RequestProductModel request)
        {
            if (request == null)
                throw new InvalidDataException("El producto no puede ser nulo.");

            if (string.IsNullOrWhiteSpace(request.Sku))
                throw new InvalidDataException("SKU es requerido");

            if (string.IsNullOrWhiteSpace(request.InternalCode))
                throw new InvalidDataException("El codigo interno es obligatorio.");

            if (string.IsNullOrWhiteSpace(request.Name))
                throw new InvalidDataException("Nombre es requerido.");

            if (string.IsNullOrWhiteSpace(request.Description))
                throw new InvalidDataException("La descripcion es requerida.");

            if (request.CurrentUnitPrice <= 0)
                throw new InvalidDataException("El precio debe ser un valor positivo.");

            if (request.StockQuantity <= 0)
                throw new InvalidDataException("La cantidad de stock debe ser mayor a 0.");
        }
    }
}
