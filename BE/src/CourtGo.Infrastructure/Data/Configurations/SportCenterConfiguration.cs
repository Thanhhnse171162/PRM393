using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourtGo.Infrastructure.Data.Configurations;

public class SportCenterConfiguration : IEntityTypeConfiguration<SportCenter>
{
    public void Configure(EntityTypeBuilder<SportCenter> builder)
    {
        builder.ToTable("SportCenters");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasDefaultValueSql("(newsequentialid())");

        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.AddressLine).HasMaxLength(300).IsRequired();
        builder.Property(c => c.Ward).HasMaxLength(100).IsRequired(false);
        builder.Property(c => c.District).HasMaxLength(100).IsRequired();
        builder.Property(c => c.City).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Latitude).HasPrecision(9, 6).IsRequired(false);
        builder.Property(c => c.Longitude).HasPrecision(9, 6).IsRequired(false);
        builder.Property(c => c.TimeZoneId).HasMaxLength(64).HasDefaultValue("Asia/Ho_Chi_Minh").IsRequired();
        builder.Property(c => c.PhoneNumber).HasMaxLength(20).IsRequired(false);
        builder.Property(c => c.CoverImageUrl).HasMaxLength(500).IsRequired(false);
        builder.Property(c => c.Status).HasConversion<byte>().HasColumnType("tinyint").HasDefaultValue(SportCenterStatus.Active).IsRequired();
        builder.Property(c => c.CreatedAt).HasColumnType("datetimeoffset").HasDefaultValueSql("(sysutcdatetime())").IsRequired();
        builder.Property(c => c.UpdatedAt).HasColumnType("datetimeoffset").IsRequired(false);

        // Indexes
        builder.HasIndex(c => new { c.District, c.City }).HasDatabaseName("IX_SportCenters_District_City");
        builder.HasIndex(c => c.Status).HasDatabaseName("IX_SportCenters_Status");

        // Relationships
        builder.HasMany(c => c.Courts)
            .WithOne(ct => ct.SportCenter)
            .HasForeignKey(ct => ct.SportCenterId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(c => c.StaffAssignments)
            .WithOne(s => s.SportCenter)
            .HasForeignKey(s => s.SportCenterId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(c => c.OperatingHours)
            .WithOne(o => o.SportCenter)
            .HasForeignKey(o => o.SportCenterId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(c => c.OperatingHourExceptions)
            .WithOne(o => o.SportCenter)
            .HasForeignKey(o => o.SportCenterId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
