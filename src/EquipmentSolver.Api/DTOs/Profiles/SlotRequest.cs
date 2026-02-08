using System.ComponentModel.DataAnnotations;

namespace EquipmentSolver.Api.DTOs.Profiles;

public class CreateSlotRequest
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = null!;
}

public class UpdateSlotRequest
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = null!;
}

public class ReorderSlotsRequest
{
    /// <summary>
    /// Ordered list of slot IDs representing the desired order.
    /// </summary>
    [Required]
    public List<int> SlotIds { get; set; } = [];
}
