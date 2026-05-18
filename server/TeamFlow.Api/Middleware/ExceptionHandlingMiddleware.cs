using System.Net;
using System.Text.Json;
using TeamFlow.Application.Common.Exceptions;
using AppValidationException = TeamFlow.Application.Common.Exceptions.ValidationException;
using FluentValidationException = FluentValidation.ValidationException;

namespace TeamFlow.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (Exception ex)
        {
            await HandleAsync(ctx, ex);
        }
    }

    private async Task HandleAsync(HttpContext ctx, Exception ex)
    {
        var (status, payload) = ex switch
        {
            NotFoundException => ((int)HttpStatusCode.NotFound, (object)new { error = ex.Message }),
            ForbiddenException => ((int)HttpStatusCode.Forbidden, new { error = ex.Message }),
            ConflictException => ((int)HttpStatusCode.Conflict, new { error = ex.Message }),
            AppValidationException ave => ((int)HttpStatusCode.BadRequest, new { error = "Validation failed.", errors = (object)ave.Errors }),
            FluentValidationException fv => ((int)HttpStatusCode.BadRequest, new { error = "Validation failed.", errors = (object)fv.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()) }),
            UnauthorizedAccessException => ((int)HttpStatusCode.Unauthorized, new { error = "Unauthorized." }),
            _ => ((int)HttpStatusCode.InternalServerError, new { error = "An unexpected error occurred." })
        };

        if (status >= 500)
            _logger.LogError(ex, "Unhandled exception");
        else
            _logger.LogWarning(ex, "Handled exception: {Message}", ex.Message);

        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}
