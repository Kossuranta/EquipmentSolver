using EquipmentSolver.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EquipmentSolver.Infrastructure.Data.Configurations;

public class EquipmentSlotCompatibilityConfiguration : IEntityTypeConfiguration<EquipmentSlotCompatibility>
{
    public void Configure(EntityTypeBuilder<EquipmentSlotCompatibility> builder)
    {
        builder.HasKey(c => new { c.EquipmentId, c.SlotId });

        builder.HasOne(c => c.Equipment)
            .WithMany(e => e.SlotCompatibilities)
            .HasForeignKey(c => c.EquipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Slot)
            .WithMany(s => s.CompatibleEquipment)
            .HasForeignKey(c => c.SlotId)
            .OnDelete(DeleteBehavior.NoAction); // Avoid cascade cycle (profile -> slot + profile -> equipment)
    }
}
