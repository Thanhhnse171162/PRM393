using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourtGo.Infrastructure.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasDefaultValueSql("(newsequentialid())");

        builder.Property(p => p.BookingId).IsRequired();
        builder.Property(p => p.PaymentKind).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        builder.Property(p => p.PaymentMethod).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        builder.Property(p => p.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(p => p.TransactionStatus).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        builder.Property(p => p.ProviderTransactionId).HasMaxLength(200).IsRequired(false);
        builder.Property(p => p.RelatedPaymentId).IsRequired(false);
        builder.Property(p => p.PaidByUserId).IsRequired(false);
        builder.Property(p => p.ConfirmedByUserId).IsRequired(false);
        builder.Property(p => p.PaidAt).HasColumnType("datetimeoffset").IsRequired(false);
        builder.Property(p => p.CreatedAt).HasColumnType("datetimeoffset").HasDefaultValueSql("(sysutcdatetime())").IsRequired();

        // Indexes
        builder.HasIndex(p => p.ProviderTransactionId)
            .IsUnique()
            .HasFilter("([ProviderTransactionId] IS NOT NULL)")
            .HasDatabaseName("UX_Payments_ProviderTransactionId");

        builder.HasIndex(p => p.BookingId).HasDatabaseName("IX_Payments_BookingId");
        builder.HasIndex(p => new { p.TransactionStatus, p.CreatedAt }).HasDatabaseName("IX_Payments_Status_CreatedAt");

        // Relationships
        builder.HasOne(p => p.Booking)
            .WithMany(b => b.Payments)
            .HasForeignKey(p => p.BookingId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(p => p.RelatedPayment)
            .WithMany(p => p.RefundPayments)
            .HasForeignKey(p => p.RelatedPaymentId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(p => p.PaidByUser)
            .WithMany()
            .HasForeignKey(p => p.PaidByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(p => p.ConfirmedByUser)
            .WithMany()
            .HasForeignKey(p => p.ConfirmedByUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
