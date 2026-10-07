using CourtGo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourtGo.Infrastructure.Data.Configurations;

public class CancellationPolicyRuleConfiguration : IEntityTypeConfiguration<CancellationPolicyRule>
{
    public void Configure(EntityTypeBuilder<CancellationPolicyRule> builder)
    {
        builder.ToTable("CancellationPolicyRules");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasDefaultValueSql("(newsequentialid())");

        builder.Property(r => r.CancellationPolicyId).IsRequired();
        builder.Property(r => r.MinHoursBeforeStart).IsRequired();
        builder.Property(r => r.MaxHoursBeforeStart).IsRequired(false);
        builder.Property(r => r.RefundPercent).HasPrecision(5, 2).IsRequired();

        // Indexes
        builder.HasIndex(r => new { r.CancellationPolicyId, r.MinHoursBeforeStart })
            .HasDatabaseName("IX_CancellationPolicyRules_Policy_MinHours");

        // Relationships
        builder.HasOne(r => r.CancellationPolicy)
            .WithMany(cp => cp.Rules)
            .HasForeignKey(r => r.CancellationPolicyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
