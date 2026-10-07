using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourtGo.Infrastructure.Data.Configurations;

public class CourtConfiguration : IEntityTypeConfiguration<Court>
{
    public void Configure(EntityTypeBuilder<Court> builder)
    {
        builder.ToTable("Courts");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasDefaultValueSql("(newsequentialid())");

        builder.Property(c => c.SportCenterId).IsRequired();
        builder.Property(c => c.SportId).IsRequired();
        builder.Property(c => c.Code).HasMaxLength(50).IsRequired();
        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.Property(c => c.SurfaceType).HasMaxLength(100).IsRequired(false);
        builder.Property(c => c.Description).HasMaxLength(500).IsRequired(false);
        builder.Property(c => c.CoverImageUrl).HasMaxLength(500).IsRequired(false);
        builder.Property(c => c.BasePricePerHour).HasPrecision(18, 2).IsRequired();
        builder.Property(c => c.Status).HasConversion<byte>().HasColumnType("tinyint").HasDefaultValue(CourtStatus.Active).IsRequired();
        builder.Property(c => c.CreatedAt).HasColumnType("datetimeoffset").HasDefaultValueSql("(sysutcdatetime())").IsRequired();
        builder.Property(c => c.UpdatedAt).HasColumnType("datetimeoffset").IsRequired(false);

        // Indexes
        builder.HasIndex(c => new { c.SportCenterId, c.Code })
            .IsUnique()
            .HasDatabaseName("UQ_Courts_Center_Code");

        builder.HasIndex(c => new { c.SportCenterId, c.SportId, c.Status })
            .HasDatabaseName("IX_Courts_Center_Sport_Status");

        builder.HasIndex(c => c.SportCenterId).HasDatabaseName("IX_Courts_SportCenterId");
        builder.HasIndex(c => c.SportId).HasDatabaseName("IX_Courts_SportId");

        // Relationships
        builder.HasOne(c => c.SportCenter)
            .WithMany(sc => sc.Courts)
            .HasForeignKey(c => c.SportCenterId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(c => c.Sport)
            .WithMany(s => s.Courts)
            .HasForeignKey(c => c.SportId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(c => c.PriceRules)
            .WithOne(p => p.Court)
            .HasForeignKey(p => p.CourtId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(c => c.CourtBlocks)
            .WithOne(cb => cb.Court)
            .HasForeignKey(cb => cb.CourtId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(c => c.Bookings)
            .WithOne(b => b.Court)
            .HasForeignKey(b => b.CourtId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(c => c.BookingSlots)
            .WithOne(bs => bs.Court)
            .HasForeignKey(bs => bs.CourtId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
