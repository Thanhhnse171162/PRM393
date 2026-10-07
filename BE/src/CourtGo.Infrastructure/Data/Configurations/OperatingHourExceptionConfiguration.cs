using CourtGo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourtGo.Infrastructure.Data.Configurations;

public class OperatingHourExceptionConfiguration : IEntityTypeConfiguration<OperatingHourException>
{
    public void Configure(EntityTypeBuilder<OperatingHourException> builder)
    {
        builder.ToTable("OperatingHourExceptions");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasDefaultValueSql("(newsequentialid())");

        builder.Property(o => o.SportCenterId).IsRequired();
        builder.Property(o => o.Date).HasColumnType("date").IsRequired();
        builder.Property(o => o.IsClosed).HasDefaultValue(false).IsRequired();
        builder.Property(o => o.OpenTime).HasColumnType("time").IsRequired(false);
        builder.Property(o => o.CloseTime).HasColumnType("time").IsRequired(false);
        builder.Property(o => o.Reason).HasMaxLength(300).IsRequired(false);

        // Indexes
        builder.HasIndex(o => new { o.SportCenterId, o.Date })
            .IsUnique()
            .HasDatabaseName("UQ_OperatingHourExceptions_Center_Date");

        // Relationships
        builder.HasOne(o => o.SportCenter)
            .WithMany(sc => sc.OperatingHourExceptions)
            .HasForeignKey(o => o.SportCenterId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
