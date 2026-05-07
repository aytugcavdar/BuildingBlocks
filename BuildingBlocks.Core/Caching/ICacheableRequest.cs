namespace BuildingBlocks.Core.Caching;

/// <summary>
/// Bir MediatR Query'sinin dönen sonucunun önbelleğe alınabileceğini belirten arayüz.
/// CachingBehavior bu arayüzü implement eden istekleri yakalayıp Redis/Memory üzerinden yönetir.
/// </summary>
public interface ICacheableRequest
{
    /// <summary>
    /// Önbellek anahtarı. Örn: "GetProductById_3"
    /// </summary>
    string CacheKey { get; }

    /// <summary>
    /// Aynı gruba ait önbellekleri tek seferde silebilmek için kullanılan grup anahtarı. Örn: "Products"
    /// </summary>
    string? CacheGroupKey { get; }

    /// <summary>
    /// True ise önbelleğe hiç bakmadan doğrudan veritabanından veri çeker ve ardından önbelleği ezer.
    /// </summary>
    bool BypassCache { get; }

    /// <summary>
    /// Önbellekte kalma süresini belirler. Tanımlanmazsa varsayılan süre kullanılır.
    /// </summary>
    TimeSpan? SlidingExpiration { get; }
}
