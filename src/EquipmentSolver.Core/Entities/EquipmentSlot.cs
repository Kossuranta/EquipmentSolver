namespace EquipmentSolver.Core.Entities;

/// <summary>
/// An equipment slot defined within a game profile (e.g., head, chest, ring1).
/// </summary>
public class EquipmentSlot
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    public GameProfile Profile { get; set; } = null!;

    public string Name { get; set; } = null!;
    public int SortOrder { get; set; }

    // Navigation properties
    public List<EquipmentSlotCompatibility> CompatibleEquipment { get; set; } = [];
    public List<UserSlotState> UserStates { get; set; } = [];
}
