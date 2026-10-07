using CourtGo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourtGo.Infrastructure.Data.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("Reviews");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasDefaultValueSql("(newsequentialid())");

        builder.Property(r => r.BookingId).IsRequired();
        builder.Property(r => r.CustomerUserId).IsRequired();
        builder.Property(r => r.Rating).HasColumnType("tinyint").IsRequired();
        builder.Property(r => r.Comment).HasMaxLength(1000).IsRequired(false);
        builder.Property(r => r.IsVisible).HasDefaultValue(true).IsRequired();
        builder.Property(r => r.CreatedAt).HasColumnType("datetimeoffset").HasDefaultValueSql("(sysutcdatetime())").IsRequired();

        // Indexes
        builder.HasIndex(r => r.BookingId)
            .IsUnique()
            .HasDatabaseName("UQ_Reviews_BookingId");

        builder.HasIndex(r => r.CustomerUserId)
            .HasDatabaseName("IX_Reviews_CustomerUserId");

        // Relationships
        builder.HasOne(r => r.Booking)
            .WithOne(b => b.Review)
            .HasForeignKey<Review>(r => r.BookingId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(r => r.CustomerUser)
            .WithMany(u => u.Reviews)
            .HasForeignKey(r => r.CustomerUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
