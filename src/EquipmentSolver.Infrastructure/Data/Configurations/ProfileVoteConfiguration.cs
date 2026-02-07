using EquipmentSolver.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EquipmentSolver.Infrastructure.Data.Configurations;

public class ProfileVoteConfiguration : IEntityTypeConfiguration<ProfileVote>
{
    public void Configure(EntityTypeBuilder<ProfileVote> builder)
    {
        builder.HasKey(v => new { v.UserId, v.ProfileId });

        builder.HasOne(v => v.User)
            .WithMany(u => u.Votes)
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(v => v.Profile)
            .WithMany(p => p.Votes)
            .HasForeignKey(v => v.ProfileId)
            .OnDelete(DeleteBehavior.NoAction); // Avoid cascade cycle (user -> profile owner + user -> voter)
    }
}
