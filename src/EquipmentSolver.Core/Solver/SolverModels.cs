namespace EquipmentSolver.Core.Solver;

/// <summary>
/// Pre-processed slot data fed into the solver.
/// </summary>
public record SlotInput(int SlotId, string SlotName, List<ItemInput> Items);

/// <summary>
/// Pre-processed equipment item data fed into the solver.
/// </summary>
public record ItemInput(int EquipmentId, string Name, Dictionary<int, double> Stats);

/// <summary>
/// A hard constraint the solver must satisfy.
/// </summary>
public record ConstraintInput(int StatTypeId, string Operator, double Value);

/// <summary>
/// A weighted priority the solver optimizes for.
/// </summary>
public record PriorityInput(int StatTypeId, double Weight);

/// <summary>
/// A single slot assignment within a solver result.
/// </summary>
public record SlotAssignment(int SlotId, string SlotName, int? EquipmentId, string? EquipmentName, Dictionary<int, double> ItemStats);

/// <summary>
/// A single solver result (one complete loadout).
/// </summary>
public record SolverResult(List<SlotAssignment> Assignments, double Score, Dictionary<int, double> StatTotals);

/// <summary>
/// Complete output from the solver engine.
/// </summary>
public record SolverOutput(List<SolverResult> Results, bool TimedOut, TimeSpan Elapsed, long CombinationsEvaluated);
