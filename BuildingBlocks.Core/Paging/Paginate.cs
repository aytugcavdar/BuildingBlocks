namespace BuildingBlocks.Core.Paging;

public class Paginate<T> : BasePageableModel
{
    public IList<T> Items { get; set; }

    public Paginate()
    {
        Items = new List<T>();
    }

    public Paginate(IList<T> items, int count, int index, int size)
    {
        Items = items;
        Count = count;
        Index = index;
        Size = size;
        Pages = (int)Math.Ceiling(count / (double)size);
        HasPrevious = Index > 0;
        HasNext = Index + 1 < Pages;
    }
}
