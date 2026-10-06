namespace ProjectNamePlaceholder.Application.Common.Exceptions;

/// <summary>
/// Thrown when the request conflicts with the current state, e.g. a duplicate name. Mapped to HTTP 409.
/// </summary>
public class ConflictException : AppException
{
    public ConflictException(string message)
        : base(message, 409)
    {
    }
}
