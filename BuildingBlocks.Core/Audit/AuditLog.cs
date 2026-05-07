namespace BuildingBlocks.Core.Audit;

/// <summary>
/// EF Core Interceptor üzerinden veritabanındaki her değişikliği kaydedecek olan sınıf.
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Değişikliği yapan kullanıcının ID'si. Eğer anlaşılamazsa "System".
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Değişen tablonun ismi. Örn: "Products"
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// Değişen verinin türü: "Added", "Modified" veya "Deleted"
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Değişen kaydın Primary Key'leri. ("Id": 5 gibi)
    /// </summary>
    public string KeyValues { get; set; } = string.Empty;

    /// <summary>
    /// Update/Delete işlemlerinde kaydın eski hali (JSON)
    /// </summary>
    public string? OldValues { get; set; }

    /// <summary>
    /// Add/Update işlemlerinde kaydın yeni hali (JSON)
    /// </summary>
    public string? NewValues { get; set; }

    /// <summary>
    /// İşlemin gerçekleştiği tarih.
    /// </summary>
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
}
