namespace EquipmentSolver.Core.Entities;

/// <summary>
/// A game profile defining equipment, slots, stats, and solver presets for a specific game.
/// </summary>
public class GameProfile
{
    public int Id { get; set; }
    public string OwnerId { get; set; } = null!;
    public ApplicationUser Owner { get; set; } = null!;

    // IGDB game data
    public int IgdbGameId { get; set; }
    public string GameName { get; set; } = null!;
    public string? GameCoverUrl { get; set; }

    public string? Description { get; set; }

    /// <summary>
    /// Version in major.minor.patch format (e.g., "0.1.0"). Stored as string.
    /// </summary>
    public string Version { get; set; } = "0.1.0";

    public bool IsPublic { get; set; }
    public int VoteScore { get; set; }
    public int UsageCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public List<EquipmentSlot> Slots { get; set; } = [];
    public List<StatType> StatTypes { get; set; } = [];
    public List<Equipment> Equipment { get; set; } = [];
    public List<SolverPreset> SolverPresets { get; set; } = [];
    public List<ProfilePatchNote> PatchNotes { get; set; } = [];
    public List<ProfileVote> Votes { get; set; } = [];
    public List<ProfileUsage> Usages { get; set; } = [];
}
