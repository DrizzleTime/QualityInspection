namespace QualityInspection.Result;

/// <summary>
/// 分页数据模型
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public class PagedData<T>
{
    /// <summary>
    /// 数据列表
    /// </summary>
    public List<T> Items { get; set; }

    /// <summary>
    /// 当前页码
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// 每页的数据量
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 总记录数
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 总页数
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="items">数据项</param>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <param name="totalCount">总记录数</param>
    public PagedData(List<T> items, int pageNumber, int pageSize, int totalCount)
    {
        Items = items;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;
    }
}