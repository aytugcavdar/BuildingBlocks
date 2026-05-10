using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure.Tests.Fixtures;

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options)
        : base(options)
    {
    }

    public DbSet<TestEntity> TestEntities => Set<TestEntity>();
}
