using CourtGo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourtGo.Infrastructure.Data.Configurations;

public class SportConfiguration : IEntityTypeConfiguration<Sport>
{
    public void Configure(EntityTypeBuilder<Sport> builder)
    {
        builder.ToTable("Sports");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasDefaultValueSql("(newsequentialid())");

        builder.Property(s => s.Code).HasMaxLength(30).IsRequired();
        builder.Property(s => s.Name).HasMaxLength(100).IsRequired();
        builder.Property(s => s.IconUrl).HasMaxLength(500).IsRequired(false);
        builder.Property(s => s.DisplayOrder).HasDefaultValue(0).IsRequired();
        builder.Property(s => s.IsActive).HasDefaultValue(true).IsRequired();
        builder.Property(s => s.CreatedAt).HasColumnType("datetimeoffset").HasDefaultValueSql("(sysutcdatetime())").IsRequired();

        // Indexes
        builder.HasIndex(s => s.Code).IsUnique().HasDatabaseName("UX_Sports_Code");
    }
}
