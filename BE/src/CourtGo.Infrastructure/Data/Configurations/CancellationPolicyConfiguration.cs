using CourtGo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourtGo.Infrastructure.Data.Configurations;

public class CancellationPolicyConfiguration : IEntityTypeConfiguration<CancellationPolicy>
{
    public void Configure(EntityTypeBuilder<CancellationPolicy> builder)
    {
        builder.ToTable("CancellationPolicies");

        builder.HasKey(cp => cp.Id);
        builder.Property(cp => cp.Id).HasDefaultValueSql("(newsequentialid())");

        builder.Property(cp => cp.Name).HasMaxLength(150).IsRequired();
        builder.Property(cp => cp.Version).IsRequired();
        builder.Property(cp => cp.EffectiveFrom).HasColumnType("datetimeoffset").IsRequired();
        builder.Property(cp => cp.EffectiveTo).HasColumnType("datetimeoffset").IsRequired(false);
        builder.Property(cp => cp.IsActive).HasDefaultValue(false).IsRequired();
        builder.Property(cp => cp.CreatedAt).HasColumnType("datetimeoffset").HasDefaultValueSql("(sysutcdatetime())").IsRequired();

        // Indexes
        builder.HasIndex(cp => new { cp.Name, cp.Version })
            .IsUnique()
            .HasDatabaseName("UQ_CancellationPolicies_Name_Version");

        builder.HasIndex(cp => cp.IsActive)
            .IsUnique()
            .HasFilter("([IsActive]=(1))")
            .HasDatabaseName("UX_CancellationPolicies_OneActive");

        // Relationships
        builder.HasMany(cp => cp.Rules)
            .WithOne(r => r.CancellationPolicy)
            .HasForeignKey(r => r.CancellationPolicyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(cp => cp.Bookings)
            .WithOne(b => b.CancellationPolicy)
            .HasForeignKey(b => b.CancellationPolicyId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
