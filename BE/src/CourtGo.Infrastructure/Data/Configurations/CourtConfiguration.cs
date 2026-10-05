using CourtGo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourtGo.Infrastructure.Data.Configurations;

public class CourtConfiguration : IEntityTypeConfiguration<Court>
{
    public void Configure(EntityTypeBuilder<Court> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.PricePerHour).HasPrecision(18, 2);

        b.HasOne(x => x.SportCenter).WithMany(c => c.Courts)
            .HasForeignKey(x => x.SportCenterId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Sport).WithMany(s => s.Courts)
            .HasForeignKey(x => x.SportId).OnDelete(DeleteBehavior.Restrict);
    }
}
