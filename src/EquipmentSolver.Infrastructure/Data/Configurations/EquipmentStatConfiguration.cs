using EquipmentSolver.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EquipmentSolver.Infrastructure.Data.Configurations;

public class EquipmentStatConfiguration : IEntityTypeConfiguration<EquipmentStat>
{
    public void Configure(EntityTypeBuilder<EquipmentStat> builder)
    {
        builder.HasKey(s => new { s.EquipmentId, s.StatTypeId });

        builder.HasOne(s => s.Equipment)
            .WithMany(e => e.Stats)
            .HasForeignKey(s => s.EquipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.StatType)
            .WithMany(t => t.EquipmentStats)
            .HasForeignKey(s => s.StatTypeId)
            .OnDelete(DeleteBehavior.NoAction); // Avoid cascade cycle
    }
}
