using System.ComponentModel.DataAnnotations;

namespace EquipmentSolver.Api.DTOs.Profiles;

public class CreateStatTypeRequest
{
    [Required]
    [StringLength(50)]
    [RegularExpression(@"^[a-z][a-z0-9_]*$", ErrorMessage = "Name must be lowercase alphanumeric with underscores, starting with a letter.")]
    public string Name { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string DisplayName { get; set; } = null!;
}

public class UpdateStatTypeRequest
{
    [Required]
    [StringLength(50)]
    [RegularExpression(@"^[a-z][a-z0-9_]*$", ErrorMessage = "Name must be lowercase alphanumeric with underscores, starting with a letter.")]
    public string Name { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string DisplayName { get; set; } = null!;
}
