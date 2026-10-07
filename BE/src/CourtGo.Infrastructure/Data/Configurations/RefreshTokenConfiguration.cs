using CourtGo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourtGo.Infrastructure.Data.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasDefaultValueSql("(newsequentialid())");

        builder.Property(r => r.UserId).IsRequired();
        builder.Property(r => r.TokenHash).HasMaxLength(500).IsRequired();
        builder.Property(r => r.ExpiresAt).HasColumnType("datetimeoffset").IsRequired();
        builder.Property(r => r.RevokedAt).HasColumnType("datetimeoffset").IsRequired(false);
        builder.Property(r => r.DeviceInfo).HasMaxLength(250).IsRequired(false);
        builder.Property(r => r.CreatedAt).HasColumnType("datetimeoffset").HasDefaultValueSql("(sysutcdatetime())").IsRequired();

        // Indexes
        builder.HasIndex(r => r.TokenHash).IsUnique().HasDatabaseName("UX_RefreshTokens_TokenHash");
        builder.HasIndex(r => new { r.UserId, r.ExpiresAt }).HasDatabaseName("IX_RefreshTokens_UserId_ExpiresAt");

        // Relationships
        builder.HasOne(r => r.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
