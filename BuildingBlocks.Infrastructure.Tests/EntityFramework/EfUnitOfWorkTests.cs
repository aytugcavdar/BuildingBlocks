using BuildingBlocks.Infrastructure.EntityFramework;
using BuildingBlocks.Infrastructure.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure.Tests.EntityFramework;

public class EfUnitOfWorkTests
{
    [Fact]
    public async Task SaveChangesAsync_ShouldPersistPendingChanges()
    {
        await using var context = CreateContext();
        var unitOfWork = new EfUnitOfWork<TestDbContext>(context);
        context.TestEntities.Add(new TestEntity { Id = Guid.NewGuid(), Name = "alpha" });

        var count = await unitOfWork.SaveChangesAsync();

        count.Should().Be(1);
        var persisted = await context.TestEntities.ToListAsync();
        persisted.Should().ContainSingle(item => item.Name == "alpha");
    }

    [Fact]
    public async Task CommitTransactionAsync_ShouldThrow_WhenNoTransactionExists()
    {
        await using var context = CreateContext();
        var unitOfWork = new EfUnitOfWork<TestDbContext>(context);

        var act = () => unitOfWork.CommitTransactionAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("No active transaction to commit.");
    }

    private static TestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TestDbContext(options);
    }
}
