namespace EquipmentSolver.Core.Entities;

/// <summary>
/// A stat value on an equipment piece (e.g., armor: 50).
/// </summary>
public class EquipmentStat
{
    public int EquipmentId { get; set; }
    public Equipment Equipment { get; set; } = null!;

    public int StatTypeId { get; set; }
    public StatType StatType { get; set; } = null!;

    public double Value { get; set; }
}
