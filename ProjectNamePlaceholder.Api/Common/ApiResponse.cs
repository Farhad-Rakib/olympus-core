namespace ProjectNamePlaceholder.Api.Common;

/// <summary>
/// Generic API response wrapper for all API endpoints
/// </summary>
/// <typeparam name="T">The type of data being returned</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Indicates whether the request was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// HTTP status code of the response
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Message describing the response (error message or success message)
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The actual response data (only populated on success)
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Additional error details (populated on error)
    /// </summary>
    public object? Errors { get; set; }

    /// <summary>
    /// Timestamp of when the response was generated
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a successful response
    /// </summary>
    public static ApiResponse<T> SuccessResponse(T data, string message = "Request successful", int statusCode = 200)
    {
        return new ApiResponse<T>
        {
            Success = true,
            StatusCode = statusCode,
            Message = message,
            Data = data,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a failure response
    /// </summary>
    public static ApiResponse<T> FailureResponse(string message, int statusCode = 400, object? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            StatusCode = statusCode,
            Message = message,
            Errors = errors,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates an error response from an exception
    /// </summary>
    public static ApiResponse<T> ErrorResponse(Exception exception, int statusCode = 500)
    {
        return new ApiResponse<T>
        {
            Success = false,
            StatusCode = statusCode,
            Message = exception.Message,
            Errors = new { Exception = exception.GetType().Name },
            Timestamp = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Non-generic API response wrapper for endpoints that don't return data
/// </summary>
public class ApiResponse
{
    /// <summary>
    /// Indicates whether the request was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// HTTP status code of the response
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Message describing the response (error message or success message)
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Additional error details (populated on error)
    /// </summary>
    public object? Errors { get; set; }

    /// <summary>
    /// Timestamp of when the response was generated
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a successful response
    /// </summary>
    public static ApiResponse SuccessResponse(string message = "Request successful", int statusCode = 200)
    {
        return new ApiResponse
        {
            Success = true,
            StatusCode = statusCode,
            Message = message,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a failure response
    /// </summary>
    public static ApiResponse FailureResponse(string message, int statusCode = 400, object? errors = null)
    {
        return new ApiResponse
        {
            Success = false,
            StatusCode = statusCode,
            Message = message,
            Errors = errors,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates an error response from an exception
    /// </summary>
    public static ApiResponse ErrorResponse(Exception exception, int statusCode = 500)
    {
        return new ApiResponse
        {
            Success = false,
            StatusCode = statusCode,
            Message = exception.Message,
            Errors = new { Exception = exception.GetType().Name },
            Timestamp = DateTime.UtcNow
        };
    }
}
