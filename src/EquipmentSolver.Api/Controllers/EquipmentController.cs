using System.Security.Claims;
using EquipmentSolver.Api.DTOs;
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
[Route("api/v1/profiles/{profileId:int}/equipment")]
[Authorize]
[EnableRateLimiting("api")]
public class EquipmentController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IGameProfileService _profileService;

    public EquipmentController(AppDbContext db, IGameProfileService profileService)
    {
        _db = db;
        _profileService = profileService;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>
    /// Get all equipment for a profile.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<EquipmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEquipment(int profileId)
    {
        var profile = await _profileService.GetProfileAsync(profileId, UserId);
        if (profile is null)
            return NotFound();

        var equipment = await _db.Equipment
            .Where(e => e.ProfileId == profileId)
            .Include(e => e.SlotCompatibilities)
            .Include(e => e.Stats)
            .OrderBy(e => e.Name)
            .ToListAsync();

        var result = equipment.Select(e => new EquipmentDto
        {
            Id = e.Id,
            Name = e.Name,
            CompatibleSlotIds = e.SlotCompatibilities.Select(sc => sc.SlotId).ToList(),
            Stats = e.Stats.Select(s => new EquipmentStatDto
            {
                StatTypeId = s.StatTypeId,
                Value = s.Value
            }).ToList()
        }).ToList();

        return Ok(result);
    }

    /// <summary>
    /// Add a new equipment piece to a profile.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(EquipmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateEquipment(int profileId, [FromBody] CreateEquipmentRequest request)
    {
        if (!await _profileService.IsOwnerAsync(profileId, UserId))
            return Forbid();

        // Validate slot IDs belong to this profile
        var validSlotIds = await _db.EquipmentSlots
            .Where(s => s.ProfileId == profileId && request.CompatibleSlotIds.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync();

        if (validSlotIds.Count != request.CompatibleSlotIds.Count)
            return BadRequest(new ErrorResponse("One or more slot IDs are invalid for this profile."));

        // Validate stat type IDs belong to this profile
        if (request.Stats.Count > 0)
        {
            var statTypeIds = request.Stats.Select(s => s.StatTypeId).ToList();
            var validStatTypeIds = await _db.StatTypes
                .Where(st => st.ProfileId == profileId && statTypeIds.Contains(st.Id))
                .Select(st => st.Id)
                .ToListAsync();

            if (validStatTypeIds.Count != statTypeIds.Distinct().Count())
                return BadRequest(new ErrorResponse("One or more stat type IDs are invalid for this profile."));
        }

        var equipment = new Equipment
        {
            ProfileId = profileId,
            Name = request.Name,
            SlotCompatibilities = request.CompatibleSlotIds
                .Select(slotId => new EquipmentSlotCompatibility { SlotId = slotId })
                .ToList(),
            Stats = request.Stats
                .Select(s => new EquipmentStat { StatTypeId = s.StatTypeId, Value = s.Value })
                .ToList()
        };

        _db.Equipment.Add(equipment);
        await _db.SaveChangesAsync();
        await TouchProfile(profileId);

        var dto = new EquipmentDto
        {
            Id = equipment.Id,
            Name = equipment.Name,
            CompatibleSlotIds = equipment.SlotCompatibilities.Select(sc => sc.SlotId).ToList(),
            Stats = equipment.Stats.Select(s => new EquipmentStatDto
            {
                StatTypeId = s.StatTypeId,
                Value = s.Value
            }).ToList()
        };

        return Created($"api/v1/profiles/{profileId}/equipment/{equipment.Id}", dto);
    }

    /// <summary>
    /// Update an equipment piece (name, slot compatibility, stats).
    /// </summary>
    [HttpPut("{equipmentId:int}")]
    [ProducesResponseType(typeof(EquipmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEquipment(int profileId, int equipmentId, [FromBody] UpdateEquipmentRequest request)
    {
        if (!await _profileService.IsOwnerAsync(profileId, UserId))
            return Forbid();

        var equipment = await _db.Equipment
            .Include(e => e.SlotCompatibilities)
            .Include(e => e.Stats)
            .FirstOrDefaultAsync(e => e.Id == equipmentId && e.ProfileId == profileId);

        if (equipment is null)
            return NotFound();

        // Validate slot IDs
        var validSlotIds = await _db.EquipmentSlots
            .Where(s => s.ProfileId == profileId && request.CompatibleSlotIds.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync();

        if (validSlotIds.Count != request.CompatibleSlotIds.Count)
            return BadRequest(new ErrorResponse("One or more slot IDs are invalid for this profile."));

        // Validate stat type IDs
        if (request.Stats.Count > 0)
        {
            var statTypeIds = request.Stats.Select(s => s.StatTypeId).ToList();
            var validStatTypeIds = await _db.StatTypes
                .Where(st => st.ProfileId == profileId && statTypeIds.Contains(st.Id))
                .Select(st => st.Id)
                .ToListAsync();

            if (validStatTypeIds.Count != statTypeIds.Distinct().Count())
                return BadRequest(new ErrorResponse("One or more stat type IDs are invalid for this profile."));
        }

        // Update name
        equipment.Name = request.Name;

        // Replace slot compatibilities
        _db.EquipmentSlotCompatibilities.RemoveRange(equipment.SlotCompatibilities);
        equipment.SlotCompatibilities = request.CompatibleSlotIds
            .Select(slotId => new EquipmentSlotCompatibility { EquipmentId = equipmentId, SlotId = slotId })
            .ToList();

        // Replace stats
        _db.EquipmentStats.RemoveRange(equipment.Stats);
        equipment.Stats = request.Stats
            .Select(s => new EquipmentStat { EquipmentId = equipmentId, StatTypeId = s.StatTypeId, Value = s.Value })
            .ToList();

        await _db.SaveChangesAsync();
        await TouchProfile(profileId);

        var dto = new EquipmentDto
        {
            Id = equipment.Id,
            Name = equipment.Name,
            CompatibleSlotIds = equipment.SlotCompatibilities.Select(sc => sc.SlotId).ToList(),
            Stats = equipment.Stats.Select(s => new EquipmentStatDto
            {
                StatTypeId = s.StatTypeId,
                Value = s.Value
            }).ToList()
        };

        return Ok(dto);
    }

    /// <summary>
    /// Delete an equipment piece (cascades to stats, compatibility, user states).
    /// </summary>
    [HttpDelete("{equipmentId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEquipment(int profileId, int equipmentId)
    {
        if (!await _profileService.IsOwnerAsync(profileId, UserId))
            return Forbid();

        var equipment = await _db.Equipment
            .FirstOrDefaultAsync(e => e.Id == equipmentId && e.ProfileId == profileId);
        if (equipment is null)
            return NotFound();

        _db.Equipment.Remove(equipment);
        await _db.SaveChangesAsync();
        await TouchProfile(profileId);

        return NoContent();
    }

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
