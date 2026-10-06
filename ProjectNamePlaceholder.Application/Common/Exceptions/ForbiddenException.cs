namespace ProjectNamePlaceholder.Application.Common.Exceptions;

/// <summary>
/// Thrown when the current user is not allowed to perform the action. Mapped to HTTP 403.
/// </summary>
public class ForbiddenException : AppException
{
    public ForbiddenException(string message)
        : base(message, 403)
    {
    }
}
