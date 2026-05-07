namespace BuildingBlocks.Core.Requests;

/// <summary>
/// Sayfalama parametrelerini taşıyan request sınıfı.
/// </summary>
public class PageRequest
{
    public const int DefaultPageIndex = 0;
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 100;

    private int _pageSize = DefaultPageSize;

    public int PageIndex { get; set; } = DefaultPageIndex;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }
}
