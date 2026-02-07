using EquipmentSolver.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EquipmentSolver.Infrastructure.Data.Configurations;

public class ProfilePatchNoteConfiguration : IEntityTypeConfiguration<ProfilePatchNote>
{
    public void Configure(EntityTypeBuilder<ProfilePatchNote> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Version).IsRequired().HasMaxLength(15);
        builder.Property(n => n.Content).IsRequired().HasMaxLength(5000);

        builder.HasOne(n => n.Profile)
            .WithMany(p => p.PatchNotes)
            .HasForeignKey(n => n.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(n => n.ProfileId);
    }
}
