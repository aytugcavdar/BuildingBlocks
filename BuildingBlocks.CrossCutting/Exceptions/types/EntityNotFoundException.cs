namespace BuildingBlocks.CrossCutting.Exceptions.Types;

/// <summary>
/// Belirli bir Id ile istenen entity bulunamadığında fırlatılan exception.
/// HTTP 404 Not Found döner.
/// Örnek: new EntityNotFoundException("Product", productId)
/// </summary>
public class EntityNotFoundException : NotFoundException
{
    public string EntityName { get; }
    public object Key { get; }

    public EntityNotFoundException(string entityName, object key)
        : base($"{entityName} with identifier '{key}' was not found.")
    {
        EntityName = entityName;
        Key = key;
    }
}