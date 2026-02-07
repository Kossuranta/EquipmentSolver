namespace EquipmentSolver.Core.Entities;

/// <summary>
/// A saved solver configuration (constraints + priorities) for a profile.
/// </summary>
public class SolverPreset
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    public GameProfile Profile { get; set; } = null!;

    public string Name { get; set; } = null!;

    // Navigation properties
    public List<SolverConstraint> Constraints { get; set; } = [];
    public List<SolverPriority> Priorities { get; set; } = [];
}
