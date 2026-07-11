using System.Collections.Generic;

namespace ProjectNamePlaceholder.Domain.Entities
{
    public class Menu : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string? Url { get; set; }
        public string? Icon { get; set; }
        public string? RequiredPermission { get; set; }
        public long? ParentMenuId { get; set; }
        public Menu? ParentMenu { get; set; }
        public List<Menu>? Children { get; set; }
    }
}
