namespace Picklink.Application.Common;

public sealed class ApiResponse<T>
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
    public object? Errors { get; init; }
    public object? Meta { get; init; }

    public static ApiResponse<T> Ok(T data, string message = "Success", object? meta = null)
        => new() { Success = true, Message = message, Data = data, Meta = meta };

    public static ApiResponse<T> Fail(string message, object? errors = null)
        => new() { Success = false, Message = message, Errors = errors };
}

public sealed class ApiResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public object? Errors { get; init; }

    public static ApiResponse Ok(string message = "Success")
        => new() { Success = true, Message = message };

    public static ApiResponse Fail(string message, object? errors = null)
        => new() { Success = false, Message = message, Errors = errors };
}
