namespace Application.DTOs.Common;

public class PagedResponse<T>
{
    public IEnumerable<T> Data { get; set; } = new List<T>();
    public int PageNumber { get; set; }
    public int  PageSize { get; set; }
    public int  TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevieousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}