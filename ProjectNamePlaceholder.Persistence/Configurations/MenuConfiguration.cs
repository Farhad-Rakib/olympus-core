using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectNamePlaceholder.Domain.Entities;

namespace ProjectNamePlaceholder.Persistence.Configurations
{
    public class MenuConfiguration : IEntityTypeConfiguration<Menu>
    {
        public void Configure(EntityTypeBuilder<Menu> builder)
        {
            builder.ToTable("menus");
            
            builder.HasKey(m => m.Id);
            builder.Property(m => m.Title).IsRequired().HasMaxLength(100);
            builder.Property(m => m.Url).HasMaxLength(200);
            builder.Property(m => m.Icon).HasMaxLength(100);
            builder.Property(m => m.RequiredPermission).HasMaxLength(100);

            builder.HasOne(m => m.ParentMenu)
                .WithMany(m => m.Children)
                .HasForeignKey(m => m.ParentMenuId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
