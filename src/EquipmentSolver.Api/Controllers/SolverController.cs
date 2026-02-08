using System.Security.Claims;
using EquipmentSolver.Api.DTOs;
using EquipmentSolver.Api.DTOs.Solver;
using EquipmentSolver.Core.Entities;
using EquipmentSolver.Core.Interfaces;
using EquipmentSolver.Core.Solver;
using EquipmentSolver.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EquipmentSolver.Api.Controllers;

[ApiController]
[Route("api/v1/profiles/{profileId:int}/solver")]
[Authorize]
public class SolverController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ISolverService _solverService;
    private readonly IGameProfileService _profileService;

    public SolverController(AppDbContext db, ISolverService solverService, IGameProfileService profileService)
    {
        _db = db;
        _solverService = solverService;
        _profileService = profileService;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>
    /// Run the solver to find optimal equipment loadouts.
    /// </summary>
    [HttpPost("solve")]
    [ProducesResponseType(typeof(SolveResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Solve(int profileId, [FromBody] SolveRequest request, CancellationToken cancellationToken)
    {
        var profile = await _profileService.GetProfileAsync(profileId, UserId);
        if (profile is null)
            return NotFound();

        // Validate stat type IDs exist in this profile
        var validStatTypeIds = await _db.StatTypes
            .Where(st => st.ProfileId == profileId)
            .Select(st => st.Id)
            .ToHashSetAsync(cancellationToken);

        var requestedStatIds = request.Constraints.Select(c => c.StatTypeId)
            .Concat(request.Priorities.Select(p => p.StatTypeId))
            .Distinct()
            .ToList();

        if (requestedStatIds.Any(id => !validStatTypeIds.Contains(id)))
            return BadRequest(new ErrorResponse("One or more stat type IDs are invalid for this profile."));

        // Filter out priorities with zero weight (they contribute nothing)
        var effectivePriorities = request.Priorities
            .Where(p => p.Weight != 0)
            .Select(p => new PriorityInput(p.StatTypeId, p.Weight))
            .ToList();

        if (effectivePriorities.Count == 0)
            return BadRequest(new ErrorResponse("At least one priority must have a non-zero weight."));

        var constraints = request.Constraints
            .Select(c => new ConstraintInput(c.StatTypeId, c.Operator, c.Value))
            .ToList();

        var output = await _solverService.SolveAsync(
            profileId, UserId, constraints, effectivePriorities, request.TopN, cancellationToken);

        // Build stat type lookup for display names
        var statTypes = await _db.StatTypes
            .Where(st => st.ProfileId == profileId)
            .ToDictionaryAsync(st => st.Id, cancellationToken);

        var response = new SolveResponse
        {
            TimedOut = output.TimedOut,
            ElapsedMs = (long)output.Elapsed.TotalMilliseconds,
            CombinationsEvaluated = output.CombinationsEvaluated,
            Results = output.Results.Select((r, index) => new SolveResultDto
            {
                Rank = index + 1,
                Score = Math.Round(r.Score, 4),
                StatTotals = r.StatTotals
                    .Where(kv => statTypes.ContainsKey(kv.Key))
                    .Select(kv => new StatTotalDto
                    {
                        StatTypeId = kv.Key,
                        StatDisplayName = statTypes[kv.Key].DisplayName,
                        Value = Math.Round(kv.Value, 4)
                    })
                    .OrderBy(st => st.StatDisplayName)
                    .ToList(),
                Assignments = r.Assignments.Select(a => new SlotAssignmentDto
                {
                    SlotId = a.SlotId,
                    SlotName = a.SlotName,
                    EquipmentId = a.EquipmentId,
                    EquipmentName = a.EquipmentName,
                    Stats = a.ItemStats
                        .Where(kv => statTypes.ContainsKey(kv.Key))
                        .Select(kv => new ItemStatDto
                        {
                            StatTypeId = kv.Key,
                            StatDisplayName = statTypes[kv.Key].DisplayName,
                            Value = Math.Round(kv.Value, 4)
                        })
                        .ToList()
                }).ToList()
            }).ToList()
        };

        return Ok(response);
    }

    // ─── Preset CRUD ─────────────────────────────────────────────────

    /// <summary>
    /// Get all solver presets for a profile.
    /// </summary>
    [HttpGet("presets")]
    [ProducesResponseType(typeof(List<PresetResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPresets(int profileId)
    {
        var profile = await _profileService.GetProfileAsync(profileId, UserId);
        if (profile is null)
            return NotFound();

        var presets = await _solverService.GetPresetsAsync(profileId);

        var result = presets.Select(MapPresetToResponse).ToList();
        return Ok(result);
    }

    /// <summary>
    /// Get a single solver preset.
    /// </summary>
    [HttpGet("presets/{presetId:int}")]
    [ProducesResponseType(typeof(PresetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPreset(int profileId, int presetId)
    {
        var profile = await _profileService.GetProfileAsync(profileId, UserId);
        if (profile is null)
            return NotFound();

        var preset = await _solverService.GetPresetAsync(presetId, profileId);
        if (preset is null)
            return NotFound();

        return Ok(MapPresetToResponse(preset));
    }

    /// <summary>
    /// Create a new solver preset. Owner only.
    /// </summary>
    [HttpPost("presets")]
    [ProducesResponseType(typeof(PresetResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePreset(int profileId, [FromBody] CreatePresetRequest request)
    {
        if (!await _profileService.IsOwnerAsync(profileId, UserId))
            return Forbid();

        var validation = await ValidatePresetStatTypes(profileId, request.Constraints, request.Priorities);
        if (validation is not null)
            return validation;

        var constraints = request.Constraints
            .Select(c => new SolverConstraint { StatTypeId = c.StatTypeId, Operator = c.Operator, Value = c.Value })
            .ToList();

        var priorities = request.Priorities
            .Select(p => new SolverPriority { StatTypeId = p.StatTypeId, Weight = p.Weight })
            .ToList();

        var preset = await _solverService.CreatePresetAsync(profileId, request.Name, constraints, priorities);

        return Created(
            $"api/v1/profiles/{profileId}/solver/presets/{preset.Id}",
            MapPresetToResponse(preset));
    }

    /// <summary>
    /// Update a solver preset. Owner only.
    /// </summary>
    [HttpPut("presets/{presetId:int}")]
    [ProducesResponseType(typeof(PresetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdatePreset(int profileId, int presetId, [FromBody] UpdatePresetRequest request)
    {
        if (!await _profileService.IsOwnerAsync(profileId, UserId))
            return Forbid();

        var validation = await ValidatePresetStatTypes(profileId, request.Constraints, request.Priorities);
        if (validation is not null)
            return validation;

        var constraints = request.Constraints
            .Select(c => new SolverConstraint { StatTypeId = c.StatTypeId, Operator = c.Operator, Value = c.Value })
            .ToList();

        var priorities = request.Priorities
            .Select(p => new SolverPriority { StatTypeId = p.StatTypeId, Weight = p.Weight })
            .ToList();

        var preset = await _solverService.UpdatePresetAsync(presetId, profileId, request.Name, constraints, priorities);
        if (preset is null)
            return NotFound();

        return Ok(MapPresetToResponse(preset));
    }

    /// <summary>
    /// Delete a solver preset. Owner only.
    /// </summary>
    [HttpDelete("presets/{presetId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePreset(int profileId, int presetId)
    {
        if (!await _profileService.IsOwnerAsync(profileId, UserId))
            return Forbid();

        var deleted = await _solverService.DeletePresetAsync(presetId, profileId);
        if (!deleted)
            return NotFound();

        return NoContent();
    }

    // ─── Helpers ─────────────────────────────────────────────────────

    private async Task<IActionResult?> ValidatePresetStatTypes(
        int profileId,
        List<PresetConstraintInput> constraints,
        List<PresetPriorityInput> priorities)
    {
        var requestedStatIds = constraints.Select(c => c.StatTypeId)
            .Concat(priorities.Select(p => p.StatTypeId))
            .Distinct()
            .ToList();

        if (requestedStatIds.Count == 0)
            return null;

        var validCount = await _db.StatTypes
            .CountAsync(st => st.ProfileId == profileId && requestedStatIds.Contains(st.Id));

        if (validCount != requestedStatIds.Count)
            return BadRequest(new ErrorResponse("One or more stat type IDs are invalid for this profile."));

        return null;
    }

    private static PresetResponse MapPresetToResponse(SolverPreset preset) => new()
    {
        Id = preset.Id,
        Name = preset.Name,
        Constraints = preset.Constraints.Select(c => new PresetConstraintDto
        {
            Id = c.Id,
            StatTypeId = c.StatTypeId,
            Operator = c.Operator,
            Value = c.Value
        }).ToList(),
        Priorities = preset.Priorities.Select(p => new PresetPriorityDto
        {
            Id = p.Id,
            StatTypeId = p.StatTypeId,
            Weight = p.Weight
        }).ToList()
    };
}
