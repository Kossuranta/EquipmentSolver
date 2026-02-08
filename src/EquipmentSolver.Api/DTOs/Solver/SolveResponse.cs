namespace EquipmentSolver.Api.DTOs.Solver;

/// <summary>
/// Solver output containing ranked equipment loadouts.
/// </summary>
public class SolveResponse
{
    public List<SolveResultDto> Results { get; set; } = [];
    public bool TimedOut { get; set; }
    public long ElapsedMs { get; set; }
    public long CombinationsEvaluated { get; set; }
}

/// <summary>
/// A single ranked loadout from the solver.
/// </summary>
public class SolveResultDto
{
    public int Rank { get; set; }
    public double Score { get; set; }
    public List<StatTotalDto> StatTotals { get; set; } = [];
    public List<SlotAssignmentDto> Assignments { get; set; } = [];
}

/// <summary>
/// Aggregated stat total across all equipped items.
/// </summary>
public class StatTotalDto
{
    public int StatTypeId { get; set; }
    public string StatName { get; set; } = null!;
    public string StatDisplayName { get; set; } = null!;
    public double Value { get; set; }
}

/// <summary>
/// What item (if any) is assigned to a specific slot.
/// </summary>
public class SlotAssignmentDto
{
    public int SlotId { get; set; }
    public string SlotName { get; set; } = null!;
    public int? EquipmentId { get; set; }
    public string? EquipmentName { get; set; }
    public List<ItemStatDto> Stats { get; set; } = [];
}

/// <summary>
/// A single stat on an assigned item.
/// </summary>
public class ItemStatDto
{
    public int StatTypeId { get; set; }
    public string StatDisplayName { get; set; } = null!;
    public double Value { get; set; }
}
