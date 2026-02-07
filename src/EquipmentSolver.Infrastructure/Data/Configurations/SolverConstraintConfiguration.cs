using EquipmentSolver.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EquipmentSolver.Infrastructure.Data.Configurations;

public class SolverConstraintConfiguration : IEntityTypeConfiguration<SolverConstraint>
{
    public void Configure(EntityTypeBuilder<SolverConstraint> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Operator).IsRequired().HasMaxLength(5);

        builder.HasOne(c => c.Preset)
            .WithMany(p => p.Constraints)
            .HasForeignKey(c => c.PresetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.StatType)
            .WithMany(s => s.Constraints)
            .HasForeignKey(c => c.StatTypeId)
            .OnDelete(DeleteBehavior.NoAction); // Avoid cascade cycle
    }
}
