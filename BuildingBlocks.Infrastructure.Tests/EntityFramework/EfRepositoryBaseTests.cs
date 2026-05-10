using BuildingBlocks.Core.Dynamic;
using BuildingBlocks.Infrastructure.EntityFramework;
using BuildingBlocks.Infrastructure.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure.Tests.EntityFramework;

public class EfRepositoryBaseTests
{
    [Fact]
    public async Task AddAsync_ShouldSetCreatedDateAndPersistEntity()
    {
        await using var context = CreateContext();
        var repository = new EfRepositoryBase<TestEntity, Guid, TestDbContext>(context);
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "alpha", Score = 10 };

        await repository.AddAsync(entity);
        await context.SaveChangesAsync();

        entity.CreatedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        var persisted = await context.TestEntities.ToListAsync();
        persisted.Should().ContainSingle(item => item.Name == "alpha");
    }

    [Fact]
    public async Task GetListAsync_ShouldExcludeSoftDeletedEntitiesByDefault()
    {
        await using var context = CreateContext();
        await SeedAsync(context,
            new TestEntity { Id = Guid.NewGuid(), Name = "active", Score = 10 },
            new TestEntity { Id = Guid.NewGuid(), Name = "deleted", Score = 20, DeletedDate = DateTime.UtcNow });
        var repository = new EfRepositoryBase<TestEntity, Guid, TestDbContext>(context);

        var result = await repository.GetListAsync(size: 10);

        result.Count.Should().Be(1);
        result.Items.Should().ContainSingle(item => item.Name == "active");
    }

    [Fact]
    public async Task GetListAsync_ShouldIncludeSoftDeletedEntities_WhenWithDeletedIsTrue()
    {
        await using var context = CreateContext();
        await SeedAsync(context,
            new TestEntity { Id = Guid.NewGuid(), Name = "active", Score = 10 },
            new TestEntity { Id = Guid.NewGuid(), Name = "deleted", Score = 20, DeletedDate = DateTime.UtcNow });
        var repository = new EfRepositoryBase<TestEntity, Guid, TestDbContext>(context);

        var result = await repository.GetListAsync(withDeleted: true, size: 10);

        result.Count.Should().Be(2);
    }

    [Fact]
    public async Task DeleteAsync_ShouldSoftDeleteByDefault()
    {
        await using var context = CreateContext();
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "alpha", Score = 10 };
        await SeedAsync(context, entity);
        var repository = new EfRepositoryBase<TestEntity, Guid, TestDbContext>(context);

        await repository.DeleteAsync(entity);
        await context.SaveChangesAsync();

        entity.DeletedDate.Should().NotBeNull();
        var persisted = await context.TestEntities.ToListAsync();
        persisted.Should().ContainSingle(item => item.Id == entity.Id);
        (await repository.GetCountAsync()).Should().Be(0);
        (await repository.GetCountAsync(withDeleted: true)).Should().Be(1);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveEntity_WhenPermanentIsTrue()
    {
        await using var context = CreateContext();
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "alpha", Score = 10 };
        await SeedAsync(context, entity);
        var repository = new EfRepositoryBase<TestEntity, Guid, TestDbContext>(context);

        await repository.DeleteAsync(entity, permanent: true);
        await context.SaveChangesAsync();

        var persisted = await context.TestEntities.ToListAsync();
        persisted.Should().BeEmpty();
    }

    [Fact]
    public async Task GetListByDynamicAsync_ShouldApplyFilterSortAndPagination()
    {
        await using var context = CreateContext();
        await SeedAsync(context,
            new TestEntity { Id = Guid.NewGuid(), Name = "alpha", Score = 10 },
            new TestEntity { Id = Guid.NewGuid(), Name = "beta", Score = 30 },
            new TestEntity { Id = Guid.NewGuid(), Name = "alphabet", Score = 20 });
        var repository = new EfRepositoryBase<TestEntity, Guid, TestDbContext>(context);
        var dynamicQuery = new DynamicQuery(
            [new Sort(nameof(TestEntity.Score), "desc")],
            new Filter(nameof(TestEntity.Name), "contains") { Value = "alpha" });

        var result = await repository.GetListByDynamicAsync(dynamicQuery, size: 10);

        result.Count.Should().Be(2);
        result.Items.Select(item => item.Score).Should().Equal(20, 10);
    }

    private static TestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TestDbContext(options);
    }

    private static async Task SeedAsync(TestDbContext context, params TestEntity[] entities)
    {
        await context.TestEntities.AddRangeAsync(entities);
        await context.SaveChangesAsync();
    }
}
