using CourtGo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourtGo.Infrastructure.Data.Configurations;

public class SportCenterConfiguration : IEntityTypeConfiguration<SportCenter>
{
    public void Configure(EntityTypeBuilder<SportCenter> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Address).HasMaxLength(500).IsRequired();
        b.Property(x => x.District).HasMaxLength(100);
        b.Property(x => x.City).HasMaxLength(100);
        b.Property(x => x.PhoneNumber).HasMaxLength(20);
        b.Property(x => x.ImageUrl).HasMaxLength(500);
    }
}
