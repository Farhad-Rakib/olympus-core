 public class BaseEntity
{
    public long Id { get; set; }
    public DateTime  CreatedAt{ get; set; } = DateTime.UtcNow;
    public long CreatedBy { get; set; }
    public long UpdatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }

}