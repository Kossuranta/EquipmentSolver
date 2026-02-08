using System.ComponentModel.DataAnnotations;

namespace EquipmentSolver.Api.DTOs.Profiles;

public class CreateStatTypeRequest
{
    [Required]
    [StringLength(200)]
    public string DisplayName { get; set; } = null!;
}

public class UpdateStatTypeRequest
{
    [Required]
    [StringLength(200)]
    public string DisplayName { get; set; } = null!;
}
