using CourtGo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourtGo.Infrastructure.Data.Configurations;

public class StaffAssignmentConfiguration : IEntityTypeConfiguration<StaffAssignment>
{
    public void Configure(EntityTypeBuilder<StaffAssignment> b)
    {
        b.HasKey(x => x.Id);

        b.HasOne(x => x.User).WithMany()
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.SportCenter).WithMany(c => c.StaffAssignments)
            .HasForeignKey(x => x.SportCenterId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.UserId, x.SportCenterId });
    }
}
