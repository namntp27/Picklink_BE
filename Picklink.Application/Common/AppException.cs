namespace Picklink.Application.Common;

public sealed class AppException : Exception
{
    public AppException(string message, int statusCode = 400, object? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }

    public int StatusCode { get; }
    public object? Errors { get; }
}
