namespace EquipmentSolver.Core.Entities;

/// <summary>
/// A hard constraint for the solver (e.g., weight &lt;= 70).
/// </summary>
public class SolverConstraint
{
    public int Id { get; set; }
    public int PresetId { get; set; }
    public SolverPreset Preset { get; set; } = null!;

    public int StatTypeId { get; set; }
    public StatType StatType { get; set; } = null!;

    /// <summary>
    /// Comparison operator (e.g., "<=", ">=", "==").
    /// </summary>
    public string Operator { get; set; } = null!;

    public double Value { get; set; }
}
