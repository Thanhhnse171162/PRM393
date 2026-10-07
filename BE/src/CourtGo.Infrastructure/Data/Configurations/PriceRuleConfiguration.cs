using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourtGo.Infrastructure.Data.Configurations;

public class PriceRuleConfiguration : IEntityTypeConfiguration<PriceRule>
{
    public void Configure(EntityTypeBuilder<PriceRule> builder)
    {
        builder.ToTable("PriceRules");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasDefaultValueSql("(newsequentialid())");

        builder.Property(p => p.CourtId).IsRequired();
        builder.Property(p => p.DayOfWeek).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        builder.Property(p => p.StartTime).HasColumnType("time").IsRequired();
        builder.Property(p => p.EndTime).HasColumnType("time").IsRequired();
        builder.Property(p => p.PricePerHour).HasPrecision(18, 2).IsRequired();
        builder.Property(p => p.EffectiveFrom).HasColumnType("date").IsRequired(false);
        builder.Property(p => p.EffectiveTo).HasColumnType("date").IsRequired(false);
        builder.Property(p => p.IsActive).HasDefaultValue(true).IsRequired();
        builder.Property(p => p.CreatedAt).HasColumnType("datetimeoffset").HasDefaultValueSql("(sysutcdatetime())").IsRequired();

        // Indexes
        builder.HasIndex(p => new { p.CourtId, p.DayOfWeek, p.IsActive })
            .HasDatabaseName("IX_PriceRules_Court_Day_Active");

        // Relationships
        builder.HasOne(p => p.Court)
            .WithMany(c => c.PriceRules)
            .HasForeignKey(p => p.CourtId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(p => p.BookingSlots)
            .WithOne(bs => bs.PriceRule)
            .HasForeignKey(bs => bs.PriceRuleId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
