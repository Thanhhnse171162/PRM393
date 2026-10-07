using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourtGo.Infrastructure.Data.Configurations;

public class CheckInConfiguration : IEntityTypeConfiguration<CheckIn>
{
    public void Configure(EntityTypeBuilder<CheckIn> builder)
    {
        builder.ToTable("CheckIns");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasDefaultValueSql("(newsequentialid())");

        builder.Property(c => c.BookingId).IsRequired();
        builder.Property(c => c.StaffUserId).IsRequired();
        builder.Property(c => c.Method).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        builder.Property(c => c.CheckedInAt).HasColumnType("datetimeoffset").HasDefaultValueSql("(sysutcdatetime())").IsRequired();
        builder.Property(c => c.OutstandingPaymentOverride).HasDefaultValue(false).IsRequired();
        builder.Property(c => c.OverrideReason).HasMaxLength(500).IsRequired(false);
        builder.Property(c => c.Note).HasMaxLength(500).IsRequired(false);

        // Indexes
        builder.HasIndex(c => c.BookingId)
            .IsUnique()
            .HasDatabaseName("UQ_CheckIns_BookingId");

        // Relationships
        builder.HasOne(c => c.Booking)
            .WithOne(b => b.CheckIn)
            .HasForeignKey<CheckIn>(c => c.BookingId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(c => c.StaffUser)
            .WithMany()
            .HasForeignKey(c => c.StaffUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
