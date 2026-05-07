namespace BuildingBlocks.Core.Caching;

public class CacheSettings
{
    /// <summary>
    /// Geçerlilik süresi belirlenmemiş öğeler için varsayılan kaydırma süresi (sliding) gün sayısı. Varsayılan 1'dir.
    /// </summary>
    public int SlidingExpirationDays { get; set; } = 1;
}
