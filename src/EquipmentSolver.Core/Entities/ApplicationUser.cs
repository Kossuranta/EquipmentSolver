using Microsoft.AspNetCore.Identity;

namespace EquipmentSolver.Core.Entities;

/// <summary>
/// Application user extending ASP.NET Identity.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public List<GameProfile> OwnedProfiles { get; set; } = [];
    public List<UserSlotState> SlotStates { get; set; } = [];
    public List<UserEquipmentState> EquipmentStates { get; set; } = [];
    public List<ProfileVote> Votes { get; set; } = [];
    public List<ProfileUsage> ProfileUsages { get; set; } = [];
}
