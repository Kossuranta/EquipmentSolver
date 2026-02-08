using System.Security.Claims;
using EquipmentSolver.Api.DTOs;
using EquipmentSolver.Api.DTOs.Profiles;
using EquipmentSolver.Core.Entities;
using EquipmentSolver.Core.Interfaces;
using EquipmentSolver.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EquipmentSolver.Api.Controllers;

[ApiController]
[Route("api/v1/profiles/{profileId:int}/slots")]
[Authorize]
public class SlotsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IGameProfileService _profileService;

    public SlotsController(AppDbContext db, IGameProfileService profileService)
    {
        _db = db;
        _profileService = profileService;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>
    /// Get all slots for a profile, ordered by SortOrder.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<SlotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSlots(int profileId)
    {
        var profile = await _profileService.GetProfileAsync(profileId, UserId);
        if (profile is null)
            return NotFound();

        var slots = await _db.EquipmentSlots
            .Where(s => s.ProfileId == profileId)
            .OrderBy(s => s.SortOrder)
            .Select(s => new SlotDto { Id = s.Id, Name = s.Name, SortOrder = s.SortOrder })
            .ToListAsync();

        return Ok(slots);
    }

    /// <summary>
    /// Add a new slot to a profile.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SlotDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSlot(int profileId, [FromBody] CreateSlotRequest request)
    {
        if (!await _profileService.IsOwnerAsync(profileId, UserId))
            return Forbid();

        // Check for duplicate name
        var exists = await _db.EquipmentSlots
            .AnyAsync(s => s.ProfileId == profileId && s.Name == request.Name);
        if (exists)
            return BadRequest(new ErrorResponse("A slot with this name already exists in the profile."));

        // Get next sort order
        var maxOrder = await _db.EquipmentSlots
            .Where(s => s.ProfileId == profileId)
            .MaxAsync(s => (int?)s.SortOrder) ?? -1;

        var slot = new EquipmentSlot
        {
            ProfileId = profileId,
            Name = request.Name,
            SortOrder = maxOrder + 1
        };

        _db.EquipmentSlots.Add(slot);
        await _db.SaveChangesAsync();
        await TouchProfile(profileId);

        var dto = new SlotDto { Id = slot.Id, Name = slot.Name, SortOrder = slot.SortOrder };
        return Created($"api/v1/profiles/{profileId}/slots/{slot.Id}", dto);
    }

    /// <summary>
    /// Update a slot's name.
    /// </summary>
    [HttpPut("{slotId:int}")]
    [ProducesResponseType(typeof(SlotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSlot(int profileId, int slotId, [FromBody] UpdateSlotRequest request)
    {
        if (!await _profileService.IsOwnerAsync(profileId, UserId))
            return Forbid();

        var slot = await _db.EquipmentSlots
            .FirstOrDefaultAsync(s => s.Id == slotId && s.ProfileId == profileId);
        if (slot is null)
            return NotFound();

        // Check for duplicate name (excluding self)
        var exists = await _db.EquipmentSlots
            .AnyAsync(s => s.ProfileId == profileId && s.Name == request.Name && s.Id != slotId);
        if (exists)
            return BadRequest(new ErrorResponse("A slot with this name already exists in the profile."));

        slot.Name = request.Name;
        await _db.SaveChangesAsync();
        await TouchProfile(profileId);

        return Ok(new SlotDto { Id = slot.Id, Name = slot.Name, SortOrder = slot.SortOrder });
    }

    /// <summary>
    /// Delete a slot (cascades to equipment compatibility and user states).
    /// </summary>
    [HttpDelete("{slotId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSlot(int profileId, int slotId)
    {
        if (!await _profileService.IsOwnerAsync(profileId, UserId))
            return Forbid();

        var slot = await _db.EquipmentSlots
            .FirstOrDefaultAsync(s => s.Id == slotId && s.ProfileId == profileId);
        if (slot is null)
            return NotFound();

        _db.EquipmentSlots.Remove(slot);
        await _db.SaveChangesAsync();
        await TouchProfile(profileId);

        return NoContent();
    }

    /// <summary>
    /// Reorder slots via drag-and-drop. Pass the ordered list of slot IDs.
    /// </summary>
    [HttpPut("reorder")]
    [ProducesResponseType(typeof(List<SlotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ReorderSlots(int profileId, [FromBody] ReorderSlotsRequest request)
    {
        if (!await _profileService.IsOwnerAsync(profileId, UserId))
            return Forbid();

        var slots = await _db.EquipmentSlots
            .Where(s => s.ProfileId == profileId)
            .ToListAsync();

        for (int i = 0; i < request.SlotIds.Count; i++)
        {
            var slot = slots.FirstOrDefault(s => s.Id == request.SlotIds[i]);
            if (slot is not null)
                slot.SortOrder = i;
        }

        await _db.SaveChangesAsync();

        var result = slots
            .OrderBy(s => s.SortOrder)
            .Select(s => new SlotDto { Id = s.Id, Name = s.Name, SortOrder = s.SortOrder })
            .ToList();

        return Ok(result);
    }

    /// <summary>
    /// Updates the profile's UpdatedAt timestamp.
    /// </summary>
    private async Task TouchProfile(int profileId)
    {
        var profile = await _db.GameProfiles.FindAsync(profileId);
        if (profile is not null)
        {
            profile.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }
}
