using Dsw2025Tpi.Application.Dtos;
using InvalidDataException = Dsw2025Tpi.Application.Exceptions.InvalidDataException;

namespace Dsw2025Tpi.Application.Validation
{
    public static class OrderValidator
    {
        public static void Validate(OrderModel.RequestOrderModel request)
        {
            if (request == null)
                throw new InvalidDataException("La orden no puede ser vacia.");

            if (string.IsNullOrWhiteSpace(request.ShippingAddress) || request.ShippingAddress.Length > 256)
                throw new InvalidDataException("La direccion de envio es requerida.");

            if (string.IsNullOrWhiteSpace(request.BillingAddress) || request.BillingAddress.Length > 256)
                throw new InvalidDataException("La direccion de facturacion es requerida.");

            if (request.OrderItems == null || request.OrderItems.Count == 0)
                throw new InvalidDataException("Se debe incluir al menos un item en la orden.");

            foreach (var item in request.OrderItems)
            {
                OrderItemValidator.Validate(item);
            }
        }
    }
}
