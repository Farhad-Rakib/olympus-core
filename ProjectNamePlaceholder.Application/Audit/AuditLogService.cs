using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Domain.Entities;

namespace ProjectNamePlaceholder.Application.Audit;

public class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public AuditLogService(IAuditLogRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task LogAsync(AuditLog entry, CancellationToken cancellationToken = default)
    {
        await _repository.AddAsync(entry, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task CreateAsync(long? userId, string action, string? data = null, CancellationToken cancellationToken = default)
    {
        var entry = new AuditLog
        {
            UserId = userId,
            Action = action,
            Data = data,
            Success = true,
            Timestamp = DateTime.UtcNow
        };

        await LogAsync(entry, cancellationToken);
    }
}
