using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourtGo.Infrastructure.Data.Configurations;

public class VerificationCodeConfiguration : IEntityTypeConfiguration<VerificationCode>
{
    public void Configure(EntityTypeBuilder<VerificationCode> builder)
    {
        builder.ToTable("VerificationCodes");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).HasDefaultValueSql("(newsequentialid())");

        builder.Property(v => v.UserId).IsRequired(false);
        builder.Property(v => v.Target).HasMaxLength(255).IsRequired();
        builder.Property(v => v.Purpose).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        builder.Property(v => v.CodeHash).HasMaxLength(500).IsRequired();
        builder.Property(v => v.ExpiresAt).HasColumnType("datetimeoffset").IsRequired();
        builder.Property(v => v.ConsumedAt).HasColumnType("datetimeoffset").IsRequired(false);
        builder.Property(v => v.AttemptCount).HasDefaultValue(0).IsRequired();
        builder.Property(v => v.CreatedAt).HasColumnType("datetimeoffset").HasDefaultValueSql("(sysutcdatetime())").IsRequired();

        // Indexes
        builder.HasIndex(v => new { v.Target, v.Purpose, v.ExpiresAt })
            .HasDatabaseName("IX_VerificationCodes_Target_Purpose_ExpiresAt");

        // Relationships
        builder.HasOne(v => v.User)
            .WithMany()
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
