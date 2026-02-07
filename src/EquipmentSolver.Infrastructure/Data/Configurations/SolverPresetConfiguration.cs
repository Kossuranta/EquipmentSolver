using EquipmentSolver.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EquipmentSolver.Infrastructure.Data.Configurations;

public class SolverPresetConfiguration : IEntityTypeConfiguration<SolverPreset>
{
    public void Configure(EntityTypeBuilder<SolverPreset> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);

        builder.HasOne(p => p.Profile)
            .WithMany(pr => pr.SolverPresets)
            .HasForeignKey(p => p.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.ProfileId);
    }
}
