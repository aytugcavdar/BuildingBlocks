namespace BuildingBlocks.CrossCutting.Exceptions.Types;

/// <summary>
/// Kaynak bulunamadığında fırlatılan base exception.
/// HTTP 404 Not Found döner.
/// Daha spesifik durumlar için EntityNotFoundException kullan.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }

    public NotFoundException(string name, object key)
        : base($"{name} with id ({key}) was not found") { }
}
