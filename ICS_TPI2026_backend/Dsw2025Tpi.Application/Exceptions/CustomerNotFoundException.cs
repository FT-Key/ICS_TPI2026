namespace Dsw2025Tpi.Application.Exceptions;

/// <summary>
/// El cliente asociado a la operación no existe (TP4-02 / TP4-05).
/// Hereda de EntityNotFoundException para que los manejadores existentes la sigan capturando.
/// </summary>
public class CustomerNotFoundException : EntityNotFoundException
{
    public CustomerNotFoundException(Guid customerId)
        : base($"Customer con ID {customerId} not found.")
    {
        CustomerId = customerId;
    }

    public Guid CustomerId { get; }
}
