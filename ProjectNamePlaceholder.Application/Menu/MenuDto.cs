namespace ProjectNamePlaceholder.Application.Menu;

public class MenuDto
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? Icon { get; set; }
    public string? RequiredPermission { get; set; }
    public long? ParentMenuId { get; set; }
    public List<MenuDto>? Children { get; set; }
}
