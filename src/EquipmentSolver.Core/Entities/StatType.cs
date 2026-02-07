namespace EquipmentSolver.Core.Entities;

/// <summary>
/// A stat type defined within a game profile (e.g., armor, weight, fire_res).
/// </summary>
public class StatType
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    public GameProfile Profile { get; set; } = null!;

    /// <summary>
    /// Internal name (e.g., "fire_res").
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// User-friendly display name (e.g., "Fire Resistance").
    /// </summary>
    public string DisplayName { get; set; } = null!;

    // Navigation properties
    public List<EquipmentStat> EquipmentStats { get; set; } = [];
    public List<SolverConstraint> Constraints { get; set; } = [];
    public List<SolverPriority> Priorities { get; set; } = [];
}
