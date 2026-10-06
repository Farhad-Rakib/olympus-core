namespace ProjectNamePlaceholder.Application.Audit.Dtos;

public sealed record AuditLogDto(
    long Id,
    long? UserId,
    string? UserName,
    string Action,
    string Path,
    string Method,
    string? Data,
    string? RequestBody,
    string? ResponseBody,
    int? StatusCode,
    string? UserAgent,
    string? Headers,
    bool Success,
    DateTime Timestamp,
    string? Ip
);
