using EquipmentSolver.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EquipmentSolver.Infrastructure.Data.Configurations;

public class SolverPriorityConfiguration : IEntityTypeConfiguration<SolverPriority>
{
    public void Configure(EntityTypeBuilder<SolverPriority> builder)
    {
        builder.HasKey(p => p.Id);

        builder.HasOne(p => p.Preset)
            .WithMany(pr => pr.Priorities)
            .HasForeignKey(p => p.PresetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.StatType)
            .WithMany(s => s.Priorities)
            .HasForeignKey(p => p.StatTypeId)
            .OnDelete(DeleteBehavior.NoAction); // Avoid cascade cycle
    }
}
