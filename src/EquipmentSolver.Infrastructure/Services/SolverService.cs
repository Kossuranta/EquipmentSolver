using EquipmentSolver.Core.Entities;
using EquipmentSolver.Core.Interfaces;
using EquipmentSolver.Core.Solver;
using EquipmentSolver.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EquipmentSolver.Infrastructure.Services;

/// <summary>
/// Orchestrates solver execution and manages solver presets.
/// </summary>
public class SolverService : ISolverService
{
    private readonly AppDbContext _db;

    public SolverService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Run the solver for a profile, respecting the user's enabled slots and items.
    /// </summary>
    public async Task<SolverOutput> SolveAsync(
        int profileId,
        string userId,
        List<ConstraintInput> constraints,
        List<PriorityInput> priorities,
        int topN = 5,
        CancellationToken cancellationToken = default)
    {
        // Load all slots ordered by SortOrder
        var allSlots = await _db.EquipmentSlots
            .Where(s => s.ProfileId == profileId)
            .OrderBy(s => s.SortOrder)
            .ToListAsync(cancellationToken);

        // Load user's disabled slot IDs (slots without a state row are enabled by default)
        var disabledSlotIds = await _db.UserSlotStates
            .Where(uss => uss.UserId == userId && uss.Slot.ProfileId == profileId && !uss.IsEnabled)
            .Select(uss => uss.SlotId)
            .ToHashSetAsync(cancellationToken);

        // Filter to active slots only
        var activeSlots = allSlots.Where(s => !disabledSlotIds.Contains(s.Id)).ToList();

        if (activeSlots.Count == 0)
            return new SolverOutput([], false, TimeSpan.Zero, 0);

        var activeSlotIds = activeSlots.Select(s => s.Id).ToHashSet();

        // Load user's disabled equipment IDs (items without a state row are enabled by default)
        var disabledEquipmentIds = await _db.UserEquipmentStates
            .Where(ues => ues.UserId == userId && ues.Equipment.ProfileId == profileId && !ues.IsEnabled)
            .Select(ues => ues.EquipmentId)
            .ToHashSetAsync(cancellationToken);

        // Load all equipment for the profile with slot compatibility and stats
        var equipment = await _db.Equipment
            .Where(e => e.ProfileId == profileId)
            .Include(e => e.SlotCompatibilities)
            .Include(e => e.Stats)
            .ToListAsync(cancellationToken);

        // Filter to enabled equipment only
        var enabledEquipment = equipment.Where(e => !disabledEquipmentIds.Contains(e.Id)).ToList();

        // Build solver input: for each active slot, collect compatible enabled items
        var slotInputs = activeSlots.Select(slot =>
        {
            var compatibleItems = enabledEquipment
                .Where(e => e.SlotCompatibilities.Any(sc => sc.SlotId == slot.Id))
                .Select(e => new ItemInput(
                    e.Id,
                    e.Name,
                    e.Stats.ToDictionary(s => s.StatTypeId, s => s.Value)))
                .ToList();

            return new SlotInput(slot.Id, slot.Name, compatibleItems);
        }).ToList();

        // Run the solver (CPU-bound, runs synchronously but respects CancellationToken)
        return await Task.Run(
            () => SolverEngine.Solve(slotInputs, constraints, priorities, topN, cancellationToken),
            cancellationToken);
    }

    /// <summary>
    /// Get all solver presets for a profile.
    /// </summary>
    public async Task<List<SolverPreset>> GetPresetsAsync(int profileId) =>
        await _db.SolverPresets
            .Where(p => p.ProfileId == profileId)
            .Include(p => p.Constraints)
            .Include(p => p.Priorities)
            .OrderBy(p => p.Name)
            .ToListAsync();

    /// <summary>
    /// Get a single preset with its constraints and priorities.
    /// </summary>
    public async Task<SolverPreset?> GetPresetAsync(int presetId, int profileId) =>
        await _db.SolverPresets
            .Where(p => p.Id == presetId && p.ProfileId == profileId)
            .Include(p => p.Constraints)
            .Include(p => p.Priorities)
            .FirstOrDefaultAsync();

    /// <summary>
    /// Create a new solver preset.
    /// </summary>
    public async Task<SolverPreset> CreatePresetAsync(
        int profileId,
        string name,
        List<SolverConstraint> constraints,
        List<SolverPriority> priorities)
    {
        var preset = new SolverPreset
        {
            ProfileId = profileId,
            Name = name,
            Constraints = constraints,
            Priorities = priorities
        };

        _db.SolverPresets.Add(preset);
        await _db.SaveChangesAsync();

        return preset;
    }

    /// <summary>
    /// Update a solver preset (replace name, constraints, and priorities).
    /// </summary>
    public async Task<SolverPreset?> UpdatePresetAsync(
        int presetId,
        int profileId,
        string name,
        List<SolverConstraint> constraints,
        List<SolverPriority> priorities)
    {
        var preset = await _db.SolverPresets
            .Include(p => p.Constraints)
            .Include(p => p.Priorities)
            .FirstOrDefaultAsync(p => p.Id == presetId && p.ProfileId == profileId);

        if (preset is null)
            return null;

        preset.Name = name;

        // Replace constraints
        _db.SolverConstraints.RemoveRange(preset.Constraints);
        preset.Constraints = constraints;

        // Replace priorities
        _db.SolverPriorities.RemoveRange(preset.Priorities);
        preset.Priorities = priorities;

        await _db.SaveChangesAsync();
        return preset;
    }

    /// <summary>
    /// Delete a solver preset.
    /// </summary>
    public async Task<bool> DeletePresetAsync(int presetId, int profileId)
    {
        var preset = await _db.SolverPresets
            .FirstOrDefaultAsync(p => p.Id == presetId && p.ProfileId == profileId);

        if (preset is null)
            return false;

        _db.SolverPresets.Remove(preset);
        await _db.SaveChangesAsync();
        return true;
    }
}
