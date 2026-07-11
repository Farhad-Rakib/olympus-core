using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using ProjectNamePlaceholder.Api.Common;
using ProjectNamePlaceholder.Application.Common.Exceptions;

namespace ProjectNamePlaceholder.Api.Middleware;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception occurred");

        var response = exception switch
        {
            ValidationException validationException => ApiResponse.FailureResponse(
                "Validation failed",
                StatusCodes.Status400BadRequest,
                new { Errors = validationException.Errors.Select(e => e.ErrorMessage).ToList() }
            ),
            AppException appException => ApiResponse.FailureResponse(
                appException.Message,
                appException.StatusCode,
                new { Exception = appException.GetType().Name }
            ),
            _ => ApiResponse.FailureResponse(
                "An unexpected error occurred.",
                StatusCodes.Status500InternalServerError,
                new { Exception = exception.GetType().Name }
            )
        };

        httpContext.Response.StatusCode = response.StatusCode;
        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

        return true;
    }
}
