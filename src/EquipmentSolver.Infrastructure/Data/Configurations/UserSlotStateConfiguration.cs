using EquipmentSolver.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EquipmentSolver.Infrastructure.Data.Configurations;

public class UserSlotStateConfiguration : IEntityTypeConfiguration<UserSlotState>
{
    public void Configure(EntityTypeBuilder<UserSlotState> builder)
    {
        builder.HasKey(s => new { s.UserId, s.SlotId });

        builder.HasOne(s => s.User)
            .WithMany(u => u.SlotStates)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Slot)
            .WithMany(sl => sl.UserStates)
            .HasForeignKey(s => s.SlotId)
            .OnDelete(DeleteBehavior.NoAction); // Avoid cascade cycle
    }
}
