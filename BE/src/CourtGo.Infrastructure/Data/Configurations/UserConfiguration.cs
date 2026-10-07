using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourtGo.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasDefaultValueSql("(newsequentialid())");

        builder.Property(u => u.FullName).HasMaxLength(150).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(255).IsRequired(false);
        builder.Property(u => u.PhoneNumber).HasMaxLength(20).IsRequired();
        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.Role).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        builder.Property(u => u.AvatarUrl).HasMaxLength(500).IsRequired(false);
        builder.Property(u => u.IsActive).HasDefaultValue(true).IsRequired();
        builder.Property(u => u.PhoneVerifiedAt).HasColumnType("datetimeoffset").IsRequired(false);
        builder.Property(u => u.EmailVerifiedAt).HasColumnType("datetimeoffset").IsRequired(false);
        builder.Property(u => u.CreatedAt).HasColumnType("datetimeoffset").HasDefaultValueSql("(sysutcdatetime())").IsRequired();
        builder.Property(u => u.UpdatedAt).HasColumnType("datetimeoffset").IsRequired(false);

        // Indexes
        builder.HasIndex(u => u.PhoneNumber).IsUnique().HasDatabaseName("UX_Users_PhoneNumber");
        builder.HasIndex(u => u.Email).IsUnique().HasFilter("[Email] IS NOT NULL").HasDatabaseName("UX_Users_Email");

        // Relationships
        builder.HasOne(u => u.UserPreference)
            .WithOne(p => p.User)
            .HasForeignKey<UserPreference>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.RefreshTokens)
            .WithOne(r => r.User)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.UserDevices)
            .WithOne(d => d.User)
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.Notifications)
            .WithOne(n => n.User)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.StaffAssignments)
            .WithOne(s => s.StaffUser)
            .HasForeignKey(s => s.StaffUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(u => u.CustomerBookings)
            .WithOne(b => b.CustomerUser)
            .HasForeignKey(b => b.CustomerUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(u => u.CreatedBookings)
            .WithOne(b => b.CreatedByUser)
            .HasForeignKey(b => b.CreatedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(u => u.Reviews)
            .WithOne(r => r.CustomerUser)
            .HasForeignKey(r => r.CustomerUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
