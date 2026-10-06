using ProjectNamePlaceholder.Domain.Entities;

namespace ProjectNamePlaceholder.Application.Common.Interfaces;

public interface IAuditLogRepository : IRepository<AuditLog>
{
	Task<(IReadOnlyList<AuditLog> Items, long Total)> QueryAsync(
		long? userId = null,
		string? action = null,
		DateTime? from = null,
		DateTime? to = null,
		bool? success = null,
		int page = 1,
		int pageSize = 20,
		CancellationToken cancellationToken = default);
}

