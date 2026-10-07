using CourtGo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourtGo.Infrastructure.Data.Configurations;

public class StaffAssignmentConfiguration : IEntityTypeConfiguration<StaffAssignment>
{
    public void Configure(EntityTypeBuilder<StaffAssignment> builder)
    {
        builder.ToTable("StaffAssignments");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasDefaultValueSql("(newsequentialid())");

        builder.Property(s => s.StaffUserId).IsRequired();
        builder.Property(s => s.SportCenterId).IsRequired();
        builder.Property(s => s.AssignedAt).HasColumnType("datetimeoffset").HasDefaultValueSql("(sysutcdatetime())").IsRequired();
        builder.Property(s => s.IsActive).HasDefaultValue(true).IsRequired();

        // Indexes
        builder.HasIndex(s => s.StaffUserId)
            .IsUnique()
            .HasFilter("([IsActive]=(1))")
            .HasDatabaseName("UX_StaffAssignments_OneActivePerStaff");

        builder.HasIndex(s => new { s.SportCenterId, s.IsActive })
            .HasDatabaseName("IX_StaffAssignments_SportCenterId_IsActive");

        // Relationships
        builder.HasOne(s => s.StaffUser)
            .WithMany(u => u.StaffAssignments)
            .HasForeignKey(s => s.StaffUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(s => s.SportCenter)
            .WithMany(c => c.StaffAssignments)
            .HasForeignKey(s => s.SportCenterId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
