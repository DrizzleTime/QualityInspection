namespace QualityInspection.Result;

public class ApiResponse<T>
{
    /// <summary>
    /// 状态码，用来标识请求是否成功
    /// </summary>
    public int Code { get; set; } = 1;

    /// <summary>
    /// 消息，用于描述状态码的意义或错误信息
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// 返回的数据主体
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// 构造成功响应
    /// </summary>
    /// <param name="data">返回数据</param>
    /// <param name="message">可选的消息</param>
    /// <returns>ApiResponse 对象</returns>
    public static ApiResponse<T> Success(T data, string? message = null)
    {
        if (data is not null && message is null)
        {
            message = data.ToString();
        }

        return new ApiResponse<T>
        {
            Code = 1,
            Message = message,
            Data = data
        };
    }


    /// <summary>
    /// 构造失败响应
    /// </summary>
    /// <param name="message">错误信息</param>
    /// <returns>ApiResponse 对象</returns>
    public static ApiResponse<T> Fail(string message)
    {
        return new ApiResponse<T>
        {
            Code = 0,
            Message = message,
            Data = default
        };
    }
}