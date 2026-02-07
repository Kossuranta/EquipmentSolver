namespace EquipmentSolver.Core.Entities;

/// <summary>
/// Tracks which users are actively using a public profile.
/// </summary>
public class ProfileUsage
{
    public string UserId { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;

    public int ProfileId { get; set; }
    public GameProfile Profile { get; set; } = null!;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
}
