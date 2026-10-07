using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourtGo.Infrastructure.Data.Configurations;

public class CourtBlockConfiguration : IEntityTypeConfiguration<CourtBlock>
{
    public void Configure(EntityTypeBuilder<CourtBlock> builder)
    {
        builder.ToTable("CourtBlocks");

        builder.HasKey(cb => cb.Id);
        builder.Property(cb => cb.Id).HasDefaultValueSql("(newsequentialid())");

        builder.Property(cb => cb.CourtId).IsRequired();
        builder.Property(cb => cb.StartAt).HasColumnType("datetimeoffset").IsRequired();
        builder.Property(cb => cb.EndAt).HasColumnType("datetimeoffset").IsRequired();
        builder.Property(cb => cb.Type).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        builder.Property(cb => cb.Reason).HasMaxLength(500).IsRequired();
        builder.Property(cb => cb.CreatedByUserId).IsRequired();
        builder.Property(cb => cb.CreatedAt).HasColumnType("datetimeoffset").HasDefaultValueSql("(sysutcdatetime())").IsRequired();

        // Indexes
        builder.HasIndex(cb => new { cb.CourtId, cb.StartAt, cb.EndAt })
            .HasDatabaseName("IX_CourtBlocks_Court_Start_End");

        // Relationships
        builder.HasOne(cb => cb.Court)
            .WithMany(c => c.CourtBlocks)
            .HasForeignKey(cb => cb.CourtId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(cb => cb.CreatedByUser)
            .WithMany()
            .HasForeignKey(cb => cb.CreatedByUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
