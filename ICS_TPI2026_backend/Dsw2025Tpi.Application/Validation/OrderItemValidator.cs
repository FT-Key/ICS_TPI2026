using Dsw2025Tpi.Application.Dtos;
using InvalidDataException = Dsw2025Tpi.Application.Exceptions.InvalidDataException;

namespace Dsw2025Tpi.Application.Validation
{
    public static class OrderItemValidator
    {
        public static void Validate(OrderItemModel.RequestOrderItemModel item)
        {
            if (item == null)
                throw new InvalidDataException("El item de la orden no puede ser nulo.");

            if (item.ProductId == Guid.Empty)
                throw new InvalidDataException("El producto es obligatorio.");

            if (item.Quantity <= 0)
                throw new InvalidDataException("La cantidad debe ser mayor a 0.");
        }
    }
}
