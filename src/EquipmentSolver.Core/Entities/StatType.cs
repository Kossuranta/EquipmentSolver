namespace EquipmentSolver.Core.Entities;

/// <summary>
/// A stat type defined within a game profile (e.g., Armor, Weight, Fire Resistance).
/// </summary>
public class StatType
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    public GameProfile Profile { get; set; } = null!;

    /// <summary>
    /// Display name (e.g., "Fire Resistance"). Unique per profile.
    /// </summary>
    public string DisplayName { get; set; } = null!;

    // Navigation properties
    public List<EquipmentStat> EquipmentStats { get; set; } = [];
    public List<SolverConstraint> Constraints { get; set; } = [];
    public List<SolverPriority> Priorities { get; set; } = [];
}
