using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourtGo.Infrastructure.Data.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).HasDefaultValueSql("(newsequentialid())");

        builder.Property(b => b.BookingCode).HasMaxLength(30).IsRequired();
        builder.Property(b => b.CustomerUserId).IsRequired(false);

        // Snapshots
        builder.Property(b => b.CustomerNameSnapshot).HasMaxLength(150).IsRequired();
        builder.Property(b => b.CustomerPhoneSnapshot).HasMaxLength(20).IsRequired();
        builder.Property(b => b.CustomerEmailSnapshot).HasMaxLength(255).IsRequired(false);

        builder.Property(b => b.CourtId).IsRequired();
        builder.Property(b => b.CourtNameSnapshot).HasMaxLength(100).IsRequired();
        builder.Property(b => b.CenterNameSnapshot).HasMaxLength(200).IsRequired();
        builder.Property(b => b.SportNameSnapshot).HasMaxLength(100).IsRequired();

        builder.Property(b => b.Source).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        builder.Property(b => b.StartAt).HasColumnType("datetimeoffset").IsRequired();
        builder.Property(b => b.EndAt).HasColumnType("datetimeoffset").IsRequired();
        builder.Property(b => b.DurationMinutes).IsRequired();

        builder.Property(b => b.TotalAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(b => b.DepositPercentSnapshot).HasPrecision(5, 2).IsRequired();
        builder.Property(b => b.DepositAmount).HasPrecision(18, 2).IsRequired();

        builder.Property(b => b.BookingStatus).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        builder.Property(b => b.PaymentStatus).HasConversion<byte>().HasColumnType("tinyint").IsRequired();

        builder.Property(b => b.HoldExpiresAt).HasColumnType("datetimeoffset").IsRequired(false);
        builder.Property(b => b.QrToken).HasMaxLength(150).IsRequired(false);
        builder.Property(b => b.CancellationPolicyId).IsRequired(false);
        builder.Property(b => b.CreatedByUserId).IsRequired();

        builder.Property(b => b.CreatedAt).HasColumnType("datetimeoffset").HasDefaultValueSql("(sysutcdatetime())").IsRequired();
        builder.Property(b => b.UpdatedAt).HasColumnType("datetimeoffset").IsRequired(false);

        // Indexes
        builder.HasIndex(b => b.BookingCode)
            .IsUnique()
            .HasDatabaseName("UQ_Bookings_BookingCode");

        builder.HasIndex(b => b.QrToken)
            .IsUnique()
            .HasFilter("([QrToken] IS NOT NULL)")
            .HasDatabaseName("UX_Bookings_QrToken");

        builder.HasIndex(b => new { b.CourtId, b.StartAt })
            .HasDatabaseName("IX_Bookings_Court_StartAt");

        builder.HasIndex(b => new { b.CustomerUserId, b.StartAt })
            .HasDatabaseName("IX_Bookings_Customer_StartAt");

        builder.HasIndex(b => new { b.BookingStatus, b.StartAt })
            .HasDatabaseName("IX_Bookings_Status_StartAt");

        builder.HasIndex(b => b.CreatedByUserId)
            .HasDatabaseName("IX_Bookings_CreatedByUser");

        // Relationships
        builder.HasOne(b => b.CustomerUser)
            .WithMany(u => u.CustomerBookings)
            .HasForeignKey(b => b.CustomerUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(b => b.CreatedByUser)
            .WithMany(u => u.CreatedBookings)
            .HasForeignKey(b => b.CreatedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(b => b.Court)
            .WithMany(c => c.Bookings)
            .HasForeignKey(b => b.CourtId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(b => b.CancellationPolicy)
            .WithMany(cp => cp.Bookings)
            .HasForeignKey(b => b.CancellationPolicyId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(b => b.Slots)
            .WithOne(s => s.Booking)
            .HasForeignKey(s => s.BookingId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(b => b.Payments)
            .WithOne(p => p.Booking)
            .HasForeignKey(p => p.BookingId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(b => b.CheckIn)
            .WithOne(c => c.Booking)
            .HasForeignKey<CheckIn>(c => c.BookingId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(b => b.StatusHistories)
            .WithOne(sh => sh.Booking)
            .HasForeignKey(sh => sh.BookingId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(b => b.CancellationRequests)
            .WithOne(cr => cr.Booking)
            .HasForeignKey(cr => cr.BookingId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(b => b.Review)
            .WithOne(r => r.Booking)
            .HasForeignKey<Review>(r => r.BookingId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
