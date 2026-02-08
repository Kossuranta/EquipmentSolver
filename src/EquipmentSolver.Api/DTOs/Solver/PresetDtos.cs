using System.ComponentModel.DataAnnotations;

namespace EquipmentSolver.Api.DTOs.Solver;

/// <summary>
/// Solver preset summary in list responses.
/// </summary>
public class PresetResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public List<PresetConstraintDto> Constraints { get; set; } = [];
    public List<PresetPriorityDto> Priorities { get; set; } = [];
}

public class PresetConstraintDto
{
    public int Id { get; set; }
    public int StatTypeId { get; set; }
    public string Operator { get; set; } = null!;
    public double Value { get; set; }
}

public class PresetPriorityDto
{
    public int Id { get; set; }
    public int StatTypeId { get; set; }
    public double Weight { get; set; }
}

/// <summary>
/// Request to create a new solver preset.
/// </summary>
public class CreatePresetRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = null!;

    public List<PresetConstraintInput> Constraints { get; set; } = [];

    [Required]
    [MinLength(1, ErrorMessage = "At least one priority is required.")]
    public List<PresetPriorityInput> Priorities { get; set; } = [];
}

/// <summary>
/// Request to update an existing solver preset.
/// </summary>
public class UpdatePresetRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = null!;

    public List<PresetConstraintInput> Constraints { get; set; } = [];

    [Required]
    [MinLength(1, ErrorMessage = "At least one priority is required.")]
    public List<PresetPriorityInput> Priorities { get; set; } = [];
}

public class PresetConstraintInput
{
    [Required]
    public int StatTypeId { get; set; }

    [Required]
    [RegularExpression(@"^(<=|>=|==|<|>)$", ErrorMessage = "Operator must be <=, >=, ==, <, or >.")]
    public string Operator { get; set; } = null!;

    public double Value { get; set; }
}

public class PresetPriorityInput
{
    [Required]
    public int StatTypeId { get; set; }

    public double Weight { get; set; }
}
