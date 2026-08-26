namespace Ensa.Application.Contracts.Common;

public interface IListResult<T>
{
    IReadOnlyList<T> Items { get; set; }
}

public class ListResultDto<T> : IListResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];

    public ListResultDto() { }

    public ListResultDto(IReadOnlyList<T> items) => Items = items;
}

public interface IPagedResult<T> : IListResult<T>
{
    long TotalCount { get; set; }
}

public class PagedResultDto<T> : ListResultDto<T>, IPagedResult<T>
{
    public long TotalCount { get; set; }

    public PagedResultDto() { }

    public PagedResultDto(long totalCount, IReadOnlyList<T> items) : base(items)
        => TotalCount = totalCount;
}

public interface IPagedRequest
{
    int SkipCount { get; set; }
    int MaxResultCount { get; set; }
}

public interface ISortedRequest
{
    string? Sorting { get; set; }
}

public class PagedAndSortedRequestDto : IPagedRequest, ISortedRequest
{
    public const int DefaultMaxResultCount = 20;
    public const int MaxAllowedResultCount = 1000;

    private int _maxResultCount = DefaultMaxResultCount;

    public int SkipCount { get; set; }

    public int MaxResultCount
    {
        get => _maxResultCount;
        set => _maxResultCount = value <= 0
            ? DefaultMaxResultCount
            : Math.Min(value, MaxAllowedResultCount);
    }

    /// <summary>For example <c>"CompanyName ASC"</c> or <c>"CreationTime DESC"</c>.</summary>
    public string? Sorting { get; set; }
}

/// <summary>A paged request that also carries a free-text search term.</summary>
public class PagedAndSortedFilterDto : PagedAndSortedRequestDto
{
    public string? Filter { get; set; }
}
