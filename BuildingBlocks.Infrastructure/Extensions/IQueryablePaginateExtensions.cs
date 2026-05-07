using System;
using System.Collections.Generic;
using System.Text;
using BuildingBlocks.Core.Paging;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure.Extensions;

public static class IQueryablePaginateExtensions
{
    public static async Task<Paginate<T>> ToPaginateAsync<T>(
        this IQueryable<T> source,
        int index,
        int size,
        CancellationToken cancellationToken = default)
    {
        int count = await source.CountAsync(cancellationToken).ConfigureAwait(false);

        List<T> items = await source
            .Skip(index * size)
            .Take(size)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new Paginate<T>(items, count, index, size);
    }
}
