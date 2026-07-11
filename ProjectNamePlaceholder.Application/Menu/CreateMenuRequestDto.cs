namespace ProjectNamePlaceholder.Application.Menu;

public class CreateMenuRequestDto
{
    public string Title { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? Icon { get; set; }
    public string? RequiredPermission { get; set; }
    public long? ParentMenuId { get; set; }
}
