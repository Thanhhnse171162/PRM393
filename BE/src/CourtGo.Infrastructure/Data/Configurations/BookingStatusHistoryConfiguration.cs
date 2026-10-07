using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourtGo.Infrastructure.Data.Configurations;

public class BookingStatusHistoryConfiguration : IEntityTypeConfiguration<BookingStatusHistory>
{
    public void Configure(EntityTypeBuilder<BookingStatusHistory> builder)
    {
        builder.ToTable("BookingStatusHistories");

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).HasDefaultValueSql("(newsequentialid())");

        builder.Property(h => h.BookingId).IsRequired();
        builder.Property(h => h.FromStatus).HasConversion<byte?>().HasColumnType("tinyint").IsRequired(false);
        builder.Property(h => h.ToStatus).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        builder.Property(h => h.ChangedByUserId).IsRequired(false);
        builder.Property(h => h.Reason).HasMaxLength(500).IsRequired(false);
        builder.Property(h => h.CreatedAt).HasColumnType("datetimeoffset").HasDefaultValueSql("(sysutcdatetime())").IsRequired();

        // Indexes
        builder.HasIndex(h => new { h.BookingId, h.CreatedAt })
            .HasDatabaseName("IX_BookingStatusHistories_Booking_CreatedAt");

        // Relationships
        builder.HasOne(h => h.Booking)
            .WithMany(b => b.StatusHistories)
            .HasForeignKey(h => h.BookingId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(h => h.ChangedByUser)
            .WithMany()
            .HasForeignKey(h => h.ChangedByUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
