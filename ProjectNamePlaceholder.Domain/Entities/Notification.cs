namespace ProjectNamePlaceholder.Domain.Entities;

public sealed class Notification : BaseEntity
{
    public long UserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }

    public User? User { get; set; }

    private Notification() { }

    public Notification(long userId, string type, string message)
    {
        UserId = userId;
        Type = !string.IsNullOrWhiteSpace(type) ? type : throw new ArgumentException("Type is required.", nameof(type));
        Message = !string.IsNullOrWhiteSpace(message) ? message : throw new ArgumentException("Message is required.", nameof(message));
        IsRead = false;
    }

    public void MarkAsRead()
    {
        IsRead = true;
    }
}
