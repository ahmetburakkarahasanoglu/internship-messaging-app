using ChatApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChatApi.Middleware;

public sealed class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ChatValidationException exception)
        {
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "İstek doğrulanamadı", exception.Message);
        }
        catch (ResourceNotFoundException exception)
        {
            await WriteProblemAsync(context, StatusCodes.Status404NotFound, "Kaynak bulunamadı", exception.Message);
        }
        catch (Exception exception) when (!context.RequestAborted.IsCancellationRequested)
        {
            logger.LogError(exception, "Beklenmeyen API hatası. Path: {Path}", context.Request.Path);
            await WriteProblemAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "Sunucu hatası",
                "İstek işlenirken beklenmeyen bir hata oluştu.");
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, int status, string title, string detail)
    {
        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };
        await context.Response.WriteAsJsonAsync(problem);
    }
}
