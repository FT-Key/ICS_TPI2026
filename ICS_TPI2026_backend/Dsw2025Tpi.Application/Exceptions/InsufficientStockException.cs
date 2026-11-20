namespace Dsw2025Tpi.Application.Exceptions;

/// <summary>
/// Se lanza cuando la cantidad pedida de un producto supera su stock disponible (TP4-02).
/// OrdersController la traduce a 400 Bad Request.
/// </summary>
public class InsufficientStockException : ApplicationException
{
    public InsufficientStockException(Guid productId, string productName, int availableStock, int requestedQuantity)
        : base($"Stock insuficiente para el producto '{productName}': disponible {availableStock}, solicitado {requestedQuantity}.")
    {
        ProductId = productId;
        AvailableStock = availableStock;
        RequestedQuantity = requestedQuantity;
    }

    public Guid ProductId { get; }
    public int AvailableStock { get; }
    public int RequestedQuantity { get; }
}
