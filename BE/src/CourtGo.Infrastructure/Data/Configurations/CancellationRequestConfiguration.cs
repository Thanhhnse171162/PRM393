using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourtGo.Infrastructure.Data.Configurations;

public class CancellationRequestConfiguration : IEntityTypeConfiguration<CancellationRequest>
{
    public void Configure(EntityTypeBuilder<CancellationRequest> builder)
    {
        builder.ToTable("CancellationRequests");

        builder.HasKey(cr => cr.Id);
        builder.Property(cr => cr.Id).HasDefaultValueSql("(newsequentialid())");

        builder.Property(cr => cr.BookingId).IsRequired();
        builder.Property(cr => cr.RequestedByUserId).IsRequired();
        builder.Property(cr => cr.RequestSource).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        builder.Property(cr => cr.Reason).HasMaxLength(500).IsRequired();
        builder.Property(cr => cr.Status).HasConversion<byte>().HasColumnType("tinyint").HasDefaultValue(CancellationRequestStatus.Pending).IsRequired();
        builder.Property(cr => cr.CalculatedRefundAmount).HasPrecision(18, 2).HasDefaultValue(0m).IsRequired();
        builder.Property(cr => cr.ApprovedRefundAmount).HasPrecision(18, 2).IsRequired(false);
        builder.Property(cr => cr.ProcessedByUserId).IsRequired(false);
        builder.Property(cr => cr.ProcessedAt).HasColumnType("datetimeoffset").IsRequired(false);
        builder.Property(cr => cr.CreatedAt).HasColumnType("datetimeoffset").HasDefaultValueSql("(sysutcdatetime())").IsRequired();
        builder.Property(cr => cr.UpdatedAt).HasColumnType("datetimeoffset").IsRequired(false);

        // Indexes
        builder.HasIndex(cr => new { cr.BookingId, cr.Status })
            .HasDatabaseName("IX_CancellationRequests_Booking_Status");

        builder.HasIndex(cr => new { cr.Status, cr.CreatedAt })
            .HasDatabaseName("IX_CancellationRequests_Status_CreatedAt");

        // Relationships
        builder.HasOne(cr => cr.Booking)
            .WithMany(b => b.CancellationRequests)
            .HasForeignKey(cr => cr.BookingId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(cr => cr.RequestedByUser)
            .WithMany()
            .HasForeignKey(cr => cr.RequestedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(cr => cr.ProcessedByUser)
            .WithMany()
            .HasForeignKey(cr => cr.ProcessedByUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
