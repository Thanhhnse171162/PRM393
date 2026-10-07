using CourtGo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourtGo.Infrastructure.Data.Configurations;

public class UserPreferenceConfiguration : IEntityTypeConfiguration<UserPreference>
{
    public void Configure(EntityTypeBuilder<UserPreference> builder)
    {
        builder.ToTable("UserPreferences");

        builder.HasKey(p => p.UserId);

        builder.Property(p => p.PreferredAreaName).HasMaxLength(150).IsRequired(false);
        builder.Property(p => p.PreferredLatitude).HasPrecision(9, 6).IsRequired(false);
        builder.Property(p => p.PreferredLongitude).HasPrecision(9, 6).IsRequired(false);
        builder.Property(p => p.BookingNotifications).HasDefaultValue(true).IsRequired();
        builder.Property(p => p.PaymentNotifications).HasDefaultValue(true).IsRequired();
        builder.Property(p => p.ReminderNotifications).HasDefaultValue(true).IsRequired();
        builder.Property(p => p.UpdatedAt).HasColumnType("datetimeoffset").HasDefaultValueSql("(sysutcdatetime())").IsRequired();

        // Relationships
        builder.HasOne(p => p.User)
            .WithOne(u => u.UserPreference)
            .HasForeignKey<UserPreference>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
