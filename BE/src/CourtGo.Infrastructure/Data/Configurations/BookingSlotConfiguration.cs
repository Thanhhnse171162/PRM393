using CourtGo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourtGo.Infrastructure.Data.Configurations;

public class BookingSlotConfiguration : IEntityTypeConfiguration<BookingSlot>
{
    public void Configure(EntityTypeBuilder<BookingSlot> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Price).HasPrecision(18, 2);

        b.HasOne(x => x.Booking).WithMany(x => x.Slots)
            .HasForeignKey(x => x.BookingId).OnDelete(DeleteBehavior.Cascade);

        // Lookup index for availability checks. A unique/filtered index to hard-block
        // double booking is added together with the booking engine.
        b.HasIndex(x => new { x.CourtId, x.StartTime });
    }
}
