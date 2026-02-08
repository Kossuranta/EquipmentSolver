using EquipmentSolver.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EquipmentSolver.Infrastructure.Data.Configurations;

public class StatTypeConfiguration : IEntityTypeConfiguration<StatType>
{
    public void Configure(EntityTypeBuilder<StatType> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.DisplayName).IsRequired().HasMaxLength(200);

        builder.HasOne(s => s.Profile)
            .WithMany(p => p.StatTypes)
            .HasForeignKey(s => s.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.ProfileId, s.DisplayName }).IsUnique();
    }
}
