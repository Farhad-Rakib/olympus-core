using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectNamePlaceholder.Domain.Entities;

namespace ProjectNamePlaceholder.Persistence.Configurations;

public sealed class SiteSettingConfiguration : IEntityTypeConfiguration<SiteSetting>
{
    public void Configure(EntityTypeBuilder<SiteSetting> builder)
    {
        builder.ToTable("site_settings");

        builder.HasKey(setting => setting.Id);
        builder.Property(setting => setting.Key).HasMaxLength(150).IsRequired();
        builder.Property(setting => setting.Value).IsRequired();
        builder.Property(setting => setting.Description).HasMaxLength(500);

        builder.HasIndex(setting => setting.Key).IsUnique();
    }
}
