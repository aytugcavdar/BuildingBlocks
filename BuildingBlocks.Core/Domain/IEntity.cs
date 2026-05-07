namespace BuildingBlocks.Core.Domain;

/// <summary>
/// Audit alanlarını tanımlar (created, updated, deleted tarihleri).
/// </summary>
public interface IEntity
{
    DateTime CreatedDate { get; set; }
    DateTime? UpdatedDate { get; set; }
    DateTime? DeletedDate { get; set; }
}

/// <summary>
/// Kimlik ve audit alanlarını birleştiren temel entity interface'i.
/// Tüm entity'lerin bu interface üzerinden erişilebilmesi için IEntity'yi extend eder.
/// </summary>
public interface IEntity<TId> : IEntity
{
    TId Id { get; set; }
}