namespace Dsw2025Tpi.Application.Exceptions;

/// <summary>
/// Datos de entrada inválidos (campos obligatorios vacíos, precio o stock fuera de rango).
/// Unifica las validaciones de la capa Application (TP4-02); los controladores la traducen a 400.
/// Nota: convive con System.IO.InvalidDataException, por eso los archivos que la usan
/// declaran un alias "using InvalidDataException = Dsw2025Tpi.Application.Exceptions.InvalidDataException;".
/// </summary>
public class InvalidDataException : ApplicationException
{
    public InvalidDataException(string message) : base(message)
    {
    }
}
