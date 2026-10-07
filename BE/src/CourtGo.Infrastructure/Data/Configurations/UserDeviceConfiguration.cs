using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourtGo.Infrastructure.Data.Configurations;

public class UserDeviceConfiguration : IEntityTypeConfiguration<UserDevice>
{
    public void Configure(EntityTypeBuilder<UserDevice> builder)
    {
        builder.ToTable("UserDevices");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasDefaultValueSql("(newsequentialid())");

        builder.Property(d => d.UserId).IsRequired();
        builder.Property(d => d.DeviceToken).HasMaxLength(500).IsRequired();
        builder.Property(d => d.Platform).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        builder.Property(d => d.DeviceName).HasMaxLength(150).IsRequired(false);
        builder.Property(d => d.IsActive).HasDefaultValue(true).IsRequired();
        builder.Property(d => d.LastSeenAt).HasColumnType("datetimeoffset").IsRequired(false);
        builder.Property(d => d.CreatedAt).HasColumnType("datetimeoffset").HasDefaultValueSql("(sysutcdatetime())").IsRequired();

        // Indexes
        builder.HasIndex(d => d.DeviceToken).IsUnique().HasDatabaseName("UX_UserDevices_DeviceToken");
        builder.HasIndex(d => new { d.UserId, d.IsActive }).HasDatabaseName("IX_UserDevices_UserId_IsActive");

        // Relationships
        builder.HasOne(d => d.User)
            .WithMany(u => u.UserDevices)
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
