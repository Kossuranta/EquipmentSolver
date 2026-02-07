using EquipmentSolver.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EquipmentSolver.Infrastructure.Data.Configurations;

public class UserEquipmentStateConfiguration : IEntityTypeConfiguration<UserEquipmentState>
{
    public void Configure(EntityTypeBuilder<UserEquipmentState> builder)
    {
        builder.HasKey(s => new { s.UserId, s.EquipmentId });

        builder.HasOne(s => s.User)
            .WithMany(u => u.EquipmentStates)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Equipment)
            .WithMany(e => e.UserStates)
            .HasForeignKey(s => s.EquipmentId)
            .OnDelete(DeleteBehavior.NoAction); // Avoid cascade cycle
    }
}
