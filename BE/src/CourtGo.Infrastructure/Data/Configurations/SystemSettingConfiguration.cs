using CourtGo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourtGo.Infrastructure.Data.Configurations;

public class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("SystemSettings");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnType("tinyint").ValueGeneratedNever();

        builder.Property(s => s.HoldDurationMinutes).IsRequired();
        builder.Property(s => s.MinBookingLeadMinutes).IsRequired();
        builder.Property(s => s.DefaultDepositPercent).HasPrecision(5, 2).IsRequired();
        builder.Property(s => s.AllowOutstandingCheckIn).HasDefaultValue(false).IsRequired();
        builder.Property(s => s.UpdatedByUserId).IsRequired(false);
        builder.Property(s => s.UpdatedAt).HasColumnType("datetimeoffset").HasDefaultValueSql("(sysutcdatetime())").IsRequired();

        // Relationships
        builder.HasOne(s => s.UpdatedByUser)
            .WithMany()
            .HasForeignKey(s => s.UpdatedByUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
