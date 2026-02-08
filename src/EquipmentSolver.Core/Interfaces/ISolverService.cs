using EquipmentSolver.Core.Entities;
using EquipmentSolver.Core.Solver;

namespace EquipmentSolver.Core.Interfaces;

/// <summary>
/// Orchestrates solver execution and manages solver presets.
/// </summary>
public interface ISolverService
{
    /// <summary>
    /// Run the solver for a profile, respecting the user's enabled slots and items.
    /// </summary>
    Task<SolverOutput> SolveAsync(
        int profileId,
        string userId,
        List<ConstraintInput> constraints,
        List<PriorityInput> priorities,
        int topN = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all solver presets for a profile.
    /// </summary>
    Task<List<SolverPreset>> GetPresetsAsync(int profileId);

    /// <summary>
    /// Get a single preset with its constraints and priorities.
    /// </summary>
    Task<SolverPreset?> GetPresetAsync(int presetId, int profileId);

    /// <summary>
    /// Create a new solver preset. Only the profile owner can create presets.
    /// </summary>
    Task<SolverPreset> CreatePresetAsync(
        int profileId,
        string name,
        List<SolverConstraint> constraints,
        List<SolverPriority> priorities);

    /// <summary>
    /// Update a solver preset (replace constraints and priorities). Owner only.
    /// </summary>
    Task<SolverPreset?> UpdatePresetAsync(
        int presetId,
        int profileId,
        string name,
        List<SolverConstraint> constraints,
        List<SolverPriority> priorities);

    /// <summary>
    /// Delete a solver preset. Owner only.
    /// </summary>
    Task<bool> DeletePresetAsync(int presetId, int profileId);
}
