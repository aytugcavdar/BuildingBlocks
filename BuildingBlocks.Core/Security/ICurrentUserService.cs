namespace BuildingBlocks.Core.Security;

/// <summary>
/// Sistemde o an aktif olan kullanıcının kimliğini ve bilgilerini almak için kullanılır.
/// Altyapı katmanlarının HTTP context'ine doğrudan bağımlı olmasını engeller.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Aktif kullanıcının ID'si. Eğer yoksa null döner.
    /// </summary>
    string? UserId { get; }
}
