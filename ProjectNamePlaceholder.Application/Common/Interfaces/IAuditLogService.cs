using ProjectNamePlaceholder.Domain.Entities;

namespace ProjectNamePlaceholder.Application.Common.Interfaces;

public interface IAuditLogService
{
    Task LogAsync(AuditLog entry, CancellationToken cancellationToken = default);
    Task CreateAsync(long? userId, string action, string? data = null, CancellationToken cancellationToken = default);
}
