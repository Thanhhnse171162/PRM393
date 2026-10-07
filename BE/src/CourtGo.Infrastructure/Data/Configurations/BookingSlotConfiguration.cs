using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourtGo.Infrastructure.Data.Configurations;

public class BookingSlotConfiguration : IEntityTypeConfiguration<BookingSlot>
{
    public void Configure(EntityTypeBuilder<BookingSlot> builder)
    {
        builder.ToTable("BookingSlots");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasDefaultValueSql("(newsequentialid())");

        builder.Property(s => s.BookingId).IsRequired();
        builder.Property(s => s.CourtId).IsRequired();
        builder.Property(s => s.StartAt).HasColumnType("datetimeoffset").IsRequired();
        builder.Property(s => s.EndAt).HasColumnType("datetimeoffset").IsRequired();
        builder.Property(s => s.UnitPrice).HasPrecision(18, 2).IsRequired();
        builder.Property(s => s.PriceRuleId).IsRequired(false);
        builder.Property(s => s.ReservationState).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        builder.Property(s => s.IsOccupying).IsRequired();
        builder.Property(s => s.HoldExpiresAt).HasColumnType("datetimeoffset").IsRequired(false);
        builder.Property(s => s.CreatedAt).HasColumnType("datetimeoffset").HasDefaultValueSql("(sysutcdatetime())").IsRequired();

        // Critical race-condition protection: filtered unique index on (CourtId, StartAt) where IsOccupying = 1
        builder.HasIndex(s => new { s.CourtId, s.StartAt })
            .IsUnique()
            .HasFilter("([IsOccupying]=(1))")
            .HasDatabaseName("UX_BookingSlots_ActiveCourtStart");

        builder.HasIndex(s => new { s.CourtId, s.StartAt, s.EndAt })
            .HasDatabaseName("IX_BookingSlots_Court_StartAt_EndAt");

        builder.HasIndex(s => s.BookingId)
            .HasDatabaseName("IX_BookingSlots_BookingId");

        // Relationships
        builder.HasOne(s => s.Booking)
            .WithMany(b => b.Slots)
            .HasForeignKey(s => s.BookingId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(s => s.Court)
            .WithMany(c => c.BookingSlots)
            .HasForeignKey(s => s.CourtId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(s => s.PriceRule)
            .WithMany(p => p.BookingSlots)
            .HasForeignKey(s => s.PriceRuleId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
