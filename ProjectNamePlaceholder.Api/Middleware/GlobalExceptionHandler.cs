using System.Text.Json;
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
        var response = exception switch
        {
            ValidationException validationException => ApiResponse.FailureResponse(
                "Validation failed",
                StatusCodes.Status400BadRequest,
                new
                {
                    Errors = validationException.Errors.Select(e => e.ErrorMessage).ToList(),
                    // Field-keyed messages so the frontend can show each error next to its input.
                    Fields = validationException.Errors
                        .GroupBy(e => ToCamelCase(e.PropertyName))
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray())
                }
            ),
            // NotFoundException, ConflictException and ForbiddenException derive from AppException.
            AppException appException => ApiResponse.FailureResponse(
                appException.Message,
                appException.StatusCode,
                new { Exception = appException.GetType().Name }
            ),
            KeyNotFoundException keyNotFoundException => ApiResponse.FailureResponse(
                keyNotFoundException.Message,
                StatusCodes.Status404NotFound,
                new { Exception = nameof(NotFoundException) }
            ),
            // Domain entities guard their invariants with ArgumentException.
            ArgumentException argumentException => ApiResponse.FailureResponse(
                argumentException.Message,
                StatusCodes.Status400BadRequest,
                new { Exception = nameof(ArgumentException) }
            ),
            UnauthorizedAccessException => ApiResponse.FailureResponse(
                "Unauthorized.",
                StatusCodes.Status401Unauthorized,
                new { Exception = nameof(UnauthorizedAccessException) }
            ),
            _ => ApiResponse.FailureResponse(
                "An unexpected error occurred.",
                StatusCodes.Status500InternalServerError,
                new { Exception = exception.GetType().Name }
            )
        };

        if (response.StatusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception occurred");
        }
        else
        {
            _logger.LogWarning("Request failed with {StatusCode}: {Message}", response.StatusCode, exception.Message);
        }

        httpContext.Response.StatusCode = response.StatusCode;
        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

        return true;
    }

    private static string ToCamelCase(string propertyName) =>
        string.Join('.', propertyName.Split('.').Select(part => JsonNamingPolicy.CamelCase.ConvertName(part)));
}
