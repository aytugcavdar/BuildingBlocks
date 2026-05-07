namespace BuildingBlocks.Core.Locking;

/// <summary>
/// Kilit (Mutex) mekanizmasını temsil eden, kilit bırakıldığında Dispose edilecek arayüz.
/// </summary>
public interface IDistributedLockHandle : IAsyncDisposable, IDisposable
{
}

/// <summary>
/// Projeniz Kubernetes veya Docker Swarm üzerinde n kopya olarak çalışıyorsa,
/// aynı işi (örneğin Outbox okuma) aynı anda 2 farklı instance'ın yapmasını (çakışmayı) engeller.
/// </summary>
public interface IDistributedLock
{
    /// <summary>
    /// Verilen isimle bir kilit almaya çalışır. 
    /// Eğer kilit başkası tarafından alınmışsa, timeout süresi boyunca bekler.
    /// Kilit alınamazsa null döner (veya exception fırlatır).
    /// </summary>
    /// <param name="name">Kilidin benzersiz adı (örn: "payment-sync-123")</param>
    /// <param name="timeout">Kilidi alabilmek için beklenecek maksimum süre.</param>
    Task<IDistributedLockHandle?> TryAcquireAsync(string name, TimeSpan timeout = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verilen isimle bir kilit alır. Kilit başkasındaysa timeout süresince bekler. 
    /// Süre dolarsa kilit alınamazsa exception fırlatır.
    /// </summary>
    Task<IDistributedLockHandle> AcquireAsync(string name, TimeSpan timeout = default, CancellationToken cancellationToken = default);
}
