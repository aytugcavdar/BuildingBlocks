namespace BuildingBlocks.Core.Caching;

/// <summary>
/// Bir MediatR Command'ının, işlem başarılı olduktan sonra belirli bir önbellek anahtarının 
/// veya grubunun temizlenmesi gerektiğini belirten arayüz.
/// </summary>
public interface ICacheRemoverRequest
{
    /// <summary>
    /// Sadece spesifik bir anahtarı silmek için kullanılır. Örn: "GetProductById_3"
    /// </summary>
    string? CacheKey { get; }

    /// <summary>
    /// Bir gruba ("Products") ait tüm listeleme veya bağımlı önbellekleri silmek için kullanılır.
    /// </summary>
    string? CacheGroupKey { get; }

    /// <summary>
    /// Bypass true ise silme işlemi atlanır.
    /// </summary>
    bool BypassCache { get; }
}
