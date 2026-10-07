using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourtGo.Infrastructure.Data.Configurations;

public class OperatingHourConfiguration : IEntityTypeConfiguration<OperatingHour>
{
    public void Configure(EntityTypeBuilder<OperatingHour> builder)
    {
        builder.ToTable("OperatingHours");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasDefaultValueSql("(newsequentialid())");

        builder.Property(o => o.SportCenterId).IsRequired();
        builder.Property(o => o.DayOfWeek).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        builder.Property(o => o.OpenTime).HasColumnType("time").IsRequired(false);
        builder.Property(o => o.CloseTime).HasColumnType("time").IsRequired(false);
        builder.Property(o => o.IsClosed).HasDefaultValue(false).IsRequired();

        // Indexes
        builder.HasIndex(o => new { o.SportCenterId, o.DayOfWeek })
            .IsUnique()
            .HasDatabaseName("UQ_OperatingHours_Center_Day");

        // Relationships
        builder.HasOne(o => o.SportCenter)
            .WithMany(sc => sc.OperatingHours)
            .HasForeignKey(o => o.SportCenterId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
