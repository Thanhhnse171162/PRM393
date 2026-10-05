using CourtGo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourtGo.Infrastructure.Data.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.TotalAmount).HasPrecision(18, 2);
        b.Property(x => x.DepositAmount).HasPrecision(18, 2);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.PaymentStatus).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.CheckInCode).HasMaxLength(64);
        b.Property(x => x.Note).HasMaxLength(500);

        b.HasOne(x => x.Customer).WithMany()
            .HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Court).WithMany()
            .HasForeignKey(x => x.CourtId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.CheckInCode);
        b.HasIndex(x => new { x.CourtId, x.StartTime });
    }
}
