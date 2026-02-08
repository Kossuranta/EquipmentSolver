using EquipmentSolver.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EquipmentSolver.Infrastructure.Data.Configurations;

public class GameProfileConfiguration : IEntityTypeConfiguration<GameProfile>
{
    public void Configure(EntityTypeBuilder<GameProfile> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(100);
        builder.Property(p => p.GameName).IsRequired().HasMaxLength(200);
        builder.Property(p => p.GameCoverUrl).HasMaxLength(500);
        builder.Property(p => p.Description).HasMaxLength(2000);
        builder.Property(p => p.Version).IsRequired().HasMaxLength(15);

        builder.HasOne(p => p.Owner)
            .WithMany(u => u.OwnedProfiles)
            .HasForeignKey(p => p.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.OwnerId);
        builder.HasIndex(p => p.IgdbGameId);
        builder.HasIndex(p => p.IsPublic);
    }
}
