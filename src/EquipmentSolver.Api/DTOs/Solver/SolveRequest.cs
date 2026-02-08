using System.ComponentModel.DataAnnotations;

namespace EquipmentSolver.Api.DTOs.Solver;

/// <summary>
/// Request body for running the solver.
/// </summary>
public class SolveRequest
{
    public List<SolveConstraintDto> Constraints { get; set; } = [];

    [Required]
    [MinLength(1, ErrorMessage = "At least one priority is required.")]
    public List<SolvePriorityDto> Priorities { get; set; } = [];

    [Range(1, 20)]
    public int TopN { get; set; } = 5;
}

public class SolveConstraintDto
{
    [Required]
    public int StatTypeId { get; set; }

    [Required]
    [RegularExpression(@"^(<=|>=|==|<|>)$", ErrorMessage = "Operator must be <=, >=, ==, <, or >.")]
    public string Operator { get; set; } = null!;

    public double Value { get; set; }
}

public class SolvePriorityDto
{
    [Required]
    public int StatTypeId { get; set; }

    public double Weight { get; set; }
}
