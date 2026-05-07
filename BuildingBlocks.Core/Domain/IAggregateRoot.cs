namespace BuildingBlocks.Core.Domain;

/// <summary>
/// Aggregate Root marker interface.
/// Bu interface'i implement eden entity'ler kendi aggregate boundary'lerini temsil eder.
/// Repository'ler sadece IAggregateRoot implement eden entity'lerle çalışmalıdır.
/// </summary>
public interface IAggregateRoot
{
}
