namespace EquipmentSolver.Core.Entities;

/// <summary>
/// Many-to-many join: which slots an equipment piece can be placed in.
/// </summary>
public class EquipmentSlotCompatibility
{
    public int EquipmentId { get; set; }
    public Equipment Equipment { get; set; } = null!;

    public int SlotId { get; set; }
    public EquipmentSlot Slot { get; set; } = null!;
}
