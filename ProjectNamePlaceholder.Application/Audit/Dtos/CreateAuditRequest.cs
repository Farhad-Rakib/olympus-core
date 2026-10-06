namespace ProjectNamePlaceholder.Application.Audit.Dtos;

public sealed record CreateAuditRequest(
    long? UserId,
    string Action,
    string? Data
);
