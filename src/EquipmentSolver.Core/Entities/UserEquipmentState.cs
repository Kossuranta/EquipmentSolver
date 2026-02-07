namespace EquipmentSolver.Core.Entities;

/// <summary>
/// Per-user equipment enable/disable state. Defaults to enabled if no record exists.
/// </summary>
public class UserEquipmentState
{
    public string UserId { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;

    public int EquipmentId { get; set; }
    public Equipment Equipment { get; set; } = null!;

    public bool IsEnabled { get; set; } = true;
}
