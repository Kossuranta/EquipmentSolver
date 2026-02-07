namespace EquipmentSolver.Core.Entities;

/// <summary>
/// A weighted priority for the solver (e.g., maximize armor with weight 1.0).
/// </summary>
public class SolverPriority
{
    public int Id { get; set; }
    public int PresetId { get; set; }
    public SolverPreset Preset { get; set; } = null!;

    public int StatTypeId { get; set; }
    public StatType StatType { get; set; } = null!;

    /// <summary>
    /// Weight multiplier. Positive = maximize, negative = minimize.
    /// </summary>
    public double Weight { get; set; }
}
