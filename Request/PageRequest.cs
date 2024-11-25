using System.ComponentModel;

namespace QualityInspection.Request;

public class PagedRequest
{
    public int PageNumber { get; set; } = 1;
    [DefaultValue(10)] public int PageSize { get; set; } = 10;
}