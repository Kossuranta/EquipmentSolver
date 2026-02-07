using EquipmentSolver.Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EquipmentSolver.Infrastructure.Data;

/// <summary>
/// Application database context with ASP.NET Identity support.
/// </summary>
public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<GameProfile> GameProfiles => Set<GameProfile>();
    public DbSet<ProfilePatchNote> ProfilePatchNotes => Set<ProfilePatchNote>();
    public DbSet<EquipmentSlot> EquipmentSlots => Set<EquipmentSlot>();
    public DbSet<StatType> StatTypes => Set<StatType>();
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<EquipmentSlotCompatibility> EquipmentSlotCompatibilities => Set<EquipmentSlotCompatibility>();
    public DbSet<EquipmentStat> EquipmentStats => Set<EquipmentStat>();
    public DbSet<UserSlotState> UserSlotStates => Set<UserSlotState>();
    public DbSet<UserEquipmentState> UserEquipmentStates => Set<UserEquipmentState>();
    public DbSet<SolverPreset> SolverPresets => Set<SolverPreset>();
    public DbSet<SolverConstraint> SolverConstraints => Set<SolverConstraint>();
    public DbSet<SolverPriority> SolverPriorities => Set<SolverPriority>();
    public DbSet<ProfileVote> ProfileVotes => Set<ProfileVote>();
    public DbSet<ProfileUsage> ProfileUsages => Set<ProfileUsage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
