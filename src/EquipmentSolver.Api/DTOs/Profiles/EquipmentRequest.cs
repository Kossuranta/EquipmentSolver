using System.ComponentModel.DataAnnotations;

namespace EquipmentSolver.Api.DTOs.Profiles;

public class CreateEquipmentRequest
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// IDs of slots this equipment is compatible with.
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "Equipment must be compatible with at least one slot.")]
    public List<int> CompatibleSlotIds { get; set; } = [];

    /// <summary>
    /// Stats for this equipment. Only include stats that apply.
    /// </summary>
    public List<EquipmentStatInput> Stats { get; set; } = [];
}

public class UpdateEquipmentRequest
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = null!;

    [Required]
    [MinLength(1, ErrorMessage = "Equipment must be compatible with at least one slot.")]
    public List<int> CompatibleSlotIds { get; set; } = [];

    public List<EquipmentStatInput> Stats { get; set; } = [];
}

public class EquipmentStatInput
{
    [Required]
    public int StatTypeId { get; set; }

    [Required]
    public double Value { get; set; }
}
