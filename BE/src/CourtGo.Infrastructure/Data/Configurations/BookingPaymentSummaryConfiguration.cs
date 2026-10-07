using CourtGo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourtGo.Infrastructure.Data.Configurations;

public class BookingPaymentSummaryConfiguration : IEntityTypeConfiguration<BookingPaymentSummary>
{
    public void Configure(EntityTypeBuilder<BookingPaymentSummary> builder)
    {
        builder.HasNoKey();
        builder.ToView("vw_BookingPaymentSummary");

        builder.Property(v => v.BookingId).IsRequired();
        builder.Property(v => v.BookingCode).HasMaxLength(30).IsRequired();
        builder.Property(v => v.TotalAmount).HasPrecision(18, 2);
        builder.Property(v => v.SuccessfulChargeAmount).HasPrecision(18, 2);
        builder.Property(v => v.SuccessfulRefundAmount).HasPrecision(18, 2);
        builder.Property(v => v.RemainingToCollect).HasPrecision(18, 2);
    }
}
