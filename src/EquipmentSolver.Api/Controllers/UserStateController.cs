using System.Security.Claims;
using EquipmentSolver.Api.DTOs.Profiles;
using EquipmentSolver.Core.Entities;
using EquipmentSolver.Core.Interfaces;
using EquipmentSolver.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace EquipmentSolver.Api.Controllers;

[ApiController]
[Route("api/v1/profiles/{profileId:int}/user-state")]
[Authorize]
[EnableRateLimiting("api")]
public class UserStateController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IGameProfileService _profileService;

    public UserStateController(AppDbContext db, IGameProfileService profileService)
    {
        _db = db;
        _profileService = profileService;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>
    /// Get per-user equipment enabled/disabled states for a profile.
    /// Equipment without an explicit state defaults to enabled.
    /// </summary>
    [HttpGet("equipment")]
    [ProducesResponseType(typeof(List<UserEquipmentStateResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEquipmentStates(int profileId)
    {
        var profile = await _profileService.GetProfileAsync(profileId, UserId);
        if (profile is null)
            return NotFound();

        var states = await _db.UserEquipmentStates
            .Where(s => s.UserId == UserId && s.Equipment.ProfileId == profileId)
            .Select(s => new UserEquipmentStateResponse { EquipmentId = s.EquipmentId, IsEnabled = s.IsEnabled })
            .ToListAsync();

        return Ok(states);
    }

    /// <summary>
    /// Set the enabled/disabled state of a single equipment item for the current user.
    /// </summary>
    [HttpPut("equipment")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetEquipmentState(int profileId, [FromBody] SetEquipmentStateRequest request)
    {
        var state = await _db.UserEquipmentStates
            .FirstOrDefaultAsync(s => s.UserId == UserId && s.EquipmentId == request.EquipmentId);

        if (state is null)
        {
            state = new UserEquipmentState
            {
                UserId = UserId,
                EquipmentId = request.EquipmentId,
                IsEnabled = request.IsEnabled
            };
            _db.UserEquipmentStates.Add(state);
        }
        else
        {
            state.IsEnabled = request.IsEnabled;
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Enable or disable ALL equipment in a profile for the current user.
    /// </summary>
    [HttpPut("equipment/bulk")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> BulkSetEquipmentState(int profileId, [FromBody] BulkEquipmentStateRequest request)
    {
        var equipmentIds = await _db.Equipment
            .Where(e => e.ProfileId == profileId)
            .Select(e => e.Id)
            .ToListAsync();

        var existingStates = await _db.UserEquipmentStates
            .Where(s => s.UserId == UserId && equipmentIds.Contains(s.EquipmentId))
            .ToListAsync();

        var existingIds = existingStates.Select(s => s.EquipmentId).ToHashSet();

        // Update existing
        foreach (var state in existingStates)
            state.IsEnabled = request.IsEnabled;

        // Insert new for equipment that doesn't have a state record yet
        foreach (var eqId in equipmentIds.Where(id => !existingIds.Contains(id)))
        {
            _db.UserEquipmentStates.Add(new UserEquipmentState
            {
                UserId = UserId,
                EquipmentId = eqId,
                IsEnabled = request.IsEnabled
            });
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Get per-user slot enabled/disabled states for a profile.
    /// Slots without an explicit state default to enabled.
    /// </summary>
    [HttpGet("slots")]
    [ProducesResponseType(typeof(List<UserSlotStateResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSlotStates(int profileId)
    {
        var profile = await _profileService.GetProfileAsync(profileId, UserId);
        if (profile is null)
            return NotFound();

        var states = await _db.UserSlotStates
            .Where(s => s.UserId == UserId && s.Slot.ProfileId == profileId)
            .Select(s => new UserSlotStateResponse { SlotId = s.SlotId, IsEnabled = s.IsEnabled })
            .ToListAsync();

        return Ok(states);
    }

    /// <summary>
    /// Set the enabled/disabled state of a single slot for the current user.
    /// </summary>
    [HttpPut("slots")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetSlotState(int profileId, [FromBody] SetSlotStateRequest request)
    {
        var state = await _db.UserSlotStates
            .FirstOrDefaultAsync(s => s.UserId == UserId && s.SlotId == request.SlotId);

        if (state is null)
        {
            state = new UserSlotState
            {
                UserId = UserId,
                SlotId = request.SlotId,
                IsEnabled = request.IsEnabled
            };
            _db.UserSlotStates.Add(state);
        }
        else
        {
            state.IsEnabled = request.IsEnabled;
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }
}
