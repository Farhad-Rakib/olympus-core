namespace ProjectNamePlaceholder.Application.Menu
{
    public class MenuItemDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Url { get; set; }
        public string? Icon { get; set; }
        public string? RequiredPermission { get; set; }
        public List<MenuItemDto>? Children { get; set; }
    }
}
