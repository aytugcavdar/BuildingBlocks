using System;
using System.Collections.Generic;
using System.Text;

namespace BuildingBlocks.Core.Dynamic;

public class Sort
{
    public string Field { get; set; }
    public string Dir { get; set; } // "asc" veya "desc"

    public Sort()
    {
        Field = string.Empty;
        Dir = string.Empty;
    }

    public Sort(string field, string dir)
    {
        Field = field;
        Dir = dir;
    }
}
