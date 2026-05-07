using System;
using System.Collections.Generic;
using System.Text;

namespace BuildingBlocks.Core.Dynamic;

public class Filter
{
    public string Field { get; set; }
    public string? Value { get; set; }
    public string Operator { get; set; } // "eq", "neq", "contains" vb.
    public string? Logic { get; set; }   // "and", "or"

    public IEnumerable<Filter>? Filters { get; set; }

    public Filter()
    {
        Field = string.Empty;
        Operator = string.Empty;
    }

    public Filter(string field, string @operator)
    {
        Field = field;
        Operator = @operator;
    }
}
