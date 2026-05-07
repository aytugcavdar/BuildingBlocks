using System;
using System.Collections.Generic;
using System.Text;

namespace BuildingBlocks.Core.Repositories;

public interface IQuery<T>
{
    IQueryable<T> Query();
}
