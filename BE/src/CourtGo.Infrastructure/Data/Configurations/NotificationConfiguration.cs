using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourtGo.Infrastructure.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasDefaultValueSql("(newsequentialid())");

        builder.Property(n => n.UserId).IsRequired();
        builder.Property(n => n.Type).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        builder.Property(n => n.Title).HasMaxLength(200).IsRequired();
        builder.Property(n => n.Message).HasMaxLength(1000).IsRequired();
        builder.Property(n => n.ReferenceType).HasMaxLength(50).IsRequired(false);
        builder.Property(n => n.ReferenceId).IsRequired(false);
        builder.Property(n => n.DeepLink).HasMaxLength(300).IsRequired(false);
        builder.Property(n => n.IsRead).HasDefaultValue(false).IsRequired();
        builder.Property(n => n.CreatedAt).HasColumnType("datetimeoffset").HasDefaultValueSql("(sysutcdatetime())").IsRequired();
        builder.Property(n => n.ReadAt).HasColumnType("datetimeoffset").IsRequired(false);

        // Indexes
        builder.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt })
            .HasDatabaseName("IX_Notifications_User_Read_CreatedAt");

        // Relationships
        builder.HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
