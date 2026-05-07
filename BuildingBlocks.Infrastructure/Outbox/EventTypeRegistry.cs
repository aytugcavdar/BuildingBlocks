namespace BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// Outbox mesaj işlerken event tiplerini isimle eşleştiren statik registry.
/// Uygulamanın başlangıcında <c>EventTypeRegistry.Register</c> ile tipler kaydedilir.
/// </summary>
public static class EventTypeRegistry
{
    public static Dictionary<string, Type> Dictionary { get; } = new();

    /// <summary>
    /// Bir event tipini kayıt eder. Aynı isim iki kez eklenemez.
    /// </summary>
    public static void Register(string eventName, Type type)
    {
        if (!Dictionary.ContainsKey(eventName))
            Dictionary.Add(eventName, type);
    }
}
