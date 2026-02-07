namespace EquipmentSolver.Core.Entities;

/// <summary>
/// An equipment piece within a game profile.
/// </summary>
public class Equipment
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    public GameProfile Profile { get; set; } = null!;

    public string Name { get; set; } = null!;

    // Navigation properties
    public List<EquipmentSlotCompatibility> SlotCompatibilities { get; set; } = [];
    public List<EquipmentStat> Stats { get; set; } = [];
    public List<UserEquipmentState> UserStates { get; set; } = [];
}
