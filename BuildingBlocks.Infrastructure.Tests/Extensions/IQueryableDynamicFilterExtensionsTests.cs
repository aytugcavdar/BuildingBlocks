using BuildingBlocks.Core.Dynamic;
using BuildingBlocks.Infrastructure.Extensions;
using BuildingBlocks.Infrastructure.Tests.Fixtures;
using FluentAssertions;

namespace BuildingBlocks.Infrastructure.Tests.Extensions;

public class IQueryableDynamicFilterExtensionsTests
{
    [Fact]
    public void ToDynamic_ShouldFilterAndSortQueryable()
    {
        var query = new[]
        {
            new TestEntity { Name = "alpha", Score = 10 },
            new TestEntity { Name = "beta", Score = 30 },
            new TestEntity { Name = "alphabet", Score = 20 }
        }.AsQueryable();

        var dynamicQuery = new DynamicQuery(
            [new Sort(nameof(TestEntity.Score), "desc")],
            new Filter(nameof(TestEntity.Name), "contains") { Value = "alpha" });

        var result = query.ToDynamic(dynamicQuery).ToList();

        result.Select(item => item.Score).Should().Equal(20, 10);
    }

    [Fact]
    public void Transform_ShouldRejectInvalidOperator()
    {
        var filter = new Filter(nameof(TestEntity.Name), "invalid") { Value = "alpha" };

        var act = () => IQueryableDynamicFilterExtensions.Transform(filter, [filter]);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Invalid Operator");
    }
}
