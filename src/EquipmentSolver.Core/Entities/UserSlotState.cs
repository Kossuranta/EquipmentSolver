namespace EquipmentSolver.Core.Entities;

/// <summary>
/// Per-user slot enable/disable state. Defaults to enabled if no record exists.
/// </summary>
public class UserSlotState
{
    public string UserId { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;

    public int SlotId { get; set; }
    public EquipmentSlot Slot { get; set; } = null!;

    public bool IsEnabled { get; set; } = true;
}
