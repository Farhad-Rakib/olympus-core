namespace ProjectNamePlaceholder.Application.Common.Exceptions;

/// <summary>
/// Thrown when the resource could not be found. Mapped to HTTP 404.
/// </summary>
public class NotFoundException : AppException
{
    public NotFoundException(string message)
        : base(message, 404)
    {
    }
}
