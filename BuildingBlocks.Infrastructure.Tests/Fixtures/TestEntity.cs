using BuildingBlocks.Core.Domain;

namespace BuildingBlocks.Infrastructure.Tests.Fixtures;

public class TestEntity : Entity<Guid>
{
    public string Name { get; set; } = string.Empty;
    public int Score { get; set; }
}
