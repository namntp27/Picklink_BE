using System.Net;
using System.Text.Json;
using Picklink.Application.Common;

namespace Picklink.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (AppException exception)
        {
            await WriteErrorAsync(context, exception.StatusCode, exception.Message, exception.Errors);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled API exception");
            await WriteErrorAsync(context, (int)HttpStatusCode.InternalServerError, "Unexpected server error.");
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, int statusCode, string message, object? errors = null)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var payload = JsonSerializer.Serialize(ApiResponse.Fail(message, errors), new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(payload);
    }
}
