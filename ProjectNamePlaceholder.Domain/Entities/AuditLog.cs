namespace ProjectNamePlaceholder.Domain.Entities;

public sealed class AuditLog : BaseEntity
{
    public long? UserId { get; set; }
    public string? UserName { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string? Data { get; set; }
    public string? RequestBody { get; set; }
    public string? ResponseBody { get; set; }
    public int? StatusCode { get; set; }
    public string? UserAgent { get; set; }
    public string? Headers { get; set; }
    public bool Success { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? Ip { get; set; }
}
