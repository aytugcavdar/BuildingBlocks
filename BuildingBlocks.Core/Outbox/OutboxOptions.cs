namespace BuildingBlocks.Core.Outbox;

/// <summary>
/// Outbox processor davranışını yapılandıran seçenekler.
/// appsettings.json'da "Outbox" section'ı ile konfigüre edilir.
/// </summary>
public class OutboxOptions
{
    public const string SectionName = "Outbox";

    /// <summary>
    /// Varsayılan maksimum retry sayısı (static erişim için).
    /// OutboxMessage.IsFailed bu değeri kullanır.
    /// </summary>
    public const int DefaultMaxRetryCount = 3;

    /// <summary>
    /// Bir mesaj kaç kez denendikten sonra başarısız sayılır.
    /// Varsayılan: 3
    /// </summary>
    public int MaxRetryCount { get; set; } = DefaultMaxRetryCount;

    /// <summary>
    /// OutboxProcessor'ın kaç saniyede bir çalışacağı.
    /// Varsayılan: 15 saniye
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 15;

    /// <summary>
    /// Tek bir polling döngüsünde kaç mesaj işleneceği.
    /// Varsayılan: 20
    /// </summary>
    public int BatchSize { get; set; } = 20;
}
