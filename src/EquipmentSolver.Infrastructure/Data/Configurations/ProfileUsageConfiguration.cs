using EquipmentSolver.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EquipmentSolver.Infrastructure.Data.Configurations;

public class ProfileUsageConfiguration : IEntityTypeConfiguration<ProfileUsage>
{
    public void Configure(EntityTypeBuilder<ProfileUsage> builder)
    {
        builder.HasKey(u => new { u.UserId, u.ProfileId });

        builder.HasOne(u => u.User)
            .WithMany(usr => usr.ProfileUsages)
            .HasForeignKey(u => u.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(u => u.Profile)
            .WithMany(p => p.Usages)
            .HasForeignKey(u => u.ProfileId)
            .OnDelete(DeleteBehavior.NoAction); // Avoid cascade cycle
    }
}
